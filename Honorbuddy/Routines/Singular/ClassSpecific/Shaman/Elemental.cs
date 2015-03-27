﻿using System.Linq;

using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
using Rest = Singular.Helpers.Rest;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Settings;
using Singular.Managers;
using Styx.Common;
using System.Drawing;
using System;
using Styx.Helpers;
using CommonBehaviors.Actions;


namespace Singular.ClassSpecific.Shaman
{
    public class Elemental
    {
        #region Common

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static ShamanSettings ShamanSettings { get { return SingularSettings.Instance.Shaman(); } }

        // private static int NormalPullDistance { get { return Math.Max( 35, CharacterSettings.Instance.PullDistance); } }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal|WoWContext.Instances)]
        public static Composite CreateElementalPreCombatBuffsNormal()
        {
            return new PrioritySelector(
                Common.CreateShamanDpsShieldBehavior(),
                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Battlegrounds )]
        public static Composite CreateElementalPreCombatBuffsPvp()
        {
            return new PrioritySelector(
                Common.CreateShamanDpsShieldBehavior(),
                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.Rest, WoWClass.Shaman, WoWSpec.ShamanElemental)]
        public static Composite CreateElementalRest()
        {
            return new PrioritySelector(
                Spell.WaitForCast(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Common.CreateShamanDpsHealBehavior(),

                        Rest.CreateDefaultRestBehaviour("Healing Surge", "Ancestral Spirit"),

                        Common.CreateShamanMovementBuff()
                        )
                    )
                );
        }

        /// <summary>
        /// perform diagnostic output logging at highest priority behavior that occurs while in Combat
        /// </summary>
        /// <returns></returns>
        [Behavior(BehaviorType.Heal | BehaviorType.Pull, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.All, 999)]
        public static Composite CreateElementalLogDiagnostic()
        {
            return CreateElementalDiagnosticOutputBehavior();
        }


        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanElemental)]
        public static Composite CreateElementalHeal()
        {
            return Common.CreateShamanDpsHealBehavior( );
        }

        #endregion

        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal)]
        public static Composite CreateElementalNormalPull()
        {
            return new PrioritySelector(

                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Common.CreateShamanDpsShieldBehavior(),

                        Totems.CreateTotemsBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        // grinding or questing, if target meets these cast Flame Shock if possible
                        // 1. mob is less than 12 yds, so no benefit from delay in Lightning Bolt missile arrival
                        // 2. area has another player competing for mobs (we want to tag the mob quickly)
                        new Decorator(
                            ret =>{
                                if (StyxWoW.Me.CurrentTarget.IsHostile && StyxWoW.Me.CurrentTarget.Distance < 12)
                                {
                                    Logger.WriteDiagnostic("NormalPull: fast pull since hostile target is {0:F1} yds away", StyxWoW.Me.CurrentTarget.Distance);
                                    return true;
                                }
                                WoWPlayer nearby = ObjectManager.GetObjectsOfType<WoWPlayer>(true, false).FirstOrDefault(p => !p.IsMe && p.DistanceSqr <= 40 * 40);
                                if (nearby != null)
                                {
                                    Logger.WriteDiagnostic("NormalPull: fast pull since player {0} nearby @ {1:F1} yds", nearby.SafeName(), nearby.Distance);
                                    return true;
                                }
                                return false;
                                },
                            new PrioritySelector(
                                // have a big attack loaded up, so don't waste it
                                Spell.Cast("Earth Shock", ret => StyxWoW.Me.HasAura("Lightning Shield", 5)),
                                Spell.Buff("Flame Shock", true, req => SpellManager.HasSpell("Lava Burst")),
                                Spell.Cast("Earth Shock", ret => !SpellManager.HasSpell("Flame Shock"))
                                )
                            ),

                        // have a big attack loaded up, so don't waste it
                        Spell.Cast("Earth Shock", ret => StyxWoW.Me.HasAura("Lightning Shield", 5)),

                        // otherwise, start with Lightning Bolt so we can follow with an instant
                        // to maximize damage at initial aggro
                        Spell.Cast("Lightning Bolt"),

                        // we are moving so throw an instant of some type
                        Spell.Buff("Flame Shock", true, req => SpellManager.HasSpell("Lava Burst")),
                        Spell.Buff("Lava Burst", true, req => Me.GotTarget() && Me.CurrentTarget.HasMyAura("Flame Shock")),
                        Spell.Cast("Earth Shock")
                        )
                    )

                // Movement.CreateMoveToUnitBehavior( on => StyxWoW.Me.CurrentTarget, 38f, 33f)
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal)]
        public static Composite CreateElementalNormalCombat()
        {
            return new PrioritySelector(

                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptBehavior(),

                        Totems.CreateTotemsBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Dispelling.CreatePurgeEnemyBehavior("Purge"),

                        Common.CreateShamanDpsShieldBehavior(),

                        Spell.BuffSelf("Thunderstorm", ret => Unit.NearbyUnfriendlyUnits.Count( u => u.Distance < 10f ) >= 3),

                        new Decorator(
                            ret => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled()),
                            new PrioritySelector(
                                new Action( act => { Logger.WriteDebug("performing aoe behavior"); return RunStatus.Failure; }),

                                new Decorator(
                                    req => SpellManager.HasSpell("Enhanced Chain Lightning")
                                        && Me.GetAuraTimeLeft("Enhanced Chain Lightning") == TimeSpan.Zero
                                        && Unit.UnfriendlyUnitsNearTarget(12f).Count() >= 3,                                        
                                    new Throttle(
                                        3,
                                        new Sequence(
                                            ctx => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(20f), ClusterType.Chained, 12),
                                            Spell.Cast(
                                                "Chain Lightning", 
                                                on => (WoWUnit) on,
                                                req => 3 <= Clusters.GetChainedClusterCount((WoWUnit) req, Unit.UnfriendlyUnitsNearTarget(20f), 12)                                                    ,
                                                cancel => false
                                                ),
                                            new WaitContinue(TimeSpan.FromSeconds(0.5), until => Me.GetAuraTimeLeft("Enhanced Chain Lightning") > TimeSpan.Zero, new ActionAlwaysSucceed())
                                            )
                                        )
                                    ),

                                new Sequence(
                                    Spell.CastOnGround("Earthquake", 
                                        on => StyxWoW.Me.CurrentTarget,
                                        req => StyxWoW.Me.GotTarget() 
                                            && StyxWoW.Me.CurrentTarget.Distance < 34
                                            && (StyxWoW.Me.ManaPercent > 50 || Me.GetAuraTimeLeft( "Lucidity") > TimeSpan.Zero) 
                                            && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 6),
                                    new Wait( TimeSpan.FromMilliseconds(500), until => Me.CurrentTarget.HasMyAura("Earthquake"), new ActionAlwaysSucceed())
                                    ),

                                Spell.Cast("Chain Lightning", ret => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(20f), ClusterType.Chained, 12))
                                )
                            ),

                        Common.CastElementalBlast(),

                        Spell.Buff("Flame Shock", true, req => SpellManager.HasSpell("Lava Burst") || Me.CurrentTarget.TimeToDeath(-1) > 30),

                        Spell.Cast("Lava Burst", on => Me.CurrentTarget, req => true, cancel => false),
                        Spell.Cast("Earth Shock",
                            ret => StyxWoW.Me.HasAura("Lightning Shield", 5) &&
                                   StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3),
                        Spell.Cast("Earth Shock", req => !SpellManager.HasSpell("Lava Burst")),

                        Spell.Cast("Chain Lightning", ret => Spell.UseAOE && Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled())),
                        Spell.Cast("Lightning Bolt")
                        )
                    )

                // Movement.CreateMoveToUnitBehavior( on => StyxWoW.Me.CurrentTarget, 38f, 33f)
                // Movement.CreateMoveToRangeAndStopBehavior(ret => Me.CurrentTarget, ret => NormalPullDistance)
                );
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Battlegrounds)]
        public static Composite CreateElementalPvPPullAndCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptBehavior(),

                        Totems.CreateTotemsPvPBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Dispelling.CreatePurgeEnemyBehavior("Purge"),

                        Common.CreateShamanDpsShieldBehavior(),

                        // Burst if 7 Stacks
                        new Decorator(
                            ret => Me.GotTarget() && Me.CurrentTarget.SpellDistance() < 40 && Me.HasAura("Lightning Shield", 7) && Spell.GetSpellCooldown("Earth Shock") == TimeSpan.Zero,
                            new PrioritySelector(
                                new Action( r => { Logger.Write( LogColor.Hilite, "Burst Rotation"); return RunStatus.Failure;} ),
                                Common.CastElementalBlast(),
                                Spell.Cast( "Lava Burst"),
                                Spell.BuffSelf("Ascendance", req => ShamanSettings.UseAscendance),       // this is more to buff following sequence since we leave burst after Earth Shock
                                Spell.Cast( "Earth Shock")
                                // Spell.Cast( "Lightning Bolt")       // filler in case Shocks on cooldown
                                )
                            ),

                        // If targeted, cast as many instants as possible
                        new Decorator(
                            ret => !Unit.NearbyUnfriendlyUnits.Any( u => u.CurrentTargetGuid == Me.Guid ),
                            new PrioritySelector(
                                new Decorator(
                                    ret => !Me.HasAura("Lightning Shield",  7),
                                    new PrioritySelector(
                                        Spell.Buff("Flame Shock", 9, on => Me.CurrentTarget, req => true),
                                        Spell.Buff(
                                            "Flame Shock", 
                                            0, 
                                            on => Unit.NearbyUnfriendlyUnits
                                                .Where(u => !u.HasMyAura("Flame Shock") && !u.IsCrowdControlled() && Me.IsSafelyFacing(u) && u.InLineOfSpellSight)
                                                .OrderByDescending( u => (int) u.HealthPercent )
                                                .FirstOrDefault(), 
                                            req => Spell.GetSpellCastTime("Lava Burst") != TimeSpan.Zero
                                            )
                                        )
                                    ),
                                Spell.Cast("Lava Burst", ret => Spell.GetSpellCastTime("Lava Burst") == TimeSpan.Zero),
                                Spell.Cast("Lava Beam"),
                                Spell.BuffSelf("Searing Totem", ret => Me.GotTarget() && Me.CurrentTarget.Distance < Totems.GetTotemRange(WoWTotem.Searing) && !Totems.Exist( WoWTotemType.Fire)),
                                Spell.BuffSelf("Thunderstorm", ret => Unit.NearbyUnfriendlyUnits.Any( u => u.IsWithinMeleeRange )),
                                Spell.Cast("Primal Strike") // might as well
                                )
                            ),

                        // Else cast freely

                        Common.CastElementalBlast( on => Me.CurrentTarget, req => !Me.HasAura("Lightning Shield", 5)),
                        Spell.Buff("Flame Shock", 9, on => Me.CurrentTarget, req => true),
                        Spell.Buff("Flame Shock", on => Unit.NearbyUnfriendlyUnits.FirstOrDefault(u => Me.IsSafelyFacing(u) && u.InLineOfSpellSight), req => Spell.GetSpellCastTime("Lava Burst") != TimeSpan.Zero),
                        Spell.Cast("Lava Burst"),
                        Spell.BuffSelf("Searing Totem", ret => Me.GotTarget() && Me.CurrentTarget.Distance < Totems.GetTotemRange(WoWTotem.Searing) && !Totems.Exist(WoWTotemType.Fire)),
                        Spell.Cast("Lightning Bolt")
                        )
                    )
                );
        }

        #endregion

        #region Instance Rotation

        // private static bool _doWeWantAcendance;

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Instances)]
        public static Composite CreateElementalInstancePullAndCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),

                Movement.CreateEnsureMovementStoppedBehavior(33f),

                Spell.WaitForCastOrChannel(),

                new PrioritySelector(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptBehavior(),

                        Totems.CreateTotemsInstanceBehavior(),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Dispelling.CreatePurgeEnemyBehavior("Purge"),

                        Common.CreateShamanDpsShieldBehavior(),

                        new Decorator(
                            ret => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled()),
                            new PrioritySelector(
                                new Action(act => { Logger.WriteDebug("performing aoe behavior"); return RunStatus.Failure; }),
                                new Sequence(
                                    Spell.CastOnGround(
                                        "Earthquake", 
                                        on => Me.CurrentTarget,
                                        req => Me.CurrentTarget.SpellDistance() < 35
                                        ),
                                    new Wait(TimeSpan.FromMilliseconds(500), until => Me.CurrentTarget.HasMyAura("Earthquake"), new ActionAlwaysSucceed())
                                    ),
                                Spell.Cast(
                                    "Earth Shock", 
                                    on => Unit.NearbyUnitsInCombatWithUsOrOurStuff
                                        .Where(u => Me.IsSafelyFacing(u) && u.InLineOfSpellSight)
                                        .OrderByDescending(u => (int) u.HealthPercent )
                                        .FirstOrDefault()
                                    ),
                                new Decorator(
                                    req => TalentManager.HasGlyph("Thunderstorm"),
                                    new PrioritySelector(                                   
                                        ctx => Clusters.GetBestUnitForCluster(Unit.NearbyUnitsInCombatWithUsOrOurStuff, ClusterType.Cone, 10),
                                        Spell.Cast(
                                            "Thunderstorm",
                                            on => (WoWUnit) on,
                                            req => 8 <= Clusters.GetClusterCount( (WoWUnit)req, Unit.NearbyUnitsInCombatWithUsOrOurStuff, ClusterType.Cone, 10)
                                            )
                                        )
                                    ),
                                Spell.Cast("Chain Lightning", ret => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(15f), ClusterType.Chained, 12))
                                )
                            ),

                        Spell.Buff("Flame Shock", 3, on => Me.CurrentTarget, req => true),

                        Spell.HandleOffGCD(Spell.Cast("Ascendance", req => ShamanSettings.UseAscendance && Me.CurrentTarget.IsBoss() && Me.CurrentTarget.SpellDistance() < 40 && !Me.IsMoving)),

                        Spell.Cast("Lava Burst", on => Me.CurrentTarget, req => true, cancel => false),

                        Spell.Cast(
                            "Earth Shock",
                            ret => Me.HasAura("Lightning Shield", 12) 
                                && Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3),

                        Common.CastElementalBlast(),

                        Spell.Cast("Chain Lightning", ret => Spell.UseAOE && Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2 && !Unit.UnfriendlyUnitsNearTarget(10f).Any(u => u.IsCrowdControlled())),
                        Spell.Cast("Lightning Bolt")
                        )
                    )
                );
        }

        #endregion

        #region Diagnostics

        private static Composite CreateElementalDiagnosticOutputBehavior()
        {
            if (!SingularSettings.Debug)
                return new ActionAlwaysFail();

            return new ThrottlePasses(1, 1,
                new Action(ret =>
                {
                    uint stks = 0;
                    string shield;

                    WoWAura aura = Me.GetAuraByName("Lightning Shield");
                    if (aura != null)
                    {
                        stks = aura.StackCount;
                        if (!Me.HasAura("Lightning Shield", (int)stks))
                            Logger.WriteDebug(Color.MediumVioletRed, "Inconsistancy Error:  have {0} stacks but Me.HasAura('Lightning Shield', {0}) was False!!!!", stks, stks);
                    }
                    else
                    {
                        aura = Me.GetAuraByName("Water Shield");
                        if (aura == null ) 
                        {
                            aura = Me.GetAuraByName("Earth Shield");
                            if (aura != null)
                                stks = aura.StackCount;
                        }
                    }

                    shield = aura == null ? "(null)" : aura.Name;
                        
                    string line = string.Format(".... [{0}] h={1:F1}%/m={2:F1}%, shield={3}, stks={4}, moving={5}",
                        CompositeBuilder.CurrentBehaviorType.ToString(),
                        Me.HealthPercent,
                        Me.ManaPercent,
                        shield,
                        stks,
                        Me.IsMoving.ToYN()
                        );

                    WoWUnit target = Me.CurrentTarget;
                    if (target == null)
                        line += ", target=(null)";
                    else
                        line += string.Format(", target={0} @ {1:F1} yds, th={2:F1}%, face={3} loss={4}, fs={5}",
                            target.SafeName(),
                            target.Distance,
                            target.HealthPercent,
                            Me.IsSafelyFacing(target).ToYN(),
                            target.InLineOfSpellSight.ToYN(),
                            (long)target.GetAuraTimeLeft("Flame Shock").TotalMilliseconds
                            );

                    
                    if (Totems.Exist(WoWTotemType.Fire))
                        line += ", fire=" + Totems.GetTotem(WoWTotemType.Fire).Name;

                    if (Totems.Exist(WoWTotemType.Earth))
                        line += ", earth=" + Totems.GetTotem(WoWTotemType.Earth).Name;

                    if (Totems.Exist(WoWTotemType.Water))
                        line += ", water=" + Totems.GetTotem(WoWTotemType.Water).Name;

                    if (Totems.Exist(WoWTotemType.Air  ))
                        line += ", air=" + Totems.GetTotem(WoWTotemType.Air).Name;

                    Logger.WriteDebug(Color.Yellow, line);
                    return RunStatus.Failure;
                })
                );
        }

        #endregion
    }
}
