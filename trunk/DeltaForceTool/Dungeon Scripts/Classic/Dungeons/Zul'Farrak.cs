using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;

using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	public class ZulFarrak : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 24; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(-6790.56, -2890.72, 8.88742); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(1206.67, 842.04, 8.900352); }
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (var unit in incomingunits.Select(obj => obj.ToUnit()))
			{
				switch (unit.Entry)
				{
					case 6017: // Lava Spout Totem
					case 6066: // Earthgrab Totem
					case 8179: // Greater Healing Ward
					case 7785: // Ward of Zum'rah
						outgoingunits.Add(unit);
						break;

					case 8877: // Sandfury Zealot
					case 7787: // Sandfury Slave
					case 7788: // Sandfury Drudge
					case 8876: // Sandfury Acolyte
						if (unit.Combat)
							outgoingunits.Add(unit);
						break;
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (var p in units)
			{
				WoWUnit unit = p.Object.ToUnit();
				switch (unit.Entry)
				{
					case 6017: // Lava Spout Totem
					case 6066: // Earthgrab Totem
					case 8179: // Greater Healing Ward
					case 7785: // Ward of Zum'rah
					case 7797: // Ruuzlu
						p.Score += 500;
						break;
				}
			}
		}

		readonly WoWPoint _holeByPrisonerLoc = new WoWPoint(1874.575, 1285.383, 41.17591);
		readonly WoWPoint _holeExitLoc = new WoWPoint(1883.203, 1277.124, 42.04437);

		public override MoveResult MoveTo(WoWPoint location)
		{
			var myLoc = Me.Location;
			if (myLoc.DistanceSqr(_holeByPrisonerLoc) < 12*12 && myLoc.Z < 41.5)
			{
				if (myLoc.Distance2DSqr(_holeExitLoc) < 4*4)
				{
					WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
					WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
				}
				Navigator.PlayerMover.MoveTowards(_holeExitLoc);
				return MoveResult.Moved;
			}
			return base.MoveTo(location);
		}

		#endregion

		private static readonly WoWPoint FinalBlyLocation = new WoWPoint(1885.295, 1202.984, 8.877242);

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}


		private WoWGameObject TrollCage
		{
			get { return ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.Entry == 141071).OrderBy(o => o.DistanceSqr).FirstOrDefault(); }
		}

		private WoWUnit SergeantBly
		{
			get { return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(o => o.Entry == 7604); }
		}

		private WoWUnit Gahzrilla
		{
			get { return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(o => o.Entry == 7273); }
		}

		#region Root

		[EncounterHandler(40712, "Mazoga's Spirit", Mode = CallBehaviorMode.Proximity, BossRange = 45)]
		public Composite QuestPickupHandler()
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

		#region Theka the Martyr

		[EncounterHandler(7272, "Theka the Martyr")]
		public Composite ThekaTheMartyrFight()
		{
			const int feveredPlagueId = 8600;
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => boss, feveredPlagueId),
				ScriptHelpers.CreateDispelGroup("Fevered Plague", ScriptHelpers.PartyDispelType.Curse));
		}

		#endregion

		#region Witch Doctor Zum'rah

		[EncounterHandler(7271, "Witch Doctor Zum'rah", 75, CallBehaviorMode.CurrentBoss)]
		public Composite WitchDoctorZumrahStart()
		{
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				new Decorator(
					ctx => boss != null && boss.IsAlive && boss.IsFriendly && Targeting.Instance.IsEmpty(),
					new Action(ctx => ScriptHelpers.SetLeaderMoveToPoiPS(boss.Location))));
		}

		[EncounterHandler(7271, "Witch Doctor Zum'rah")]
		public Composite WitchDoctorZumrahFight()
		{
			const int shadowboltVolley = 15245;
			const int healingWave = 12491;
			WoWUnit boss = null;
			return new PrioritySelector(ctx => boss = ctx as WoWUnit, ScriptHelpers.CreateInterruptCast(ctx => boss, shadowboltVolley, healingWave));
		}

		#endregion

		#region Nekrum Gutchewer

		[EncounterHandler(7796, "Nekrum Gutchewer")]
		public Composite NekrumGutchewerFight()
		{
			const int feveredPlagueId = 15245;
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => boss, feveredPlagueId),
				ScriptHelpers.CreateDispelEnemy("Renew", ScriptHelpers.EnemyDispelType.Magic, ctx => boss));
		}

		#endregion

		#region Hydromancer Velratha

		[EncounterHandler(7795, "Hydromancer Velratha")]
		public Composite HydromancerVelrathaFight()
		{
			const int healingWaveId = 12491;
			WoWUnit boss = null;
			return new PrioritySelector(ctx => boss = ctx as WoWUnit, ScriptHelpers.CreateInterruptCast(ctx => boss, healingWaveId));
		}

		#endregion

		#region Gahz'rilla

		private const uint GahzrillId = 7273;

		[ObjectHandler(141832, "Gong of Zul'Farrak", 100)]
		public Composite GongHandler()
		{
			WoWGameObject gong = null;
			return new PrioritySelector(
				ctx => gong = ctx as WoWGameObject,
				new Decorator(
					ctx => BossManager.CurrentBoss != null && BossManager.CurrentBoss.Entry == GahzrillId,
					new PrioritySelector(
						new Decorator(
							ctx =>
							ScriptHelpers.GetUnfriendlyNpsAtLocation(gong.Location, 100, u => u.IsHostile).Any() && Targeting.Instance.IsEmpty() &&
							BotPoi.Current.Type == PoiType.None,
							ScriptHelpers.CreateClearArea(() => gong.Location, 100, u => u.IsHostile)),
						new Decorator(
							ctx =>
							ScriptHelpers.IsBossAlive("Gahz'rilla") && Me.IsTank() && Targeting.Instance.IsEmpty() && Gahzrilla == null && BotPoi.Current.Type == PoiType.None &&
							!ScriptHelpers.GetUnfriendlyNpsAtLocation(gong.Location, 100, u => u.IsHostile).Any(),
							new PrioritySelector( 
								new ActionSetActivity("Interacting with {0}", gong),
								new Decorator(ctx => gong.DistanceSqr > 4 * 4, new Action(ctx => Navigator.MoveTo(gong.Location))),
								new Decorator(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
								new Sequence(
									new Action(ctx => gong.Interact()),
									new Wait(5, ret => Gahzrilla != null, new Sleep(250))))))));
		}

		[EncounterHandler(7273, "Gahz'rilla")]
		public Composite GahzrillaFight()
		{
			WoWUnit boss = null;
			return new PrioritySelector(ctx => boss = ctx as WoWUnit, ScriptHelpers.CreateDispelGroup("Freeze Solid", ScriptHelpers.PartyDispelType.Magic));
		}

		#endregion

		#region Shadowpriest Sezz'ziz

		[EncounterHandler(7275, "Shadowpriest Sezz'ziz")]
		public Composite ShadowpriestSezzzizFight()
		{
			const int healId = 12039;
			const int renewId = 8362;
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => boss, healId, renewId),
				ScriptHelpers.CreateDispelEnemy("Renew", ScriptHelpers.EnemyDispelType.Magic, ctx => boss));
		}

		#endregion

		#region Chief Ukorz Sandscalp

		[EncounterHandler(7275, "Chief Ukorz Sandscalp")]
		[EncounterHandler(7797, "Ruuzlu")]
		public Composite ChiefUkorzSandscalpFight()
		{
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(ctx => !Me.IsTank() && boss.Distance < 8, ctx => boss, new ScriptHelpers.AngleSpan(0, 189)),
				ScriptHelpers.CreateTankFaceAwayGroupUnit(ctx => boss, 8));
		}

		#endregion

		[ObjectHandler(146084, "End Door", 100)]
		public Composite PrisonerEvent()
		{
			WoWGameObject endDoor = null;
			WoWGameObject trollCage = null;
			WoWUnit sergeantBly = null;

			return new PrioritySelector(
				ctx => endDoor = ctx as WoWGameObject,
				new Decorator(
					ctx => endDoor.State != WoWGameObjectState.Active && endDoor.State != WoWGameObjectState.ActiveAlternative && StyxWoW.Me == ScriptHelpers.Tank,
				// context switch to troll cage
					new PrioritySelector(
						ctx => trollCage = TrollCage,
				// cages are closed, lets go open them.
						new Decorator(
							ctx => trollCage != null && trollCage.State == WoWGameObjectState.Ready,
							new PrioritySelector(
								new Decorator(ctx => trollCage.DistanceSqr > 4 * 4, new Action(ctx => ScriptHelpers.SetLeaderMoveToPoiPS(trollCage.Location))),
								new Decorator(ctx => trollCage.DistanceSqr <= 4 * 4, ScriptHelpers.CreateInteractWithObject(141071)))),
				// cages are open lets help the prisoners fight off the waves of NPCs
						new Decorator(
							ctx => trollCage != null && trollCage.State == WoWGameObjectState.Active,
				// context switch to sergent bly,
							new PrioritySelector(
								ctx => sergeantBly = SergeantBly,
								new Decorator(
									ctx => sergeantBly != null && Targeting.Instance.IsEmpty() && sergeantBly.CanGossip && sergeantBly.Location.DistanceSqr(FinalBlyLocation) < 7,
									ScriptHelpers.CreateTalkToNpc(ctx => sergeantBly)),
								new Decorator(
									ctx => sergeantBly != null && sergeantBly.IsAlive && sergeantBly.DistanceSqr >= 6 * 6,
									new Action(ctx => ScriptHelpers.SetLeaderMoveToPoiPS(sergeantBly.Location))))),
						new Decorator(ctx => Targeting.Instance.IsEmpty() && BotPoi.Current.Type == PoiType.None && Me.IsTank(), new ActionAlwaysSucceed()))));
		}
	}
}