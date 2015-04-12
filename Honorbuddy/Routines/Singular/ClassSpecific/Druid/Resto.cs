﻿using System;
using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;

using Styx.WoWInternals;
using Styx.CommonBot;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

using Rest = Singular.Helpers.Rest;
using Action = Styx.TreeSharp.Action;
using CommonBehaviors.Actions;
using Styx.Common.Helpers;
using System.Collections.Generic;
using System.Drawing;

namespace Singular.ClassSpecific.Druid
{
    public class Resto
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static DruidSettings DruidSettings { get { return SingularSettings.Instance.Druid(); } }
        public static bool HasTalent(DruidTalents tal) { return TalentManager.IsSelected((int)tal); }

        const int PriEmergencyBase = 500;
        const int PriHighBase = 400;
        const int PriAoeBase = 300;
        const int PriHighAtone = 200;
        const int PriSingleBase = 100;
        const int PriLowBase = 0;

        const int MUSHROOM_ID = 47649;
        const int CLEARCASTING = 155631;

        public static bool glyphRegrowth { get; set; }
        public static bool glyphRejuv { get; set; }

        public static bool talentGermination { get; set; }
        public static uint MaxRejuvStacks { get; set; }

        static IEnumerable<WoWUnit> Mushrooms
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(o => o.Entry == MUSHROOM_ID && o.CreatedByUnitGuid == StyxWoW.Me.Guid && o.Distance <= 40 ); 
            }
        }

        static int MushroomCount
        {
            get { return Mushrooms.Count(); }
        }


        [Behavior(BehaviorType.Initialize, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidInitialize()
        {
            glyphRegrowth = TalentManager.HasGlyph("Regrowth");
            glyphRejuv = TalentManager.HasGlyph("Rejuvenation");
            talentGermination = HasTalent(DruidTalents.Germination);

            MaxRejuvStacks = talentGermination ? 2u : 1u;
            return null;
        }

        [Behavior(BehaviorType.Rest, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidRest()
        {
            return new PrioritySelector(                
                new Decorator(
                    req => !Rest.IsEatingOrDrinking,
                    CreateRestoNonCombatHeal(true)
                    ),
                Rest.CreateDefaultRestBehaviour(null, "Revive"),
                CreateRestoNonCombatHeal(false)
                );
        }

        public static Composite CreateRestoDruidHealOnlyBehavior()
        {
            return CreateRestoDruidHealOnlyBehavior(false, true);
        }

        public static Composite CreateRestoDruidHealOnlyBehavior(bool selfOnly)
        {
            return CreateRestoDruidHealOnlyBehavior(selfOnly, false);
        }

        public static Composite CreateRestoDruidHealOnlyBehavior(bool selfOnly, bool moveInRange)
        {
            HealerManager.NeedHealTargeting = true;

            return CreateHealingOnlyBehavior(selfOnly, moveInRange);
        }

        //private static WoWUnit _moveToHealTarget = null;
        //private static WoWUnit _lastMoveToTarget = null;

        // temporary lol name ... will revise after testing
        public static Composite CreateHealingOnlyBehavior(bool selfOnly, bool moveInRange)
        {
            BehaviorType behaveType = Dynamics.CompositeBuilder.CurrentBehaviorType;

            if (SingularRoutine.CurrentWoWContext == WoWContext.Normal)
                return new ActionAlwaysFail();

            HealerManager.NeedHealTargeting = true;
            PrioritizedBehaviorList behavs = new PrioritizedBehaviorList();
            int cancelHeal = (int)Math.Max(SingularSettings.Instance.IgnoreHealTargetsAboveHealth, Math.Max(DruidSettings.Heal.HealingTouch, DruidSettings.Heal.Regrowth));
            int maxDirectHeal = Math.Max(DruidSettings.Heal.HealingTouch, DruidSettings.Heal.Regrowth);

            Logger.WriteDebugInBehaviorCreate("Druid Healing: will cancel cast of direct heal if health reaches {0:F1}%", cancelHeal);

            #region Cleanse

            if (SingularSettings.Instance.DispelDebuffs != RelativePriority.None)
            {
                int dispelPriority = (SingularSettings.Instance.DispelDebuffs == RelativePriority.HighPriority) ? 999 : -999;
                behavs.AddBehavior(dispelPriority, "Nature's Cure", "Nature's Cure", Dispelling.CreateDispelBehavior());
            }

            #endregion

            #region Save the Group

            // Tank: Rebirth
            if (Helpers.Common.CombatRezTargetSetting != CombatRezTarget.None)
            {
                behavs.AddBehavior(799, "Rebirth Tank/Healer", "Rebirth",
                    Helpers.Common.CreateCombatRezBehavior( "Rebirth", filter => true, requirements => true)
                    );
            }

            if (DruidSettings.Heal.HeartOfTheWild != 0)
            {
                behavs.AddBehavior(795, "Heart of the Wild @ " + DruidSettings.Heal.HeartOfTheWild + "% MinCount: " + DruidSettings.Heal.CountHeartOfTheWild, "Heart of the Wild",
                    new Decorator(
                        ret => Me.IsInGroup(),
                        Spell.BuffSelf(
                            "Heart of the Wild",
                            req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.HeartOfTheWild
                                && DruidSettings.Heal.CountHeartOfTheWild <= HealerManager.Instance.TargetList
                                    .Count(p => p.IsAlive && p.HealthPercent <= DruidSettings.Heal.HeartOfTheWild && p.Location.DistanceSqr(((WoWUnit)req).Location) <= 30 * 30)
                            )
                        )
                    );
            }

            if (DruidSettings.Heal.NaturesSwiftness != 0)
            {
                behavs.AddBehavior(797, "Nature's Swiftness Heal @ " + DruidSettings.Heal.NaturesSwiftness + "%", "Nature's Swiftness",
                    new Decorator(
                        req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.NaturesSwiftness
                            && !Spell.IsSpellOnCooldown("Nature's Swiftness")
                            && Spell.CanCastHack("Rejuvenation", (WoWUnit)req, skipWowCheck: true),
                        new Sequence(
                            Spell.BuffSelfAndWait("Nature's Swiftness", gcd: HasGcd.No),
                            new PrioritySelector(
                                Spell.Cast("Healing Touch", on => (WoWUnit)on, req => true, cancel => false),
                                new Sequence(
                                    Spell.Cast("Regrowth", on => (WoWUnit)on, req => true, cancel => false),
                                    new DecoratorContinue(
                                        req => !glyphRegrowth,
                                        new Action(ret => Spell.UpdateDoubleCast("Regrowth", (WoWUnit) ret))
                                        )
                                    )
                                )
                            )
                        )
                    );
            }

            if (DruidSettings.Heal.Tranquility != 0)
                behavs.AddBehavior(798, "Tranquility @ " + DruidSettings.Heal.Tranquility + "% MinCount: " + DruidSettings.Heal.CountTranquility, "Tranquility",
                    new Decorator(
                        ret => Me.IsInGroup(),
                        Spell.Cast(
                            "Tranquility", 
                            mov => true,
                            on => (WoWUnit)on,
                            req => HealerManager.Instance.TargetList.Count( h => h.IsAlive && h.HealthPercent < DruidSettings.Heal.Tranquility && h.SpellDistance() < 40) >= DruidSettings.Heal.CountTranquility,
                            cancel => false
                            )
                        )
                    );

            if (DruidSettings.Heal.Swiftmend != 0)
            {
                behavs.AddBehavior(797, "Swiftmend Direct @ " + DruidSettings.Heal.Swiftmend + "%", "Swiftmend",
                    new Decorator(
                        ret => (!Spell.IsSpellOnCooldown("Swiftmend") || Spell.GetCharges("Force of Nature") > 0)
                            && ((WoWUnit)ret).PredictedHealthPercent(includeMyHeals: true) < DruidSettings.Heal.Swiftmend
                            && (Me.IsInGroup())
                            && Spell.CanCastHack("Rejuvenation", (WoWUnit)ret, skipWowCheck: true),
                        new Sequence(
                            new DecoratorContinue(
                                req => !((WoWUnit)req).HasAnyAura("Rejuvenation", "Regrowth"),
                                new PrioritySelector(
                                    Spell.Buff("Rejuvenation", on => (WoWUnit)on),
                                    Spell.Cast("Regrowth", on => (WoWUnit)on, req => !glyphRegrowth, cancel => false)
                                    )
                                ),
                            new Wait(TimeSpan.FromMilliseconds(500), until => ((WoWUnit)until).HasAnyAura("Rejuvenation","Regrowth"), new ActionAlwaysSucceed()),
                            new Wait(TimeSpan.FromMilliseconds(1500), until => !Spell.IsGlobalCooldown() && !Spell.IsCastingOrChannelling(), new ActionAlwaysSucceed()),
                            new PrioritySelector(
                                Spell.Cast("Force of Nature", on => (WoWUnit)on, req => Spell.GetCharges("Force of Nature") > 1),
                                Spell.Cast("Swiftmend", on => (WoWUnit)on)
                                )
                            )
                        )
                    );
            }

            if (DruidSettings.Heal.Genesis != 0)
                behavs.AddBehavior(798, "Genesis @ " + DruidSettings.Heal.Genesis + "% MinCount: " + DruidSettings.Heal.CountGenesis, "Genesis",
                    new Decorator(
                        ret => Me.IsInGroup(),
                        Spell.Buff(
                            "Genesis",
                            on => HealerManager.Instance.TargetList
                                .FirstOrDefault( h => h.IsAlive && h.HasAura("Rejuvenation")),
                            req => HealerManager.Instance.TargetList
                                .Count(h => h.IsAlive && h.HealthPercent < DruidSettings.Heal.Genesis && h.Distance < 60) >= DruidSettings.Heal.CountGenesis
                            )
                        )
                    );

            #endregion

            #region Tank Buffing


            // Priority Buff: buff Mastery: Harmony
            if (Me.Level >= 80 && DruidSettings.Heal.BuffHarmony)
            {
                behavs.AddBehavior(100 + PriHighBase, "Buff Harmony w/ Healing Touch", "Healing Touch",
                    new Sequence(
                        Spell.Cast(
                            "Healing Touch",
                            mov => true,
                            on => (WoWUnit)on,
                            req =>
                            {
                                if (Me.GetAuraTimeLeft("Harmony").TotalMilliseconds > 1500)
                                    return false;
                                if (((WoWUnit)req).HealthPercent < maxDirectHeal)
                                    return false;
                                if (Spell.DoubleCastContains(Me, "Harmony"))
                                    return false;
                                if (!Spell.CanCastHack("Healing Touch", (WoWUnit)req))
                                    return false;

                                Logger.Write(LogColor.Hilite, "^Harmony: buffing with Healing Touch");
                                return true;
                            },
                            cancel => Me.GetAuraTimeLeft("Harmony").TotalMilliseconds > 1500
                                && ((WoWUnit)cancel).HealthPercent > cancelHeal
                            ),
                        new Action( r => Spell.UpdateDoubleCast("Harmony", Me))
                        )
                    );
            }

            // Tank: Lifebloom
            behavs.AddBehavior(99 + PriHighBase, "Lifebloom - Tank", "Lifebloom",
                Spell.Buff("Lifebloom", on => GetLifebloomTarget(), req => Me.Combat)
                );

            // Tank: Rejuv if Lifebloom not trained yet
            if (DruidSettings.Heal.Rejuvenation != 0)
            {
                behavs.AddBehavior(98 + PriHighBase, "Rejuvenation - Tank", "Rejuvenation",
                    Spell.Buff("Rejuvenation", on =>
                    {
                        WoWUnit unit = GetBestTankTargetFor("Rejuvenation");
                        if (unit != null && Spell.CanCastHack("Rejuvenation", unit, skipWowCheck: true))
                        {
                            Logger.WriteDebug("Buffing Rejuvenation ON TANK: {0}", unit.SafeName());
                            return unit;
                        }
                        return null;
                    })
                    );
            }

            if (DruidSettings.Heal.Ironbark != 0)
            {
                if (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.Ironbark) + PriHighBase, "Ironbark @ " + DruidSettings.Heal.Ironbark + "%", "Ironbark",
                        Spell.Buff("Ironbark", on => (WoWUnit)on, req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.Ironbark)
                        );
                else
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.Ironbark) + PriHighBase, "Ironbark - Tank @ " + DruidSettings.Heal.Ironbark + "%", "Ironbark",
                        Spell.Buff("Ironbark", on => Group.Tanks.FirstOrDefault(u => u.IsAlive && u.HealthPercent < DruidSettings.Heal.Ironbark && !u.HasAura("Ironbark")))
                        );
            }

            if (DruidSettings.Heal.CenarionWard != 0)
            {
                if (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.CenarionWard) + PriHighBase, "Cenarion Ward @ " + DruidSettings.Heal.CenarionWard + "%", "Cenarion Ward",
                        Spell.Buff("Cenarion Ward", on => (WoWUnit)on, req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.CenarionWard)
                        );
                else
                    behavs.AddBehavior(100 + PriHighBase, "Cenarion Ward - Tanks", "Cenarion Ward",
                        Spell.Buff("Cenarion Ward", on => GetLifebloomTarget(), req => Me.Combat)
                        );
            }

            if (DruidSettings.Heal.NaturesVigil != 0)
            {
                if (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.NaturesVigil) + PriHighBase, "Nature's Vigil @ " + DruidSettings.Heal.NaturesVigil + "%", "Nature's Vigil",
                        Spell.Buff("Nature's Vigil", on => (WoWUnit)on, req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.NaturesVigil)
                        );
                else
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.NaturesVigil) + PriHighBase, "Nature's Vigil - Tank @ " + DruidSettings.Heal.NaturesVigil + "%", "Nature's Vigil",
                        Spell.Buff("Nature's Vigil", on => Group.Tanks.FirstOrDefault(u => u.IsAlive && u.HealthPercent < DruidSettings.Heal.NaturesVigil && !u.HasAura("Nature's Vigil")))
                        );
            }

            if (DruidSettings.Heal.TreeOfLife != 0)
            {
                behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.TreeOfLife) + PriHighBase, "Incarnation: Tree of Life @ " + DruidSettings.Heal.TreeOfLife + "% MinCount: " + DruidSettings.Heal.CountTreeOfLife, "Incarnation: Tree of Life",
                    Spell.BuffSelf("Incarnation: Tree of Life", 
                        req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.TreeOfLife
                            && DruidSettings.Heal.CountTreeOfLife <= HealerManager.Instance.TargetList.Count(h => h.IsAlive && h.HealthPercent < DruidSettings.Heal.TreeOfLife))
                    );
            }

            #endregion

            #region Atonement Only
            // only Atonement healing if above Health %
            if (AddAtonementBehavior() && DruidSettings.Heal.DreamOfCenariusAbovePercent > 0)
            {
                behavs.AddBehavior( 100 + PriHighAtone, "DreamOfCenarius Above " + DruidSettings.Heal.DreamOfCenariusAbovePercent + "%", "Wrath",
                    new Decorator(
                        req => (Me.Combat || SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds) && HealerManager.Instance.TargetList.Count(h => h.HealthPercent < DruidSettings.Heal.DreamOfCenariusAbovePercent) < DruidSettings.Heal.DreamOfCenariusAboveCount,
                        new PrioritySelector(
                            HealerManager.CreateAttackEnsureTarget(),
                            Helpers.Common.EnsureReadyToAttackFromLongRange(),
                            new Decorator(
                                req => Unit.ValidUnit(Me.CurrentTarget),
                                new PrioritySelector(
                                    new Action(r => { Logger.WriteDebug("--- DreamOfCenarius only! ---"); return RunStatus.Failure; }),
                                    Movement.CreateFaceTargetBehavior(),
                                    Spell.Cast("Wrath", mov => true, on => Me.CurrentTarget, req => true, cancel => false)
                                    )
                                )
                            )
                        )
                    );

                behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.DreamOfCenariusAbovePercent) + PriHighAtone, "DreamOfCenarius Above " + DruidSettings.Heal.DreamOfCenariusAbovePercent + "%", "DreamOfCenarius",
                    new Decorator(
                        req => (Me.Combat || SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                            && !HealerManager.Instance.TargetList.Any(h => h.HealthPercent < DruidSettings.Heal.DreamOfCenariusCancelBelowHealthPercent && h.SpellDistance() < 50)
                            && HealerManager.Instance.TargetList.Count(h => h.HealthPercent < DruidSettings.Heal.DreamOfCenariusAbovePercent) < DruidSettings.Heal.DreamOfCenariusAboveCount,
                        new PrioritySelector(
                            HealerManager.CreateAttackEnsureTarget(),
                            Helpers.Common.EnsureReadyToAttackFromLongRange(),
                            new Decorator(
                                req => Unit.ValidUnit(Me.CurrentTarget),
                                new PrioritySelector(
                                    Movement.CreateFaceTargetBehavior(),
                                    new Decorator(
                                        req => Me.IsSafelyFacing(Me.CurrentTarget, 150),
                                        Spell.Cast("Wrath", mov => true, on => Me.CurrentTarget, req => true, cancel => CancelDreamOfCenariusDPS())
                                        )
                                    )
                                )
                            )
                        )
                    );                   

            }
            #endregion

            #region AoE Heals

            if (DruidSettings.Heal.WildMushroom != 0)
                behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.WildMushroom) + PriAoeBase, "Wild Mushroom @ " + DruidSettings.Heal.WildMushroom + "% MinCount: " + DruidSettings.Heal.CountWildMushroom, "Wild Mushroom",
                    new Decorator(
                        ret => Me.IsInGroup(),
                        CreateMushroomSetBehavior()
                        )
                    );

            if (DruidSettings.Heal.WildGrowth != 0)
                behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.WildGrowth) + PriAoeBase, "Wild Growth @ " + DruidSettings.Heal.WildGrowth + "% MinCount: " + DruidSettings.Heal.CountWildGrowth, "Wild Growth",
                    new Decorator(
                        ret => Me.IsInGroup(),
                        new PrioritySelector(
                    // ctx => HealerManager.GetBestCoverageTarget("Wild Growth", Settings.Heal.WildGrowth, 40, 30, Settings.Heal.CountWildGrowth),
                            Spell.Buff(
                                "Wild Growth",
                                on => (WoWUnit)on,
                                req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.WildGrowth
                                    && DruidSettings.Heal.CountWildGrowth <= HealerManager.Instance.TargetList
                                        .Count(p => p.IsAlive && p.HealthPercent <= DruidSettings.Heal.WildGrowth && p.Location.DistanceSqr(((WoWUnit)req).Location) <= 30 * 30))
                            )
                        )
                    );

            /*
                                    if (Settings.Heal.SwiftmendAOE != 0)
                                        behavs.AddBehavior(HealerManager.HealthToPriority(Settings.Heal.SwiftmendAOE) + PriAoeBase, "Swiftmend @ " + Settings.Heal.SwiftmendAOE + "% MinCount: " + Settings.Heal.CountSwiftmendAOE, "Swiftmend",
                                            new Decorator(
                                                ret => Me.IsInGroup(),
                                                new PrioritySelector(
                                                    ctx => HealerManager.GetBestCoverageTarget("Swiftmend", Settings.Heal.SwiftmendAOE, 40, 10, Settings.Heal.CountSwiftmendAOE
                                                        , mainTarget: Unit.NearbyGroupMembersAndPets.Where(p => p.HealthPercent < Settings.Heal.SwiftmendAOE && p.SpellDistance() <= 40 && p.IsAlive && p.HasAnyOfMyAuras("Rejuvenation", "Regrowth"))),
                                                    Spell.Cast("Force of Nature", on => (WoWUnit)on, req => Spell.GetCharges("Force of Nature") > 1),
                                                    Spell.Cast(spell => "Swiftmend", mov => false, on => (WoWUnit)on, req => true, skipWowCheck: true)
                                                    )
                                                )
                                            );
                        */
            #endregion

            #region Direct Heals

            // Regrowth above ToL: Lifebloom so we use Clearcasting procs 
            behavs.AddBehavior(200 + PriSingleBase, "Regrowth on Clearcasting", "Regrowth",
                new Sequence(
                    CastRegrowth(
                        on => 
                        {
                            if (Spell.DoubleCastContains(Me, "Clearcasting"))
                                return null;

                            double clearLeft = Me.GetAuraTimeLeft("Clearcasting").TotalMilliseconds;

                            // ignore if less than regrowth cast time left
                            if ( clearLeft < Spell.GetSpellCastTime("Regrowth").TotalMilliseconds)
                                return null;

                            WoWUnit target = (WoWUnit)on;
                            double healthPercent = target == null ? 0.0 : target.HealthPercent;

                            if (clearLeft < 3000)
                            {
                                if (target == null || healthPercent > 92 || !Spell.CanCastHack("Regrowth", target))
                                {
                                    WoWUnit lbTarget = HealerManager.Instance.TargetList.FirstOrDefault(u => u.GetAuraTimeLeft("Lifebloom").TotalMilliseconds.Between(1500, 10000));
                                    if (lbTarget != null && Spell.CanCastHack("Regrowth", lbTarget))
                                    {
                                        Logger.Write(LogColor.Hilite, "^Clearcasting: Regrowth refresh of Lifebloom @ {0:F1} seconds", lbTarget.GetAuraTimeLeft("Lifebloom").TotalSeconds);
                                        return lbTarget;
                                    }
                                }
                            }

                            // clearLeft > 3000, so clear target if not needed now and try again next pass
                            if (target != null)
                            {
                                // still have enough time remaining on Clearcasting buff, so hold free Regrowth a bit longer to see if greater need arises
                                if (healthPercent > maxDirectHeal)
                                    target = null;
                                else if (!Spell.CanCastHack("Regrowth", target))
                                    target = null;
                            }

                            if (target != null)
                                Logger.Write(LogColor.Hilite, "^Clearcasting: Regrowth at Health {0:F1}%", healthPercent);

                            return target;
                        },
                        req => true,
                        cancel => ((WoWUnit)cancel).HealthPercent > cancelHeal && Me.GetAuraTimeLeft("Clearcasting").TotalMilliseconds > 4000 && !((WoWUnit)cancel).GetAuraTimeLeft("Lifebloom").TotalMilliseconds.Between(Me.CurrentCastTimeLeft.TotalMilliseconds,8750)
                        ),

                    // add double cast entry to make sure we don't try to reuse immediately
                    new Action( r => Spell.UpdateDoubleCast("Clearcasting", Me, 500))
                    )
                );

            behavs.AddBehavior(198 + PriSingleBase, "Rejuvenation @ " + DruidSettings.Heal.Rejuvenation + "%", "Rejuvenation",
                new PrioritySelector(
                    Spell.Buff("Rejuvenation",
                        1,
                        on => (WoWUnit)on,
                        req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.Rejuvenation
                        )
                    )
                );

            if (DruidSettings.Heal.HealingTouch != 0)
            {
                // roll 3 Rejuvs if Glyph of Rejuvenation equipped
                if (glyphRejuv)
                {
                    // make priority 1 higher than Noursh (-1 here due to way HealerManager.HealthToPriority works)
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.HealingTouch - 1) + PriSingleBase, "Roll 3 Rejuvenations for Glyph", "Rejuvenation",
                        new PrioritySelector(
                            Spell.Buff("Rejuvenation",
                                1,
                                on =>
                                {
                                    // iterate through so we can stop at either 3 with rejuv or first without
                                    int cntHasAura = 0;
                                    foreach (WoWUnit h in HealerManager.Instance.TargetList)
                                    {
                                        if (h.IsAlive)
                                        {
                                            if (!h.HasKnownAuraExpired("Rejuvenation", 1))
                                            {
                                                cntHasAura++;
                                                if (cntHasAura >= 3)
                                                    return null;
                                            }
                                            else
                                            {
                                                if (h.InLineOfSpellSight)
                                                {
                                                    return h;
                                                }
                                            }
                                        }
                                    }

                                    return null;
                                },
                                req => true
                                )
                            )
                        );
                }

            }


            int regrowthInstead = 0;
            bool healingTouchKnown = SpellManager.HasSpell("Healing Touch");

            if (DruidSettings.Heal.HealingTouch != 0)
            {
                string whyRegrowth = "";
                if (SpellManager.HasSpell("Regrowth"))
                {
                    if (!healingTouchKnown)
                    {
                        regrowthInstead = Math.Max(DruidSettings.Heal.Regrowth, DruidSettings.Heal.HealingTouch);
                        whyRegrowth = "Regrowth (since Healing Touch unknown) @ ";
                    }                        
                    //else if (TalentManager.HasGlyph("Regrowth"))
                    //{
                    //    regrowthInstead = Math.Max(DruidSettings.Heal.Regrowth, DruidSettings.Heal.HealingTouch);
                    //    whyRegrowth = "Glyphed Regrowth (instead of Healing Touch) @ ";
                    //}
                }

                if (regrowthInstead != 0)
                {
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.HealingTouch) + PriSingleBase, whyRegrowth + regrowthInstead + "%", "Regrowth",
                        new PrioritySelector(
                            Spell.Cast(
                                sp => (Me.Combat || !healingTouchKnown) ? "Regrowth" : "Healing Touch",
                                mov => true,
                                on => (WoWUnit)on,
                                req => ((WoWUnit)req).HealthPercent < regrowthInstead,
                                cancel => ((WoWUnit)cancel).HealthPercent > cancelHeal
                                )
                            )
                        );
                }
                else
                {
                    behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.HealingTouch) + PriSingleBase, "Healing Touch @ " + DruidSettings.Heal.HealingTouch + "%", "Healing Touch",
                        new PrioritySelector(
                            Spell.Cast("Healing Touch",
                                mov => true,
                                on => (WoWUnit)on,
                                req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.HealingTouch,
                                cancel => ((WoWUnit)cancel).HealthPercent > cancelHeal
                                )
                            )
                        );
                }
            }

            if (DruidSettings.Heal.Regrowth != 0 && regrowthInstead == 0)
                behavs.AddBehavior(HealerManager.HealthToPriority(DruidSettings.Heal.Regrowth) + PriSingleBase, "Regrowth @ " + DruidSettings.Heal.Regrowth + "%", "Regrowth",
                new PrioritySelector(
                    Spell.Cast("Regrowth",
                        mov => true,
                        on => (WoWUnit)on,
                        req => ((WoWUnit)req).HealthPercent < DruidSettings.Heal.Regrowth,
                        cancel => ((WoWUnit)cancel).HealthPercent > cancelHeal
                        )
                    )
                );

            #endregion

            #region Lowest Priority Healer Tasks

            behavs.AddBehavior(3, "Rejuvenation while Moving @ " + SingularSettings.Instance.IgnoreHealTargetsAboveHealth + "%", "Rejuvenation",
                new Decorator(
                    req => Me.IsMoving,
                    Spell.Buff("Rejuvenation",
                        on => HealerManager.Instance.TargetList.FirstOrDefault(h => h.IsAlive && h.HealthPercent < SingularSettings.Instance.IgnoreHealTargetsAboveHealth && !h.HasMyAura("Rejuvenation") && Spell.CanCastHack("Rejuvenation", h, true)),
                        req => true
                        )
                    )
                );

/*
            // Atonement
            if (AddAtonementBehavior() && (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds || SingularRoutine.CurrentWoWContext == WoWContext.Instances) && DruidSettings.Heal.DreamOfCenariusWhenIdle)
            {
                // check less than # below DreamOfCenarius Health
                behavs.AddBehavior(1, "DreamOfCenarius when Idle = " + DruidSettings.Heal.DreamOfCenariusWhenIdle.ToString(), "Wrath",
                    new Decorator(
                        req => (Me.Combat || SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds),
                        new PrioritySelector(
                            HealerManager.CreateAttackEnsureTarget(),
                            Helpers.Common.EnsureReadyToAttackFromLongRange(),
                            new Decorator(
                                req => Unit.ValidUnit(Me.CurrentTarget),
                                new PrioritySelector(
                                    new Action(r => { Logger.WriteDebug("--- DreamOfCenarius when idle ---"); return RunStatus.Failure; }),
                                    Movement.CreateFaceTargetBehavior(),
                                    Spell.Cast("Wrath", mov => true, on => Me.CurrentTarget, req => true, cancel => false)
                                    )
                                )
                            )
                        )
                    );
            }
*/
            #endregion

            behavs.OrderBehaviors();

            if (selfOnly == false && Singular.Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.Heal)
                behavs.ListBehaviors();

            return new PrioritySelector(
                ctx => selfOnly ? StyxWoW.Me : HealerManager.FindHighestPriorityTarget(), // HealerManager.Instance.FirstUnit,

                Spell.WaitForCastOrChannel(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown() && ret != null,
                    behavs.GenerateBehaviorTree()
                    ),

                new Decorator(
                    ret => moveInRange,
                    Movement.CreateMoveToUnitBehavior(
                        ret => Battlegrounds.IsInsideBattleground ? (WoWUnit)ret : Group.Tanks.Where(a => a.IsAlive).OrderBy(a => a.Distance).FirstOrDefault(),
                        35f
                        )
                    )
                );
        }


        private static Composite CastRegrowth(UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements, SimpleBooleanDelegate cancel)
        {
            SimpleBooleanDelegate require = r => (glyphRegrowth || !Spell.DoubleCastContains((WoWUnit)r, "Regrowth")) && requirements(r);
            return new Sequence(
                ctx => onUnit(ctx),
                Spell.Cast("Regrowth", on => (WoWUnit)on, req => require(req), cancel),
                new DecoratorContinue(
                    req => !glyphRegrowth,
                    new Action(ret => Spell.UpdateDoubleCast("Regrowth", (WoWUnit)ret))
                    )
                );
        }

        private static uint RejuvStacksFound { get; set; }

        private static Composite CastRejuvenation(UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            SimpleBooleanDelegate require = r => RejuvStacksFound < MaxRejuvStacks && requirements(r);
            return new Sequence(
                ctx => {
                    WoWUnit unit = onUnit(ctx);
                    RejuvStacksFound = (unit == null) ? 0 : unit.GetAuraStacks("Rejuvenation");
                    return unit;
                    },
                Spell.Cast("Rejuvenation", on => (WoWUnit)on, req => require(req)),
                new WaitContinue( TimeSpan.FromMilliseconds(500), until => ((WoWUnit)until).GetAuraStacks("Rejuvenation") != RejuvStacksFound, new ActionAlwaysSucceed())
                );
        }

        private static bool AddAtonementBehavior()
        {
            if (!HasTalent(DruidTalents.DreamOfCenarius))
                return false;

            return Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.Heal
                || Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.PullBuffs
                || Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.Pull
                || Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.CombatBuffs
                || Dynamics.CompositeBuilder.CurrentBehaviorType == BehaviorType.Combat;
        }


        /// <summary>
        /// non-combat heal to top people off and avoid lifebloom, buffs, etc.
        /// </summary>
        /// <param name="selfOnly"></param>
        /// <returns></returns>
        public static Composite CreateRestoNonCombatHeal(bool selfOnly = false)
        {
            return new PrioritySelector(
                ctx => selfOnly || !Me.IsInGroup() ? StyxWoW.Me : HealerManager.FindLowestHealthTarget(), // HealerManager.Instance.FirstUnit,

                new Decorator( 
                    req => req != null && ((WoWUnit)req).Combat && ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < SingularSettings.Instance.IgnoreHealTargetsAboveHealth,
                    CreateRestoDruidHealOnlyBehavior()
                    ),

                new Decorator(
                    req => req != null && !((WoWUnit)req).Combat && ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < SingularSettings.Instance.IgnoreHealTargetsAboveHealth,

                    new Sequence(
                        new Action(on => Logger.WriteDebug("NonCombatHeal on {0}: health={1:F1}% predicted={2:F1}% +mine={3:F1}", ((WoWUnit)on).SafeName(), ((WoWUnit)on).HealthPercent, ((WoWUnit)on).PredictedHealthPercent(), ((WoWUnit)on).PredictedHealthPercent(includeMyHeals: true))),
                        new PrioritySelector(
                            // BUFFS First
                            Spell.Buff("Rejuvenation", 1, on => (WoWUnit)on, req => ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < 95),
                            Spell.Buff("Regrowth", 1, on => (WoWUnit)on, req => !glyphRegrowth && ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < 80),

                            // Direct Heals After
                            Spell.Cast("Healing Touch", on => (WoWUnit)on, req => ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < 65),
                            Spell.Cast("Regrowth", on => (WoWUnit)on, req => ((WoWUnit)req).PredictedHealthPercent(includeMyHeals: true) < 75),

                            // if Moving, spread Rejuv around on those that need to be topped off
                            new Decorator(
                                req => Me.IsMoving,
                                new PrioritySelector(
                                    ctx => HealerManager.Instance.TargetList.FirstOrDefault( h => h.HealthPercent < 95 && !h.HasMyAura("Rejuvenation") && Spell.CanCastHack("Rejuvenation", (WoWUnit) ctx, skipWowCheck: true)),
                                    Spell.Buff("Rejuvenation", on => (WoWUnit) on)
                                    )
                                )
                            )
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidHealBehavior()
        {
            return new PrioritySelector(
                CreateRestoDiagnosticOutputBehavior(),

                HealerManager.CreateStayNearTankBehavior(),

                new Decorator(
                    ret => HealerManager.Instance.TargetList.Any( h => h.Distance < 40 && h.IsAlive && !h.IsMe),
                    new PrioritySelector(
                        CreateRestoDruidHealOnlyBehavior()
                        )
                    )
                );
        }


        [Behavior(BehaviorType.Pull, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidPull()
        {
            return new PrioritySelector(
                HealerManager.CreateStayNearTankBehavior(),
                new Decorator(
                    req => !HealerManager.Instance.TargetList.Any(h => h.IsAlive && !h.IsMe && h.Distance < 40),
                    new PrioritySelector(
                        Helpers.Common.EnsureReadyToAttackFromLongRange(),
                        Spell.WaitForCastOrChannel(),
                        new Decorator(
                            req => !Spell.IsGlobalCooldown(),
                            new PrioritySelector(
                                Helpers.Common.CreateInterruptBehavior(),

                                Movement.WaitForFacing(),
                                Movement.WaitForLineOfSpellSight(),

                                Spell.Buff("Moonfire"),
                                Spell.Cast("Wrath"),
                                Movement.CreateMoveToUnitBehavior(on => Me.CurrentTarget, 35f, 30f)
                                )
                            )
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidCombat()
        {
            return new PrioritySelector(
                HealerManager.CreateStayNearTankBehavior(),
                new Decorator(
                    req => HealerManager.AllowHealerDPS(),
                    new PrioritySelector(
                        Helpers.Common.EnsureReadyToAttackFromLongRange(),
                        Spell.WaitForCastOrChannel(),
                        new Decorator(
                            req => !Spell.IsGlobalCooldown(),
                            new PrioritySelector(
                                Helpers.Common.CreateInterruptBehavior(),

                                Movement.WaitForFacing(),
                                Movement.WaitForLineOfSpellSight(),

                                new Decorator(
                                    ret => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3,
                                    new PrioritySelector(

                                        new PrioritySelector(
                                            ctx => Unit.UnitsInCombatWithUsOrOurStuff(35).Where(u => !u.IsCrowdControlled() && Me.IsSafelyFacing(u)).ToList(),
                                            Spell.Buff("Moonfire", ret => ((List<WoWUnit>)ret).FirstOrDefault(u => u.HasAuraExpired("Moonfire", 2))),
                                            new Decorator(
                                                req => SingularRoutine.CurrentWoWContext == WoWContext.Normal,
                                                Common.CastHurricaneBehavior(
                                                    on => Me.CurrentTarget,
                                                    cancel => {
                                                        if (Me.HealthPercent < 30)
                                                        {
                                                            Logger.Write(LogColor.Cancel, "/cancel Hurricane since my health at {0:F1}%", Me.HealthPercent);
                                                            return true;
                                                        }
                                                        return false;
                                                    }

                                                    )
                                                )
                                            )
                                        )
                                    ),


                                Spell.Buff("Moonfire"),
                                Spell.Cast(
                                    "Wrath", 
                                    on => Unit.UnitsInCombatWithUsOrOurStuff(40).Where(u => !u.IsCrowdControlled() && u.InLineOfSpellSight).OrderByDescending(u => (uint)u.HealthPercent).FirstOrDefault(), 
                                    req => true, 
                                    cancel => HealerManager.CancelHealerDPS()
                                    )
                                )
                            )
                        )
                    )
                );
        }

        [Behavior(BehaviorType.CombatBuffs, WoWClass.Druid, WoWSpec.DruidRestoration)]
        public static Composite CreateRestoDruidCombatBuffs()
        {
            return new PrioritySelector(
                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent <= DruidSettings.Barkskin || Unit.NearbyUnitsInCombatWithMe.Any())
                );
        }


        public static WoWUnit GetBestTankTargetFor(string hotName, int stacks = 1, float health = 100f)
        {
/*
            // fast test unless RaFHelper.Leader is whacked
            try
            {
                if (RaFHelper.Leader != null && RaFHelper.Leader.SpellDistance() < 40 && RaFHelper.Leader.IsAlive)
                {
                    if (SingularSettings.Debug)
                        Logger.WriteDebug("GetBestTankTargetFor('{0}'): found Leader {1} @ {2:F1}%, hasmyaura={3}", hotName, RaFHelper.Leader.SafeName(), RaFHelper.Leader.HealthPercent, RaFHelper.Leader.HasMyAura(hotName));
                    return RaFHelper.Leader;
                }
            }
            catch { }
*/
            WoWUnit hotTarget = null;
            hotTarget = Group.Tanks
                .Where(u => u.IsAlive 
                    && u.Combat 
                    && u.HealthPercent < health
                    && (u.GetAuraStacks(hotName) < stacks || u.GetAuraTimeLeft(hotName).TotalSeconds < 3) 
                    && Spell.CanCastHack(hotName, u))
                .OrderBy(u => (int) u.HealthPercent)
                .FirstOrDefault();

            if (hotTarget != null && SingularSettings.Debug)
                Logger.WriteDebug("GetBestTankTargetFor('{0}'): found Tank {1} @ {2:F1}%, hasmyaura={3}", hotName, hotTarget.SafeName(), hotTarget.HealthPercent, hotTarget.HasMyAura(hotName));

            return hotTarget;
        }

        /// <summary>
        /// returns the Target that has Lifebloom on it or should.  identifies 
        /// </summary>
        /// <returns></returns>
        public static WoWUnit GetLifebloomTarget()
        {
            string hotName = "Lifebloom";
            string canCastSpell = "Rejuvenation";

            // find tank already with HOT 
            WoWUnit hotTarget = Group.Tanks.FirstOrDefault(u => u.IsAlive && u.HasMyAura(hotName));
            if (hotTarget != null)
                return hotTarget;

            // if no Leader or out of range, find Tank that needs HOT (so will allow replacing non-Tank that has currently
            hotTarget = Group.Tanks
                .Where(u => u.IsAlive && Spell.CanCastHack(canCastSpell, u))
                .OrderBy(u => (int) u.HealthPercent)
                .FirstOrDefault();
            if (hotTarget != null)
            {
                if (SingularSettings.Debug)
                    Logger.WriteDebug("GetLifebloomTarget(): tank needs - {0} @ {1:F1}%", hotTarget.SafeName(), hotTarget.HealthPercent);
                return hotTarget;
            }

            // if no tanks in range, see if anyone has Lifebloom
            hotTarget = HealerManager.Instance.TargetList.FirstOrDefault(u => u.IsAlive && u.HasMyAura(hotName));
            if (hotTarget != null)
                return hotTarget;

            // if no tanks in range, find target with most attackers
            var t = HealerManager.Instance.TargetList
                .Where(u => u.IsAlive && Spell.CanCastHack(canCastSpell, u))
                .Select(p => new
                {
                    Unit = p,
                    Count = Unit.UnfriendlyUnits()
                        .Where(u => u.CurrentTargetGuid == p.Guid)
                        .Count(),
                    Health = (int)p.GetPredictedHealthPercent(true)
                })
                .OrderByDescending(v => v.Count)
                .ThenBy(v => v.Health)
                .DefaultIfEmpty(null)
                .FirstOrDefault();

            if (t != null && t.Count > 0)
            {
                if (SingularSettings.Debug)
                    Logger.WriteDebug("GetLifebloomTarget():  found {0} @ {1:F1}% with {2} attackers", t.Unit.SafeName(), t.Health, t.Count);
                return t.Unit;
            }

            return null;
        }


        private static Composite CreateMushroomSetBehavior()
        {
            Composite castShroom;

            if (!TalentManager.HasGlyph("the Sprouting Mushroom"))
                castShroom = Spell.Cast("Wild Mushroom", on => (WoWUnit) on, req => true);
            else
                castShroom = Spell.CastOnGround("Wild Mushroom", on => (WoWUnit) on, req => true, false);

            return new ThrottlePasses(
                1, TimeSpan.FromSeconds(3), RunStatus.Failure,
                new Decorator(
                    req => Me.Combat && Me.IsInGroup(),
                    new Sequence(
                        ctx => GetLifebloomTarget(),

                        new Action( r =>
                        {
                            // if current mushroom still good, fail for now
                            WoWUnit mushroom = Mushrooms.FirstOrDefault();
                            if (mushroom != null && HealerManager.Instance.TargetList.Count( u => u.IsAlive && u.HealthPercent <= DruidSettings.Heal.WildMushroom && mushroom.SpellDistance(u) < 10) >= DruidSettings.Heal.CountWildMushroom)
                                return RunStatus.Failure;

                            // if no target given, fail for now
                            if (r == null)
                                return RunStatus.Failure;

                            // if target is moving, fail for now
                            if ((r as WoWUnit).IsMoving)
                                return RunStatus.Failure;

                            // if we don't have enough heal targets close and not moving, fail for now
                            if (HealerManager.Instance.TargetList.Count(u => u.IsAlive && !u.IsMoving && u.HealthPercent <= DruidSettings.Heal.WildMushroom && (r as WoWUnit).SpellDistance(u) < 10) < DruidSettings.Heal.CountWildMushroom)
                                return RunStatus.Failure;

                            // continue, as group appears to be staying in spot for awhile
                            return RunStatus.Success;
                        }),

                        castShroom,

                        // following just in case
                        new Action(ctx => Lua.DoString("SpellStopTargeting()"))
                        )
                    )
                );

        }

        public static bool CancelDreamOfCenariusDPS()
        {
            // always let DPS casts while solo complete
            WoWContext ctx = SingularRoutine.CurrentWoWContext;
            if (ctx == WoWContext.Normal)
                return false;

            bool castInProgress = Spell.IsCastingOrChannelling();
            if (!castInProgress)
            {
                return false;
            }

            // allow casts that are close to finishing to finish regardless
            if (castInProgress && Me.CurrentCastTimeLeft.TotalMilliseconds < 333 && Me.CurrentChannelTimeLeft.TotalMilliseconds < 333)
            {
                Logger.WriteDebug("CancelDreamOfCenariusDPS: suppressing /cancel since less than 333 ms remaining");
                return false;
            }
/*
            // use a window less than actual to avoid cast/cancel/cast/cancel due to mana hovering at setting level
            if (Me.ManaPercent < (DruidSettings.Heal.DreamOfCenariusCancelBelowManaPercent - 3))
            {
                Logger.Write(LogColor.Hilite, "^DreamOfCenarius: cancel since mana={0:F1}% below min={1}%", Me.ManaPercent, DruidSettings.Heal.DreamOfCenariusCancelBelowManaPercent);
                return true;
            }
*/
            // check if group health has dropped below setting
            WoWUnit low = HealerManager.FindLowestHealthTarget();
            if (low != null && low.HealthPercent < DruidSettings.Heal.DreamOfCenariusCancelBelowHealthPercent)
            {
                Logger.Write(LogColor.Hilite, "^DreamOfCenarius: cancel since {0} @ {1:F1}% fell below minimum {2}%", low.SafeName(), low.HealthPercent, SingularSettings.Instance.HealerCombatMinHealth);
                return true;
            }

            return false;
        }


        #region Diagnostics

        private static Composite CreateRestoDiagnosticOutputBehavior()
        {
            if (!SingularSettings.Debug)
                return new ActionAlwaysFail();

            return new ThrottlePasses(1, 1,
                new Action(ret =>
                {
                    string log;
                    log = string.Format(".... h={0:F1}%/m={1:F1}%, form:{2}, mushcnt={3}",
                        Me.HealthPercent,
                        Me.ManaPercent,
                        Me.Shapeshift.ToString(),
                        MushroomCount
                        );

                    WoWUnit target = Me.CurrentTarget;
                    if (target != null)
                    {
                        log += string.Format(", th={0:F1}%/tm={1:F1}%, dist={2:F1}, face={3}, loss={4}, sfire={5}, mfire={6}",
                            target.HealthPercent,
                            target.ManaPercent,
                            target.Distance,
                            Me.IsSafelyFacing(target),
                            target.InLineOfSpellSight,
                            (long)target.GetAuraTimeLeft("Sunfire", true).TotalMilliseconds,
                            (long)target.GetAuraTimeLeft("Moonfire", true).TotalMilliseconds
                            );
                    }

                    Logger.WriteDebug(Color.AntiqueWhite, log);
                    return RunStatus.Failure;
                })
                );
        }

        #endregion
    }

}
