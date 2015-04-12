﻿using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;

using Styx;

using Styx.CommonBot;
using Styx.Helpers;


using Styx.WoWInternals;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Singular.Lists;
using Styx.WoWInternals.WoWObjects;
using Rest = Singular.Helpers.Rest;
using System.Drawing;
using CommonBehaviors.Actions;
using Styx.CommonBot.POI;

namespace Singular.ClassSpecific.Shaman
{
    public class Enhancement
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static ShamanSettings ShamanSettings { get { return SingularSettings.Instance.Shaman(); } }

        private static bool NeedFeralSpirit
        {
            get 
            {
                return ShamanSettings.FeralSpiritCastOn == CastOn.All
                    || (ShamanSettings.FeralSpiritCastOn == CastOn.Bosses && StyxWoW.Me.CurrentTarget.Elite)
                    || (ShamanSettings.FeralSpiritCastOn == CastOn.Players && Unit.NearbyUnfriendlyUnits.Any(u => u.IsPlayer && u.Combat && u.IsTargetingMeOrPet));
            }
        }

        #region Common

        [Behavior(BehaviorType.PreCombatBuffs|BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances | WoWContext.Normal)]
        public static Composite CreateShamanEnhancementPreCombatBuffs()
        {
            return new PrioritySelector(

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.PreCombatBuffs|BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds)]
        public static Composite CreateShamanEnhancementPvpPreCombatBuffs()
        {
            return new PrioritySelector(

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.Rest, WoWClass.Shaman, WoWSpec.ShamanEnhancement)]
        public static Composite CreateShamanEnhancementRest()
        {
            return new PrioritySelector(
                Spell.WaitForCast(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        new Decorator(
                            ret => !Me.HasAnyAura("Drink", "Food", "Refreshment"),
                            Common.CreateShamanDpsHealBehavior()
                            ),

                        Rest.CreateDefaultRestBehaviour("Healing Surge", "Ancestral Spirit"),

                        Common.CreateShamanMovementBuff()
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Healing Surge", on => Me, 
                    ret => Me.PredictedHealthPercent(includeMyHeals: true) < ShamanSettings.MaelHealingSurge && StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),

                Common.CreateShamanDpsHealBehavior()
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances)]
        public static Composite CreateShamanEnhancementHealInstances()
        {
            return Common.CreateShamanDpsHealBehavior();
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds )]
        public static Composite CreateShamanEnhancementHealPvp()
        {
            return new PrioritySelector(
                new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5),
                    new PrioritySelector(
                        Spell.Cast("Healing Surge", ret => StyxWoW.Me, ret => StyxWoW.Me.PredictedHealthPercent() < ShamanSettings.MaelHealingSurge),
                        Spell.Cast("Healing Surge", ret => (WoWPlayer)Unit.GroupMembers.Where(p => p.IsAlive && p.PredictedHealthPercent() < ShamanSettings.MaelPvpOffHeal && p.Distance < 40).FirstOrDefault())
                        )
                    ),

                new Decorator(
                    ret => !StyxWoW.Me.Combat || (!Me.IsMoving && !Unit.NearbyUnfriendlyUnits.Any()),
                    Common.CreateShamanDpsHealBehavior( )
                    )
                );
        }

        #endregion

        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementNormalPull()
        {
            return new PrioritySelector(
                new Decorator(req => Me.Level < 20, Helpers.Common.EnsureReadyToAttackFromMediumRange()),
                new Decorator(req => Me.Level >= 20, Helpers.Common.EnsureReadyToAttackFromMelee()),
                Spell.WaitForCastOrChannel(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Common.CreateShamanDpsShieldBehavior(),

                        Totems.CreateTotemsBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Spell.Cast("Lightning Bolt", ret => !ShamanSettings.AvoidMaelstromDamage && StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),
                        Spell.Cast("Unleash Elements", 
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null 
                                && StyxWoW.Me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id == 5),
                        new Decorator(
                            req => Spell.UseAOE,
                            new PrioritySelector(
                                Spell.Cast("Flame Shock", req => StyxWoW.Me.HasAura("Unleash Flame")),
                                Spell.Buff("Flame Shock", true, req => Me.CurrentTarget.Elite || (!Me.CurrentTarget.IsTrivial() && Unit.UnfriendlyUnits(12).Count() > 1) )
                                )
                            ),

                        Spell.Cast("Frost Shock"),

                        Spell.Cast("Lightning Bolt", ret => Me.Level < 20 || Me.CurrentTarget.IsFlying || !Styx.Pathing.Navigator.CanNavigateFully(Me.Location, Me.CurrentTarget.Location))
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementNormalCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptBehavior(),

                        Totems.CreateTotemsBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Spell.BuffSelf("Feral Spirit", ret => !Unit.IsTrivial(Me.CurrentTarget) && NeedFeralSpirit),

                        new Decorator(
                            req => AttackEvenIfGhostWolf,
                            new PrioritySelector(
                               
                                Dispelling.CreatePurgeEnemyBehavior("Purge"),

                                Common.CreateShamanDpsShieldBehavior(),
                                // Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),

                                // pull more logic (use instants first, then ranged pulls if possible)


                                Common.CastElementalBlast(),
                                Spell.Cast("Unleash Elements", ret => Common.HasTalent(ShamanTalents.UnleashedFury)),

                                Spell.Cast("Stormstrike"),
                                new Decorator(
                                    req => Spell.UseAOE,
                                    new PrioritySelector(
                                        Spell.Cast("Flame Shock", req => StyxWoW.Me.HasAura("Unleash Flame")),
                                        Spell.Buff("Flame Shock", true, req => Me.CurrentTarget.Elite || (!Me.CurrentTarget.IsTrivial() && Unit.UnfriendlyUnitsNearTarget(12).Count(u => u.IsTargetingUs()) > 1))
                                        )
                                    ),
                                Spell.Buff("Frost Shock",
                                    ret => Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3 
                                        || !SpellManager.HasSpell("Improved Flame Shock")
                                        || (Me.CurrentTarget.IsPlayer && !Me.CurrentTarget.IsSlowed())
                                    ),
                                Spell.Cast("Lava Lash",
                                    ret => StyxWoW.Me.Inventory.Equipped.OffHand != null &&
                                           StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon
                                    ),
                                Spell.Buff("Fire Nova",
                                    on => StyxWoW.Me.CurrentTarget,
                                    ret => Spell.UseAOE 
                                        && StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock") 
                                        && Unit.NearbyUnfriendlyUnits
                                            .Count(u => u.IsTargetingUs() && u.Location.DistanceSqr(StyxWoW.Me.CurrentTarget.Location) < 10 * 10) >= 3
                                    ),
                                Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                                Spell.Cast("Unleash Elements"),

                                new Decorator(ret => !ShamanSettings.AvoidMaelstromDamage && StyxWoW.Me.HasAura("Maelstrom Weapon", 5) && (StyxWoW.Me.GetAuraTimeLeft("Maelstom Weapon", true).TotalSeconds < 3000 || StyxWoW.Me.PredictedHealthPercent(includeMyHeals: true) > 90),
                                    new PrioritySelector(
                                        Spell.Cast("Chain Lightning", ret => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled())),
                                        Spell.Cast("Lightning Bolt")
                                        )
                                    ),

                                // won't happen often, but if at range and no abilities enter ghost wolf 
                                CreateInCombatGapCloser()
                                )
                            )
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static bool AttackEvenIfGhostWolf
        {
            get
            {
                if (!Me.GotTarget())
                    return false;

                if (Me.CurrentTarget.SpellDistance() > 10 || Me.CurrentTarget.IsMovingAway())
                {
                    if (!Me.CurrentTarget.IsAboveTheGround())
                    {
                        if (Me.IsMoving && Me.HasAura("Ghost Wolf"))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds)]
        public static Composite CreateShamanEnhancementPvPPullAndCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(), 
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptBehavior(),

                        CreateInCombatGapCloser(),

                        Totems.CreateTotemsBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        new Decorator(
                            req => AttackEvenIfGhostWolf,
                            new PrioritySelector(

                                Dispelling.CreatePurgeEnemyBehavior("Purge"),

                                Common.CreateShamanDpsShieldBehavior(),

                                // Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),
                                Spell.BuffSelf("Feral Spirit", ret => !Unit.IsTrivial(Me.CurrentTarget) && NeedFeralSpirit),

                                Common.CastElementalBlast(),
                                Spell.Cast("Unleash Elements", ret => Common.HasTalent(ShamanTalents.UnleashedFury)),

                                Spell.Cast("Stormstrike"),
                                Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                                Spell.Cast("Lava Lash", 
                                    ret => StyxWoW.Me.Inventory.Equipped.OffHand != null && 
                                            StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon),

                                new Decorator(req => 
                                    {
                                        if (ShamanSettings.AvoidMaelstromDamage || !StyxWoW.Me.HasAura("Maelstrom Weapon", 5))
                                            return false;                                        
                                        if (StyxWoW.Me.PredictedHealthPercent() > 90)
                                            return true;
                                        if (StyxWoW.Me.GetAuraTimeLeft("Maelstom Weapon", true).TotalSeconds > 3000)
                                            return true;
                                        return false;
                                    },
                                    Spell.Cast("Lightning Bolt")
                                    ),

                                Spell.Cast("Unleash Elements"),
                                Spell.Buff("Flame Shock", true, ret => StyxWoW.Me.HasAura("Unleash Wind") || !SpellManager.HasSpell("Unleash Elements")),
                                Spell.Cast("Frost Shock", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 6)
                                )
                            )
                        )
                    )
                );
        }

        #endregion

        #region Instance Rotation
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances)]
        public static Composite CreateShamanEnhancementInstancePullAndCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptBehavior(),
                        Dispelling.CreatePurgeEnemyBehavior("Purge"),

                        Common.CreateShamanDpsShieldBehavior(),

                        // Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),
                        Spell.BuffSelf("Feral Spirit", ret =>
                            !Unit.IsTrivial(Me.CurrentTarget)
                            && Me.CurrentTarget.SpellDistance() < 12
                            && NeedFeralSpirit 
                            ),

                        new Decorator(
                            req => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled()),
                            new PrioritySelector(
                                Spell.Buff("Flame Shock", true),
                                Spell.Cast("Lava Lash", req => StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock")),
                                new PrioritySelector(
                                    ctx => Unit.UnfriendlyUnitsNearTarget(10f).FirstOrDefault(p => p.HasMyAura("Flame Shock")),
                                    Spell.Cast("Unleash Element", req => ((WoWUnit)req) != null && !Spell.IsSpellOnCooldown("Fire Nova")),
                                    Spell.Buff("Fire Nova", on => (WoWUnit) on)
                                    ),
                                Spell.Cast("Chain Lightning", ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),
                                Spell.Cast("Stormstrike"),
                                Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                                Movement.CreateMoveToMeleeBehavior(true)
                                )
                            ),

                        Spell.Cast("Lightning Bolt", ret => !ShamanSettings.AvoidMaelstromDamage && StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),
                        Common.CastElementalBlast(),
                        Spell.Cast("Stormstrike"),
                        Spell.Buff(
                            "Flame Shock", 
                            9, 
                            on => Me.CurrentTarget, 
                            req => (StyxWoW.Me.HasAura("Unleash Flame") || !SpellManager.HasSpell("Unleash Elements"))
                            ),
                        Spell.Cast("Lava Lash",
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null &&
                                   StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon),
                        Spell.Buff("Flame Shock", 9, on => Me.CurrentTarget, req => StyxWoW.Me.HasAura("Unleash Flame") || !SpellManager.HasSpell("Unleash Elements")),
                        Spell.Cast("Unleash Elements"),
                        Spell.Cast("Frost Shock", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 6),

                        Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),

                        Spell.Cast("Lightning Bolt", ret => !ShamanSettings.AvoidMaelstromDamage && StyxWoW.Me.HasAura("Maelstrom Weapon", 1))

                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion


        public static Composite CreateInCombatGapCloser()
        {
            if (!ShamanSettings.UseGhostWolf)
                return new ActionAlwaysFail();

            if (SingularRoutine.CurrentWoWContext == WoWContext.Instances)
                return new ActionAlwaysFail();

            if (!SpellManager.HasSpell("Ghost Wolf"))
                return new ActionAlwaysFail();

            if (Me.Specialization != WoWSpec.ShamanEnhancement)
                return new ActionAlwaysFail();

            return new Decorator(
                req => Unit.ValidUnit(Me.CurrentTarget)
                    && !Me.CurrentTarget.IsWithinMeleeRange
                    && !Me.Mounted,
                new PrioritySelector(

                    // slow or root based on distance and cooldown
                    new Decorator(
                        req => !Me.CurrentTarget.IsSlowed()
                            && !Me.CurrentTarget.IsRooted()
                            && !Me.CurrentTarget.IsStunned(),
                        new PrioritySelector(

                            // project root totems if needed
                            new Decorator(
                                req => Common.HasTalent(ShamanTalents.TotemicProjection) && !Spell.IsSpellOnCooldown("Totemic Projection"),
                                new PrioritySelector(
                                    Spell.Cast(
                                        Totems.ToSpellId(WoWTotem.Earthgrab), 
                                        req => !Totems.Exist(WoWTotem.Earthbind, WoWTotem.Earthgrab)
                                        ),
                                    Spell.CastOnGround(
                                        "Totemic Projection", 
                                        on => Me.CurrentTarget, 
                                        req => Totems.Exist( WoWTotem.Earthbind, WoWTotem.Earthgrab)
                                            && !Totems.ExistInRange( Me.CurrentTarget.Location, WoWTotem.Earthbind, WoWTotem.Earthgrab)
                                        )
                                    )
                                ),

                            // quick single spell root
                            Spell.Cast("Frost Shock")

                            )
                        ),

                    // speed boost if needed
                    new Throttle(
                        2,
                        new Decorator(
                            ret => MovementManager.IsClassMovementAllowed
                                && !Me.HasAura("Ghost Wolf")
                                && Me.IsMoving // (DateTime.Now - GhostWolfRequest).TotalMilliseconds < 1000
                                && !Me.OnTaxi && !Me.InVehicle 
                                && !Utilities.EventHandlers.IsShapeshiftSuppressed,

                            Spell.BuffSelfAndWait("Ghost Wolf")
                            )
                        )
                    )
                );
        }


        #region Diagnostics

        private static Composite CreateEnhanceDiagnosticOutputBehavior()
        {
            return new ThrottlePasses(1,
                new Decorator(
                    ret => SingularSettings.Debug,
                    new Action(ret =>
                    {
                        uint maelStacks = 0;
                        WoWAura aura = Me.ActiveAuras.Where( a => a.Key == "Maelstrom Weapon").Select( d => d.Value ).FirstOrDefault();
                        if (aura != null)
                        {
                            maelStacks = aura.StackCount;
                            if (maelStacks == 0)
                                Logger.WriteDebug(Color.MediumVioletRed, "Inconsistancy Error:  Maelstrom Weapon buff exists with 0 stacks !!!!");
                            else if ( !Me.HasAura("Maelstrom Weapon", (int)maelStacks))
                                Logger.WriteDebug(Color.MediumVioletRed, "Inconsistancy Error:  Me.HasAura('Maelstrom Weapon', {0}) was False!!!!", maelStacks );
                        }

                        string line = string.Format(".... h={0:F1}%/m={1:F1}%, mov={2}, mael={3}",
                            Me.HealthPercent,
                            Me.ManaPercent,
                            Me.IsMoving.ToYN(),
                            maelStacks
                            );

                        WoWUnit target = Me.CurrentTarget;
                        if (target == null)
                            line += ", target=(null)";
                        else
                            line += string.Format(", target={0} @ {1:F1} yds, th={2:F1}%, tmelee={3}, tface={4}, tloss={5}, flame={6}, frost={7}", 
                                target.SafeName(), 
                                target.SpellDistance(), 
                                target.HealthPercent,
                                target.IsWithinMeleeRange.ToYN(), 
                                Me.IsSafelyFacing(target,180).ToYN(),
                                target.InLineOfSpellSight.ToYN(),
                                (long) target.GetAuraTimeLeft("Flame Shock").TotalMilliseconds,
                                (long) target.GetAuraTimeLeft("Frost Shock").TotalMilliseconds
                                );

                        Logger.WriteDebug(line);
                        return RunStatus.Failure;
                    }))
                );
        }

        #endregion
    }
}
