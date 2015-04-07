using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Enums;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Extensions = Styx.Helpers.Extensions;

// ReSharper disable CheckNamespace

namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{

	#region Normal Difficulty

	public class ShadowmoonBurialGrounds : WoDDungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId { get { return 783; } }

		public override WoWPoint ExitLocation { get { return new WoWPoint(1712.285, 254.3997, 328.5056); } }

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			var isTank = Me.IsTank();

			units.RemoveAll(
				obj =>
				{
					var unit = obj as WoWUnit;
					if (unit == null)
						return false;

					if (isTank && unit.Entry == MobId_RitualofBones)
						return true;

					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			var isDps = Me.IsDps();
			foreach (var obj in incomingunits)
			{
				var unit = obj as WoWUnit;
				if (unit != null)
				{
					if (unit.Entry == MobId_DefiledSpirit || unit.Entry == MobId_PossessedSoul)
						outgoingunits.Add(unit);
						// Only dps should be killing these since tank needs to be facing Ner'zhul away from group
					else if (unit.Entry == MobId_RitualofBones && isDps)
						outgoingunits.Add(unit);
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> priorities)
		{
			var isDps = Me.IsDps();

			foreach (var priority in priorities)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					switch (unit.Entry)
					{
						case MobId_ShadowmoonBoneMender:
							if (isDps)
								priority.Score += 3500;
							break;
						case MobId_DefiledSpirit:
							if (isDps || ScriptHelpers.IsViable(_sadana) && _sadana.Combat)
								priority.Score += 4500;
							break;
						case MobId_CarrionWorm:
						case MobId_PossessedSoul:
							priority.Score += 4500;
							break;
						case MobId_RitualofBones:
							priority.Score = unit == _selectedRitualOfBonesTarget ? 5000 : -5000;
							break;
					}
				}
			}
		}

		public override void OnEnter()
		{
			_dynamicBlackspots = GetEntranceTrashBlackspots()
				.Concat(GetBonemawTrashBlackspots()).ToList();

			DynamicBlackspotManager.AddBlackspots(_dynamicBlackspots);
			Lua.Events.AttachEvent("RAID_BOSS_EMOTE", OnRaidBossEmote);
		}

		public override void OnExit()
		{
			DynamicBlackspotManager.RemoveBlackspots(_dynamicBlackspots);
			_dynamicBlackspots = null;
			Lua.Events.DetachEvent("RAID_BOSS_EMOTE", OnRaidBossEmote);
			TreeHooks.Instance.RemoveHook("Dungeonbuddy_Main", _backStepCancelBehavior);
			_backStepCancelBehavior = null;
		}

		readonly WoWPoint _bonemawLoc = new WoWPoint(1849.425, -551.4028, 201.3045);
		readonly WoWPoint _bonemawMoveToLoc = new WoWPoint(1843.35, -542.5502, 201.6532);

		public override async Task<bool> HandleMovement(WoWPoint location)
		{
			var myLoc = Me.Location;

			var destinationInNerzhulsRoom = location.DistanceSqr(_nerzhulRoomCenterLoc) <= 75*75;
			// If the portal to Ner'zhul is not up then just move to 'location' to get ported down.
			// The portal that spawns after a wipe is handled in mesh.
			if (destinationInNerzhulsRoom && Targeting.Instance.IsEmpty() && LootTargeting.Instance.IsEmpty()
				&& ObjectManager.GetObjectsOfType<WoWGameObject>().All(a => a.Entry != GameObjectId_EntertheShadowlands))
			{
				return (await CommonCoroutines.MoveTo(_nerzhulAutoPortFromLoc)).IsSuccessful();
			}

			// We can't navigate out of Ner'zhul's room, block any attempt to
			if (myLoc.DistanceSqr(_nerzhulRoomCenterLoc) < 75*75 && !destinationInNerzhulsRoom)
				return true;

			if (location.DistanceSqr(_bonemawLoc) < 3*3)
				return (await CommonCoroutines.MoveTo(_bonemawMoveToLoc)).IsSuccessful();

			return false;
		}

		public override bool CanNavigateFully(WoWPoint @from, WoWPoint to)
		{
			if (to.DistanceSqr(_bonemawLoc) < 3*3)
				return true;

			return base.CanNavigateFully(@from, to);
		}

		#endregion

		#region Root

		private const uint AreaTriggerId_ShadowRune1 = 5992;
		private const uint AreaTriggerId_ShadowRune2 = 5994;
		private const uint AreaTriggerId_ShadowRune3 = 5996;
		private Composite _backStepCancelBehavior;
		private List<DynamicBlackspot> _dynamicBlackspots;

		private static LocalPlayer Me { get { return StyxWoW.Me; } }

		[EncounterHandler(0, "Root")]
		public Func<WoWUnit, Task<bool>> RootHandler()
		{
			AddAvoidObject(
				ctx => true,
				2f,
				o => o.Entry == AreaTriggerId_ShadowRune2 && o.ZDiff < 15 && o.DistanceSqr < 60*60,
				ignoreIfBlocking: true);

			AddAvoidObject(
				ctx => true,
				2.5f,
				o => o.Entry == AreaTriggerId_ShadowRune3 && o.ZDiff < 15 && o.DistanceSqr < 60*60,
				ignoreIfBlocking: true);

			return async boss => await ScriptHelpers.CancelCinematicIfPlaying();
		}

		[LocationHandler(1912.797, -26.04675, 286.9844, 10)]
		public async Task<bool> HandleShortcutAtEntrance(WoWPoint loc)
		{
			if (!Me.GotAlivePet || BotPoi.Current.Type != PoiType.None)
				return false;

			// a bit of a hack. Current navigator doesn't handle dismissing pets so we need to do it.
			var meshNavigator = Navigator.NavigationProvider as MeshNavigator;
			if (meshNavigator == null || meshNavigator.CurrentMovePath == null)
				return false;

			var index = Extensions.IndexOf(meshNavigator.CurrentMovePath.Path.AbilityFlags, AbilityFlags.Jump);
			if (index == -1 || meshNavigator.CurrentMovePath.Index > index
				|| index + 1 >= meshNavigator.CurrentMovePath.Path.Points.Length)
			{
				return false;
			}

			if (meshNavigator.CurrentMovePath.Path.Points[index].DistanceSqr(loc) > 10*10)
				return false;

			var moveTo = meshNavigator.CurrentMovePath.Path.Points[index + 1];
			Logging.Write("Dismissing pet before taking shortcut");
			await WoWPetControl.DismissPet();
			// Note. This will cause a new CurrentMovePath to be generated so no need to set any indexes... 
			await ScriptHelpers.MoveToContinue(() => moveTo);
			return true;
		}

		#endregion

		#region Garrison Inn Quests

		// Shadowy Secrets
		[ObjectHandler(237470, "Dark Parchment", ObjectRange = 30)]
		public async Task<bool> DarkParchmentHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 40);
		}

		// The Void-Gate
		[ObjectHandler(237482, "Void-Gate Key", ObjectRange = 45)]
		public async Task<bool> VoidGateKeyHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 55);
		}

		// The Huntress
		[ObjectHandler(237471, "Silver-Lined Arrow", ObjectRange = 65)]
		public async Task<bool> SilverLinedArrowHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 75);
		}

		#endregion

		#region Sadana Bloodfury

		#region Trash

		private const int SpellId_ShadowMend = 152818;
		private const int SpellId_VoidPulse = 152964;

		private const uint MobId_ShadowmoonBoneMender = 75713;
		private const uint MobId_VoidSpawn = 75652;
		private const uint MobId_ShadowmoonLoyalist = 75506;
		private const uint AreaTriggerId_VoidSphere = 6016;

		private static readonly WoWPoint RightEntranceTrashLoc = new WoWPoint(1715.288, 48.13368, 287.0209);
		private static readonly WoWPoint LeftEntranceTrashLoc = new WoWPoint(1881.267, 59.7691, 287.0202);

		private readonly TimeCachedValue<bool> ShouldAvoidLeftEntranceSide = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(5),
			() => ScriptHelpers.GetUnfriendlyNpsAtLocation(LeftEntranceTrashLoc, 20, unit => unit.IsHostile).Any());

		private readonly TimeCachedValue<bool> ShouldAvoidRightEntranceSide = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(5),
			() => ScriptHelpers.GetUnfriendlyNpsAtLocation(RightEntranceTrashLoc, 20, unit => unit.IsHostile).Any());

		[EncounterHandler((int) MobId_ShadowmoonLoyalist, "Shadowmoon Loyalist")]
		public Func<WoWUnit, Task<bool>> ShadowmoonLoyalistEncounter()
		{
			AddAvoidObject(
				ctx => true,
				3,
				o => o.Entry == AreaTriggerId_VoidSphere,
				o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 15)));

			return async npc => false;
		}

		[EncounterHandler((int) MobId_ShadowmoonBoneMender, "Shadowmoon Bone-Mender")]
		public Func<WoWUnit, Task<bool>> ShadowmoonBoneMenderEncounter()
		{
			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_ShadowMend);
		}

		[EncounterHandler((int) MobId_VoidSpawn, "Void Spawn")]
		public Func<WoWUnit, Task<bool>> VoidSpawnEncounter()
		{
			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_VoidPulse);
		}


		private IEnumerable<DynamicBlackspot> GetEntranceTrashBlackspots()
		{
			yield return new DynamicBlackspot(
				() => ShouldAvoidRightEntranceSide,
				() => RightEntranceTrashLoc,
				LfgDungeon.MapId,
				40,
				20,
				"Right Entrance Trash group");

			yield return new DynamicBlackspot(
				() => ShouldAvoidLeftEntranceSide,
				() => LeftEntranceTrashLoc,
				LfgDungeon.MapId,
				40,
				20,
				"Left Entrance Trash group");
		}

		#endregion

		private const int SpellId_DarkEclipse = 164974;
		private const int MissileSpellId_Daggerfall = 153225;

		private const uint MobId_Daggerfall = 75981;
		private const uint MobId_DefiledSpirit = 75966;
		private const uint AreaTriggerId_LunarRune2 = 6975;

		private const uint MobId_SadanaBloodfury = 75509;

		private readonly WoWPoint[] DarkEclipseSafePoints =
		{
			new WoWPoint(1795.755, -12.44243, 261.3086),
			new WoWPoint(1805.134, -16.60828, 261.3086),
			new WoWPoint(1809.745, -26.89476, 261.3086),
			new WoWPoint(1805.725, -37.13824, 261.3086),
			new WoWPoint(1795.753, -40.47881, 261.3086),
			new WoWPoint(1786.224, -36.83677, 261.3086),
			new WoWPoint(1781.868, -26.86782, 261.3086),
			new WoWPoint(1786.319, -16.44705, 261.3086),
		};

		private WoWUnit _sadana;
		// http://www.wowhead.com/guide=2668/shadowmoon-burial-grounds-dungeon-strategy-guide#sadana-bloodfury
		[EncounterHandler((int) MobId_SadanaBloodfury, "Sadana Bloodfury", Mode= CallBehaviorMode.Proximity)]
		public Func<WoWUnit, Task<bool>> SadanaBloodfuryEncounter()
		{
			var roomCenterLoc = new WoWPoint(1795.512, -27.01042, 261.3087);

			AddAvoidLocation(
				ctx => true,
				() => roomCenterLoc,
				18,
				3,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_Daggerfall));

			AddAvoidObject(ctx => true, () => roomCenterLoc, 18, 4, MobId_Daggerfall);

			// ignore these unless inside the circle that boss stands in.. 
			AddAvoidObject(
				ctx => true,
				() => roomCenterLoc,
				18,
				1.5f,
				o => o.Entry == AreaTriggerId_ShadowRune1 && o.ZDiff < 15 && o.DistanceSqr < 60*60,
				ignoreIfBlocking: true);

			var inDarkEclipsePhase =
				new PerFrameCachedValue<bool>(() => ScriptHelpers.IsViable(_sadana) && _sadana.HasAura(SpellId_DarkEclipse));

			Func<bool> handleDarkEclipse = () => inDarkEclipsePhase;

			var rightDoorEdge = new WoWPoint(1787.233, 24.70311, 261.1445);
			var leftDoorEdge = new WoWPoint(1805.031, 23.9772, 261.0555);

			var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(new WoWPoint (1795.797, -0.1476936, 261.3087), 2);

			return async boss =>
			{
				_sadana = boss;

				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, 
					randomPointInsideRoom, player => player.Z < 265))
				{
					return true;
				}

				if (!boss.Combat)
					return false;

				if (boss.HealthPercent > 25 && await ScriptHelpers.CastHeroism())
					return true;

				if (inDarkEclipsePhase)
				{
					var safePoint = DarkEclipseSafePoints.Where(l => !AvoidanceManager.Avoids.Any(a => a.IsPointInAvoid(l)))
						.OrderBy(l => l.DistanceSqr(Me.Location))
						.FirstOrDefault();

					return await ScriptHelpers.StayAtLocationWhile(handleDarkEclipse, safePoint, "Dark Eclipse safe spot", 1f);
				}

				return false;
			};
		}

		#endregion

		#region Nhallish

		#region Trash

		private const int SpellId_RendingVoidlash = 156776;
		private const uint MobId_ShadowmoonEnslaver = 76446;

		[EncounterHandler((int) MobId_ShadowmoonEnslaver, "Shadowmoon Enslaver")]
		public Func<WoWUnit, Task<bool>> ShadowmoonEnslaverEncounter()
		{
			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_RendingVoidlash);
		}

		#endregion

		private const int SpellId_VoidVortex = 152801;
		private const uint MobId_PossessedSoul = 75899;
		private const uint MobId_Nhallish = 75829;
		private const uint AreaTriggerId_SummonAncestors = 6045;
		private const uint AreaTriggerId_VoidDevastation = 6036;
		private const uint AreaTriggerId_VoidVortex = 6017;

		// http://www.wowhead.com/guide=2668/shadowmoon-burial-grounds-dungeon-strategy-guide#nhallish

		[EncounterHandler((int) MobId_Nhallish, "Nhallish", Mode = CallBehaviorMode.Proximity)]
		public Func<WoWUnit, Task<bool>> NhallishEncounter()
		{
			AddAvoidObject(ctx => true, 4.5f, AreaTriggerId_SummonAncestors);
			AddAvoidObject(ctx => true, 6, AreaTriggerId_VoidDevastation);
			AddAvoidObject(
				ctx => true,
				15.5f,
				o => o.Entry == AreaTriggerId_VoidVortex || o.Entry == MobId_Nhallish && o.ToUnit().CastingSpellId == SpellId_VoidVortex);

			var rightDoorEdge = new WoWPoint(1744.237, -189.2551, 257.9099);
			var leftDoorEdge = new WoWPoint (1744.646, -206.0277, 255.882);
			var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(1725.798, -194.8995, 252.0052), 3);

			return async boss =>
			{
				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointInsideRoom))
					return true;

				var deadPossessedSoul = ObjectManager.GetObjectsOfType<WoWUnit>()
					.FirstOrDefault(u => u.Entry == MobId_PossessedSoul && u.HasAura("Reclaim Soul"));

				// reclaim the soul to get a 40 % damage/heal buff.
				if (deadPossessedSoul != null)
					return await ScriptHelpers.InteractWithObject(deadPossessedSoul, 3000, true);

				return false;
			};
		}

		#endregion

		#region Bonemaw

		#region Trash

		private const int SpellId_BodySlam_Trash = 153395;
		private const int SpellId_DeathVenom = 156717;
		private const uint MobId_MonstrousCorpseSpider = 76104;
		private const uint MobId_PlaguedBat = 75459;
		private const uint MobId_CarrionWorm = 76057;

		private static readonly WoWPoint RightBonemawTrashLoc = new WoWPoint(1725.447, -282.8209, 251.9631);
		private static readonly WoWPoint LeftBonemawTrashLoc = new WoWPoint(1722.297, -245.576, 251.0521);

		private readonly TimeCachedValue<bool> ShouldAvoidLeftBonemawSide = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(5),
			() => ScriptHelpers.GetUnfriendlyNpsAtLocation(LeftBonemawTrashLoc, 20, unit => unit.IsHostile).Any());

		private readonly TimeCachedValue<bool> ShouldAvoidRightBonemawSide = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(5),
			() => ScriptHelpers.GetUnfriendlyNpsAtLocation(RightBonemawTrashLoc, 20, unit => unit.IsHostile).Any());

		private IEnumerable<DynamicBlackspot> GetBonemawTrashBlackspots()
		{
			yield return new DynamicBlackspot(
				() => ShouldAvoidRightBonemawSide,
				() => RightBonemawTrashLoc,
				LfgDungeon.MapId,
				40,
				20,
				"Right Bonemaw Trash group");

			yield return new DynamicBlackspot(
				() => ShouldAvoidLeftBonemawSide,
				() => LeftBonemawTrashLoc,
				LfgDungeon.MapId,
				40,
				20,
				"Left Bonemaw Trash group");
		}


		[EncounterHandler((int) MobId_MonstrousCorpseSpider, "Monstrous Corpse Spider")]
		public Func<WoWUnit, Task<bool>> MonstrousCorpseSpiderEncounter()
		{
			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_DeathVenom);
		}


		[EncounterHandler((int) MobId_PlaguedBat, "Plagued Bat")]
		public Func<WoWUnit, Task<bool>> PlaguedBatEncounter()
		{
			return async npc =>
						{
							if (Me.PartyMembers.Any(g => g.HasAura("Plague Spit") && g.Auras["Plague Spit"].StackCount >= 5))
							{
								if (await ScriptHelpers.DispelGroup("Plague Spit", ScriptHelpers.PartyDispelType.Disease))
									return true;
							}
							return false;
						};
		}

		[EncounterHandler((int) MobId_CarrionWorm, "Carrion Worm")]
		public Func<WoWUnit, Task<bool>> CarrionWormEncounter()
		{
			WoWUnit worm = null;
			AddAvoidLocation(
				ctx => ScriptHelpers.IsViable(worm) && worm.CastingSpellId == SpellId_BodySlam_Trash,
				6*1.33f,
				o => (WoWPoint) o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					worm.Location,
					worm.Location.RayCast(worm.Rotation, 60),
					6/2).OfType<object>());

			return async npc =>
						{
							worm = npc;
							return false;
						};
		}

		#endregion
		
		private readonly WaitTimer _inhaleEmoteTimer = new WaitTimer(TimeSpan.FromSeconds(5));

		private readonly WaitTimer _inhaleEmoteResetTimer = new WaitTimer(TimeSpan.FromSeconds(20));

		private const int SpellId_Inhale = 153804;
		private const int SpellId_BodySlam = 154175;
		private const int MissileSpellId_NecroticPitch = 153689;

		private const uint MobId_Bonemaw = 75452;
		private const uint AreaTriggerId_NecroticPitch = 6098;
		private const uint MobId_WaterBurst = 77676;

		private const uint GameObjectId_BonemawEntranceDoor = 233990;
		WoWPoint _bonemawStartPosition = new WoWPoint(1849.425, -551.4028, 201.3045);

		[EncounterHandler((int) MobId_Bonemaw, "Bonemaw")]
		// http://www.wowhead.com/guides/dungeons/shadowmoon-burial-grounds-dungeon-strategy-guide#bonemaw
		public Func<WoWUnit, Task<bool>> BonemawEncounter()
		{
			const float doorAvoidLineWidth = 2;
			const float bodySlamAvoidLineWidth = 7;

			WoWUnit boss = null;

			var isInhaling = new PerFrameCachedValue<bool>(
					() => ScriptHelpers.IsViable(boss) && (boss.HasAura(SpellId_Inhale) || ! _inhaleEmoteTimer.IsFinished));

			var isDoorClosed = new PerFrameCachedValue<bool>(
				() =>
				{
					if (!ScriptHelpers.IsViable(boss) || !boss.Combat)
						return false;

					var door = ObjectManager.GetObjectsOfType<WoWGameObject>()
						.FirstOrDefault(g => g.Entry == GameObjectId_BonemawEntranceDoor);
					return door != null && ((WoWDoor) door.SubObj).IsClosed;
				});

			var rightDoorEdge = new WoWPoint(1815.843, -495.1395, 201.0704);
			var leftDoorEdge = new WoWPoint(1830.936, -490.8846, 201.521);

			var jumpInWaterStartLoc = new WoWPoint(1815.321, -482.4096, 200.836);
			var jumpInWaterEndLoc = new WoWPoint(1798.076, -484.8755, 194.7952);

			var waterSpoutLocs = new[] {new WoWPoint(1798.934, -523.8507, 197.0144), new WoWPoint(1765.194, -412.3889, 197.0144)};

			AddAvoidLocation(
				ctx => !Me.IsSwimming && ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_BodySlam,
				bodySlamAvoidLineWidth*1.33f,
				o => (WoWPoint) o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					boss.Location,
					boss.Location.RayCast(boss.Rotation, 60),
					bodySlamAvoidLineWidth/2).OfType<object>());

			AddAvoidLocation(
				ctx => !isInhaling,
				4.5f,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_NecroticPitch));

			AddAvoidObject(
				ctx => !isInhaling,
				4.5f,
				AreaTriggerId_NecroticPitch);

			// Avoids running into the barrier at room entrance while running from something else.
			AddAvoidLocation(
				ctx => isDoorClosed,
				doorAvoidLineWidth*1.33f,
				o => (WoWPoint) o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					leftDoorEdge,
					rightDoorEdge,
					doorAvoidLineWidth/2).OfType<object>());

			// Range need to stay away from boss so the necrotic pitch isn't placed near boss.
			AddAvoidObject(ctx => Me.IsRange() && !isInhaling, 30, o => o.Entry == MobId_Bonemaw && o.ToUnit().IsAlive);

			var noMovebehind =CapabilityManager.Instance.CreateNewHandle();
			
			return async npc =>
			{
				boss = npc;

				// Melee can't move behind this NPC
				CapabilityManager.Instance.Update(
					noMovebehind,
					CapabilityFlags.MoveBehind,
					() => ScriptHelpers.IsViable(boss) && boss.Combat);

				if (boss.HealthPercent > 25 && boss.HealthPercent <= 97 && await ScriptHelpers.CastHeroism())
					return true;

				// handle getting out of the water
				if (Me.IsSwimming)
				{
					// We can only use the 2 water bursts by the boss platform. The other one throws player on the walkway 
					// which gets blocked when encounter starts.
					var waterBurst = ObjectManager.GetObjectsOfType<WoWUnit>()
						.Where(u => u.Entry == MobId_WaterBurst && waterSpoutLocs.Any(l => l.Distance2DSqr(u.Location) < 10*10))
						.OrderBy(u => u.DistanceSqr)
						.FirstOrDefault();
					var moveTo = waterBurst != null ? waterBurst.Location : waterSpoutLocs[0];
					TreeRoot.StatusText = "Getting out of the water";
					return (await CommonCoroutines.MoveTo(moveTo)).IsSuccessful();
				}

				if (!Me.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge) && isDoorClosed)
				{
					// Get in the water if on the walkway and door to boss is closed
					// We'll take a portal from water to get to the boss.
					TreeRoot.StatusText = "Going for a swim";
					if (!Navigator.AtLocation(jumpInWaterStartLoc))
						return (await CommonCoroutines.MoveTo(jumpInWaterStartLoc)).IsSuccessful();
					Navigator.PlayerMover.MoveTowards(jumpInWaterEndLoc);
					await Coroutine.Wait(4000, () => Me.IsSwimming);
					return true;
				}

				if (!Me.IsSwimming && (!_inhaleEmoteTimer.IsFinished || boss.HasAura(SpellId_Inhale)))
				{
					TreeRoot.StatusText = "Standing on top of Necrotic Pitch to avoid getting inhaled";
								
					Navigator.NavigationProvider.StuckHandler.Reset();
								
					// Stand in pitch to prevent getting sucked into Bonemaw's mouth during inhale.
					var pitchList = ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
						.Where(a => a.Entry == AreaTriggerId_NecroticPitch && a.ZDiff < 5)
						.OrderBy(a => a.DistanceSqr).ToList();

					if (pitchList.Any())
					{
						// try find a pitch that is further then 25 yards from boss. You can still get sucked off if too close to boss.
						var pitch = pitchList.FirstOrDefault(p => p.Location.DistanceSqr(boss.Location) > 25*25) ??
									pitchList.OrderByDescending(p => p.Location.DistanceSqr(boss.Location)).First();

						if (pitch != null)
						{
							return await ScriptHelpers.StayAtLocationWhile(
								() => isInhaling && ScriptHelpers.IsViable(pitch),
								pitch.Location,
								"Necrotic Pitch",
								4);
						}
					}
				}
				return false;
			};
		}

		private void OnRaidBossEmote(object sender, LuaEventArgs args)
		{
			if (!_inhaleEmoteResetTimer.IsFinished)
				return;

			_inhaleEmoteTimer.Reset();
			_inhaleEmoteResetTimer.Reset();
		}

		#endregion

		#region Ner'zhul

		protected virtual int MinRequireGroupItemLevelForNerzhul
		{
			get { return 608; }
		}

		private float AverageGroupItemLevel
		{
			get { return ScriptHelpers.GroupMembers.Where(g => g.Player!= null).Average(g => g.Player.AverageItemLevelTotal); }
		}


		[EncounterHandler((int)MobId_Nerzhul, "Ner'zhul", Mode = CallBehaviorMode.CurrentBoss)]
		public Func<WoWUnit, Task<bool>> LeaveDungeonBehavior()
		{
			bool ranOnce = false;
			return async boss =>
			{
				if (ranOnce || DungeonBuddySettings.Instance.PartyMode != PartyMode.Leader || !LootTargeting.Instance.IsEmpty())
					return false;

				if (AverageGroupItemLevel >= MinRequireGroupItemLevelForNerzhul)
					return false;

				Alert.Show(
					"Dungeonbuddy: Skip the Ner'zhul boss",
					"Dungeonbuddy has a difficult time completing the Ner'zhul boss encounter due to complex mechanics. " +
					"If you wish to stay in group and play manually then press 'Cancel'. Otherwise Dungeonbuddy will automatically leave group.",
					30,
					true,
					true,
					() => Lua.DoString("LeaveParty()"),
					null,
					"Leave",
					"Cancel");

				ranOnce = true;
				return false;
			};
		}


		private const int SpellId_Malevolence = 154442;
		private const uint MobId_OmenofDeath = 76462;
		private const uint MobId_RitualofBones = 76518;
		private const uint MobId_Nerzhul = 76407;
		private const uint GameObjectId_EntertheShadowlands = 239083;
		private const uint AreaTriggerId_RitualofBones = 6166;
		private readonly WoWPoint _nerzhulAutoPortFromLoc = new WoWPoint(1737.913, -759.2268, 235.9705);
		private readonly WoWPoint _nerzhulRoomCenterLoc = new WoWPoint(1712.156, -820.2639, 73.73562);

		private WoWUnit _selectedRitualOfBonesTarget;
		private WaitTimer _updateRitualOfBonesTimer = WaitTimer.OneSecond;
		// used for calculating the safe zone to stand at/move through when dealing with skeletons.
		private WoWPoint _selectedRitualOfBonesStartLoc, _selectedRitualOfBonesEndLoc;
		
		// http://www.wowhead.com/guide=2668/shadowmoon-burial-grounds-dungeon-strategy-guide#nerzhul
		[EncounterHandler((int) MobId_Nerzhul, "Ner'zhul")]
		public Func<WoWUnit, Task<bool>> NerzhulEncounter()
		{
			bool skeletonPhase = false;
			bool skeletonKillPhase = false;
			bool skeletonSurvivePhase = false;
			WoWUnit boss = null;

			// Don't bother avoiding the Omen of Death during kill phase it interfers with positioning
			AddAvoidObject(
				ctx => !skeletonKillPhase,
				o => 15,
				o => o.Entry == MobId_OmenofDeath);

			AddAvoidObject(
				ctx => true,
				6,
				o => o.Entry == AreaTriggerId_RitualofBones || o.Entry == MobId_RitualofBones && !skeletonKillPhase,
				o => WoWMathHelper.CalculatePointAtSide(o.Location, o.Rotation, 6, true),
				priority: AvoidancePriority.High);

			AddAvoidObject(
				ctx => true,
				o => skeletonSurvivePhase && Me.IsMoving ? 9 : 5,
				o => o.Entry == AreaTriggerId_RitualofBones || o.Entry == MobId_RitualofBones && !skeletonKillPhase,
				o => WoWMathHelper.CalculatePointAtSide(o.Location, o.Rotation, 3, true),
				priority: AvoidancePriority.High);

			AddAvoidObject(
				ctx => true,
				o => skeletonSurvivePhase && Me.IsMoving ? 9 : 5,
				o => o.Entry == AreaTriggerId_RitualofBones || o.Entry == MobId_RitualofBones && !skeletonKillPhase,
				priority: AvoidancePriority.High);

			AddAvoidObject(
				ctx => true,
				o => skeletonSurvivePhase && Me.IsMoving ? 9 : 5,
				o => o.Entry == AreaTriggerId_RitualofBones || o.Entry == MobId_RitualofBones && !skeletonKillPhase,
				o => WoWMathHelper.CalculatePointAtSide(o.Location, o.Rotation, 3, false));

			AddAvoidObject(
				ctx => true,
				6,
				o => o.Entry == AreaTriggerId_RitualofBones || o.Entry == MobId_RitualofBones && !skeletonKillPhase,
				o => WoWMathHelper.CalculatePointAtSide(o.Location, o.Rotation, 6, false),
				priority: AvoidancePriority.High);

			// Force ranged to stay away from center of room to prevent getting Omens of Death placed in center.
			AddAvoidObject(ctx => Me.IsRange() && !skeletonPhase, 15, o => o.Entry == MobId_Nerzhul && o.ToUnit().Combat);

			#region Malevolence Avoidance

			// line up a bunch of avoids to make a lone narrow cone.
			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 4,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 2));

			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 5.5f,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 6));

			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 7f,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 11));

			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 8.5f,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 17));

			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 10,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 24));

			AddAvoidObject(
				ctx => !skeletonKillPhase || Me.IsTank(),
				o => 11.5f,
				o => o.Entry == MobId_Nerzhul && o.ToUnit().CastingSpellId == SpellId_Malevolence,
				o => o.Location.RayCast(o.Rotation, 32));

			#endregion

			var unitPathDistanceSqrToRoomCenter = new Func<WoWUnit, float>(
				u => _nerzhulRoomCenterLoc.GetNearestPointOnLine(u.Location, u.Location.RayCast(u.Rotation, 100))
					.DistanceSqr(_nerzhulRoomCenterLoc));


			var unitDoesntPathNearOmenOfDeath = new Func<WoWUnit, bool>(
				u =>
				{
					var start = u.Location;
					var end = start.RayCast(u.Rotation, 30);

					return !ObjectManager.GetObjectsOfType<WoWUnit>()
							.Any(v => v.Entry == MobId_OmenofDeath && v.Location.GetNearestPointOnSegment(start, end).DistanceSqr(v.Location) < 10*10);
				});

			var skeletonTimer = new WaitTimer(TimeSpan.FromSeconds(8));

			return async npc =>
			{
				boss = npc;
				var ritualOfBones = ObjectManager.GetObjectsOfType<WoWUnit>()
					.Where(u => u.Entry == MobId_RitualofBones && u.IsAlive)
					.OrderBy(u => u.HealthPercent)
					.ToList();

				// skeletons leave behind stuff on ground athat still needs to be avoided for some time after they despawn
				if (ritualOfBones.Any())
					skeletonTimer.Reset();

				skeletonPhase = !skeletonTimer.IsFinished;
				skeletonKillPhase = ritualOfBones.Count == 6;
				skeletonSurvivePhase = skeletonPhase && !skeletonKillPhase;

				// cast heroism at start of fight after 
				if (boss.HealthPercent <= 97 && await ScriptHelpers.CastHeroism())
					return true;

				if (skeletonKillPhase)
				{
					// select the lowest HP skeleton or the one pathing the nearest to room center.
					// When selecting a new one, pick one that doesn't path near an Omen of Death.

					_selectedRitualOfBonesTarget = ritualOfBones
						.FirstOrDefault(u => u.HealthPercent < 99 && unitPathDistanceSqrToRoomCenter(u) < 25*25)
						//?? ritualOfBones.Where(unitDoesntPathNearOmenOfDeath).OrderBy(unitPathDistanceSqrToRoomCenter).FirstOrDefault()
						?? ritualOfBones.OrderBy(unitPathDistanceSqrToRoomCenter).First();

					if (_updateRitualOfBonesTimer.IsFinished)
					{
						var start = _selectedRitualOfBonesTarget.Location;
						_selectedRitualOfBonesEndLoc = start.RayCast(_selectedRitualOfBonesTarget.Rotation, 120);

						WoWPoint hitLoc;
						var hitResult = Avoidance.Helpers.MeshTraceline(start, _selectedRitualOfBonesEndLoc, out hitLoc);

						if (!hitResult.HasValue)
							return false;

						if (hitResult.Value)
							_selectedRitualOfBonesEndLoc = hitLoc;

						_selectedRitualOfBonesStartLoc = WoWMathHelper.CalculatePointBehind(start, _selectedRitualOfBonesTarget.Rotation, 120);

						hitResult = Avoidance.Helpers.MeshTraceline(start, _selectedRitualOfBonesStartLoc, out hitLoc);
						if (!hitResult.HasValue)
							return false;

						if (hitResult.Value)
							_selectedRitualOfBonesStartLoc = hitLoc;

						_updateRitualOfBonesTimer.Reset();
					}
										
					if (Me.IsMeleeDps())
						return await HandleMeleeDpsRitualOfBones(_selectedRitualOfBonesTarget);

					if (Me.IsRange())
						return await HandleRangeRitualOfBones(_selectedRitualOfBonesTarget);

					if (Me.IsTank())
						return await HandleTankRitualOfBones(_selectedRitualOfBonesTarget);
				}


				if (Me.IsTank())
				{
					if (boss.HealthPercent > 25 && boss.HealthPercent <= 97 && await ScriptHelpers.CastHeroism())
						return true;

					var tankLoc = skeletonPhase
						? Me.Location.GetNearestPointOnSegment(_selectedRitualOfBonesStartLoc, _selectedRitualOfBonesEndLoc)
						: _nerzhulRoomCenterLoc;

					var precision = skeletonPhase ? 3 : 15;

					if (!AvoidanceManager.IsRunningOutOfAvoid)
					{
						//return await ScriptHelpers.TankUnitAtLocation(tankLoc, precision);
						return await ScriptHelpers.StayAtLocationWhile(
									() => ScriptHelpers.IsViable(boss) && boss.Aggro,
									tankLoc,
									"Tank location",
									precision);
					}
				}
				return false;
			};
		}


		#region Tank

		private async Task<bool> HandleTankRitualOfBones(WoWUnit currentTarget)
		{
			var targetLoc = currentTarget.Location;

			// if the skeletions have crossed the room do nothing. we're fuked
			if (_selectedRitualOfBonesEndLoc.DistanceSqr(targetLoc) <= 5 * 5)
				return false;

			foreach (var point in ScriptHelpers.GetPointsAlongLineSegment(_selectedRitualOfBonesEndLoc, targetLoc, 2))
			{
				if (AvoidanceManager.Avoids.Any(a => a.IsPointInAvoid(point)))
					continue;

				MoveToLocationWhileFacingUnit(
					point,
					Me.CurrentTarget,
					() => ScriptHelpers.IsViable(currentTarget) && Me.CurrentTarget != null && Me.CurrentTarget.CastingSpellId != SpellId_Malevolence,
					"Moving boss to room edge");

				break;
			}

			return false;
		}

		#endregion

		#region Ranged
		private async Task<bool> HandleRangeRitualOfBones(WoWUnit target)
		{
			var myLoc = Me.Location;
			var unitLoc = target.Location;

			var nearestPoint = myLoc.GetNearestPointOnSegment(unitLoc, _selectedRitualOfBonesEndLoc);

			if (myLoc.Distance(nearestPoint) > 3*3 || unitLoc.DistanceSqr(myLoc) < 10*10)
			{
				var moveTo = nearestPoint.DistanceSqr(unitLoc) >= 30*30
					? nearestPoint
					: unitLoc.RayCast(target.Rotation, Math.Min(30, unitLoc.Distance(_selectedRitualOfBonesEndLoc)));

				await ScriptHelpers.MoveToContinue(() => moveTo, ignoreCombat:true);
				
			}
			return false;
		}

		#endregion

		#region Melee DPS

		private async Task<bool> HandleMeleeDpsRitualOfBones(WoWUnit target)
		{
			MoveToLocationWhileFacingUnit(
				target.Location.RayCast(target.Rotation, 6),
				target,
				() => ScriptHelpers.IsViable(target),
				string.Format("Staying in front of skeleton" ));

			return false;
		}

		#endregion

		#region Backstep

		private CapabilityManagerHandle _backstepNoMoveCapabilityHandle;

		private void MoveToLocationWhileFacingUnit(WoWPoint destination, WoWUnit unit, Func<bool> conditon, string reason = null)
		{
			var dumbCR = RoutineManager.Current.SupportedCapabilities == CapabilityFlags.None;
			if (dumbCR && ScriptHelpers.MovementEnabled)
			{
				ScriptHelpers.DisableMovement(conditon);
			}
			else if (!dumbCR)
			{
				if (_backstepNoMoveCapabilityHandle == null)
					_backstepNoMoveCapabilityHandle = ScriptHelpers.CombatRoutineCapabilityManager.CreateNewHandle();

				ScriptHelpers.CombatRoutineCapabilityManager.Update(
					_backstepNoMoveCapabilityHandle,
					CapabilityFlags.Movement,
					conditon,
					reason);
			}

			ScriptHelpers.StrafeManager.Move(() => destination, () => unit.Location, () =>ScriptHelpers.IsViable(unit) && conditon(), reason);
		}

		#endregion

		#endregion
	}

	#endregion

	#region Heroic Difficulty

	public class ShadowmoonBurialGroundsHeroic : ShadowmoonBurialGrounds
	{
		#region Overrides of Dungeon

		public override uint DungeonId { get { return 784; } }

		#endregion

		protected override int MinRequireGroupItemLevelForNerzhul
		{
			get { return 625; }
		}
	}

	#endregion
}