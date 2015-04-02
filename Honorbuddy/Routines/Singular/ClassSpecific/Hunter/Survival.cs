﻿using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Styx.WoWInternals.WoWObjects;
using System.Drawing;
using Styx.WoWInternals;
using CommonBehaviors.Actions;
using System.Collections.Generic;

namespace Singular.ClassSpecific.Hunter
{
    public class Survival
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static WoWUnit Pet { get { return StyxWoW.Me.Pet; } }
        private static HunterSettings HunterSettings { get { return SingularSettings.Instance.Hunter(); } }

        #region Normal Rotation
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Hunter, WoWSpec.HunterSurvival, WoWContext.Normal)]
        public static Composite CreateHunterSurvivalNormalPullAndCombat()
        {
            return new PrioritySelector(

                Common.CreateHunterEnsureReadyToAttackFromLongRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator(

                    ret => !Spell.IsGlobalCooldown(),

                    new PrioritySelector(

                        // updated time to death tracking values before we need them
                        new Action( ret => { Me.CurrentTarget.TimeToDeath(); return RunStatus.Failure; } ),

                        CreateSurvivalDiagnosticOutputBehavior(),

                        Common.CreateMisdirectionBehavior(),

                        Common.CreateHunterAvoidanceBehavior(null, null),


                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Helpers.Common.CreateInterruptBehavior(),

                        Common.CreateHunterNormalCrowdControl(),

                        Spell.Cast("Tranquilizing Shot", req => Me.GetAllAuras().Any(a => a.Spell.DispelType == WoWDispelType.Enrage)),

                        Spell.Buff("Concussive Shot",
                            ret => Me.CurrentTarget.CurrentTargetGuid == Me.Guid
                                && Me.CurrentTarget.Distance > Spell.MeleeRange),

                        // Defensive Stuff
                        Spell.Cast("Intimidation",
                            ret => Me.GotTarget()
                                && Me.CurrentTarget.IsAlive
                                && Me.GotAlivePet
                                && (!Me.CurrentTarget.GotTarget() || Me.CurrentTarget.CurrentTarget == Me)),

                        // AoE Rotation
                        new Decorator(
                            ret => Spell.UseAOE && !(Me.CurrentTarget.IsBoss() || Me.CurrentTarget.IsPlayer) && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 3,
                            new PrioritySelector(
                                ctx => Unit.NearbyUnitsInCombatWithUsOrOurStuff.Where(u => u.InLineOfSpellSight).OrderByDescending(u => (uint)u.HealthPercent).FirstOrDefault(),
                                Common.CreateHunterTrapBehavior("Explosive Trap", true, on => Me.CurrentTarget, req => Spell.UseAOE),
                                Spell.Cast("Multi-Shot", req => Me.CurrentFocus > 70),
                                Spell.Cast("Black Arrow", on => (WoWUnit) on),
                                Spell.Cast("Explosive Shot", on => (WoWUnit) on, req => Me.HasAura("Lock and Load") && Spell.UseAOE),
                                Spell.Cast("Arcane Shot", on => (WoWUnit) on, ret => Me.CurrentFocus > 70 || !Me.CurrentTarget.HasMyAura("Serpent Sting")),
                                Spell.Cast("Cobra Shot", on => (WoWUnit) on),
                                Common.CastSteadyShot(on => (WoWUnit) on, ret => !SpellManager.HasSpell("Cobra Shot"))
                                )
                            ),

                        // Single Target Rotation
                        Spell.Cast("Black Arrow", ret => Me.CurrentTarget.TimeToDeath() > 12),
                        Spell.Cast("Explosive Shot", req => Spell.UseAOE),
						Spell.Cast("Arcane Shot", ret => Me.CurrentFocus > 70 || !Me.CurrentTarget.HasMyAura("Serpent Sting")),
						Spell.Cast("Cobra Shot", ret => Me.CurrentFocus < 70),
						Spell.Cast("Focusing Shot", ret => Me.CurrentFocus < 50),

                        Common.CastSteadyShot( on => Me.CurrentTarget, ret => !SpellManager.HasSpell("Cobra Shot") && !SpellManager.HasSpell("Focusing Shot"))
                        )
                    ),

                Movement.CreateMoveToUnitBehavior( on => StyxWoW.Me.CurrentTarget, 35f, 30f)
                );
        }

        #endregion

		#region Instance Rotation

        // place holder for Raph
	    public static Composite CreateHunterSurvivalInstancesPull()
	    {
		    return new PrioritySelector();
	    }

        [Behavior(BehaviorType.Pull, WoWClass.Hunter, WoWSpec.HunterSurvival, WoWContext.Instances)]
        [Behavior(BehaviorType.Combat, WoWClass.Hunter, WoWSpec.HunterSurvival, WoWContext.Instances)]
		public static Composite CreateHunterSurvivalInstancesCombat()
		{
			return new PrioritySelector(

				Common.CreateHunterEnsureReadyToAttackFromLongRange(),

				Spell.WaitForCastOrChannel(),

				// updated time to death tracking values before we need them
				new Action(ret => { Me.CurrentTarget.TimeToDeath(); return RunStatus.Failure; }),

				CreateSurvivalDiagnosticOutputBehavior(),

				Helpers.Common.CreateInterruptBehavior(),

				// We don't really want to do any of those below in raids
				new Decorator(ret => !Me.CurrentMap.IsRaid,
					new PrioritySelector(

						Common.CreateMisdirectionBehavior(),

						Common.CreateHunterAvoidanceBehavior(null, null),

						Common.CreateHunterNormalCrowdControl(),

						Spell.Cast("Tranquilizing Shot", req => Me.GetAllAuras().Any(a => a.Spell.DispelType == WoWDispelType.Enrage)),

						Spell.Buff("Concussive Shot",
							ret => Me.CurrentTarget.CurrentTargetGuid == Me.Guid
								&& Me.CurrentTarget.Distance > Spell.MeleeRange),

						// Defensive Stuff
						Spell.Cast("Intimidation",
							ret => Me.GotTarget()
								&& Me.CurrentTarget.IsAlive
								&& Me.GotAlivePet
								&& (!Me.CurrentTarget.GotTarget() || Me.CurrentTarget.CurrentTarget == Me))						
						)
					),

				Spell.Cast("A Murder of Crows"),
				Spell.Cast("Black Arrow"),
				Spell.Cast("Arcane Shot", ret => Me.HasAura("Thrill of the Hunt")),
				Spell.Cast("Explosive Shot", req => Spell.UseAOE),
				Spell.Cast("Dire Beast"),

				// Spell.Cast("Barrage", ret => Clusters.GetConeCluster(Me.Location, 60f, 40f, Unit.NearbyUnitsInCombatWithUsOrOurStuff).Count() >= 3),
                Spell.Cast(
                    "Barrage",
                    on =>
                    {
                        if (!Spell.UseAOE)
                            return null;

                        // Does not require CurrentTarget to be non=null
                        IEnumerable<WoWUnit> ienum = Clusters.GetConeCluster(60f, 42f, Unit.UnfriendlyUnits(50));
                        int cntCC = 0;
                        int cntTarget = 0;
                        int cntNeutral = 0;
                        WoWUnit target = null;

                        foreach (WoWUnit u in ienum)
                        {
                            cntTarget++;
                            if (u.IsCrowdControlled())
                                cntCC++;
                            if (!u.Combat && !u.IsTrivial() && !u.Aggro && !u.PetAggro && !(u.IsTargetingMeOrPet || u.IsTargetingMyRaidMember))
                                cntNeutral++;
                            if (target == null)
                                target = u;
                            if (Me.CurrentTargetGuid == u.Guid)
                                target = u;
                        }

                        if (cntNeutral > 0)
                        {
                            Logger.WriteDebug("Barrage: skipping, {0} additional targets would be pulled", cntNeutral);
                            return null;
                        }

                        if (cntCC > 0)
                        {
                            Logger.WriteDebug("Barrage: skipping, {0} crowd controlled targets", cntCC);
                            return null;
                        }

                        if (cntTarget == 0)
                        {
                            Logger.WriteDebug("Barrage: skipping, no targets would be hit");
                            return null;
                        }

                        return target;
                    }
                    ),

				Spell.Cast("Multi-Shot", ret => (Me.CurrentFocus > 70 || Me.CurrentTarget.HasKnownAuraExpired("Serpent Sting", 4)) && Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 2),
				Spell.Cast("Arcane Shot", ret => Me.CurrentFocus > 70 || Me.CurrentTarget.HasKnownAuraExpired("Serpent Sting", 4)),
				Common.CreateHunterTrapBehavior("Explosive Trap", true, on => Me.CurrentTarget, req => Spell.UseAOE ),
				Spell.Cast("Cobra Shot", ret => Me.CurrentFocus < 70),
				Spell.Cast("Focusing Shot", ret => Me.CurrentFocus < 50),
				
                Common.CastSteadyShot( on => Me.CurrentTarget, ret => !SpellManager.HasSpell("Cobra Shot") && !SpellManager.HasSpell("Focusing Shot")),
				Movement.CreateMoveToUnitBehavior(on => StyxWoW.Me.CurrentTarget, 35f, 30f)
				);
		}

		#endregion

		#region Battleground Rotation

		[Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Hunter, WoWSpec.HunterSurvival, WoWContext.Battlegrounds)]
        public static Composite CreateHunterSurvivalPvPPullAndCombat()
        {
            return new PrioritySelector(

                Common.CreateHunterEnsureReadyToAttackFromLongRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator(

                    ret => !Spell.IsGlobalCooldown(),

                    new PrioritySelector(

                        // updated time to death tracking values before we need them
                        new Action(ret => { Me.CurrentTarget.TimeToDeath(); return RunStatus.Failure; }),

                        CreateSurvivalDiagnosticOutputBehavior(),

                        Common.CreateHunterAvoidanceBehavior(null, null),

                        Helpers.Common.CreateInterruptBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Common.CreateHunterPvpCrowdControl(),

                        Spell.Cast("Tranquilizing Shot", req => Me.GetAllAuras().Any(a => a.Spell.DispelType == WoWDispelType.Enrage)),

                        Spell.Buff("Concussive Shot",
                            ret => Me.CurrentTarget.CurrentTargetGuid == Me.Guid
                                && Me.CurrentTarget.Distance > Spell.MeleeRange),

                        // Defensive Stuff
                        Spell.Cast("Intimidation",
                            ret => Me.GotTarget()
                                && Me.CurrentTarget.IsAlive
                                && Me.GotAlivePet
                                && (!Me.CurrentTarget.GotTarget() || Me.CurrentTarget.CurrentTarget == Me)),

                        // Single Target Rotation
                        Spell.Cast("Explosive Shot", req => Spell.UseAOE),
                        Spell.Cast("Black Arrow", ret => Me.CurrentTarget.TimeToDeath() > 12),
                        Spell.Cast("Arcane Shot", ret => Me.CurrentFocus > 60 || Me.HasAura("Thrill of the Hunt")),
                        Spell.Cast("Cobra Shot"),
                        Common.CastSteadyShot(on => Me.CurrentTarget, ret => !SpellManager.HasSpell("Cobra Shot"))
                        )
                    )
                );
        }

        #endregion

        private static Composite CreateSurvivalDiagnosticOutputBehavior()
        {
            if (!SingularSettings.Debug)
                return new ActionAlwaysFail();

            return new ThrottlePasses( 1,
                new Action(ret =>
                {
                    string sMsg;
                    sMsg = string.Format(".... h={0:F1}%, focus={1:F1}, moving={2}",
                        Me.HealthPercent,
                        Me.CurrentFocus,
                        Me.IsMoving
                        );

                    if ( !Me.GotAlivePet)
                        sMsg += ", no pet";
                    else
                        sMsg += string.Format( ", peth={0:F1}%", Me.Pet.HealthPercent);

                    WoWUnit target = Me.CurrentTarget;
                    if (target != null)
                    {
                        sMsg += string.Format(
                            ", {0}, {1:F1}%, {2:F1} yds, loss={3}",
                            target.SafeName(),
                            target.HealthPercent,
                            target.Distance,
                            target.InLineOfSpellSight
                            );
                    }

                    Logger.WriteDebug(Color.LightYellow, sMsg);
                    return RunStatus.Failure;
                })
                );
        }

    }
}
