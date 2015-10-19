using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;
using Vector2 = Tripper.Tools.Math.Vector2;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class BlackhandsCrucible : BlackrockFoundry
	{
		#region Overrides of Dungeon
		public override uint DungeonId
		{
			get { return 823; }
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

					// Player can't navigate to these NPCs unless he/she is thrown up on the platform that NPC is on.
					if (unit.Entry == MobId_IronSoldier && unit.ZDiff > 12)
						return true;

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
						case MobId_IronSoldier:
							priority.Score += 5000;
							break;

						case MobId_Siegemaker:
							priority.Score += 4000;
							break;
					}
				}
			}
		}

		public override void RemoveHealTargetsFilter(List<WoWObject> objects)
		{
			var blackHandStageTwo = ScriptHelpers.IsViable(_blackHand) && _blackHand.Combat && _blackHandStage == 2;

            objects.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit == null)
						return false;

					// Remove targets that are on platform while player is not on platform, or vice versa 
					if (blackHandStageTwo && unit.ZDiff > 12)
						return true;

					return false;
				});
		}

		public override async Task<bool> HandleMovement(WoWPoint location)
		{
			return await CrucibleDoorHandler(location) || await ElevatorLogic(location) ;
		}

		#endregion


		#region Elevator Logic

		protected const uint GameObjectId_Elevator = 231014;

		private readonly WoWPoint _elevatorBottomBoardLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(443.0896f, 3493.924f, 306.6379f), 2);

		private readonly WoWPoint _elevatorBottomExitLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(431.2929, 3494.608, 306.929), 1);

		private readonly float _elevatorBottomZ = 305.4167f;

		private readonly WoWPoint _elevatorTopBoardLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(443.0896f, 3493.924f, 741.5526f), 2);

		// randomize points in order to avoid stacking of bots.
		private readonly WoWPoint _elevatorTopExitLoc =
			WoWMathHelper.GetRandomPointInCircle(new WoWPoint(457.5137f, 3494.16f, 742.5745f), 1);

		private readonly float _elevatorTopZ = 740.3314f;

		private BoundingBox3 _elevatorShaftBounds = new BoundingBox3(new Vector3(435.5273f, 3485.564f, 300),
			new Vector3(453.0355f, 3502.836f, 741.9951f));

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
				if ((elevator == null || myloc.DistanceSqr(elevatorBoardLoc) > 20 * 20
					 || (!elevatorIsResting && myloc.DistanceSqr(elevatorExitLoc) > 4 * 4)
					 && !Navigator.AtLocation(elevatorBoardLoc)))
				{
					Logger.Write("[Elevator Manager] Moving To Elevator");
					var moveResult = Navigator.MoveTo(elevatorExitLoc);
					return moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
				}

				// Get onboard of the elevator.
				if (elevatorIsResting && myloc.DistanceSqr(elevatorBoardLoc) > 1.5 * 1.5)
				{
					Logger.Write("[Elevator Manager] Boarding Elevator");
					// avoid getting stuck on lever
					Navigator.PlayerMover.MoveTowards(elevatorBoardLoc);
				}
				else if (elevatorIsResting && myloc.DistanceSqr(elevatorBoardLoc) <= 1.5 * 1.5 && !Me.TransportGuid.IsValid)
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

				elevator = (WoWGameObject)transport;
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
				case FloorLevel.Upper:
					elevatorBoardLoc = _elevatorTopBoardLoc;
					elevatorExitLoc = _elevatorTopExitLoc;
					elevatorRestingZ = _elevatorTopZ;
					break;
				case FloorLevel.Main:
					elevatorBoardLoc = _elevatorBottomBoardLoc;
					elevatorExitLoc = _elevatorBottomExitLoc;
					elevatorRestingZ = _elevatorBottomZ;
					break;
				default:
					throw new ArgumentException(string.Format("Unknown floor level: {0}", myFloorLevel));
			}
			return true;
		}

		private readonly WoWPoint _blackhandAreaCenterLoc = new WoWPoint(567.9324, 3494.572, 741.7349);
		private FloorLevel GetFloorLevel(WoWPoint loc)
		{
			if (loc.Distance2DSqr(_blackhandAreaCenterLoc) < 67 * 67 && loc.Z > 500 || loc.Z > 700)
				return FloorLevel.Upper;

			return FloorLevel.Main;
		}

		private enum FloorLevel
		{
			Main,
			Upper,
		}


		#endregion Elevator Logic

		#region Crucible Door

		private const uint GameObjectId_CrucibleDoor = 233006;
		private readonly WoWPoint _crucibleDoorRightEdge = new WoWPoint(326.6111, 3424.123, 306.7117);
		private readonly WoWPoint _crucibleDoorLeftEdge = new WoWPoint(326.7765, 3435.413, 306.7117);

		private async Task<bool> CrucibleDoorHandler(WoWPoint location)
		{
			var myLoc = Me.Location;
			if (myLoc.DistanceSqr(_crucibleDoorLeftEdge) > 15*15)
				return false;

			var door = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_CrucibleDoor);
			if (door == null || ((WoWDoor)door.SubObj).IsOpen)
				return false;

			var movePath = ((MeshNavigator) Navigator.NavigationProvider).CurrentMovePath;
			if (movePath == null || movePath.Index >= movePath.Path.Points.Length || movePath.Index < 0)
				return false;

			var waypoint = (WoWPoint)movePath.Path.Points[movePath.Index];
			var isPlayerLeftOfDoor = myLoc.IsPointLeftOfLine(_crucibleDoorLeftEdge, _crucibleDoorRightEdge);
			var isWaypointLeftOfDoor = waypoint.IsPointLeftOfLine(_crucibleDoorLeftEdge, _crucibleDoorRightEdge);

			// player and waypoint are on same side then return.
            if (isPlayerLeftOfDoor == isWaypointLeftOfDoor)
				return false;

			TreeRoot.StatusText = "Waiting on Crucible door to open";
			await CommonCoroutines.StopMoving();
			return true;
		}

		#endregion Crucible Door


		#region Root

		#endregion


		#region Blackhand

		private WoWUnit _blackHand;
		private int _blackHandStage;

		private const uint MobId_SlagBomb = 77343;
		private const uint MobId_Blackhand = 77325;
		private const uint MobId_RubblePile = 77405;
		private const uint MobId_Siegemaker = 77342;
		private const uint MobId_IronSoldier = 77665;
		private const uint MobId_MoltenSlag = 77558;
		private const uint MobId_SlagHole = 77357;

		private const int MissileSpellId_Demolition = 156496;
		private const uint AreaTriggerId_FallingAsh = 7022;
		private const uint AreaTriggerId_Blaze = 6380;
		private const uint AreaTriggerId_SlagCrater = 6330;

		[EncounterHandler((int)MobId_Blackhand, "Blackhand")]
		public Func<WoWUnit, Task<bool>> BlackhandEncounter()
		{
			AddAvoidObject(2, MobId_SlagBomb, AreaTriggerId_FallingAsh);
			AddAvoidObject(5, AreaTriggerId_Blaze);
			AddAvoidObject(ctx => true, o => o.ToUnit().CurrentTargetGuid == Me.Guid ? (Me.IsMoving ? 20 : 15) : 10,
				o => o.Entry == MobId_Siegemaker && o.ToUnit().CanSelect,
				o => o.Location.RayCast(o.Rotation, 5));
			AddAvoidObject(10, AreaTriggerId_SlagCrater);

			AddAvoidObject(ctx => true, 5, o => o.Entry == MobId_RubblePile && o.ToUnit().HasAura("Ore Visual"));
			AddAvoidLocation(ctx => true, 6, o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_Demolition));

			AddAvoidObject(ctx => ScriptHelpers.IsViable(_blackHand), 6, o => o is WoWPlayer && !o.IsMe && o.ToPlayer().HasAura("Marked for Death"),
				o => Me.Location.GetNearestPointOnSegment(o.Location, _blackHand.Location));

			AddAvoidObject(5, MobId_MoltenSlag, MobId_SlagHole);
			AddAvoidObject(5, MobId_SlagHole);

			WoWPoint markedForDeathLosLoc = WoWPoint.Zero;
			WaitTimer markedForDeathTimer = new WaitTimer(TimeSpan.FromMilliseconds(10000));
			return async npc =>
			{
				_blackHand = npc;
				_blackHandStage = _blackHand.Z > 620 ? 1 : (_blackHand.Z > 580 ? 2 : 3);

                if (Me.HasAura("Marked for Death") && _blackHandStage != 3)
				{
					if (markedForDeathTimer.IsFinished || markedForDeathLosLoc == WoWPoint.Zero)
					{
						var radius = _blackHandStage == 1 ? 7 : 12;
                        markedForDeathLosLoc = 
                            ObjectManager
								.GetObjectsOfType<WoWUnit>()
								.Where(u => _blackHandStage == 1 ?  (u.Entry == MobId_RubblePile && u.HasAura("Ore Visual")) : u.Entry == MobId_Siegemaker)
								.Select(u => WoWMathHelper.CalculatePointFrom(_blackHand.Location, u.Location, -radius))
								.OrderBy(l => Me.Location.DistanceSqr(l))
								.FirstOrDefault(l => Navigator.CanNavigateFully(Me.Location, l));

						markedForDeathTimer.Reset();
					}
					if (await ScriptHelpers.StayAtLocationWhile(
						() => Me.HasAura("Marked for Death") && markedForDeathLosLoc != WoWPoint.Zero && _blackHandStage != 3,
						markedForDeathLosLoc, "Marked for Death LOS", 1))
					{
						return true;
					}
				}
				return false;
			};
		}
		#endregion
	}
}