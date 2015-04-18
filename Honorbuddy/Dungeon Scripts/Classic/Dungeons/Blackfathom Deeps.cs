using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	internal class BlackfathomDeeps : Dungeon
	{
		#region Overrides of Dungeon

		private WoWPoint poolExitLoc = new WoWPoint(-364.091f, 17.35647f, -58.10578f);
		private WoWPoint poolLoc = new WoWPoint(-313.4475f, 70.60279f, -53.60029f);

		public override uint DungeonId
		{
			get { return 10; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(4247.842, 750.5999, -22.39564); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(-150.17, 113.01, -40.54); }
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (var unit in incomingunits.OfType<WoWUnit>())
			{
				if (unit.Entry == MobId_RestorativeWaters)
					outgoingunits.Add(unit);
			}
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret.ToUnit();
					if (unit != null)
					{
						if (!unit.Combat && unit.Entry != MobId_GuardianoftheDeep && (Me.IsSwimming || !Me.IsSwimming && unit.Z < -58))
							return true;
						if (unit.Entry == SkitteringCrustaceanId && !unit.Combat)
							return true;
					}
					return false;
				});
		}

		public override void WeighHealTargetsFilter(List<Targeting.TargetPriority> objPriorities)
		{
			foreach (var priority in objPriorities)
			{
				var unit = priority.Object as WoWUnit;
				if (unit == null)
					continue;
				if (unit.Entry == MobId_RestorativeWaters || unit.Entry == MobId_DeepTerror)
					priority.Score += 4500;
			}
		}

		#endregion

		private const uint SkitteringCrustaceanId = 4821;

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root

		private const int NightmareOfTheDeeps = 26888;
		private const uint MobId_GuardianoftheDeep = 74508;
		private const uint JeneuSancreaId = 12736;

		[EncounterHandler(44375, "Zeya", Mode = CallBehaviorMode.Proximity, BossRange = 40)]
		[EncounterHandler(12736, "Je'neu Sancrea", Mode = CallBehaviorMode.Proximity, BossRange = 40)]
		[EncounterHandler(44387, "Flaming Eradicator", Mode = CallBehaviorMode.Proximity, BossRange = 40)]
		public Composite QuestGiversBehavior()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				new Decorator(
					ctx => !Me.Combat && unit.QuestGiverStatus == QuestGiverStatus.Available,
					new PrioritySelector(
						new Decorator(
							ctx =>
							unit.Entry == JeneuSancreaId && Me.QuestLog.GetQuestById(NightmareOfTheDeeps) == null &&
							Me.QuestLog.GetCompletedQuests().All(id => id != NightmareOfTheDeeps),
							ScriptHelpers.CreatePickupQuest(ctx => unit, NightmareOfTheDeeps)),
						new Decorator(ctx => unit.Entry != JeneuSancreaId, ScriptHelpers.CreatePickupQuest(ctx => unit)))),
				new Decorator(ctx => !Me.Combat && unit.QuestGiverStatus == QuestGiverStatus.TurnIn, ScriptHelpers.CreateTurninQuest(ctx => unit)));
		}

		[EncounterHandler(0)]
		public Composite RootLogic()
		{
			var tankWaitSpotByPool = new WoWPoint(-299.8438f, 90.33894f, -51.45882f);

			return new PrioritySelector(
				// don't drown..
				new Decorator(
					ctx =>
					StyxWoW.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).IsVisible && StyxWoW.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime < 15000 &&
					!StyxWoW.Me.MovementInfo.IsAscending,
					new Action(ctx => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend))),
				new Decorator(
					ctx => !StyxWoW.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).IsVisible && StyxWoW.Me.MovementInfo.IsAscending,
					new Action(ctx => WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend))),
				ScriptHelpers.CreateForceJump(
					nat => StyxWoW.Me.Location.Distance(new WoWPoint(-360.4622f, 35.82073f, -53.28525f)) < 3,
					false,
					(new WoWPoint(-360.4622f, 35.82073f, -53.28525f)),
					(new WoWPoint(-354.1001f, 35.91189f, -53.12907f))),
				ScriptHelpers.CreateForceJump(
					nat => StyxWoW.Me.Location.Distance(new WoWPoint(-337.5258f, 43.51491f, -53.12798f)) < 3,
					false,
					(new WoWPoint(-337.5258f, 43.51491f, -53.12798f)),
					(new WoWPoint(-334.1969f, 48.62948f, -53.12798f))),
				ScriptHelpers.CreateForceJump(
					nat => StyxWoW.Me.Location.Distance(new WoWPoint(-329.5626f, 49.79887f, -53.12798f)) < 2.8,
					false,
					(new WoWPoint(-329.5626f, 49.79887f, -53.12798f)),
					(new WoWPoint(-322.9993f, 49.80068f, -53.12935f))),
				ScriptHelpers.CreateForceJump(
					nat => StyxWoW.Me.Location.Distance(new WoWPoint(-314.7013f, 62.1413f, -53.12996f)) < 3,
					false,
					(new WoWPoint(-314.7013f, 62.1413f, -53.12996f)),
					(new WoWPoint(-314.6349f, 68.79098f, -53.5784f))),
				// tank waits for followers to make it across the pillars.
				new Decorator(
					ctx =>
					StyxWoW.Me.IsTank() && StyxWoW.Me.PartyMembers.Any(p => p.IsSwimming || p.Y < 65f) && StyxWoW.Me.Location.DistanceSqr(tankWaitSpotByPool) < 10 * 10 &&
					StyxWoW.Me.PartyMembers.All(p => p.IsAlive && !p.Combat),
					new PrioritySelector(new Decorator(ctx => StyxWoW.Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())), new ActionAlwaysSucceed())));
		}

		[EncounterHandler(4831, "Lady Sarevess")]
		public Composite LadySarevessBehavior()
		{
			const int forkedLightningId = 8435;

			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(ctx => !Me.IsTank(), ctx => boss, new ScriptHelpers.AngleSpan(0, 180)),
				ScriptHelpers.CreateInterruptCast(ctx => boss, forkedLightningId),
				ScriptHelpers.CreateDispelGroup("Slow", ScriptHelpers.PartyDispelType.Magic));
		}

		[ObjectHandler(224720, "Fire of Aku'mai", 50)]
		public async Task<bool> FireOfAkumaiHandler(WoWGameObject fire)
		{
			if (!Me.IsTank() || !Targeting.Instance.IsEmpty() || Me.Z < -30)
				return false;

			if (IsAkumaiDoorOpen(true))
				return false;

			if (fire.State != WoWGameObjectState.Ready)
				return false;

			if (!fire.WithinInteractRange)
			{
				return (await CommonCoroutines.MoveTo(fire.Location, "Fire of Aku'mai")).IsSuccessful();
			}

			await CommonCoroutines.StopMoving("Reached Fire of Aku'mai");

			fire.Interact();

			if (!await Coroutine.Wait(3000, () => IsAkumaiDoorOpen(false)))
			{
				Logging.WriteDiagnostic("Aku'mai door did not open after interaction with Fire of Aku'mai");
				return true;
			}

			// Door is open, continue onwards
			return false;
		}

		private TimeCachedValue<bool> _akumaiDoorOpen;
		private bool IsAkumaiDoorOpen(bool cached)
		{
			if (_akumaiDoorOpen == null)
			{
				_akumaiDoorOpen = new TimeCachedValue<bool>(TimeSpan.FromSeconds(2), () =>
				{
					WoWGameObject portalToAkumai =
						ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().FirstOrDefault(g => g.Entry == 21117);
					if (portalToAkumai == null) // Just regard it as open until we're close
						return true;

					WoWDoor door = (WoWDoor)portalToAkumai.SubObj;
					return door.IsOpen;
				});
			}

			return cached ? _akumaiDoorOpen.Value : _akumaiDoorOpen.Refreshed();
		}

		#endregion

		#region Thruk

		private const int SpellId_FilletOfFlesh = 149913;

		private const uint MobId_Thruk = 74505;
		[EncounterHandler((int)MobId_Thruk, "Thruk")]
		public Func<WoWUnit, Task<bool>> ThrukEncounter()
		{
			AddAvoidObject(10, o => o.Entry == MobId_Thruk && o.ToUnit().CastingSpellId == SpellId_FilletOfFlesh);
			return async boss => false;
		}

		#endregion

		#region Executioner Gore

		private const int SpellId_ExecutionersStrike = 149943;
		private const uint MobId_ExecutionerGore = 74518;
		private const uint AreaTriggerId_ExecutionersStrike = 5982;

		[EncounterHandler((int)MobId_ExecutionerGore, "Executioner Gore")]
		public Func<WoWUnit, Task<bool>> ExecutionerGoreEncounter()
		{
			AddAvoidObject(5, AreaTriggerId_ExecutionersStrike);

			AddAvoidObject(5, o => o.Entry == MobId_ExecutionerGore && o.ToUnit().CastingSpellId == SpellId_ExecutionersStrike, o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(6, o => o.Entry == MobId_ExecutionerGore && o.ToUnit().CastingSpellId == SpellId_ExecutionersStrike, o => o.Location.RayCast(o.Rotation, 9));
			AddAvoidObject(7, o => o.Entry == MobId_ExecutionerGore && o.ToUnit().CastingSpellId == SpellId_ExecutionersStrike, o => o.Location.RayCast(o.Rotation, 15));
			AddAvoidObject(8, o => o.Entry == MobId_ExecutionerGore && o.ToUnit().CastingSpellId == SpellId_ExecutionersStrike, o => o.Location.RayCast(o.Rotation, 22));
			AddAvoidObject(9, o => o.Entry == MobId_ExecutionerGore && o.ToUnit().CastingSpellId == SpellId_ExecutionersStrike, o => o.Location.RayCast(o.Rotation, 30));

			// Devouring Blackness is not interruptable like the dungeon journal shows.
			return async boss => false;
		}

		#endregion Executioner Gore


		#region Twilight Lord Bathiel

		private const uint MobId_RestorativeWaters = 74569;
		private const uint MobId_TwilightLordBathiel = 74728;
		private const int MissileSpellId_PiercingRain = 152884;

		[EncounterHandler((int)MobId_TwilightLordBathiel, "Twilight Lord Bathiel")]
		public Func<WoWUnit, Task<bool>> TwilightLordBathielEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				4,
				m => ((WoWMissile) m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_PiercingRain));

			return async boss => false;
		}

		#endregion

		#region Aku'mai the Devourer

		private const int SpellId_Crush = 150660;
		private const uint MobId_DeepTerror = 75172;
		private const uint MobId_AkumaitheDevourer = 75155;
		private const int MissileSpellId_FallingDebris = 152966;

		[EncounterHandler((int)MobId_AkumaitheDevourer, "Aku'mai the Devourer")]
		public Func<WoWUnit, Task<bool>> AkumaitheDevourerEncounter()
		{
			AddAvoidObject(5, o => o.Entry == MobId_DeepTerror && o.ToUnit().CastingSpellId == SpellId_Crush, o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(6, o => o.Entry == MobId_DeepTerror && o.ToUnit().CastingSpellId == SpellId_Crush, o => o.Location.RayCast(o.Rotation, 9));
			AddAvoidObject(7, o => o.Entry == MobId_DeepTerror && o.ToUnit().CastingSpellId == SpellId_Crush, o => o.Location.RayCast(o.Rotation, 15));
			AddAvoidObject(8, o => o.Entry == MobId_DeepTerror && o.ToUnit().CastingSpellId == SpellId_Crush, o => o.Location.RayCast(o.Rotation, 20));

			AddAvoidLocation(
				ctx => true,
				4,
				m => ((WoWMissile)m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FallingDebris));

			return async boss => false;
		}

		#endregion

	}
}