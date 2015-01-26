
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	public class DireMaulGordokCommons : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 38; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(-3520.225, 1094.745, 161.0336); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(255.0915, -12.56872, -2.564304); }
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (var p in units)
			{
				var unit = p.Object.ToUnit();
				if ((unit.Entry == CaptainKromcrushId || unit.Entry == GordokMageLordId) && Me.IsDps())
					p.Score += 1000;
			}
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				o =>
				{
					WoWUnit unit = o.ToUnit();
					if (unit != null)
					{
						if (unit.Entry == CaptainKromcrushId && unit.HasAura("Retaliation") && Me.IsMelee())
							return true;
					}
					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (var unit in outgoingunits.Select(u => u.ToUnit()))
			{
				if (unit.Entry == WanderingEyeofKilroggId && (Me.IsTank() || unit.Combat))
				{
					outgoingunits.Add(unit);
				}
			}
		}

		#endregion

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root

		private const uint GordokMageLordId = 11444;
		private const uint GordokReaverId = 11450;
		const uint WanderingEyeofKilroggId = 14386;
		[EncounterHandler(45052, "Stonemaul Ogre", Mode = CallBehaviorMode.Proximity, BossRange = 30)]
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

		[EncounterHandler(14325, "Stomper Kreeg")]
		public Composite StomperKreegFight()
		{
			WoWUnit boss = null;
			const uint stomperKreegId = 14325;

			AddAvoidObject(ctx => !Me.IsTank(), 8, o => o.Entry == stomperKreegId && o.ToUnit().HasAura("Whirlwind"));

			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateTankFaceAwayGroupUnit(ctx => boss, 15),
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(ctx => !Me.IsTank() && boss.Distance < 15, ctx => boss, new ScriptHelpers.AngleSpan(0, 180)));
		}

		[EncounterHandler(14323, "Guard Slip'kik", Mode = CallBehaviorMode.Proximity, BossRange = 120)]
		public Composite GuardSlipkikEncounter()
		{
			WoWUnit boss = null;
			var pullToLoc = new WoWPoint(535.7172, 542.0845, -25.40273);
			var trap = new WoWPoint(559.7982, 547.8613, -25.40069);
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				new Decorator(
					ctx => Me.IsTank(),
					ScriptHelpers.CreatePullNpcToLocation(ctx => !boss.Combat, ctx => boss.Location.Distance(trap) <= 15, ctx => boss, ctx => pullToLoc, ctx => pullToLoc, 4)),
				new Decorator(ctx => Targeting.Instance.TargetList.All(t => t.CurrentTargetGuid == Me.Guid), ScriptHelpers.CreateTankUnitAtLocation(ctx => pullToLoc, 5)));
		}

		#region Captain Kromcrush

		private const uint CaptainKromcrushId = 14325;

		[EncounterHandler(14325, "Captain Kromcrush")]
		public Composite CaptainKromcrushFight()
		{
			WoWUnit boss = null;
			const uint captainKromcrushId = 14325;
			// range need to stay away to avoid getting feared.
			AddAvoidObject(ctx => Me.IsRange() && !Me.IsCasting, 8, o => o.Entry == captainKromcrushId && o.ToUnit().CurrentTargetGuid != Me.Guid && o.ToUnit().IsAlive && !o.ToUnit().IsMoving);

			var tankLoc = new WoWPoint(584.6393, 481.2674, 29.4628);
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				// avoid gatting cleaved.
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(ctx => !Me.IsTank() && boss.Distance < 8 && !Me.IsCasting && !boss.IsMoving && boss.CurrentTargetGuid != Me.Guid, ctx => boss, new ScriptHelpers.AngleSpan(0, 180)),
				new Decorator(
					ctx => Me.IsTank() && Targeting.Instance.TargetList.All(t => t.CurrentTargetGuid == Me.Guid), ScriptHelpers.CreateTankUnitAtLocation(ctx => tankLoc, 7)),
				new Decorator(ctx => Me.IsTank() && Targeting.Instance.IsEmpty(), new ActionAlwaysSucceed()));
		}


		[EncounterHandler(11450, "Gordok Reaver")]
		public Composite GordokReaverFight()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				// avoid gatting cleaved.
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(ctx => !Me.IsTank() && unit.Distance < 8 && !Me.IsCasting && !unit.IsMoving && unit.CurrentTargetGuid != Me.Guid, ctx => unit, new ScriptHelpers.AngleSpan(0, 180)));
		}

		#endregion

		#region KingGordok

		[EncounterHandler(11501, "King Gordok")]
		public Composite KingGordokFight()
		{
			const uint kingGordokId = 11501;

			WoWUnit boss = null;
			// range need to stay away to avoid getting stunned.
			AddAvoidObject(ctx => Me.IsRange(), 5, o => o.Entry == kingGordokId && o.ToUnit().CurrentTargetGuid != Me.Guid);
			return new PrioritySelector(ctx => boss = ctx as WoWUnit, ScriptHelpers.CreateDispelEnemy("Bloodlust", ScriptHelpers.EnemyDispelType.Magic, ctx => boss));
		}


		[EncounterHandler(14324, "ChorushTheObserver")]
		public Composite ChorushTheObserverFight()
		{
			WoWUnit unit = null;
			const int healingWaveId = 15982;
			const int healId = 38209;
			return new PrioritySelector(ctx => unit = ctx as WoWUnit, ScriptHelpers.CreateInterruptCast(ctx => unit, healingWaveId, healId));
		}

		#endregion
	}
}