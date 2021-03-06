﻿using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;

using Styx.CommonBot;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Styx;
using Styx.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using Styx.CommonBot.Routines;
using Singular.Utilities;

namespace Singular.ClassSpecific.Mage
{
    public static class Common
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static MageSettings MageSettings { get { return SingularSettings.Instance.Mage(); } }

        private static DateTime _cancelIceBlockForCauterize = DateTime.MinValue;
        private static WoWPoint locLastFrostArmor = WoWPoint.Empty;
        private static WoWPoint locLastIceBarrier = WoWPoint.Empty;

        public static bool TreatAsFrozen(this WoWUnit unit)
        {
            return Me.HasAura("Brain Freeze") || unit.IsFrozen();
        }

        public static bool IsFrozen(this WoWUnit unit)
        {
            return unit.GetAllAuras().Any(a => a.Spell.Mechanic == WoWSpellMechanic.Frozen || (a.Spell.School == WoWSpellSchool.Frost && a.Spell.SpellEffects.Any(e => e.AuraType == WoWApplyAuraType.ModRoot)));
        }

        [Behavior(BehaviorType.Initialize, WoWClass.Mage)]
        public static Composite CreateMageInitialize()
        {
            if (SingularRoutine.CurrentWoWContext == WoWContext.Normal || SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                Kite.CreateKitingBehavior( CreateSlowMeleeBehavior(), null, null);

            return null;
        }

        [Behavior(BehaviorType.PreCombatBuffs, WoWClass.Mage)]
        public static Composite CreateMagePreCombatBuffs()
        {
            return new PrioritySelector(
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        // Defensive 
                        Spell.BuffSelf("Slow Fall", req => MageSettings.UseSlowFall && Me.IsFalling),

                        PartyBuff.BuffGroup("Dalaran Brilliance", "Arcane Brilliance"),
                        PartyBuff.BuffGroup("Arcane Brilliance", "Dalaran Brilliance"),

                        // Additional armors/barriers for BGs. These should be kept up at all times to ensure we're as survivable as possible.
                        /*
                        new Decorator(
                            ret => SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds,
                            new PrioritySelector(
                                // Don't put up mana shield if we're arcane. Since our mastery works off of how much mana we have!
                                Spell.BuffSelf("Mana Shield", ret => TalentManager.CurrentSpec != WoWSpec.MageArcane)
                                )
                            ),
                        */
//                        CreateMageArmorBehavior(),
/*
                        new PrioritySelector(
                            ctx => MageTable,
                            new Decorator(
                                ctx => ctx != null && CarriedMageFoodCount < 60 && StyxWoW.Me.FreeNormalBagSlots > 1,
                                new Sequence(
                                    new Action(ctx => Logger.Write( LogColor.Hilite, "^Getting Mage food")),
                // Move to the Mage table
                                    new DecoratorContinue(
                                        ctx => ((WoWGameObject)ctx).DistanceSqr > 5 * 5,
                                        new Action(ctx => Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, ((WoWGameObject)ctx).Location, 5))))
                                        ),
                // interact with the mage table
                                    new Action(ctx => ((WoWGameObject)ctx).Interact()),
                                    new WaitContinue(2, ctx => false, new ActionAlwaysSucceed())
                                    )
                                )
                            ),
*/
                        new ThrottlePasses(
                            1, TimeSpan.FromSeconds(10),
                            RunStatus.Failure,
                            new Decorator(
                                ctx => ShouldSummonTable && NeedTableForBattleground,
                                new Sequence(
                                    new Action(ctx => Logger.Write( LogColor.Hilite, "^Conjure Refreshment Table")),
                                    Spell.Cast("Conjure Refreshment Table", mov => true, on => Me, req => true, cancel => false, LagTolerance.No ),
                                    // new Action(ctx => Spell.CastPrimative("Conjure Refreshment Table")),
                                    new WaitContinue(4, ctx => !StyxWoW.Me.IsCasting, new ActionAlwaysSucceed())
                                    )
                                )
                            ),

                        Spell.BuffSelf("Conjure Refreshment", ret => !Gotfood && !ShouldSummonTable)
                        )
                    )
                );
        }

        [Behavior(BehaviorType.LossOfControl, WoWClass.Mage)]
        public static Composite CreateMageLossOfControlBehavior()
        {
            return new Decorator(
                req => Me.Combat,
                new PrioritySelector(

                    // deal with Ice Block here (a stun of our own doing)
                    new Decorator(
                        ret => Me.ActiveAuras.ContainsKey("Ice Block"),
                        new PrioritySelector(
                            new Throttle(10, new Action(r => Logger.Write(Color.DodgerBlue, "^Ice Block for 10 secs"))),
                            new Decorator(
                                ret => DateTime.UtcNow < _cancelIceBlockForCauterize && !Me.ActiveAuras.ContainsKey("Cauterize"),
                                new Action(ret => {
                                    Logger.Write(LogColor.Cancel, "/cancel Ice Block since Cauterize has expired");
                                    _cancelIceBlockForCauterize = DateTime.MinValue ;
                                    // Me.GetAuraByName("Ice Block").TryCancelAura();
                                    Me.CancelAura("Ice Block");
                                    return RunStatus.Success;
                                    })
                                ),
                            new ActionIdle()
                            )
                        ),

                    Spell.BuffSelf("Cold Snap", req => Me.Combat && Me.HealthPercent < MageSettings.ColdSnapHealthPct)
                    // Spell.BuffSelf("Blink", ret => MovementManager.IsClassMovementAllowed && Me.Stunned && !TalentManager.HasGlyph("Rapid Displacement")),
                    )
                );
        }

        /// <summary>
        /// PullBuffs that must be called only when in Pull and in range of target
        /// </summary>
        /// <returns></returns>
        public static Composite CreateMagePullBuffs()
        {
            return new Decorator(
                req => Me.GotTarget() && Me.CurrentTarget.SpellDistance() < 40,
                new PrioritySelector(
                    CreateMageRuneOfPowerBehavior()
                    )
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Mage, priority: 1)]
        public static Composite CreateMageCombatHeal()
        {
            return new PrioritySelector(
                // handle Cauterize debuff if we took talent and get it
                new Decorator(
                    ret => Me.ActiveAuras.ContainsKey("Cauterize"),
                    new PrioritySelector(
                        Spell.BuffSelf("Ice Block",
                            ret =>
                            {
                                _cancelIceBlockForCauterize = DateTime.UtcNow.AddSeconds(10);
                                return true;
                            }),

                        Spell.BuffSelf("Ice Barrier"),

                        new Throttle(8, Item.CreateUsePotionAndHealthstone(100, 0))
                        )
                    ),

                // Ice Block cast if we didn't take Cauterize
                Spell.BuffSelf("Ice Block",
                    ret => SingularRoutine.CurrentWoWContext != WoWContext.Instances
                        && !SpellManager.HasSpell("Cauterize")
                        && StyxWoW.Me.HealthPercent < 20
                        && !StyxWoW.Me.ActiveAuras.ContainsKey("Hypothermia")
                    ),

                Spell.BuffSelf("Slow Fall", req => MageSettings.UseSlowFall && Me.IsFalling),

                Spell.BuffSelf(
                    "Evanesce",
                    req =>
                    {
                        if (EventHandlers.TimeSinceAttackedByEnemyPlayer.TotalSeconds < 3)
                            return true;
                        if (!Me.Combat)
                            return false;
                        int cntMobs = Unit.UnfriendlyUnits(40).Count(u => u.Combat && u.CurrentTargetGuid == Me.Guid);
                        if (cntMobs == 0)
                        {
                            return false;
                        }
                        if (cntMobs < 3 && Me.HealthPercent > MageSettings.EvanesceHealthPct)
                        {
                            return false;
                        }

                        return true;
                    }),

                Spell.BuffSelf("Cold Snap", req => Me.Combat && Me.HealthPercent < MageSettings.ColdSnapHealthPct),
                Spell.BuffSelf("Ice Ward"),

                // cast Evocation for Heal or Mana
                new Decorator(
                    req => Me.Specialization == WoWSpec.MageArcane,
                    new Throttle(3, Spell.Cast("Evocation", mov => true, on => Me, ret => NeedEvocation, cancel => false))
                    ),

                Spell.BuffSelf("Ice Barrier")

                );
        }

        [Behavior(BehaviorType.CombatBuffs, WoWClass.Mage)]
        public static Composite CreateMageCombatBuffs()
        {
            return new Decorator(
                req => !Unit.IsTrivial(Me.CurrentTarget),
                new PrioritySelector(

                    // Defensive 
                    CastAlterTime(),

                    // new Wait( 1, until => !HasTalent(MageTalents.Invocation) || Me.HasAura("Invoker's Energy"), new ActionAlwaysSucceed())

                    Dispelling.CreatePurgeEnemyBehavior("Spellsteal"),
                    // CreateMageSpellstealBehavior(),

                    CreateMageRuneOfPowerBehavior(),
                    CreateMageInvocationBehavior(),

                    Spell.Buff("Nether Tempest", 1, on => Me.CurrentTarget, req => true),
                    Spell.Buff("Living Bomb", 0, on => Me.CurrentTarget, req => true),
                    Spell.Buff("Frost Bomb", 0, on => Me.CurrentTarget, req => true),

                    Spell.Cast("Mirror Image", 
                         req => Me.GotTarget() &&  (Me.CurrentTarget.IsBoss() || (Me.CurrentTarget.Elite && SingularRoutine.CurrentWoWContext != WoWContext.Instances) || Me.CurrentTarget.IsPlayer || Unit.NearbyUnitsInCombatWithMeOrMyStuff.Count() >= 3)),

                    Spell.BuffSelf("Time Warp", ret => MageSettings.UseTimeWarp && NeedToTimeWarp),

                    CreateHealWaterElemental()
                    )
                );
        }

        public static Composite CreateHealWaterElemental()
        {
#if REMOVED_IN_WOD
            return Spell.Cast("Frostbolt", mov => true, on => Me.Pet,
                req => 
                {
                    if (Me.Pet != null && Me.Pet.IsAlive)
                    {
                        if (Me.Pet.PredictedHealthPercent(true) < MageSettings.HealWaterElementalPct)
                        {
                            if (Spell.CanCastHack("Frostbolt", Me.Pet, false))
                            {
                                Logger.Write( LogColor.Hilite, "^Heal Water Elemental: currently at {0:F1}%", Me.Pet.HealthPercent);
                                return true;
                            }
                        }
                    }
                    return false;
                },
                cancel => Me.Pet == null || !Me.Pet.IsAlive || Me.Pet.PredictedHealthPercent(false) >= 100
                )
            ;
#else
            return new ActionAlwaysFail();
#endif
        }

        private static readonly uint[] MageFoodIds = new uint[]
                                                         {
                                                             65500,
                                                             65515,
                                                             65516,
                                                             65517,
                                                             43518,
                                                             43523,
                                                             65499, //Conjured Mana Cake - Pre Cata Level 85
                                                             80610, //Conjured Mana Pudding - MoP Lvl 85+
                                                             80618  //Conjured Mana Buns 
                                                             //This is where i made a change.
                                                         };

        private const uint ArcanePowder = 17020;

        /// <summary>
        /// True if config allows conjuring tables, we have the spell, are not moving, group members
        /// are within 15 yds, and no table within 40 yds
        /// </summary>
        private static bool ShouldSummonTable
        {
            get
            {
                return MageSettings.SummonTableIfInParty 
                    && SpellManager.HasSpell("Conjure Refreshment Table") 
                    && !StyxWoW.Me.IsMoving
                    && MageTable == null
                    && Unit.GroupMembers.Any(p => !p.IsMe && p.DistanceSqr < 15 * 15);
            }
        }

       static readonly Dictionary<uint, uint> RefreshmentTableIds = new Dictionary<uint,uint>() 
                                         {
                                             { 186812, 70 }, //Level 70
                                             { 207386, 80 }, //Level 80
                                             { 207387, 85 }, //Level 85
                                             { 211363, 90 }, //Level 90
                                         };

        /// <summary>
        /// finds a level appropriate Mage Table if one exists.
        /// </summary>
        static public WoWGameObject MageTable
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Where(
                            i => RefreshmentTableIds.ContainsKey(i.Entry) 
                                && RefreshmentTableIds[i.Entry] <= Me.Level 
                                && (StyxWoW.Me.PartyMembers.Any(p => p.Guid == i.CreatedByGuid) || StyxWoW.Me.Guid == i.CreatedByGuid)
                                && i.Distance <= SingularSettings.Instance.TableDistance
                            )
                        .OrderByDescending( t => t.Level )
                        .FirstOrDefault();
            }
        }

        public static int CarriedMageFoodCount
        {
            get
            {

                return (int)StyxWoW.Me.CarriedItems.Sum(i => i != null
                                                      && i.ItemInfo != null
                                                      && i.ItemInfo.ItemClass == WoWItemClass.Consumable
                                                      && i.Effects != null
                                                      && i.Effects.Count > 0
                                                      && i.Effects[0].Spell.Name.Contains("Refreshment")
                                                          ? i.StackCount
                                                          : 0);
            }
        }
        
   
        public static bool Gotfood { get { return StyxWoW.Me.BagItems.Any(item => MageFoodIds.Contains(item.Entry)); } }

        private static bool HaveManaGem { get { return StyxWoW.Me.BagItems.Any(i => i.Entry == 36799 || i.Entry == 81901); } }

        public static Composite CreateUseManaGemBehavior() { return CreateUseManaGemBehavior(ret => true); }

        public static Composite CreateUseManaGemBehavior(SimpleBooleanDelegate requirements)
        {
            return new Throttle( 2, 
                new PrioritySelector(
                    ctx => StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == 36799 || i.Entry == 81901),
                    new Decorator(
                        ret => ret != null && StyxWoW.Me.ManaPercent < 100 && ((WoWItem)ret).Cooldown == 0 && requirements(ret),
                        new Sequence(
                            new Action(ret => Logger.Write("Using {0}", ((WoWItem)ret).Name)),
                            new Action(ret => ((WoWItem)ret).Use())
                            )
                        )
                    )
                );
        }

        public static Composite CreateStayAwayFromFrozenTargetsBehavior()
        {
#if NOPE
            return new PrioritySelector(
                ctx => Unit.NearbyUnfriendlyUnits
                           .Where( u => u.IsFrozen() && Me.SpellDistance(u) < 8)
                           .OrderBy(u => u.DistanceSqr).FirstOrDefault(),
                new Decorator(
                    ret => ret != null && MovementManager.IsClassMovementAllowed,
                    new PrioritySelector(
                        new Decorator(
                            ret => Spell.GetSpellCooldown("Blink").TotalSeconds > 0
                                && Spell.GetSpellCooldown("Rocket Jump").TotalSeconds > 0,
                            new Action(
                                ret =>
                                {
                                    if (Me.IsMoving && StopMoving.Type == StopMoving.StopType.Location)
                                    {
                                        Logger.WriteDebug(Color.LightBlue, "StayAwayFromFrozen:  looks like we are already moving away");
                                        return RunStatus.Success;
                                    }

                                    WoWPoint moveTo = WoWMathHelper.CalculatePointBehind(
                                        ((WoWUnit)ret).Location,
                                        ((WoWUnit)ret).Rotation,
                                        -Me.SpellRange(12f, (WoWUnit) ret)
                                        );

                                    if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, moveTo))
                                    {
                                        Logger.WriteDebug(Color.LightBlue, "StayAwayFromFrozen:  unable to navigate to point behind me {0:F1} yds away", StyxWoW.Me.Location.Distance(moveTo));
                                        return RunStatus.Failure;
                                    }

                                    Logger.Write( Color.LightBlue, "Getting away from frozen target {0}", ((WoWUnit)ret).SafeName());
                                    Navigator.MoveTo(moveTo);
                                    StopMoving.AtLocation(moveTo);
                                    return RunStatus.Success;
                                })
                            ),

                        new Decorator(
                            ret => !Me.IsMoving,
                            new PrioritySelector(
                                Disengage.CreateDisengageBehavior("Blink", Disengage.Direction.Frontwards, 20, null),
                                Disengage.CreateDisengageBehavior("Rocket Jump", Disengage.Direction.Frontwards, 20, null)
                                )
                            )
                        )
                    )
                );
             */
#else
            return Avoidance.CreateAvoidanceBehavior(
                "Blink", 
                TalentManager.HasGlyph("Blink") ? 28 : 20, 
                Disengage.Direction.Frontwards, 
                crowdControl: CreateSlowMeleeBehavior(),
                needDisengage: nd => Me.GotTarget() && Me.CurrentTarget.IsCrowdControlled() && Me.CurrentTarget.SpellDistance() < SingularSettings.Instance.KiteAvoidDistance,
                needKiting: nk => Me.GotTarget() && (Me.CurrentTarget.IsCrowdControlled() || Me.CurrentTarget.IsSlowed(60)) && Me.CurrentTarget.SpellDistance() < SingularSettings.Instance.KiteAvoidDistance
                );
#endif
        }

        /*
        public static Composite CreateMageSpellstealBehavior()
        {
            return Spell.Cast("Spellsteal", 
                mov => false, 
                on => {
                    WoWUnit unit = GetSpellstealTarget();
                    if (unit != null)
                        Logger.WriteDebug("Spellsteal:  found {0} with a triggering aura, cancast={1}", unit.SafeName(), Spell.CanCastHack("Spellsteal", unit));
                    return unit;
                    },
                ret => SingularSettings.Instance.DispelTargets != CheckTargets.None 
                );                   
        }

        public static WoWUnit GetSpellstealTarget()
        {
            if (SingularSettings.Instance.DispelTargets == CheckTargets.Current)
            {
                if ( Me.GotTarget() && null != GetSpellstealAura( Me.CurrentTarget))
                {
                    return Me.CurrentTarget;
                }
            }
            else if (SingularSettings.Instance.DispelTargets != CheckTargets.None)
            {
                WoWUnit target = Unit.NearbyUnfriendlyUnits.FirstOrDefault(u => Me.IsSafelyFacing(u) && null != GetSpellstealAura(u));
                return target;
            }

            return null;
        }

        public static WoWAura GetSpellstealAura(WoWUnit target)
        {
            return target.GetAllAuras().FirstOrDefault(a => a.TimeLeft.TotalSeconds > 5 && a.Spell.DispelType == WoWDispelType.Magic && PurgeWhitelist.Instance.SpellList.Contains(a.SpellId) && !Me.HasAura(a.SpellId));
        }
        */

        public static Composite CreateMagePolymorphOnAddBehavior()
        {
            if (!MageSettings.UsePolymorphOnAdds)
                return new ActionAlwaysFail();

            return new Decorator(
                req => !Unit.NearbyUnfriendlyUnits.Any(u => u.HasMyAura("Polymorph")),
                Spell.Buff(
                    "Polymorph", 
                    on => Unit.UnfriendlyUnits()
                        .Where(IsViableForPolymorph)
                        .OrderByDescending(u => u.CurrentHealth)
                        .FirstOrDefault()
                    )
                );
        }

        private static bool IsViableForPolymorph(WoWUnit unit)
        {
            if (StyxWoW.Me.CurrentTargetGuid == unit.Guid)
                return false;

            if (!unit.Combat)
                return false;

            if (unit.CreatureType != WoWCreatureType.Beast && unit.CreatureType != WoWCreatureType.Humanoid)
                return false;

            if (unit.IsCrowdControlled())
                return false;

            if (!unit.IsTargetingMeOrPet && !unit.IsTargetingMyPartyMember)
                return false;

            if (StyxWoW.Me.RaidMembers.Any(m => m.CurrentTargetGuid == unit.Guid && m.IsAlive))
                return false;

            if (!unit.SpellDistance().Between(14, 30))
                return false;

            return true;
        }

        public static bool NeedToTimeWarp
        {
            get
            {
                if ( !MageSettings.UseTimeWarp || MovementManager.IsMovementDisabled)
                    return false;

                if (Me.HasAnyAura("Temporal Displacement", PartyBuff.SatedDebuffName))
                    return false;

                if (!Spell.CanCastHack("Time Warp", Me))
                    return false;

                if (Battlegrounds.IsInsideBattleground && Shaman.Common.IsPvpFightWorthLusting)
                {
                    Logger.Write(LogColor.Hilite, "^Time Warp: using in balanced PVP fight");
                    return true;
                }

                if (Me.GotTarget() && Unit.ValidUnit(Me.CurrentTarget) && !Me.CurrentTarget.IsTrivial())
                {
                    if (SingularRoutine.CurrentWoWContext == WoWContext.Normal && Me.CurrentTarget.IsPlayer && Me.HealthPercent > Math.Max(65, Me.HealthPercent ))
                    {
                        Logger.Write(LogColor.Hilite, "^Time Warp: using due to combat with enemy player");
                        return true;
                    }

                    if (Me.CurrentTarget.IsBoss())
                    {
                        Logger.Write(LogColor.Hilite, "^Time Warp: using for Boss encounter with '{0}'", Me.CurrentTarget.SafeName());
                        return true;
                    }

                    if (Me.HealthPercent > 50)
                    {
                        int count = Unit.UnitsInCombatWithUsOrOurStuff(40).Count();
                        if ( count >= 4)
                        {
                            Logger.Write(LogColor.Hilite, "^Time Warp: using due to combat with {0} enemy targets", count);
                            return true;
                        }
                        if ( Me.CurrentTarget.TimeToDeath() > 45)
                        {
                            Logger.Write(LogColor.Hilite, "^Time Warp: using for since {0} expected to live {1:F0} seconds", Me.CurrentTarget.SafeName(), Me.CurrentTarget.TimeToDeath());
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public static bool HasTalent( MageTalents tal)
        {
            return TalentManager.IsSelected((int)tal);
        }

        private static int _secsBeforeBattle = 0;

        public static int secsBeforeBattle
        {
            get
            {
                if (_secsBeforeBattle == 0)
                    _secsBeforeBattle = new Random().Next(30, 60);

                return _secsBeforeBattle;
            }

            set
            {
                _secsBeforeBattle = value;
            }
        }

        public static bool NeedTableForBattleground 
        {
            get
            {
                return SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds 
                    && PVP.PrepTimeLeft < secsBeforeBattle && Me.HasAnyAura("Preparation", "Arena Preparation");
            }
        }

/* passive in WoD
        /// <summary>
        /// behavior to cast appropriate Armor 
        /// </summary>
        /// <returns></returns>
        public static Composite CreateMageArmorBehavior()
        {
            return new Throttle(TimeSpan.FromMilliseconds(500),
                new Sequence(
                    new Action(ret => _Armor = GetBestArmor()),
                    new Decorator(
                        ret => _Armor != MageArmor.None
                            && !Me.HasMyAura(ArmorSpell(_Armor))
                            && Spell.CanCastHack(ArmorSpell(_Armor), Me),
                        Spell.BuffSelf(s => ArmorSpell(_Armor), ret => !Me.HasAura(ArmorSpell(_Armor)))
                        )
                    )
                );
        }

        static MageArmor _Armor;

        static string ArmorSpell(MageArmor s)
        {
            return s.ToString() + " Armor";
        }

        /// <summary>
        /// determines the best MageArmor value to use.  Attempts to use 
        /// user setting first, but defaults to something reasonable otherwise
        /// </summary>
        /// <returns>MageArmor to use</returns>
        public static MageArmor GetBestArmor()
        {
            if (MageSettings.Armor == MageArmor.None)
                return MageArmor.None;

            if (TalentManager.CurrentSpec == WoWSpec.None)
                return MageArmor.None;

            MageArmor bestArmor;
            if (MageSettings.Armor != Settings.MageArmor.Auto)
                bestArmor = MageSettings.Armor;
            else if (SingularRoutine.CurrentWoWContext == WoWContext.Battlegrounds)
                bestArmor = MageArmor.Frost;
            else
            {
                if (TalentManager.CurrentSpec == WoWSpec.MageArcane)
                    bestArmor = MageArmor.Mage;
                else if (TalentManager.CurrentSpec == WoWSpec.MageFrost)
                    bestArmor = MageArmor.Frost;
                else
                    bestArmor = MageArmor.Molten;
            }

            if (bestArmor == MageArmor.Mage && Me.Level < 80)
                bestArmor = MageArmor.Frost;

            if (bestArmor == MageArmor.Frost && Me.Level < 54)
                bestArmor = MageArmor.Molten;

            if (bestArmor == MageArmor.Molten && Me.Level < 34)
                bestArmor = MageArmor.None;

            return bestArmor;
        }
*/
        public static Composite CreateSpellstealEnemyBehavior()
        {
            return Dispelling.CreatePurgeEnemyBehavior("Spellsteal");
        }

        #region Avoidance and Disengage

        /// <summary>
        /// creates a Mage specific avoidance behavior based upon settings.  will check for safe landing
        /// zones before using Blink or Rocket Jump.  will additionally do a running away or jump turn
        /// attack while moving away from attacking mob if behaviors provided
        /// </summary>
        /// <param name="nonfacingAttack">behavior while running away (back to target - instants only)</param>
        /// <param name="jumpturnAttack">behavior while facing target during jump turn (instants only)</param>
        /// <returns></returns>
        public static Composite CreateMageAvoidanceBehavior()
        {
            int distBlink = TalentManager.HasGlyph("Blink") ? 28 : 20;
            return Avoidance.CreateAvoidanceBehavior(
                "Blink", 
                distBlink, 
                Disengage.Direction.Frontwards, 
                crowdControl: CreateSlowMeleeBehavior(),
                needDisengage: nd => false,
                needKiting: nk => Me.GotTarget() && Me.CurrentTarget.IsFrozen() && Me.CurrentTarget.SpellDistance() < 8
                );
        }

        /*
        private static Composite CreateSlowMeleeBehavior()
        {
            return new Decorator(
                ret => Unit.NearbyUnfriendlyUnits.Any(u => u.SpellDistance() <= 8 && !u.Stunned && !u.Rooted && !u.IsSlowed()),
                new PrioritySelector(
                    new Decorator(
                        ret => TalentManager.CurrentSpec == WoWSpec.MageFrost,
                        Mage.Frost.CastFreeze(on => Clusters.GetBestUnitForCluster(Unit.NearbyUnfriendlyUnits.Where(u => u.SpellDistance() < 8), ClusterType.Radius, 8))
                        ),
                    Spell.Buff("Frost Nova"),
                    Spell.Buff("Frostjaw"),
                    // Spell.CastOnGround("Ring of Frost", loc => Me.Location, req => true, false),
                    Spell.Buff("Cone of Cold")
                    )
                );
        }
        */

        public static Composite CreateSlowMeleeBehavior()
        {
            return new PrioritySelector(
                ctx => SafeArea.NearestEnemyMobAttackingMe,
                new Action( ret => {
                    if (SingularSettings.Debug)
                    {
                        if (ret == null)
                            Logger.WriteDebug("SlowMelee: no nearest mob found");
                        else
                            Logger.WriteDebug("SlowMelee: crowdcontrolled: {0}, slowed: {1}", ((WoWUnit)ret).IsCrowdControlled(), ((WoWUnit)ret).IsSlowed());
                    }
                    return RunStatus.Failure;
                    }),
                new Decorator(
                    // ret => ret != null && !((WoWUnit)ret).Stunned && !((WoWUnit)ret).Rooted && !((WoWUnit)ret).IsSlowed(),
                    ret => ret != null,
                    new PrioritySelector(
                        new Decorator(
                            req => ((WoWUnit)req).IsCrowdControlled(),
                            new SeqDbg( 1f, s => "SlowMelee: target already crowd controlled")
                            ),
                        Spell.CastOnGround("Ring of Frost", onUnit => (WoWUnit)onUnit, req => ((WoWUnit)req).SpellDistance() < 30, true),
                        Spell.Cast("Frost Nova", mov => true, onUnit => (WoWUnit)onUnit, req => ((WoWUnit)req).SpellDistance() < 12, cancel => false),
                        new Decorator(
                            ret => TalentManager.CurrentSpec == WoWSpec.MageFrost,
                            Mage.Frost.CastFreeze(on => Clusters.GetBestUnitForCluster(Unit.NearbyUnfriendlyUnits.Where(u => u.SpellDistance() < 8), ClusterType.Radius, 8))
                            ),
                        Spell.Cast("Frostjaw", mov => true, onUnit => (WoWUnit)onUnit, req => true, cancel => false)
/*
                        ,
                        new Decorator(
                            req => ((WoWUnit)req).IsSlowed(60),
                            new Action(r => Logger.WriteDebug("SlowMelee: target already slowed at least 50%"))
                            ),
                        Spell.Cast(
                            "Cone of Cold", 
                            mov => true, 
                            on => (WoWUnit)on, 
                            req => ((WoWUnit)req).SpellDistance() < 12 && Me.IsSafelyFacing((WoWUnit)req,90), 
                            cancel => false
                            )
 */
                        )
                    )
                );
        }

        #endregion

        public static bool NeedEvocation 
        { 
            get 
            {
                /* changed in WoD
                // never cast Evocation if we talent rune of power
                if (HasTalent(MageTalents.RuneOfPower))
                    return false;
                */

                if (!Spell.CanCastHack("Evoation"))
                    return false;

                // always evocate if low mana
                if (Me.ManaPercent <= MageSettings.EvocationManaPct)
                {
                    Logger.Write( LogColor.Hilite, "^Evocation: casting due to low Mana @ {0:F1}%", Me.ManaPercent);
                    return true;
                }

                /* changed in WoD
                // if low health, return true if we are glyphed (made sure no invocation or rune of power talented chars reach here already)
                if (Me.HealthPercent < 40)
                {
                    bool needHeal = TalentManager.HasGlyph("Evocation");
                    if (needHeal)
                        Logger.Write( LogColor.Hilite, "^Evocation: casting for glyphed heal");
                    return needHeal;
                }
                */
                return false;
            }
        }

        private static Composite _runeOfPower;

        public static Composite CreateMageRuneOfPowerBehavior()
        {
            if (!Common.HasTalent(MageTalents.RuneOfPower))
                return new ActionAlwaysFail();

            if (_runeOfPower == null)
            {
                _runeOfPower = new ThrottlePasses(
                    1,
                    TimeSpan.FromSeconds(6),
                    RunStatus.Failure,
                    Spell.CastOnGround("Rune of Power", on => Me, req => !Me.IsMoving && !Me.InVehicle && !Me.HasAura("Rune of Power") && Singular.Utilities.EventHandlers.LastNoPathFailure.AddSeconds(15) < DateTime.UtcNow, false)
                    );
            }

            return _runeOfPower;
        }

        public static Composite CreateMageInvocationBehavior()
        {
/* WOD:
            if (!Common.HasTalent(MageTalents.Invocation))
                return new ActionAlwaysFail();

            return new Decorator(
                req => !Me.HasAura("Invoker's Energy") && Spell.CanCastHack("Evocation"),
                new Sequence(
                    new Action(r => Logger.Write( LogColor.Hilite, "^Invocation: buffing Invoker's Energy")),
                    Spell.Cast("Evocation", on => Me, req => true, cancel => false),
                    Helpers.Common.CreateWaitForLagDuration(),
                    new Wait(TimeSpan.FromMilliseconds(500), until => Me.HasAura("Invoker's Energy"), new ActionAlwaysSucceed())
                    )
                );
 */
            return new ActionAlwaysFail();
        }

        /// <summary>
        /// handle Alter Time cast (both initial and secondary to reset)
        /// </summary>
        /// <returns></returns>
        public static Composite CastAlterTime()
        {
            return new Throttle(
                1,
                new PrioritySelector(
                    ctx => Me.HasAura("Alter Time"),
                    new Sequence(
                        Spell.BuffSelf(
                            "Alter Time", 
                            req =>
                            {
                                if ((bool) req)
                                    return false;

                                int countEnemy = Unit.UnitsInCombatWithMeOrMyStuff(40).Count();
                                if (countEnemy >= MageSettings.AlterTimeMobCount)
                                    return true;
                                int countPlayers = Unit.UnfriendlyUnits(45).Count(u => u.IsPlayer);
                                if (countPlayers >= MageSettings.AlterTimePlayerCount)
                                    return true;

                                return false;
                            }),
                            new Action( r => {
                                _healthAlterTime = (int)Me.HealthPercent;
                                _locAlterTime = Me.Location;
                                Logger.Write( LogColor.Hilite, "^Alter Time: cast again if health falls below {0}%", (_healthAlterTime * MageSettings.AlterTimeHealthPct) / 100);
                                })
                            ),

                    new Decorator(
                        req => ((bool)req) && Me.HealthPercent <= ((_healthAlterTime * MageSettings.AlterTimeHealthPct) / 100),
                        new Action( r => {
                            Logger.Write( LogColor.Hilite, "^Alter Time: restoring to {0}% at {1:F1} yds away", _healthAlterTime, _locAlterTime.Distance(Me.Location));
                            Spell.LogCast("Alter Time", Me, true);
                            Spell.CastPrimative("Alter Time");
                            })
                        )
                    )
                );
        }

        private static WoWPoint _locAlterTime { get; set; }
        private static int _healthAlterTime { get; set; }

    }

    public enum MageTalents
    {
#if PRE_WOD
        None = 0,
        PresenceOfMind,
        BlazingSpeed,
        IceFloes,
        TemporalShield,
        Flameglow,
        IceBarrier,
        RingOfFrost,
        IceWard,
        Frostjaw,
        GreaterInivisibility,
        Cauterize,
        ColdSnap,
        NetherTempest,
        LivingBomb,
        FrostBomb,
        Invocation,
        RuneOfPower,
        IncantersWard
#else

        Evanesce = 1,
        BlazingSpeed,
        IceFloes,

        AlterTime,
        Flameglow,
        IceBarrier,

        RingOfFrost,
        IceWard,
        Frostjaw,

        GreaterInvisibility,
        Cauterize,
        ColdSnap,

        NetherTempest,
        LivingBomb = NetherTempest,
        FrostBomb = NetherTempest,
        UnstableMagic,
        Supernova,
        BlastWave = Supernova,
        IceNova = Supernova,

        MirrorImage,
        RuneOfPower,
        IncantersFlow,

        Overpowered,
        Kindling = Overpowered,
        ThermalVoid = Overpowered,
        PrismaticCrystal,
        ArcaneOrb,
        Meteor = ArcaneOrb,
        CometStorm = ArcaneOrb

#endif
    }
}