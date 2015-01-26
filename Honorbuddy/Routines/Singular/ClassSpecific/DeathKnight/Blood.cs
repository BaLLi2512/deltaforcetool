﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;

using Styx;

using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using System.Drawing;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Blood
    {
        private static DeathKnightSettings DeathKnightSettings { get { return SingularSettings.Instance.DeathKnight(); } }
        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        #region CombatBuffs

        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood)]
        public static Composite CreateDeathKnightBloodCombatBuffs()
        {
            return new Decorator(
                req => !Me.GotTarget() || !Me.CurrentTarget.IsTrivial(),
                new PrioritySelector(

                    // *** Defensive Cooldowns ***
                    // Anti-magic shell - no cost and doesnt trigger GCD 
                    Spell.BuffSelf("Anti-Magic Shell",
                        ret => Unit.NearbyUnfriendlyUnits.Any(u => (u.IsCasting || u.ChanneledCastingSpellId != 0) && u.CurrentTargetGuid == StyxWoW.Me.Guid)),

                    // we want to make sure our primary target is within melee range so we don't run outside of anti-magic zone.
                    Spell.CastOnGround("Anti-Magic Zone", 
                        loc => StyxWoW.Me,
                        ret => Common.HasTalent( DeathKnightTalents.AntiMagicZone) 
                            && !StyxWoW.Me.HasAura("Anti-Magic Shell") 
                            && Unit.NearbyUnfriendlyUnits.Any(u => (u.IsCasting || u.ChanneledCastingSpellId != 0) && u.CurrentTargetGuid == StyxWoW.Me.Guid) 
                            && Targeting.Instance.FirstUnit != null 
                            && Targeting.Instance.FirstUnit.IsWithinMeleeRange),

                    Spell.Cast("Dancing Rune Weapon",
                        ret => Unit.NearbyUnfriendlyUnits.Count() > 2),

                    Spell.BuffSelf("Bone Shield",
                        ret => !DeathKnightSettings.BoneShieldExclusive || !Me.HasAnyAura("Bone Shield", "Vampiric Blood", "Dancing Rune Weapon", "Lichborne", "Icebound Fortitude")),

                    Spell.BuffSelf("Vampiric Blood",
                        ret => Me.HealthPercent < DeathKnightSettings.VampiricBloodPercent
                            && (!DeathKnightSettings.VampiricBloodExclusive || !Me.HasAnyAura("Bone Shield", "Vampiric Blood", "Dancing Rune Weapon", "Lichborne", "Icebound Fortitude"))),

                    Spell.BuffSelf("Icebound Fortitude",
                        ret => StyxWoW.Me.HealthPercent < DeathKnightSettings.IceboundFortitudePercent
                            && (!DeathKnightSettings.IceboundFortitudeExclusive || !Me.HasAnyAura("Bone Shield", "Vampiric Blood", "Dancing Rune Weapon", "Lichborne", "Icebound Fortitude"))),

                    Spell.BuffSelf("Lichborne",ret => StyxWoW.Me.IsCrowdControlled()),

                    Spell.BuffSelf("Desecrated Ground", ret => Common.HasTalent( DeathKnightTalents.DesecratedGround) && StyxWoW.Me.IsCrowdControlled()),

                    // use army of the dead defensively
                    Spell.BuffSelf("Army of the Dead",
                        ret => DeathKnightSettings.UseArmyOfTheDead 
                            && SingularRoutine.CurrentWoWContext == WoWContext.Instances 
                            && StyxWoW.Me.HealthPercent < DeathKnightSettings.ArmyOfTheDeadPercent),

                    // I need to use Empower Rune Weapon to use Death Strike
                    Spell.BuffSelf("Empower Rune Weapon",
                        ret => StyxWoW.Me.HealthPercent < DeathKnightSettings.EmpowerRuneWeaponPercent
                            && Me.GotTarget()
                            && Me.CurrentTarget.IsWithinMeleeRange
                            && Me.IsSafelyFacing(Me.CurrentTarget)
                            && Me.CurrentTarget.InLineOfSpellSight
                            && !Spell.CanCastHack("Death Strike")
                        ),

                    Helpers.Common.CreateCombatRezBehavior("Raise Ally", on => ((WoWUnit)on).SpellDistance() < 40 && ((WoWUnit)on).InLineOfSpellSight),

                    // *** Offensive Cooldowns ***
                    Spell.BuffSelf("Death's Advance",
                        ret => Common.HasTalent( DeathKnightTalents.DeathsAdvance) 
                            && StyxWoW.Me.GotTarget() && !Spell.CanCastHack("Death Grip") 
                            && StyxWoW.Me.CurrentTarget.DistanceSqr > 10*10),

                    Spell.HandleOffGCD( Spell.BuffSelf("Blood Tap", ret => NeedBloodTap() ) ),

                    Spell.Cast("Plague Leech", ret => Common.CanCastPlagueLeech),

                    // Attack Power Buff
                    Spell.BuffSelf("Horn of Winter", ret => !Me.HasPartyBuff(PartyBuffType.AttackPower))
                    )
                );
        }

        #endregion

        #region Normal Rotation

        private readonly static WaitTimer DeathStrikeTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private static List<WoWUnit> _nearbyUnfriendlyUnits;

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Normal)]
        public static Composite CreateDeathKnightBloodNormalCombat()
        {
            return new PrioritySelector(

                Helpers.Common.EnsureReadyToAttackFromMelee(),

                Spell.WaitForCast(FaceDuring.Yes),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptBehavior(),

                        Common.CreateDeathKnightPullMore(),

                        Common.CreateDarkSuccorBehavior(),

                        Common.CreateDarkSimulacrumBehavior(),

                        Common.CreateSoulReaperHasteBuffBehavior(),

                        Spell.Buff("Chains of Ice",
                            ret => StyxWoW.Me.CurrentTarget.Fleeing && !StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),

                        Common.CreateDeathGripBehavior(),

                        // Start AoE section
                        new PrioritySelector(
                            ctx => _nearbyUnfriendlyUnits = Unit.UnfriendlyUnitsNearTarget(15f).ToList(),
                            new Decorator(
                                ret => Spell.UseAOE && _nearbyUnfriendlyUnits.Count() >= DeathKnightSettings.DeathAndDecayCount,
                                new PrioritySelector(
                                    Spell.CastOnGround("Death and Decay", ret => StyxWoW.Me.CurrentTarget, ret => true, false),

                                    // Spell.Cast("Gorefiend's Grasp", ret => Common.HasTalent( DeathKnightTalents.GorefiendsGrasp)),
                                    Spell.BuffSelf("Remorseless Winter", ret => Me.CurrentTarget.IsWithinMeleeRange && Common.HasTalent( DeathKnightTalents.RemorselessWinter)),

                                    // refresh diseases if possible
                                    new Throttle(2, Spell.Cast("Blood Boil", ret => UseBloodBoilForDiseases()) ),

                                    // Apply Diseases
                                    Common.CreateApplyDiseases(),

                                    // Active Mitigation (5 second rule does not apply)
                                    Spell.Cast("Death Strike"),

                                    // AoE Damage
                                    Spell.Cast("Blood Boil", ret => Spell.UseAOE && _nearbyUnfriendlyUnits.Count >= DeathKnightSettings.BloodBoilCount),
                                    Spell.Cast("Icy Touch", ret => !StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),

                                    new ActionAlwaysSucceed()
                                    )
                                )
                            ),

                        // refresh diseases if possible
                        new Throttle( 2, Spell.Cast("Blood Boil", ret => Spell.UseAOE && UseBloodBoilForDiseases()) ),

                        Common.CreateApplyDiseases(),

                        Spell.Cast("Death Coil", ret => StyxWoW.Me.CurrentRunicPower >= 80),
                        Spell.Cast("Death Coil", ret => !StyxWoW.Me.CurrentTarget.IsWithinMeleeRange),

                        // Active Mitigation
                        new Sequence(
                            Spell.Cast("Death Strike", ret => DeathStrikeTimer.IsFinished),
                            new Action(ret => DeathStrikeTimer.Reset())
                            ),


                        Spell.Cast("Blood Boil", ret => Spell.UseAOE && _nearbyUnfriendlyUnits.Count >= DeathKnightSettings.BloodBoilCount),
                        Spell.Cast("Soul Reaper", ret => StyxWoW.Me.CurrentTarget.HealthPercent < 35),
                        Spell.Cast("Icy Touch", ret => !StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),

                        // *** 3 Lowbie Cast what we have Priority
                        // ... not much to do here, just use our Unholy Runes on PS prior to learning DS
                        Spell.Cast("Plague Strike", ret => !SpellManager.HasSpell( "Death Strike"))
                        )
                    )
                );
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Battlegrounds)]
        public static Composite CreateDeathKnightBloodPvPCombat()
        {
            return new PrioritySelector(

                Helpers.Common.EnsureReadyToAttackFromMelee(),

                Spell.WaitForCast(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptBehavior(),
                        Common.CreateDeathGripBehavior(),
                        Spell.Buff("Chains of Ice",
                            ret => StyxWoW.Me.CurrentTarget.DistanceSqr > 10 * 10),

                        Common.CreateDarkSuccorBehavior(),

                        Common.CreateSoulReaperHasteBuffBehavior(),

                        Common.CreateDarkSimulacrumBehavior(),

                        // Start AoE section
                        Spell.CastOnGround("Death and Decay", ret => StyxWoW.Me.CurrentTarget, ret => true, false),
                        Spell.Cast("Remorseless Winter", ret => Common.HasTalent( DeathKnightTalents.RemorselessWinter)),

                        // renew/spread disease if possible
                        new Throttle( 2, Spell.Cast("Blood Boil", ret => Spell.UseAOE && UseBloodBoilForDiseases())),

                        // apply / refresh disease if needed 
                        Common.CreateApplyDiseases(),

                        // If we don't have RS yet, just resort to DC. Its not the greatest, but oh well. Make sure we keep enough RP banked for a self-heal if need be.
                        Spell.Cast("Soul Reaper", ret => StyxWoW.Me.CurrentTarget.HealthPercent < 35),
                        Spell.Cast("Death Coil", ret => StyxWoW.Me.CurrentRunicPower >= 80),
                        Spell.Cast("Death Strike"),
                        Spell.Cast("Icy Touch"),
                        Spell.Cast("Death Coil"),
                        Spell.Cast("Horn of Winter")
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Tanking - Instances and Raids

        // Blood DKs no longer pull with DG... now save cooldown for Taunt if possible
        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Instances)]
        public static Composite CreateDeathKnightBloodInstancePull()
        {
            return
                new PrioritySelector(

                    Helpers.Common.EnsureReadyToAttackFromMelee(),
                    Spell.WaitForCast(),

                    CreateDiagnosticOutputBehavior("Pull"),

                    new Decorator(
                        ret => !Spell.IsGlobalCooldown(),
                        new PrioritySelector(
                            Spell.BuffSelf("Horn of Winter"),
                            Spell.Cast("Outbreak"),
                            Spell.Cast("Icy Touch", ret => !StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),
                            Spell.Cast("Plague Strike"),
                            Spell.Cast("Death Coil")
                            )
                        ),

                    Movement.CreateMoveToMeleeBehavior(true)
                    );
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Instances)]
        public static Composite CreateDeathKnightBloodInstanceCombat()
        {
            TankManager.NeedTankTargeting = (SingularRoutine.CurrentWoWContext == WoWContext.Instances);

            return new PrioritySelector(

                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),

                        Helpers.Common.CreateInterruptBehavior(),

                        Common.CreateDeathKnightPullMore(),

                        // Taunts
                        //------------------------------------------------------------------------
                        new Decorator( 
                            ret => SingularSettings.Instance.EnableTaunting 
                                && TankManager.Instance.NeedToTaunt.Any()
                                && TankManager.Instance.NeedToTaunt.FirstOrDefault().InLineOfSpellSight,
                            new Throttle( TimeSpan.FromMilliseconds(1500),
                                new PrioritySelector(
                                    // Direct Taunt
                                    Spell.Cast("Dark Command",
                                        ctx => TankManager.Instance.NeedToTaunt.FirstOrDefault(),
                                        ret => true),

                                    new Decorator(
                                        ret => TankManager.Instance.NeedToTaunt.Any()   /*recheck just before referencing member*/
                                            && Me.SpellDistance(TankManager.Instance.NeedToTaunt.FirstOrDefault()) > 10,

                                        new PrioritySelector(
                                            // use DG if we have to (be sure to stop movement)
                                            Common.CreateDeathGripBehavior(),

                                            // CoI for the agro and to slow
                                            Spell.Cast("Chains of Ice",
                                                ctx => TankManager.Instance.NeedToTaunt.FirstOrDefault(),
                                                req => Me.IsSafelyFacing(TankManager.Instance.NeedToTaunt.FirstOrDefault())),

                                            // everything else on CD, so hit with a DC if possible
                                            Spell.Cast("Death Coil", 
                                                ctx => TankManager.Instance.NeedToTaunt.FirstOrDefault(), 
                                                req => Me.IsSafelyFacing(TankManager.Instance.NeedToTaunt.FirstOrDefault()))
                                            )
                                        )
                                    )
                                )
                                ),

                        Common.CreateDarkSuccorBehavior(),

                        // Start AoE section
                        //------------------------------------------------------------------------
                        new PrioritySelector(
                            ctx => _nearbyUnfriendlyUnits = Unit.UnfriendlyUnitsNearTarget(15f).ToList(),
                            new Decorator(
                                ret => Spell.UseAOE && _nearbyUnfriendlyUnits.Count() >= DeathKnightSettings.DeathAndDecayCount,
                                new PrioritySelector(

                                    Spell.Cast("Death Strike", req => Me.HealthPercent <= DeathKnightSettings.DeathStrikeEmergencyPercent),

                                    Spell.CastOnGround("Death and Decay", ret => StyxWoW.Me.CurrentTarget, ret => true, false),

                                    // Spell.Cast("Gorefiend's Grasp", ret => Common.HasTalent( DeathKnightTalents.GorefiendsGrasp)),
                                    Spell.Cast("Remorseless Winter", ret => Common.HasTalent(DeathKnightTalents.RemorselessWinter)),

                                    // Apply Diseases
                                    Common.CreateApplyDiseases(),

                                    // Spread Diseases
                                    new Throttle( 2,
                                        Spell.Cast("Blood Boil",
                                            ret => // WOD: Common.HasTalent(DeathKnightTalents.RollingBlood) &&
                                                !StyxWoW.Me.HasAura("Unholy Blight")
                                                && StyxWoW.Me.CurrentTarget.DistanceSqr <= 10 * 10
                                                && Common.ShouldSpreadDiseases)
                                        ),

                                    // Active Mitigation
                                    new Sequence(
                                        Spell.Cast("Death Strike", ret => DeathStrikeTimer.IsFinished),
                                        new Action(ret => DeathStrikeTimer.Reset())
                                        ),

                                    // AoE Damage
                                    Spell.Cast("Blood Boil", ret => Spell.UseAOE && _nearbyUnfriendlyUnits.Count >= DeathKnightSettings.BloodBoilCount),
                                    Spell.Cast("Death Coil"),
                                    Spell.Cast("Icy Touch", ret => !StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),

                                    new ActionAlwaysSucceed()
                                    )
                                )
                            ),

                        // refresh diseases if possible and needed
                        //------------------------------------------------------------------------
                        Spell.Cast(
                            "Blood Boil",
                            req => Spell.UseAOE
                                && StyxWoW.Me.CurrentTarget.DistanceSqr <= Common.BloodBoilRangeSqr
                                && Unit.NearbyUnfriendlyUnits.Any(u =>
                                {
                                    long frostTimeLeft = (long)u.GetAuraTimeLeft("Frost Fever").TotalMilliseconds;
                                    long bloodTimeLeft = (long)u.GetAuraTimeLeft("Blood Plauge").TotalMilliseconds;
                                    return frostTimeLeft > 500 && bloodTimeLeft > 500 && (frostTimeLeft < 3000 || bloodTimeLeft < 3000);
                                })
                            ),

                        // Taunts
                        //------------------------------------------------------------------------
                        Common.CreateApplyDiseases(),

                        CreateDeathKnightBloodInstanceSingleTargetCombat(),

                        // *** 3 Lowbie Cast what we have Priority
                        // ... not much to do here, just use our Frost and Unholy Runes on IT+PS prior to learning DS
                        Spell.Cast("Plague Strike", ret => !SpellManager.HasSpell("Death Strike")),
                        Spell.Cast("Icy Touch", ret => !SpellManager.HasSpell("Death Strike"))
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static Composite CreateDeathKnightBloodInstanceSingleTargetCombat()
        {
            return new PrioritySelector(
                // Runic Power Dump if approaching capp
                Spell.Cast("Death Coil", req => Me.CurrentRunicPower >= 80),

                // use Crimson Scourge
                new Decorator(
                    req => Me.HasAura("Crimson Scourge"),
                    new PrioritySelector(
                        Spell.CastOnGround( "Death and Decay", on => Me.CurrentTarget, req => !Me.CurrentTarget.IsMoving),
                        Spell.Cast( "Blood Boil" )
                        )
                    ),

                // Blood Tap behavior
                new Decorator(
                    req => Common.NeedBloodTap(),
                    new Sequence(
                        ctx => Common.DeathRuneSlotsActive,
                        Spell.HandleOffGCD(Spell.BuffSelf("Blood Tap", req => ((int)req) < 1) ),
                        Spell.HandleOffGCD(Spell.BuffSelf("Blood Tap", req => ((int)req) < 2) )
                        )
                    ),

                // use Frost, Unholy, and Death Runes
                Spell.Cast("Death Strike"),

                // use Blood Runes
                new Decorator(
                    req => Common.BloodRuneSlotsActive > (Common.HasTalent(DeathKnightTalents.RunicEmpowerment) ? 1 : 0),
                    new PrioritySelector(
                        Spell.Cast("Soul Reaper", req => Me.CurrentTarget.HealthPercent <= 35),
                        Spell.Cast("Blood Boil")
                        )
                    ),

                Spell.Cast("Death Coil"),
                Spell.Cast("Horn of Winter", on => Me),

                new Decorator(
                    req => Spell.UseAOE && Me.HasAura("Crimson Scourge"),
                    new PrioritySelector(
                        Spell.CastOnGround("Death and Decay", on => Me.CurrentTarget, req => !Me.CurrentTarget.IsMoving, waitForSpell: false),
                        Spell.Cast("Blood Boil", on => Me.CurrentTarget)
                        )
                    )
                );

        }

        private static bool UseBloodBoilForDiseases()
        {
            if ( !Spell.UseAOE)
                return false;

            // check if refresh needed
            if ( NeedsRefresh( Me.CurrentTarget))
                return true;

            // check if spread needed
            if ( !NeedsDisease(Me.CurrentTarget))
            {
                return Unit.NearbyUnfriendlyUnits.Any( 
                    u => u.Guid != Me.CurrentTargetGuid 
                        && Me.SpellDistance(u) < Common.BloodBoilRange && NeedsDiseaseOrRefresh(u)
                    );
            }

            return false;
        }


        private static bool NeedBloodTap()
        {
            return StyxWoW.Me.HasAura("Blood Charge", 5) && (Common.BloodRuneSlotsActive == 0 || Common.FrostRuneSlotsActive == 0 || Common.UnholyRuneSlotsActive == 0);
        }

        private static bool NeedsDisease(WoWUnit unit)
        {
            return !Me.CurrentTarget.HasAura("Frost Fever") || !Me.CurrentTarget.HasAura("Blood Plague"); 
        }

        private static bool NeedsRefresh( WoWUnit unit)
        {
            long frostTimeLeft = (long)unit.GetAuraTimeLeft("Frost Fever").TotalMilliseconds;
            long bloodTimeLeft = (long)unit.GetAuraTimeLeft("Blood Plauge").TotalMilliseconds;
            return frostTimeLeft > 500 && bloodTimeLeft > 500 && (frostTimeLeft < 3000 || bloodTimeLeft < 3000);
        }

        private static bool NeedsDiseaseOrRefresh( WoWUnit unit)
        {
            long frostTimeLeft = (long)unit.GetAuraTimeLeft("Frost Fever").TotalMilliseconds;
            long bloodTimeLeft = (long)unit.GetAuraTimeLeft("Blood Plauge").TotalMilliseconds;
            return (frostTimeLeft < 3000 || bloodTimeLeft < 3000);
        }

        #endregion

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.All, priority: 1)]
        public static Composite CreateDeathKnightBloodPullDiagnostic()
        {
            return CreateDiagnosticOutputBehavior("Pull");
        }

        [Behavior(BehaviorType.Heal, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.All, priority: 1)]
        public static Composite CreateDeathKnightBloodHealsDiagnostic()
        {
            return CreateDiagnosticOutputBehavior("Combat");
        }

        private static Composite CreateDiagnosticOutputBehavior(string context = null)
        {
            if (context == null)
                context = "...";
            else
                context = "<<" + context + ">>";

            if (!SingularSettings.Debug)
                return new ActionAlwaysFail();

            return new ThrottlePasses(1,
                new Action(ret =>
                {
                    string log;
                    log = string.Format(context + " h={0:F1}%/r={1:F1}%, Runes-BFUD={2}/{3}/{4}/{5} BloodChg={6} BoneShield={7} CrimScrg={8} aoe={9}",
                        Me.HealthPercent,
                        Me.RunicPowerPercent,
                        Common.BloodRuneSlotsActive, Common.FrostRuneSlotsActive, Common.UnholyRuneSlotsActive, Common.DeathRuneSlotsActive,                       
                        Spell.GetCharges("Blood Charge"),
                        (long) Me.GetAuraTimeLeft("Bone Shield").TotalMilliseconds,
                        (long) Me.GetAuraTimeLeft("Crimson Scourge").TotalMilliseconds,
                        Unit.UnfriendlyUnitsNearTarget(15f).Count()
                        );

                    WoWUnit target = Me.CurrentTarget;
                    if (target != null)
                    {
                        log += string.Format(" th={0:F1}% dist={1:F1} inmelee={2} face={3} loss={4} dead={5}s ffvr={6} bplg={7}",
                            target.HealthPercent,
                            target.Distance,
                            target.IsWithinMeleeRange.ToYN(),
                            Me.IsSafelyFacing(target).ToYN(),
                            target.InLineOfSpellSight.ToYN(),
                            target.TimeToDeath(),
                            
                            (long) target.GetAuraTimeLeft("Frost Fever").TotalMilliseconds,
                            (long) target.GetAuraTimeLeft("Blood Plague").TotalMilliseconds
                            );
                    }

                    Logger.WriteDebug(Color.AntiqueWhite, log);
                    return RunStatus.Failure;
                })
                );
        }
    }
}
