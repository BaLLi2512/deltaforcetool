using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.Helpers;
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
using Styx.WoWInternals.WoWObjects.AreaTriggerShapes;
using Tripper.Tools.Math;
using Vector2 = Tripper.Tools.Math.Vector2;
using Vector3 = Tripper.Tools.Math.Vector3;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class TheBlackForge : BlackrockFoundry
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 846; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(1491.402, 3071.209, 110.1424); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(1491.654, 2952.643, 35.23913); }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit == null)
						return false;

					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (var obj in incomingunits)
			{
				var unit = obj as WoWUnit;
				if (unit != null)
				{
					if (unit.Entry == MobId_GraspingEarth)
						outgoingunits.Add(unit);
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			var isMelee = Me.IsMelee();
			var israngedDps = !isMelee && Me.IsDps();
			
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					switch (unit.Entry)
					{
						case MobId_AknorSteelbringer:
							priority.Score += 5500;
							break;

						case MobId_CinderWolf:
							priority.Score = 6000 + ((int)(unit.HealthPercent / 10) * 10);
							break;

						case MobId_GraspingEarth:
							priority.Score = 6000 - unit.Distance;
							break;
					}
				}
			}
		}


		private List<DynamicBlackspot> _dynamicBlackspots;

		private ActionRunCoroutine _followerMovementHookBehavior;
		public override void OnEnter()
		{
			_followerMovementHookBehavior = new ActionRunCoroutine(ctx => HandleFollowerMovement());
            TreeHooks.Instance.InsertHook("Dungeonbot_FollowerMovement", 0, _followerMovementHookBehavior);
			_dynamicBlackspots = GetDynamicBlackspots().ToList();
            DynamicBlackspotManager.AddBlackspots(_dynamicBlackspots);

			base.OnEnter();
		}

		public override void OnExit()
		{
			if (_dynamicBlackspots != null)
			{
				DynamicBlackspotManager.RemoveBlackspots(_dynamicBlackspots);
				_dynamicBlackspots = null;
			}

			if (_followerMovementHookBehavior != null)
			{
				TreeHooks.Instance.RemoveHook("Dungeonbot_FollowerMovement", _followerMovementHookBehavior);
				_followerMovementHookBehavior = null;
			}

			base.OnExit();
		}

		public override bool CanNavigateFully(WoWPoint @from, WoWPoint to)
		{
			// Trick the navigation into believing Kromog can be navigatied to so the default loot behavior will loot him. 
			if (to.DistanceSqr(_kromogLoc) <= 1*1)
				return true;

            return base.CanNavigateFully(@from, to);
		}

		#region Elevator Logic

		protected const uint GameObjectId_Elevator = 231013;

		private readonly WoWPoint _elevatorBottomBoardLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(252.5892, 3499.54, 140.2807), 2);

		private readonly WoWPoint _elevatorBottomExitLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(243.4718f, 3499.109f, 141.1958f), 1);

		private readonly float _elevatorBottomZ = 139.6442f;

		private readonly WoWPoint _elevatorTopBoardLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(252.8751, 3499.738, 306.7645), 2);

		// randomize points in order to avoid stacking of bots.
		private readonly WoWPoint _elevatorTopExitLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(254.2236, 3488.808, 307.7599), 1);

		private readonly float _elevatorTopZ = 305.5395f;

		private BoundingBox3 _elevatorShaftBounds = new BoundingBox3(new Vector3(260.2494f, 3505.748f, 135),
			new Vector3(246.0173f, 3492.971f, 310));

		private BoundingBox3 _sectionBounds = new BoundingBox3(new Vector3(-40, 3337, 63), new Vector3(549, 3850, 228));

		public override async Task<bool> HandleMovement(WoWPoint location)
		{
			return await ElevatorLogic(location) || await MoveToKromog(location);
		}

		public async Task<bool> ElevatorLogic(WoWPoint destination)
		{
			var myloc = StyxWoW.Me.Location;

			var myFloorLevel = GetFloorLevel(myloc);
			var destinationFloorLevel = GetFloorLevel(destination);

			WoWPoint elevatorBoardLoc, elevatorExitLoc;
			float elevatorRestingZ;
			WoWGameObject elevator;

			var handleElevator =
				GetElevatorMoveInfo(myloc, destination, out elevatorBoardLoc, out elevatorExitLoc, out elevatorRestingZ,
					out elevator);

			if (!handleElevator)
				return false;

			var elevatorIsResting = elevator != null && Math.Abs(elevator.Z - elevatorRestingZ) <= 0.5f;

			if (elevator != null && Me.Transport == elevator)
			{
				if (elevatorIsResting && myFloorLevel == destinationFloorLevel && !_elevatorShaftBounds.Contains(destination))
				{
					Logger.Write("[Elevator Manager] Exiting Elevator");
					Navigator.PlayerMover.MoveTowards(elevatorExitLoc);
				}
			}
			else
			{
				// move to the elevator exit location
				if ((elevator == null || myloc.DistanceSqr(elevatorBoardLoc) > 20*20
				     || (!elevatorIsResting && myloc.DistanceSqr(elevatorExitLoc) > 4*4)
				     && !Navigator.AtLocation(elevatorBoardLoc)))
				{
					Logger.Write("[Elevator Manager] Moving To Elevator");
					var moveResult = Navigator.MoveTo(elevatorExitLoc);
					return moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
				}

				// Get onboard of the elevator.
				if (elevatorIsResting && myloc.DistanceSqr(elevatorBoardLoc) > 1.5*1.5)
				{
					Logger.Write("[Elevator Manager] Boarding Elevator");
					// avoid getting stuck on lever
					Navigator.PlayerMover.MoveTowards(elevatorBoardLoc);
				}
				else if (elevatorIsResting && myloc.DistanceSqr(elevatorBoardLoc) <= 1.5*1.5 && !Me.TransportGuid.IsValid)
				{
					Logger.Write("[Elevator Manager] Jumping");
					try
					{
						WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
						await Coroutine.Sleep(110);
					}
					finally
					{
						WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
					}
				}
			}
			return true;
		}


		private bool GetElevatorMoveInfo(WoWPoint myLoc, WoWPoint destination,
			out WoWPoint elevatorBoardLoc, out WoWPoint elevatorExitLoc, out float elevatorRestingZ,
			out WoWGameObject elevator)
		{
			var myFloorLevel = GetFloorLevel(myLoc);
			var destinationFloorLevel = GetFloorLevel(destination);

			if (myFloorLevel == destinationFloorLevel && !_elevatorShaftBounds.Contains(destination))
			{
				var transport = Me.Transport;
				if (transport == null || transport.Entry != GameObjectId_Elevator)
				{
					elevatorBoardLoc = elevatorExitLoc = WoWPoint.Zero;
					elevatorRestingZ = 0;
					elevator = null;
					return false;
				}

				elevator = (WoWGameObject) transport;
				if (Math.Abs(myLoc.Z - _elevatorTopZ) > Math.Abs(myLoc.Z - _elevatorBottomZ))
				{
					elevatorExitLoc = _elevatorBottomExitLoc;
					elevatorRestingZ = _elevatorBottomZ;
				}
				else
				{
					elevatorExitLoc = _elevatorTopExitLoc;
					elevatorRestingZ = _elevatorTopZ;
				}
				// We don't care about elevatorBoardLoc while riding on the elevator. 
				elevatorBoardLoc = WoWPoint.Zero;
				return true;
			}

			elevator = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_Elevator);
			switch (myFloorLevel)
			{
				case FloorLevel.Main:
					elevatorBoardLoc = _elevatorTopBoardLoc;
					elevatorExitLoc = _elevatorTopExitLoc;
					elevatorRestingZ = _elevatorTopZ;
					break;
				case FloorLevel.Lower:
					elevatorBoardLoc = _elevatorBottomBoardLoc;
					elevatorExitLoc = _elevatorBottomExitLoc;
					elevatorRestingZ = _elevatorBottomZ;
					break;
				default:
					throw new ArgumentException(string.Format("Unknown floor level: {0}", myFloorLevel));
			}
			return true;
		}

		private FloorLevel GetFloorLevel(WoWPoint loc)
		{
			if (_sectionBounds.Contains(loc))
				return FloorLevel.Lower;

			return FloorLevel.Main;
		}

		private enum FloorLevel
		{
			Main,
			Lower,
		}

		#endregion


		#endregion

		#region Root

		IEnumerable<Tuple<WoWUnit, Vector2[]>> GetBossRooms()
		{
			yield return new Tuple<WoWUnit, Vector2[]>(_franzok, _franzokRoomPoly);
			yield return new Tuple<WoWUnit, Vector2[]>(_kagraz, _kagrazRoomPoly);
			yield return new Tuple<WoWUnit, Vector2[]>(_kromog, _kromogRoomPoly);
		}

		private const uint MobId_FlameVents = 80681;
		private const uint MobId_SpinningBlade = 88008;


		[EncounterHandler(0)]
		public Func<WoWUnit, Task<bool>> RootHandler()
		{
			AddAvoidObject(5, MobId_SpinningBlade);
			AddAvoidObject(ctx => true, o => Me.IsMoving ? 12: 8, MobId_FlameVents);

			return async boss =>
			{
				return false;
			};
		}

		private static readonly WoWPoint LeftKagrazTrashLoc = new WoWPoint(37.03819, 3548.694, 130.3701);

		private static readonly TimeCachedValue<bool> ShouldLeftKagrazTrash = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(5),
			() => ScriptHelpers.GetUnfriendlyNpsAtLocation(
				LeftKagrazTrashLoc,
				20,
				unit => unit.IsHostile && Math.Abs(unit.Z - LeftKagrazTrashLoc.Z) < 8).Any());

		private IEnumerable<DynamicBlackspot> GetDynamicBlackspots()
		{
			yield return new DynamicBlackspot(
				() => ShouldLeftKagrazTrash,
				() => LeftKagrazTrashLoc,
				LfgDungeon.MapId,
				30,
				10,
				"Left Ka'graz Trash group");
		}

		private async Task<bool> HandleFollowerMovement()
		{
			if (Me.Combat)
				return false;

			foreach (var bossRoom in GetBossRooms())
			{
				// Prevent normal raid following logic from kicking in while in boss room and waiting for pull. 
				// This fixes movement conflictions between 'Raid Following' and 'GetInBossRoom' logic. 
				var waitingForBossPull = ScriptHelpers.IsViable(bossRoom.Item1) && !bossRoom.Item1.Combat &&
										 bossRoom.Item1.IsAlive && WoWMathHelper.IsPointInPoly(Me.Location, bossRoom.Item2);
				if (!waitingForBossPull)
					continue;
				TreeRoot.StatusText = string.Format("Waiting for {0} to get pulled", bossRoom.Item1.SafeName);
				return true;
			}
			return false;
		}

		#endregion

		#region Franzok and Hans'gar

		#region Trash

		private const uint MobId_IronFlametwister = 80676;

		private const uint AreaTriggerId_LavaBlast = 8201;
		private const int MissileSpellId_LavaBlast = 178177;

		[EncounterHandler((int)MobId_IronFlametwister, "Iron Flametwister")]
		public Func<WoWUnit, Task<bool>> IronFlametwisterEncounter()
		{
			AddAvoidObject(3, AreaTriggerId_LavaBlast);
			AddAvoidLocation(ctx => true, 3, o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_LavaBlast));

			return async boss => false;
		}

		#endregion

		private const int SpellId_DisruptingRoar = 160838;

		private const uint GameObjectId_Boss1RampDoor1 = 229318;
		private const uint GameObjectId_Boss1RampDoor2 = 229319;
		private const uint GameObjectId_SmartStampCollision = 231082;
		private const uint MobId_Franzok = 76974;
		private const uint MobId_StampingPresses = 78358;

		private WoWUnit _franzok;

		private Vector2[] _franzokRoomPoly =
		{
			new Vector2(98.81543f, 3459.169f),
			new Vector2(98.90372f, 3527.175f),
			new Vector2(167.6441f, 3527.184f),
			new Vector2(167.9122f, 3459.377f)
		};


		[EncounterHandler((int) MobId_Franzok, "Franzok", Mode = CallBehaviorMode.Proximity, BossRange = 100)]
		public Func<WoWUnit, Task<bool>> FranzokEncounter()
		{
			var roomEnterLoc = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(161.8833, 3494.166, 130.8692), 2);

			AddAvoidObject(8, o => o.Entry == MobId_StampingPresses && o.ToUnit().HasAura("Jump Aura"), o => o.Location.RayCast(o.Rotation, 8));
			AddAvoidObject(8, o => o.Entry == MobId_StampingPresses && o.ToUnit().HasAura("Jump Aura"), o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(8, o => o.Entry == MobId_StampingPresses && o.ToUnit().HasAura("Jump Aura"));
			AddAvoidObject(8, o => o.Entry == MobId_StampingPresses && o.ToUnit().HasAura("Jump Aura"), o => o.Location.RayCast(o.Rotation, -4));
			AddAvoidObject(8, o => o.Entry == MobId_StampingPresses && o.ToUnit().HasAura("Jump Aura"), o => o.Location.RayCast(o.Rotation, -8));

			AddAvoidObject(8, o => o.Entry == GameObjectId_SmartStampCollision, o => o.Location.RayCast(o.Rotation, 8));
			AddAvoidObject(8, o => o.Entry == GameObjectId_SmartStampCollision, o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(8, o => o.Entry == GameObjectId_SmartStampCollision);
			AddAvoidObject(8, o => o.Entry == GameObjectId_SmartStampCollision, o => o.Location.RayCast(o.Rotation, -4));
			AddAvoidObject(8, o => o.Entry == GameObjectId_SmartStampCollision, o => o.Location.RayCast(o.Rotation, -8));

			var rampDoor1 = new TimeCachedValue<WoWGameObject>(TimeSpan.FromMilliseconds(300),
				() => ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_Boss1RampDoor1));
			var ramp1Start = new WoWPoint(172.7271, 3474.362,134.7094);
			var ramp1End = new WoWPoint(172.7012, 3512.257, 134.4723);
			AddAvoidLine(ctx => ScriptHelpers.IsViable(rampDoor1) && ((WoWDoor)rampDoor1.Value.SubObj).IsClosed, 
				() => 2,
				() => ramp1Start,
				() => ramp1End,
				priority: AvoidancePriority.High);

			var rampDoor2 = new TimeCachedValue<WoWGameObject>(TimeSpan.FromMilliseconds(300),
				() => ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_Boss1RampDoor2));
			var ramp2Start =new WoWPoint(94.07433, 3526.962, 134.4724);
			var ramp2End = new WoWPoint(94.12428, 3459.167, 134.5223);
			AddAvoidLine(ctx => ScriptHelpers.IsViable(rampDoor2) && ((WoWDoor)rampDoor1.Value.SubObj).IsClosed,
				() => 2,
				() => ramp2Start,
				() => ramp2End,
				priority: AvoidancePriority.High);

			var hasBodySlam = new PerFrameCachedValue<bool>(() => Me.HasAura("Body Slam"));
			
			// Stay away from anyone targeted by body slam.
			AddAvoidObject(8, o =>
			{
				var player = o as WoWPlayer;
				return player != null && !player.IsMe && (player.HasAura("Body Slam") || hasBodySlam);
			});

			return async boss =>
			{
				_franzok = boss;
                if (await ScriptHelpers.MoveInsideBossRoom(boss, ramp1Start, ramp1End, roomEnterLoc))
					return true;

				if (!boss.Combat)
					return false;

				if (boss.CastingSpellId == SpellId_DisruptingRoar && Me.IsCasting && boss.CurrentCastTimeLeft < TimeSpan.FromMilliseconds(StyxWoW.WoWClient.Latency + 150))
				{
					SpellManager.StopCasting();
					await Coroutine.Wait(2000, () => ScriptHelpers.IsViable(boss) && boss.CastingSpellId != SpellId_DisruptingRoar);
					return true;
				}
				return false;
			};
		}

		private const uint MobId_Hansgar = 76973;
		private const uint MobId_ForgeOverdrive = 77258;
		private const uint MobId_ScorchingBurns = 78823;

		[EncounterHandler((int)MobId_Hansgar, "Hans'gar")]
		public Func<WoWUnit, Task<bool>> HansgarEncounter()
		{

			AddAvoidObject(8, o => o.Entry == MobId_ScorchingBurns && o.ToUnit().HasAura("Scorching Burns"), o => o.Location.RayCast(o.Rotation, 8));
			AddAvoidObject(8, o => o.Entry == MobId_ScorchingBurns && o.ToUnit().HasAura("Scorching Burns"), o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(8, o => o.Entry == MobId_ScorchingBurns && o.ToUnit().HasAura("Scorching Burns"));
			AddAvoidObject(8, o => o.Entry == MobId_ScorchingBurns && o.ToUnit().HasAura("Scorching Burns"), o => o.Location.RayCast(o.Rotation, -4));
			AddAvoidObject(8, o => o.Entry == MobId_ScorchingBurns && o.ToUnit().HasAura("Scorching Burns"), o => o.Location.RayCast(o.Rotation, -8));

			AddAvoidObject(8, o => o.Entry == MobId_ForgeOverdrive && o.ToUnit().HasAura("Searing Plates"), o => o.Location.RayCast(o.Rotation, 8));
			AddAvoidObject(8, o => o.Entry == MobId_ForgeOverdrive && o.ToUnit().HasAura("Searing Plates"), o => o.Location.RayCast(o.Rotation, 4));
			AddAvoidObject(8, o => o.Entry == MobId_ForgeOverdrive && o.ToUnit().HasAura("Searing Plates"));
			AddAvoidObject(8, o => o.Entry == MobId_ForgeOverdrive && o.ToUnit().HasAura("Searing Plates"), o => o.Location.RayCast(o.Rotation, -4));
			AddAvoidObject(8, o => o.Entry == MobId_ForgeOverdrive && o.ToUnit().HasAura("Searing Plates"), o => o.Location.RayCast(o.Rotation, -8));

			return async boss => false;
		}

		#endregion

		#region Flamebender Ka'graz

        private const uint GameObjectId_Boss2Door = 236845;

		private const uint MobId_AknorSteelbringer = 77337;

		[EncounterHandler((int)MobId_AknorSteelbringer, "Aknor Steelbringer")]
		public Func<WoWUnit, Task<bool>> AknorSteelbringerEncounter()
		{
			return async boss => false;
		}

		private const uint MobId_FlamebenderKagraz = 76814;
		private const uint AreaTriggerId_LavaSlash = 6229;
		private const uint MobId_EnchantedArmament = 77709;
		private const uint MobId_EnchantedArmament_Trash = 80683;
		private const uint MobId_CinderWolf = 76794;

		private WoWUnit _kagraz;
		private Vector2[] _kagrazRoomPoly =
		{
			new Vector2(170.8592f, 3714.404f),
			new Vector2(94.95706f, 3714.57f),
			new Vector2(89.82564f, 3772.632f),
			new Vector2(170.4019f, 3775.898f)
		};

		[EncounterHandler((int)MobId_FlamebenderKagraz, "Flamebender Ka'graz", Mode=CallBehaviorMode.Proximity)]
		public Func<WoWUnit, Task<bool>> FlamebenderKagrazEncounter()
		{
			var hasBlazingRadiance = new PerFrameCachedValue<bool>(() => Me.HasAura("Blazing Radiance"));

			AddAvoidObject(1, o => o.Entry == AreaTriggerId_LavaSlash, ignoreIfBlocking: true);
			AddAvoidObject(7,
				o =>
					(o.Entry == MobId_EnchantedArmament || o.Entry == MobId_EnchantedArmament_Trash) &&
					o.ToUnit().HasAura("Unquenchable Flame"));

			AddAvoidObject(ctx => Me.HasAura("Fixate"),
				o => Me.IsMoving ? 12 : 8,
				o =>
				{
					if (o.Entry != MobId_CinderWolf)
						return false;
					var aura = Me.GetAuraByName("Fixate");
					return aura != null && aura.CreatorGuid == o.Guid;
				});

			AddAvoidObject(10, o =>
			{
				var player = o as WoWPlayer;
				return player != null && !player.IsMe && (player.HasAura("Blazing Radiance") || hasBlazingRadiance);
			});

			var moltenTorrentMoveTo = new TimeCachedValue<WoWPoint>(TimeSpan.FromSeconds(1), () => ScriptHelpers.GetGroupCenterLocation(g => g.IsMelee, 10));

			var doorleftSideLoc = new WoWPoint(119.9758, 3711.172, 105.6425);
			var doorRightSideLoc = new WoWPoint(145.3081, 3711.144, 105.6676);
			var roomEnterLoc = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(133.2746,3720.729, 105.6422), 2);

			var door = new TimeCachedValue<WoWGameObject>(TimeSpan.FromMilliseconds(300),
				() => ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_Boss2Door));

			AddAvoidLine(ctx => ScriptHelpers.IsViable(door) && ((WoWDoor)door.Value.SubObj).IsClosed,
				() => 2,
				() => doorleftSideLoc,
				() => doorRightSideLoc,
				priority: AvoidancePriority.High);

			return async boss =>
			{
				_kagraz = boss;
                if (await ScriptHelpers.MoveInsideBossRoom(boss, doorleftSideLoc, doorRightSideLoc, roomEnterLoc))
					return true;

				if (!boss.Combat)
					return false;

				// Move to melee group to cause damage from Molten Torrent to divide among all players hit by it.
				return await ScriptHelpers.StayAtLocationWhile(() => Me.HasAura("Molten Torrent"), moltenTorrentMoveTo, "Molten Torrent", 5);
			};
		}

		#endregion

		#region Kromog

		private readonly WoWPoint _kromogLoc = new WoWPoint(369.026, 3625.134, 104.7347);

		private readonly WoWPoint[] _kromogEdgePath = {
			new WoWPoint(368.6474, 3652.989, 105.3),
			new WoWPoint(353.453, 3639.944, 105.4903),
			new WoWPoint(350.3905, 3628.012, 105.4661),
			new WoWPoint(354.4625, 3611.05, 105.3127),
			new WoWPoint(364.3554, 3604.973, 105.3),
		};

		private const uint MobId_GraspingEarth = 77893;
		private const uint MobId_RuneofCrushingEarth = 77844;
		private const uint MobId_Kromog = 77692;
		private const uint MobId_RipplingSmash = 78055;

		private const uint GameObjectId_Boss3Door = 236839;

		private const int SpellId_RuneofGraspingEarth = 157060;

		private const uint AreaTriggerId_RipplingSmash = 6493;
		private const uint AreaTriggerId_Reverberations = 6489;

		private const int SpellId_Slam = 156704;
		private WoWUnit _kromog;

		private Vector2[] _kromogRoomPoly =
		{
			new Vector2(290.068f, 3584.528f),
			new Vector2(291.0554f, 3670.705f),
			new Vector2(327.5301f, 3707.03f),
			new Vector2(356.789f, 3694.377f),
			new Vector2(369.7462f, 3650.96f),
			new Vector2(367.086f, 3606.571f),
			new Vector2(342.8461f, 3561.26f),
			new Vector2(306.5781f, 3565.226f),
			new Vector2(289.9159f, 3584.74f)
		};

		// Range should stack on other range and melee stack on other melee
		[EncounterHandler((int)MobId_Kromog, "Kromog", Mode = CallBehaviorMode.Proximity, BossRange = 100)]
		public Func<WoWUnit, Task<bool>> KromogEncounter()
		{
			var ripplingSmash =
				new PerFrameCachedValue<WoWAreaTrigger>(
					() => ObjectManager.GetObjectsOfType<WoWAreaTrigger>().FirstOrDefault(a => a.Entry == AreaTriggerId_RipplingSmash));

			AddAvoidObject(18,
				o => o.Entry == MobId_Kromog && o.ToUnit().CastingSpellId == SpellId_Slam,
				o => o.ToUnit().CurrentTarget.Location);

			AddAvoidObject(6, MobId_RuneofCrushingEarth);
			AddAvoidObject(2.5f, AreaTriggerId_Reverberations);

			AddAvoidLine(ctx => ScriptHelpers.IsViable(ripplingSmash), () => 5,
				() => GetRipplingSmashPositionOffset(ripplingSmash, (float)Math.PI / 2),
				() => GetRipplingSmashPositionOffset(ripplingSmash, (float)-Math.PI / 2));

			var graspingRuneMoveTo = new PerFrameCachedValue<WoWPoint>(
				() =>
				{
					if (!ScriptHelpers.IsViable(_kromog))
						return WoWPoint.Zero;

					if (!_kromog.HasAura("Rune of Grasping Earth") && _kromog.CastingSpellId != SpellId_RuneofGraspingEarth)
						return WoWPoint.Zero;

					return ObjectManager.GetObjectsOfType<WoWUnit>()
						.Where(u => u.Entry == MobId_GraspingEarth &&
								!Me.RaidMembers.Any(r => !r.IsMe && r.IsAlive && r.Location.Distance2DSqr(u.Location) < 3*3))
						.OrderBy(u => u.DistanceSqr)
						.Select(u => WoWMathHelper.GetRandomPointInCircle(u.Location, 0.3f))
						.FirstOrDefault();
				});

			var rangeGroupLoc = new TimeCachedValue<WoWPoint>(TimeSpan.FromSeconds(2),
				() => ScriptHelpers.GetGroupCenterLocation(g => g.IsRange, 20));

			var doorleftSideLoc = new WoWPoint(291.5191, 3637.857, 104.8518);
			var doorRightSideLoc = new WoWPoint(290.5601, 3618.553, 104.8518);
			var roomEnterLoc = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(299.285, 3628.486, 104.7858), 3);

			var door = new TimeCachedValue<WoWGameObject>(TimeSpan.FromMilliseconds(300),
				() => ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_Boss3Door));

			AddAvoidLine(ctx => ScriptHelpers.IsViable(door) && ((WoWDoor)door.Value.SubObj).IsClosed,
				() => 2,
				() => doorleftSideLoc,
				() => doorRightSideLoc,
				priority: AvoidancePriority.High);

			return async boss =>
			{
				_kromog = boss;
				if (await ScriptHelpers.MoveInsideBossRoom(boss, doorleftSideLoc, doorRightSideLoc, roomEnterLoc))
					return true;

				if (!boss.Combat)
					return false;

				if (await ScriptHelpers.StayAtLocationWhile(() => graspingRuneMoveTo != WoWPoint.Zero, graspingRuneMoveTo, "Grasping Earth", 1))
					return true;

				return await ScriptHelpers.StayAtLocationWhile(() => Me.IsRange() && graspingRuneMoveTo == WoWPoint.Zero, rangeGroupLoc, "Ranged group");
			};
		}

		private WoWPoint GetRipplingSmashPositionOffset(WoWAreaTrigger ripplingSmash, float rotationAngle)
		{
			var rotation = WoWMathHelper.NormalizeRadian(ripplingSmash.Rotation + rotationAngle);
			var shape = (AreaTriggerBox) ripplingSmash.Shape;
			var location = ripplingSmash.Location.RayCast(rotation, shape.CurrentExtents.Y);
			// Move the point in front because object is moving so bot needs to move out of the way
			rotation = WoWMathHelper.NormalizeRadian(ripplingSmash.Rotation - rotationAngle);
			return location.RayCast(rotation, 10);
		}

		private async Task<bool> MoveToKromog(WoWPoint destination)
		{
			if (destination.DistanceSqr(_kromogLoc) > 1*1)
				return false;
			return (await CommonCoroutines.MoveTo(GetKromogMoveTo(Me.Location))).IsSuccessful();
		}

		private WoWPoint GetKromogMoveTo(WoWPoint location)
		{
			var edgePoints = _kromogEdgePath
				.Take(_kromogEdgePath.Length -1)
				.Select((p, i) => location.GetNearestPointOnSegment(_kromogEdgePath[i], _kromogEdgePath[i + 1]));

			return edgePoints.OrderBy(location.DistanceSqr).First();
		} 

		#endregion

	}

}