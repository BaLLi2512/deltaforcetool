
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Profiles.Handlers;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	#region Normal Difficulty

	public class ScarletHalls : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 163; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(2864.618, -823.2136, 160.3323); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(820.4927, 614.8466, 13.50341); }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit != null)
					{
						if (unit.Entry == ObedientHound && unit.Location.DistanceSqr(_houndMasterRoomCenter) > 13 * 13)
							return true;
						// ignore Commander Lindon or the master archers if they don't have a target and I don't have an archery target on.
						if ((unit.Entry == CommanderLindonId || unit.Entry == MasterArcher) && StyxWoW.Me.IsTank() && !StyxWoW.Me.HasAura("Heroic Defense") &&
							!unit.GotTarget)
							return true;

						if (unit.Entry == ScarletGuardianId || unit.Entry == SergeantVerdoneId)
							return true;

						if (unit.Entry == ScarletDefender)
						{
							if (StyxWoW.Me.IsMelee() && unit.ZDiff > 3)
								return true;

							bool harlanIsSpinning = _armsmasterHarlan != null && _armsmasterHarlan.IsValid &&
													(_armsmasterHarlan.HasAura("Blades of Light") || _armsmasterHarlan.CastingSpellId == BladesOfLightSpellId);

							if (harlanIsSpinning && (StyxWoW.Me.Z > 2 || _armsmasterHarlan.ZDiff < 5 || !unit.InLineOfSpellSight) )
								return true;

							if (!harlanIsSpinning)
								return true;

							

							//// ignore them if they're not in melee range while waiting at top of stairs.
							//if (StyxWoW.Me.Z > 4 && unit.Distance > 4.5)
							//    return true;
							//if (StyxWoW.Me.Z < 2 && unit.Z > 2 || unit.Distance > 8)
							//    return true;
						}
						if (unit.Entry == ArmsmasterHarlan && (unit.HasAura("Blades of Light") || unit.CastingSpellId == BladesOfLightSpellId) && !StyxWoW.Me.IsHealer())
							return true;
					}
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
					if (unit.Entry == ScarletCannoneer)
					{
						if (Me.IsTank() && unit.Distance <= 40)
							outgoingunits.Add(unit);
						else
						{
							var tank = ScriptHelpers.Tank;
							if (tank != null && tank.IsAlive && tank.CurrentTargetGuid == unit.Guid)
								outgoingunits.Add(unit);
						}
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
					if (unit.Entry == ObedientHound && unit.Location.DistanceSqr(_houndMasterRoomCenter) <= 15 * 15)
						priority.Score += 400;

					if (unit.Entry == ScarletScholar && StyxWoW.Me.IsDps())
						priority.Score += 400;

					// if (unit.Entry == ScarletDefender && unit.HasAura("Unarmored") && unit.ZDiff < 2 && StyxWoW.Me.IsDps())
					//      priority.Score += 500;
				}
			}
		}

        public override void IncludeLootTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
        {
            foreach (var obj in incomingObjects)
            {
                var gObj = obj as WoWGameObject;
                if (gObj != null)
                {
                    if (obj.Entry == GoldCoinId && gObj.DistanceSqr < 30 * 30
                        && !ScriptHelpers.WillPullAggroAtLocation(gObj.Location) && gObj.CanUse()
                        && DungeonBuddySettings.Instance.LootMode != LootMode.Off)
                    {
                        outgoingObjects.Add(obj);
                    }
                }
            }
        }

		#endregion

		private const int LootSparklesAuraId = 113628;

		private const uint CommanderLindonId = 59191;
		private const uint MasterArcher = 59175;
		private const uint ObedientHound = 59309;
		private const uint ArmsmasterHarlan = 58632;
		private const uint ScarletScholar = 59372;
		private const uint ScarletCannoneer = 59293;
		private const uint ScarletDefender = 58998;
		private const int BladesOfLightSpellId = 111216;
		private const uint ScarletGuardianId = 59299;
		private const uint SergeantVerdoneId = 59302;

		private readonly WoWPoint _houndMasterRoomCenter = new WoWPoint(1009.557, 513.6875, 12.9061);
		private WoWUnit _armsmasterHarlan;

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		private WoWUnit ReinforcedArcheryTarget
		{
			get
			{
				return
					ObjectManager.GetObjectsOfType<WoWUnit>()
								 .Where(u => u.Entry == 59163 && u.DistanceSqr <= 25 * 25 && u.HasAura(LootSparklesAuraId))
								 .OrderBy(u => u.DistanceSqr)
								 .FirstOrDefault();
			}
		}

		#region Root

	    private const uint GoldCoinId = 210894;

		[EncounterHandler(64738, "Hooded Crusader", Mode = CallBehaviorMode.Proximity)]
		[EncounterHandler(64764, "Hooded Crusader", Mode = CallBehaviorMode.Proximity)]
		public async Task<bool> QuestPickupTurninLogic(WoWUnit unit)
		{
			if (unit.HasQuestAvailable())
				return await ScriptHelpers.PickupQuest(unit);
			if (unit.HasQuestTurnin())
				return await ScriptHelpers.TurninQuest(unit);
			return false;
		}

		#endregion


		#region Commander Lindon

		const uint ExplodingShotStalker = 59683;

		[EncounterHandler(59191, "Commander Lindon", BossRange = 100, Mode = CallBehaviorMode.Proximity)]
		public Func<WoWUnit,Task<bool>> CommanderLindonLogic()
		{
			AddAvoidObject(
				ctx => !StyxWoW.Me.HasAura("Heroic Defense") && StyxWoW.Me.IsTank() || !StyxWoW.Me.IsTank(),
				7,
				ExplodingShotStalker);

			return async boss =>
			{
				if (Me.HasAura("Heroic Defense"))
					return (await CommonCoroutines.MoveTo(boss.Location, "Commander Lindon")).IsSuccessful();
				// pickup a shield
				if (!Me.IsHealer() && (Targeting.Instance.IsEmpty() || boss.Combat)
					&& (Me.IsTank() || ScriptHelpers.Tank.HasAura("Heroic Defense")))
				{
					var shield = ReinforcedArcheryTarget;
					if (shield != null)
					{
						if (!shield.WithinInteractRange)
							return (await CommonCoroutines.MoveTo(shield.Location, shield.SafeName)).IsSuccessful();
						shield.Interact();
						await ScriptHelpers.SleepForRandomReactionTime();
						return true;
					}
				}
				return false;
			};
		}

		#endregion

		#region Houndmaster Braun
		WoWUnit _houndmasterBraun ;

		[EncounterHandler(59303, "Houndmaster Braun")]
		public Func<WoWUnit,Task<bool>> HoundmasterBraunBehavior()
		{
			AddAvoidObject(
				ctx => _houndmasterBraun != null && _houndmasterBraun.IsValid && _houndmasterBraun.Combat,
				() => _houndMasterRoomCenter,
				10,
				3,
				u => u is WoWPlayer && ((WoWPlayer)u).HasAura("Piercing Throw") && !u.IsMe,
				o =>
				{
					var loc = o.Location;
					return StyxWoW.Me.Location.GetNearestPointOnSegment(loc, _houndmasterBraun.Location);
				});

			AddAvoidObject(ctx => true, 8, o => o.Entry == SergeantVerdoneId && o.ToUnit().IsAlive);

			return async boss =>
			{
				_houndmasterBraun = boss;
				if (Me.Location.Distance(_houndMasterRoomCenter) > 10)
					return (await CommonCoroutines.MoveTo(_houndMasterRoomCenter, "Houndmaster Brawn")).IsSuccessful();
				if (!Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.CurrentTargetGuid == Me.Guid)
				{
					if (await ScriptHelpers.TankUnitAtLocation(_houndMasterRoomCenter, 10))
						return true;
				}
				return false;
			};
		}

		#endregion


	    [EncounterHandler(58632, "Armsmaster Harlan", BossRange = 60, Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> ArmsmasterHarlanBehavior()
	    {
	        var roomCenter = new WoWPoint(1206.436, 443.9618, 0.9878645);
	        var leftMovetoLoc = new WoWPoint(1204.795, 457.1326, 1.071114);
	        var rightMovetoLoc = new WoWPoint(1204.293, 430.942, 1.072392);
	        var bladesOfLightMoveToTopLoc = new WoWPoint(1218.714, 443.9204, 6.082577);
	        var rightDoorEdge = new WoWPoint(1175.316, 441.0984, 13.48874);
	        var leftDoorEdge = new WoWPoint(1175.265, 447.1639, 13.50381);
	        var pointInsideRoom = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(1185.541, 444.0585, 12.34396), 3);
	        var frontalAngleSpan = new ScriptHelpers.AngleSpan(0, 160);
	        bool runInsideRoom = false;

		    Func<WoWPoint, bool> isInsideRoom = loc => loc.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge) && loc.DistanceSqr(roomCenter) < 35 * 35;

	        return async boss =>
	        {
	            _armsmasterHarlan = boss;
	            bool isCastingBladesOfLight = _armsmasterHarlan.HasAura("Blades of Light") 
                    || _armsmasterHarlan.CastingSpellId == BladesOfLightSpellId;

	            var leader = ScriptHelpers.Leader;
	            if (!runInsideRoom && leader != null && !leader.IsMe &&
					isInsideRoom(leader.Location)
	                && !isInsideRoom(Me.Location) &&
	                BotPoi.Current.Type == PoiType.None)
	            {
	                runInsideRoom = true;
	            }

	            // Get inside room to avoid getting locked outside and keep running until we arrive at our ranomized point inside door
	            if (runInsideRoom && !Navigator.AtLocation(pointInsideRoom) &&
	                (await CommonCoroutines.MoveTo(pointInsideRoom)).IsSuccessful())
	            {
	                return true;
	            }

	            runInsideRoom = false;

	            // tank should wait some distance from door.
	            if (leader != null && leader.IsMe && isInsideRoom(Me.Location) &&
	                (BotPoi.Current.Type == PoiType.None || BotPoi.Current.Type == PoiType.Kill && BotPoi.Current.AsObject == boss)
					&& !Me.Combat && Me.Location.GetNearestPointOnLine(leftDoorEdge, rightDoorEdge).DistanceSqr(Me.Location) > 7*7
	                && !ScriptHelpers.GroupMembers.All(
	                    g => g.Guid == Me.Guid || isInsideRoom(g.Location)))
	            {
	                await CommonCoroutines.StopMoving();
	                return true;
	            }

	            if (!Me.IsTank() && !isCastingBladesOfLight && StyxWoW.Me.Z < 2
	                && await ScriptHelpers.AvoidUnitAngles(boss, frontalAngleSpan))
	            {
	                return true;
	            }
	            if (Me.IsTank() && !isCastingBladesOfLight &&
	                await ScriptHelpers.TankFaceUnitAwayFromGroup(boss, 8))
	                return true;

	            if (boss.HasAura("Berserker Rage") && boss.Auras["Berserker Rage"].StackCount >= 4
	                &&
	                await ScriptHelpers.DispelEnemy("Berserker Rage", ScriptHelpers.EnemyDispelType.Enrage, boss))
	            {
	                return true;
	            }

	            // dodge blades of light

	            if (isCastingBladesOfLight)
	            {
	                if (boss.Location.DistanceSqr(roomCenter) <= 2*2 &&
	                    Me.Location.DistanceSqr(bladesOfLightMoveToTopLoc) > 3*3)
	                {
	                    return
	                        (await
	                            CommonCoroutines.MoveTo(bladesOfLightMoveToTopLoc, "Running from Blades Of Light"))
	                            .IsSuccessful();
	                }

	                if (boss.Location.DistanceSqr(roomCenter) > 2*2 && Me.Location.DistanceSqr(roomCenter) > 3*3)
	                {
	                    return
	                        (await CommonCoroutines.MoveTo(roomCenter, "Moving back to center of room"))
	                            .IsSuccessful();
	                }

	                // do nothing if tank and waiting for something to attack.
	                if (Me.IsTank() && Targeting.Instance.IsEmpty())
	                    return true;
	            }

	            // move off stairs.
	            if (Me.Z >= 2f && !isCastingBladesOfLight && boss.Combat && boss.Z < 2)
	            {
	                var loc = Me.Location.DistanceSqr(leftMovetoLoc) < Me.Location.DistanceSqr(rightMovetoLoc)
	                    ? leftMovetoLoc
	                    : rightMovetoLoc;
	                return (await CommonCoroutines.MoveTo(loc, "Moving off stairs")).IsSuccessful();
	            }
	            return false;
	        };
	    }

	    #region Flameweaver Koegler

		private const uint FlameweaverKoeglerId = 59150;

		[EncounterHandler(59150, "Flameweaver Koegler")]
		public Composite FlameweaverKoeglerBehavior()
		{
			var flamebreathMoveLoc = new WoWPoint(1300.889, 545.713, 12.80321);
			const int dragonsBreath = 113641;
			const int pyroBlastId = 113690;
			const uint BookCase = 59155;
			WoWUnit boss = null;
			AddAvoidObject(
				ctx => true,
				() => flamebreathMoveLoc,
				10,
				15,
				u => u.Entry == BookCase && ((WoWUnit) u).HasAura("Burning Books"));

			AddAvoidObject(ctx => true, 5, 
				o => o.Entry == FlameweaverKoeglerId && o.ToUnit().CastingSpellId == dragonsBreath,
				o => o.Location.RayCast(o.Rotation, 3));

			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
					new Decorator(ctx => boss.IsSafelyFacing(Me, 200) && boss.CastingSpellId == dragonsBreath,
						ScriptHelpers.GetBehindUnit(ctx => true, ctx => boss)),
					ScriptHelpers.CreateDispelEnemy("Quickened Mind", ScriptHelpers.EnemyDispelType.Magic, ctx => boss),
					ScriptHelpers.CreateInterruptCast(ctx => boss, pyroBlastId),
					// move close to boss so when she does the Dragons breath abilty it's easier to dodge
					new Decorator(ctx => boss.DistanceSqr > 15*15, new Action(ctx => Navigator.MoveTo(boss.Location)))
				);
		}

		#endregion

	}

	#endregion

	#region Heroic Difficulty

	public class ScarletHallsHeroic : ScarletHalls
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 473; }
		}

		#endregion
	}

	#endregion

}