﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Enums;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.Patchables;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals.World;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Dungeon_Scripts.Mists_of_Pandaria
{
	public class ShadoPanMonastery : Dungeon
	{
		#region Overrides of Dungeon

		private readonly WoWPoint _azureSerpentFollowerLoc = new WoWPoint(3736.51, 2670.499, 768.0417);
		private readonly WoWPoint _azureSerpentTankLoc = new WoWPoint(3727.666, 2688.185, 768.0416);
		private readonly WoWPoint _exitDoorAt3rdBossLoc = new WoWPoint(3947.273, 2893.141, 772.5763);

		public override uint DungeonId
		{
			get { return 466; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(3643.886, 2544.806, 769.9496); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(3649.855, 2548.755, 766.9684); }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret.ToUnit();
					if (unit != null)
					{
					    if (unit.Entry == MobId_TrainingTarget)
					        return true;

						// remove targets that being kited by group members.
						if (unit.Combat && unit.IsTargetingMyPartyMember)
						{
							var targetOfUnit = unit.CurrentTarget;
							var unitLoc = unit.Location;
							if (targetOfUnit != null && targetOfUnit.Location.DistanceSqr(unitLoc) > 35 * 35)
							{
								return true;
							}
						}

						if (unit.Entry == GuCloudstrikeId && unit.HasAura("Charging Soul"))
							return true;
						if (unit.Entry == ShadoPanNoviceId && unit.HasAura("Parry Stance") && unit.IsFacing(Me))
							return true;
						if (unit.Entry == MasterSnowdriftId && !Me.IsHealer())
						{
							if (unit.CastingSpellId == ParryStanceId || unit.HasAura(ParryStanceId))
								return true;
							if ((unit.HasAura(PursuitId) && unit.CurrentTargetGuid == Me.Guid || unit.CastingSpellId == TornadoKickId) && Me.IsMelee())
								return true;
						}
						if (unit.Entry == LesserVolatileEnergyId &&
							ScriptHelpers.GroupMembers.Where(m => m.IsRange && m.IsDps).OrderByDescending(r => r.MaxHealth).Select(r => r.Guid).FirstOrDefault() != Me.Guid)
							return true;

						if (unit.Entry == FlyingSnowId && unit.HasAura("Whirling Steel") && Me.IsDps() && Me.IsMelee())
							return true;

						if (unit.Entry == AzureSerpentId && unit.HasAura("Lightning Shielded") && !Me.IsHealer())
							return true;

						if (unit.Entry == GrippingHatred && StyxWoW.Me.IsTank())
							return true;
					}
					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			var tank = ScriptHelpers.Tank;
			foreach (var obj in incomingunits)
			{
				var unit = obj as WoWUnit;
				if (unit != null)
				{
					if (unit.Entry == MasterSnowCopyId)
						outgoingunits.Add(unit);
					// kill any hatefull essences that are withing range of a slain shado Pan defender which is protecting the hostile units I'm in combat with atm
					if (unit.Entry == HatefulEssenceId)
					{
						if (!ScriptHelpers.GroupMembers.Any(g => g.Player != null && g.Player.Combat))
							continue;
						// force followers to attack
						if (tank != null && tank.CurrentTargetGuid == unit.Guid)
						{
							outgoingunits.Add(unit);
							continue;
						}
						var immuneUnit = Targeting.Instance.TargetList.FirstOrDefault(t => t.HasAura("Apparitions"));
						if (immuneUnit != null)
						{
							var defender = ObjectManager.GetObjectByGuid<WoWUnit>(immuneUnit.Auras["Apparitions"].CreatorGuid);
							if (defender != null && defender.Location.Distance(unit.Location) <= 15)
								outgoingunits.Add(unit);
						}
					}

					if (unit.Entry == GrippingHatred && Me.IsDps() &&
						(Me.IsRange() || Me.IsMelee() && ScriptHelpers.GetGroupMembersByRole(ScriptHelpers.GroupRoleFlags.Ranged).Count(p => p.IsAlive) < 3))
						outgoingunits.Add(unit);
					if (unit.Entry == LesserVolatileEnergyId &&
						GroupMember.GroupMembers.Where(m => m.IsDps).OrderByDescending(r => r.MaxHealth).Select(r => r.Guid).FirstOrDefault() == Me.Guid)
					{
						outgoingunits.Add(unit);
					}
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					if (StyxWoW.Me.IsDps())
					{
						if (unit.Entry == ShadoPanWardenId && unit.CastingSpellId == FocusEnergy)
							priority.Score += 500;

						if (unit.Entry == GrippingHatred)
							priority.Score += 50000;
					}

					if (unit.Entry == HatefulEssenceId && Me.IsDps())
						priority.Score += 50000;
					if (unit.Entry == LesserVolatileEnergyId &&
						GroupMember.GroupMembers.Where(r => r.IsRange && r.IsDps).OrderByDescending(r => r.MaxHealth).Select(r => r.Guid).FirstOrDefault() == Me.Guid)
					{
						priority.Score += 50000;
					}
				}
			}
		}


		public override async Task<bool> HandleMovement (WoWPoint location)
		{
			var myLoc = Me.Location;
			if (!Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.Entry == AzureSerpentId && location == Targeting.Instance.FirstUnit.Location)
			{
			    if (Me.IsTank() && myLoc.DistanceSqr(_azureSerpentTankLoc) > 4*4)
			        return (await CommonCoroutines.MoveTo(_azureSerpentTankLoc)).IsSuccessful();
				if (!Me.IsTank() && myLoc.DistanceSqr(_azureSerpentFollowerLoc) > 4 * 4)
                    return (await CommonCoroutines.MoveTo(_azureSerpentFollowerLoc)).IsSuccessful();
				return true;
			}

			// Fixes a stuck issue where some toom, especially rogues with 'Cloak and Dagger' talent get stuck on the 
			// wrong side of the door.
			if (!Me.Combat && myLoc.DistanceSqr(_exitDoorAt3rdBossLoc) < 8*8 
				&& WoWMathHelper.IsPointInPoly(location, _first3BossesArea)
				&& ShaOfViolenceExitDoorIsClosed)
			{
				Logger.Write("Stuck on other side of closed door past Sha of Violence");
				await CommonCoroutines.StopMoving();
				await ScriptHelpers.PortOutsideAndBackIn();
			}
		    return false;
		}

		public override void OnEnter()
		{
			GameWorld.UnitSpellLineOfSightTest += GameWorld_UnitSpellLineOfSightTest;
			if (!Me.IsTank())
			{
				TimeCachedValue<bool> firstTrashPackPulledOrGone = new TimeCachedValue<bool>(TimeSpan.FromMilliseconds(250),
					() => ScriptHelpers.UnfriendlyNpcsArePulledOrGone(SkipPack1Loc, 22));

				_trashAfter2ndLastBridgeBs = new DynamicBlackspot(
					() => !firstTrashPackPulledOrGone.Value,
					() => Pack1BlackspotLoc,
					LfgDungeon.MapId,
					6,
					10,
					"trash pack after second last bridge");
				DynamicBlackspotManager.AddBlackspot(_trashAfter2ndLastBridgeBs);
			}

			if (!Me.IsTank())
			{
				TimeCachedValue<bool> secondTrashPackPulledOrGone = new TimeCachedValue<bool>(TimeSpan.FromMilliseconds(250),
					() => ScriptHelpers.UnfriendlyNpcsArePulledOrGone(SkipPack2Loc, 22));
					
				_trashBeforeLastBossBs = new DynamicBlackspot(
					() => !secondTrashPackPulledOrGone.Value,
						() => SkipPack2Loc,
						LfgDungeon.MapId,
						15,
						10,
						"Last trash pack before last boss");
				DynamicBlackspotManager.AddBlackspot(_trashBeforeLastBossBs);
			}
		}

		public override void OnExit()
		{
			GameWorld.UnitSpellLineOfSightTest -= GameWorld_UnitSpellLineOfSightTest;
			DynamicBlackspotManager.RemoveBlackspot(_trashBeforeLastBossBs);
			_trashBeforeLastBossBs = null;
		}

	    public override void IncludeLootTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingunits)
	    {
	        foreach (var obj in incomingObjects)
	        {
                var gObj = obj as WoWGameObject;
                if (gObj != null && _chestIds.Contains(gObj.Entry) && gObj.CanUse() &&
	                DungeonBuddySettings.Instance.LootMode != LootMode.Off)
	            {
	                outgoingunits.Add(gObj);
	            }
	        }
	    }

		private void GameWorld_UnitSpellLineOfSightTest(object sender, UnitSpellLineOfSightTestEventArgs e)
		{
			if (e.Unit.Entry != AzureSerpentId)
				return;

			var spellDist = e.Unit.CombatReach + 40;
			e.InSpellLineOfSight = e.Unit.DistanceSqr < spellDist*spellDist;
			e.Handled = true;
		}

		#endregion

		private const uint ShadoPanWardenId = 59751;
		private const uint GuCloudstrikeId = 56747;
		private const uint AzureSerpentId = 56754;
		private const uint FlyingSnowId = 56473;
		private const uint BallOfFireId = 59225;
		private const uint ShadoPanNoviceId = 56395;
		private const uint FireFlowerId = 56646;
		private const uint MasterSnowDriftsBallOfFireId = 56640;
		private const uint MasterSnowCopyId = 56713;
		private const int ParryStanceId = 106454;
		private const int FistsOfFuryId = 106853;
		private const int PursuitId = 106880;
		private const int TornadoKickId = 106434;
		private const int FocusEnergy = 115009;
		private const uint MasterSnowdriftId = 56541;
		private const int SmokeBladesId = 106826;
		private const uint ShaOfViolence = 56719;
		private const uint LesserVolatileEnergyId = 66652;
		private const uint HatefulEssenceId = 58812;
		private const uint GrippingHatred = 59804;
		private const uint ResidualHatredId = 58803;
		private const uint SlainShadoPanDefender = 58794;
        const uint MobId_TrainingTarget = 60162;
        private readonly uint[] _chestIds =
	    {
	        214518, 214519,213888, 213889
	    };

		private static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root

		private Vector2[] _first3BossesArea = new[]
											  {
												  new Vector2(3992.671f, 2784.757f), new Vector2(3930.853f, 3010.861f), new Vector2(3834.559f, 3138.905f),
												  new Vector2(3713.046f, 2991.065f), new Vector2(3736.759f, 2672.043f), new Vector2(3774.408f, 2531.415f),
												  new Vector2(3634.32f, 2486.246f), new Vector2(3508.878f, 2725.093f), new Vector2(3569.901f, 2863.264f),
												  new Vector2(3594.771f, 3093.342f), new Vector2(3728.923f, 3251.155f), new Vector2(3801.244f, 3259.495f),
												  new Vector2(4019.388f, 3095.193f), new Vector2(4097.335f, 2996.045f), new Vector2(4127.745f, 2895.484f),
												  new Vector2(4076.809f, 2769.453f),
											  };


		[EncounterHandler(0, "Root")]
		public Composite RootEncounter()
		{
			AddAvoidObject(ctx => !Me.IsTank(), 7, u => u.Entry == FlyingSnowId && ((WoWUnit)u).HasAura("Whirling Steel"));
			AddAvoidObject(ctx => true, 5, FireFlowerId);

			return new PrioritySelector(
				// from shado-pan novices
				ScriptHelpers.CreateDispelGroup("Black Cleave", ScriptHelpers.PartyDispelType.Magic),
				// deal with Slain Shado-Pan Defender.
				new Decorator(
					ctx => Me.Combat && Me.IsDps(),
					new PrioritySelector(
						ctx =>
						{
							var immuneUnit = Targeting.Instance.TargetList.FirstOrDefault(t => t.HasAura("Apparitions"));
							return immuneUnit != null ? ObjectManager.GetObjectByGuid<WoWUnit>(immuneUnit.Auras["Apparitions"].CreatorGuid) : null;
						},
						new Decorator<WoWUnit>(
							defender =>
								Me.PartyMembers.All(p => p.ChannelObject != defender) &&
								!ScriptHelpers.GetUnfriendlyNpsAtLocation(defender.Location, 12, u => u.Entry == HatefulEssenceId).Any(),
							new PrioritySelector(
								new Decorator<WoWUnit>(defender => defender.DistanceSqr > 4 * 4, new Helpers.Action<WoWUnit>(defender => Navigator.MoveTo(defender.Location))),
								new Decorator<WoWUnit>(
									defender => defender.DistanceSqr <= 4 * 4 && !Me.IsChanneling,
									new Helpers.Action<WoWUnit>(defender => defender.Interact())))))),
				
							new Decorator(ctx => Targeting.Instance.IsEmpty() || Targeting.Instance.FirstUnit.Entry != ShadoPanNoviceId,
								ScriptHelpers.CreateRunToTankIfAggroed()));
		}

		[EncounterHandler(64387, "Master Snowdrift", Mode = CallBehaviorMode.Proximity, BossRange = 30)]
		[EncounterHandler(62236, "Ban Bearheart", Mode = CallBehaviorMode.Proximity, BossRange = 30)]
		[EncounterHandler(56541, "Master Snowdrift", Mode = CallBehaviorMode.Proximity, BossRange = 30)]
		[EncounterHandler(56884, "Corrupted Taran Zhu", Mode = CallBehaviorMode.Proximity, BossRange = 30)]
		public Composite QuestHandler()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				new Decorator(
					ctx => !Me.Combat && !ScriptHelpers.WillPullAggroAtLocation(unit.Location) && unit.QuestGiverStatus == QuestGiverStatus.Available,
					ScriptHelpers.CreatePickupQuest(ctx => unit)),
				new Decorator(
					ctx => !Me.Combat && !ScriptHelpers.WillPullAggroAtLocation(unit.Location) && unit.QuestGiverStatus == QuestGiverStatus.TurnIn,
					ScriptHelpers.CreateTurninQuest(ctx => unit)));
		}

		#endregion

		#region Gu Cloudstrike

		private const uint MobId_GuCloudstrike = 56747;

		[EncounterHandler((int)MobId_GuCloudstrike, "Gu Cloudstrike", Mode = CallBehaviorMode.Proximity, BossRange = 100)]
		public Func<WoWUnit,Task<bool>> GuCloudstrikeEncounter()
		{
			WoWUnit boss = null;
			const uint staticFieldStalkerId = 56803;
			const int invokeLightningSpellId = 106984;
			const int staticFieldSpellId = 106923;

			var leftDoorEdge = new WoWPoint(3680.837, 2633.943, 771.2503);
			var rightDoorEdge = new WoWPoint(3685.094, 2624.101, 771.2479);

			var randomPointInsideDoor = WoWMathHelper.GetRandomPointInCircle(new WoWPoint (3695.623, 2636.118, 770.0417), 3);

			AddAvoidObject(ctx => true, 10, staticFieldStalkerId);
			AddAvoidLocation(ctx => true, 10, m => ((WoWMissile)m).ImpactPosition, () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == staticFieldSpellId));
			AddAvoidObject(
				ctx => true,
				5,
				u => ScriptHelpers.IsViable(boss) && boss.CastingSpellId == invokeLightningSpellId && u.Guid == boss.CurrentTargetGuid && u.Guid != Me.Guid);

			var chargingSoul = new PerFrameCachedValue<bool>(() => ScriptHelpers.IsViable(boss) && boss.HasAura("Charging Soul"));

			return async npc =>
			{
				boss = npc;

				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointInsideDoor))
					return true;

				// stack up for aoe heals and to deal with the debuff in phase 2.
				if (chargingSoul)
				{
					var loc = Me.IsTank() ? _azureSerpentTankLoc : _azureSerpentFollowerLoc;
					return await ScriptHelpers.StayAtLocationWhile(() => chargingSoul, loc, "Charging Soul", 10);
				}
				return false;
			};
		}

		#endregion

		#region Master Snowdrift

		// this is the wall before Master Snowdrift that blocks that path until mini-bosses have been defeated..
		// tanks needs to wait for NPCs to spawn and this is what this behavior does.
		[ObjectHandler(212908, "Shadowpan Hideout - Weaponmaster Gauntlet - Collision")]
		public Composite ShadowpanHideoutWeaponmasterGauntletCollisionHandler()
		{
			var roomCenterLoc = new WoWPoint(3658.823, 3015.969, 804.6611);
			return new Decorator<WoWGameObject>(
				wall => wall.State == WoWGameObjectState.Ready && Me.IsTank() && Targeting.Instance.IsEmpty(),
				new PrioritySelector(
					new Decorator(ctx => Me.Location.Distance(roomCenterLoc) > 10, new Action(ctx => Navigator.MoveTo(roomCenterLoc))),
					new ActionAlwaysSucceed()));
		}

		[EncounterHandler(56541, "Master Snowdrift", Mode = CallBehaviorMode.Proximity, BossRange = 100)]
		public Composite MasterSnowdriftEncounter()
		{
			var roomCenter = new WoWPoint(3713.436, 3091.417, 817.3193);
			var swDoorOpeningSideLoc = new WoWPoint(3682.04, 3067.78, 816.4683);
			var neDoorOpeningSideLoc = new WoWPoint(3701.133, 3054.188, 816.3525);
			var insideDoorLoc = new WoWPoint(3695.023, 3065.702, 816.201);
			var tornadoKickTimer = new WaitTimer(TimeSpan.FromSeconds(1));

			AddAvoidObject(
				ctx => true,
				4,
				u => u.Entry == MasterSnowDriftsBallOfFireId || u.Entry == BallOfFireId,
				u =>
				{
					var start = u.Location;
					return Me.Location.GetNearestPointOnSegment(start, start.RayCast(WoWMathHelper.NormalizeRadian(u.Rotation), 20));
				});

			// this guy casts the tornado kicks in rabid succession so need a time to prevent stop/go behavior when running out.
		   AddAvoidObject(ctx => true, () => roomCenter, 17, 10,
			   o =>
				{
					if (o.Entry != MasterSnowdriftId) return false;
					if (!tornadoKickTimer.IsFinished) return true;
					if (o.ToUnit().CastingSpellId == TornadoKickId)
					{
						tornadoKickTimer.Reset();
						return true;
					}
					return false;
				});

			AddAvoidObject(
				ctx => true,
				() => roomCenter,
				17,
				12,
				u => u.Entry == MasterSnowdriftId && ((WoWUnit)u).HasAura(PursuitId) && ((WoWUnit)u).CurrentTargetGuid == Me.Guid);

			AddAvoidObject( ctx => true, 6,
				u => u.Entry == MasterSnowdriftId && u.ToUnit().CastingSpellId == FistsOfFuryId,
				o => o.Location.RayCast(o.Rotation,5));

			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				new Decorator(
					ctx => boss.IsFriendly && ScriptHelpers.IsBossAlive("Master Snowdrift"),
					new Action(ctx => ScriptHelpers.MarkBossAsDead("Master Snowdrift", "He got his ass whooped"))),
				// pickup quest.
				new Decorator<WoWUnit>(unit => unit.QuestGiverStatus == QuestGiverStatus.Available, ScriptHelpers.CreatePickupQuest(ctx => boss)),

				// move inside door before encounter starts
				new Decorator(
					ctx => !boss.IsFriendly,
					new PrioritySelector(ctx => ScriptHelpers.Tank,
						new Decorator<WoWPlayer>(
							tank => !tank.IsMe && tank.Location.IsPointLeftOfLine(swDoorOpeningSideLoc, neDoorOpeningSideLoc) &&
									!Me.Location.IsPointLeftOfLine(swDoorOpeningSideLoc, neDoorOpeningSideLoc),
							new Action(ctx => Navigator.MoveTo(insideDoorLoc))),

				// if not all the followers are inside the door then wait..
						new Decorator(
							ctx =>
								Me.IsTank() && Me.Location.IsPointLeftOfLine(swDoorOpeningSideLoc, neDoorOpeningSideLoc) &&
								!Me.PartyMembers.All(p => p.Location.IsPointLeftOfLine(swDoorOpeningSideLoc, neDoorOpeningSideLoc)),
							new PrioritySelector(
								new ActionSetActivity("Waiting on followers to get inside door."),
								new Decorator(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
								new ActionAlwaysSucceed())))),
				new Decorator<WoWUnit>(
					unit => unit.Combat,
					new PrioritySelector(
						new Decorator(
							ctx => (boss.CastingSpellId == ParryStanceId || boss.HasAura(ParryStanceId)) && !Me.IsHealer() && Me.CurrentTargetGuid.IsValid,
							new Action(ctx => Me.ClearTarget())),
				// don't go anywhere if target list is empty.
						new Decorator(ctx => Me.IsTank() && Targeting.Instance.IsEmpty(), new ActionAlwaysSucceed()))));
		}

		#endregion

		#region Sha of Violence

		private const uint ShaOfViolenceId = 56719;
		private const uint GameObjectId_ShaOfViolenceExitDoor = 210866;

		[EncounterHandler(56764, "Consuming Sha")]
		public Composite ConsumingShaEncounter()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				ScriptHelpers.CreateDispelGroup("Consumption", ScriptHelpers.PartyDispelType.Magic),
				ScriptHelpers.CreateDispelEnemy("Consuming Bite", ScriptHelpers.EnemyDispelType.Enrage, ctx => unit));
		}

		[EncounterHandler(56763, "Regenerating Sha")]
		public Composite RegeneratingShaEncounter()
		{
			const int RegenerateSpellId = 106920;

			WoWUnit unit = null;
			return new PrioritySelector(ctx => unit = ctx as WoWUnit, ScriptHelpers.CreateInterruptCast(ctx => unit, RegenerateSpellId));
		}

		private const uint DestroyingShaId = 56765;
		const int ShadowsOfDestructionSpellId = 106942;

		[EncounterHandler(56765, "Destroying Sha")]
		public Composite DestroyingShaEncounter()
		{
			WoWUnit unit = null;
		   
			AddAvoidObject(ctx => !Me.IsTank(), 7,
				o => o.Entry == DestroyingShaId && o.ToUnit().CastingSpellId == ShadowsOfDestructionSpellId,
				o => o.Location.RayCast(o.Rotation, 7));

			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => unit, ShadowsOfDestructionSpellId),
				new Decorator(
					ctx => Me.IsTank() && Targeting.Instance.TargetList.All(t => t.CurrentTargetGuid == Me.Guid),
					ScriptHelpers.CreateTankFaceAwayGroupUnit(ctx => unit, 15)));
		}

		[EncounterHandler(56719, "Sha of Violence")]
		public Composite ShaofViolenceEncounter()
		{
			var roomCenter = new WoWPoint(3996.92, 2905.266, 770.3101);
			const int shaSpikeId = 106877;
			var shaSpikeTimer = new WaitTimer(TimeSpan.FromSeconds(5));
			var shaSpikeLoc = WoWPoint.Zero;

			AddAvoidObject(ctx => Me.IsRange(), () => roomCenter, 25, 12, u => u.Entry == ShaOfViolence && ((WoWUnit)u).CastingSpellId == SmokeBladesId);

			AddAvoidObject(
				ctx => true,
				5,
				o => o.Entry == ShaOfViolenceId && o.ToUnit().CastingSpellId == shaSpikeId && o.ToUnit().CurrentTargetGuid.IsValid,
				o =>
				{
					if (shaSpikeTimer.IsFinished)
					{
						shaSpikeLoc = o.ToUnit().CurrentTarget.Location;
						shaSpikeTimer.Reset();
					}
					return shaSpikeLoc;
				});

			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateDispelEnemy("Enrage", ScriptHelpers.EnemyDispelType.Enrage, ctx => boss),
				ScriptHelpers.CreateDispelGroup("Disorienting Smash", ScriptHelpers.PartyDispelType.Magic));
		}

		private bool ShaOfViolenceExitDoorIsClosed
		{
			get
			{
				return
					ObjectManager.GetObjectsOfType<WoWGameObject>()
						.Any(g => g.Entry == GameObjectId_ShaOfViolenceExitDoor && ((WoWDoor) g.SubObj).IsClosed);
			}
		}

		#endregion

		#region Shortcuts

		static readonly WoWPoint SkipPack1Loc = new WoWPoint(3839.725f,2783.861f, 745.8732);
		static readonly WoWPoint Pack1BlackspotLoc = new WoWPoint(3851.942, 2793.881, 747.158);

		private static readonly WoWPoint SkipPack2Loc = new WoWPoint(3855.411, 2650.771, 752.2302);
		private DynamicBlackspot _trashBeforeLastBossBs;
		private DynamicBlackspot _trashAfter2ndLastBridgeBs;

		#endregion

		#region Corrupted Taran Zhu

		private const uint TaranZhu = 56884;

		[EncounterHandler(58810, "Fragment of Hatred")]
		public Composite FragmentOfHatredEncounter()
		{
			WoWUnit unit = null;
			const int volleyOfHatred = 112911;
			return new PrioritySelector(ctx => unit = ctx as WoWUnit, ScriptHelpers.CreateInterruptCast(ctx => unit, volleyOfHatred));
		}

		[EncounterHandler(58803, "Residual Hatred")]
		public Composite ResidualHatredEncounter()
		{
			// avoid the ring of malice
			AddAvoidObject(
				ctx => !Me.IsMoving,
				3,
				o => o.Entry == ResidualHatredId && o.ToUnit().HasAura("Ring of Malice"),
				o =>
				{
					WoWPoint objLoc = o.Location;
					WoWPoint objToMeVector = Me.Location - objLoc;
					var objToMeRadians = (float)Math.Atan2(objToMeVector.Y, objToMeVector.X);
					return objLoc.RayCast(objToMeRadians, 11);
				});

			return new PrioritySelector();
		}

		[EncounterHandler(56884, "Corrupted Taran Zhu", Mode = CallBehaviorMode.Proximity)]
		public Composite CorruptedTaranZhuEncounter()
		{
			var tankLoc = new WoWPoint(3861.51, 2615.981, 754.5428);
			WoWUnit boss = null;
			int hatred = 0;
			const int risingHateId = 107356;

			// run away from Gripping Hatred if not killing it.
			AddAvoidObject(
				ctx => true,
				() => tankLoc,
				30,
				9,
				u => u.Entry == GrippingHatred && u.ToUnit().HasAura("Pool of Shadows") && (Targeting.Instance.FirstUnit != u || Me.IsRange()));

			AddAvoidObject(
						   ctx => Me.IsRange() || Me.IsDps() && Me.IsMelee() && !Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.Entry != TaranZhu,
						   () => tankLoc,
						   30,
						   17,
						   u => u.Entry == TaranZhu && ((WoWUnit)u).HasAura("Ring of Malice") && u.Distance >= 9);

            // If the pack at end of bridge is skipped then avoid it while in combat with boss and I have passed it (Me.Y <= 2650)
		    AddAvoidLocation(
                ctx => ScriptHelpers.IsViable(boss) && boss.Combat && Me.Y <= 2650 && ScriptHelpers.GetUnfriendlyNpsAtLocation(SkipPack2Loc, 10).Any(),
		        30,
		        o => SkipPack2Loc);

			return new PrioritySelector(
				ctx =>
				{
					hatred = Lua.GetReturnVal<int>("return UnitPower('player',10)", 0);
					return boss = ctx as WoWUnit;
				},
				new Decorator(
					ctx => boss.IsFriendly,
					new PrioritySelector(
						new Decorator(
							ctx => ScriptHelpers.IsBossAlive("Corrupted Taran Zhu"),
							new Action(ctx => ScriptHelpers.MarkBossAsDead("Corrupted Taran Zhu", "He got his ass whooped"))))),
				new Decorator(
					ctx => boss.Combat,
					new PrioritySelector(
						ScriptHelpers.CreateInterruptCast(ctx => boss, risingHateId),
						new Decorator(ctx => hatred >= 45, new Action(ctx => Lua.DoString("ExtraActionButton1:Click()"))),
						new Decorator(ctx => !boss.HasAura("Ring of Malice"), ScriptHelpers.CreateTankUnitAtLocation(ctx => tankLoc, 5)))));
		}

		#endregion
	}

	public class ShadoPanMonasteryHeroic : ShadoPanMonastery
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 470; }
		}

		#endregion
	}
}