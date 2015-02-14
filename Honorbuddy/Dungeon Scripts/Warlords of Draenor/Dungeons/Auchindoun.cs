using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
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
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals.WoWObjects.AreaTriggerShapes;
using Tripper.Tools.Math;
using Vector2 = Tripper.Tools.Math.Vector2;
using Vector3 = Tripper.Tools.Math.Vector3;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	#region Normal Difficulty

    public class Auchindoun : Dungeon
	{
		#region Overrides of Dungeon
	
		public override uint DungeonId
		{
			get { return 820; }
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

					// unit is immune to damage when it has Claws of Argus
                    if (unit.Entry == MobId_Azzakel && unit.HasAura("Claws of Argus") && !Me.IsHealer())
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
            var isDps = Me.IsDps();
	        var isRangedDps = isDps && Me.IsRange();

            foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
                    switch (unit.Entry)
                    {
                        case MobId_BlazingTrickster:
                            if (isDps)
                                priority.Score += 3500;
                            break;
                        case MobId_SpitefulArbiter:
                        case MobId_FelborneAbyssal:
                        case MobId_TwistedMagus:
                        case MobId_Shaadum:
                        case MobId_Zipteq:
                        case MobId_Zarshuul:
                            if (isDps)
                                priority.Score += 4000;
                            break;
                        case MobId_CacklingPyromaniac:
                            if (isDps)
                                priority.Score += 4500;
                            break;
                    }
				}
			}
		}

	    #region Portal logic

        public override async Task<bool> HandleMovement(WoWPoint location)
        {
            var myLoc = Me.Location;

	        var destInTerogorsRoom = IsInTerogorsRoom(location);
			var meInTerogorsRoom = IsInTerogorsRoom(myLoc);
            var destPlatform = PlatformAreas.FirstOrDefault(p => Math.Abs(location.Z - p.AreaCenter.Z) < 15 &&  p.AreaCenter.DistanceSqr(location) < p.AreaRadiusSqr);
            var destIsOnPlatformImNotOn = destPlatform != null 
                && myLoc.DistanceSqr(destPlatform.AreaCenter) > destPlatform.AreaRadiusSqr;

            // check if we need to use portals to get to last boss.
            if (!meInTerogorsRoom && (destInTerogorsRoom || destIsOnPlatformImNotOn))
            {
                var gameObjects = ObjectManager.GetObjectsOfType<WoWGameObject>();
                // first check if we're on a platform
                var platform = PlatformAreas.FirstOrDefault(p => p.AreaCenter.DistanceSqr(myLoc) < p.AreaRadiusSqr);
                WoWGameObject portal = null;
                if (platform != null)
                {
                    portal = gameObjects.FirstOrDefault(g => g.Entry == platform.PortalId);
                    if (portal != null)
                        return await TakePortal(portal);
                    return false;
                }
                portal = gameObjects
                    .Where(g => GameObjectIds_OutsidePortals.Contains(g.Entry))
                    .OrderBy(g => g.DistanceSqr)
                    .FirstOrDefault();

                if (portal != null)
                    return await TakePortal(portal);
            }
            else if (meInTerogorsRoom && !destInTerogorsRoom)
            {
                // take portal out of last boss's room
                var portal = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .FirstOrDefault(g => g.Entry == GameObjectId_ExitPortal);
                if (portal != null)
                    return await TakePortal(portal);
            }
            return false;
        }

        private async Task<bool> TakePortal(WoWGameObject portal)
        {
            if (!Navigator.AtLocation(portal.Location))
                return (await CommonCoroutines.MoveTo(portal.Location)).IsSuccessful();

            await CommonCoroutines.StopMoving();
            portal.Interact();
            await Coroutine.Sleep(2000);
            return true;
        }

        private class PortalArea
        {
            public WoWPoint AreaCenter { get; private set; }
            public float AreaRadiusSqr { get; private set; }
            public uint PortalId { get; private set; }

            public PortalArea(uint portalId, WoWPoint areaCenter, float areaRadius)
            {
                AreaCenter = areaCenter;
                AreaRadiusSqr = areaRadius * areaRadius;
                PortalId = portalId;
            }
        }

        private readonly uint[] GameObjectIds_OutsidePortals = {231737, 231743, 231736};
        private const uint GameObjectId_ExitPortal = 231742;

        private readonly PortalArea[] PlatformAreas =
        {
            new PortalArea(
                231739,
                new WoWPoint(2002.855, 2864.003, 36.39625),
                40),

            new PortalArea(
                231740,
                new WoWPoint(1998.072, 3038.738, 36.10955),
                40),

            new PortalArea(
                231738,
                new WoWPoint(1825.547, 2868.231, 36.10955),
                40),

            new PortalArea(
                231741,
                new WoWPoint(1825.304, 3038.039, 36.10955),
                40),
        };

        #endregion

        #endregion
		
		private static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root

        [EncounterHandler(0, "Root Handler")]
        public Func<WoWUnit,Task<bool>>  RootBehavior()
        {
            return async npc =>
            {
                if (await ScriptHelpers.CancelCinematicIfPlaying())
                    return true;

                // do nothign if either bot or tank is taking a portal. Stops navigation errors.
                var tank = ScriptHelpers.Tank;
                if (tank != null && tank.HasAura("Transcend"))
                    return true;
                if (tank != null && !tank.IsMe && Me.HasAura("Transcend"))
                    return true;

                return false;
            };
        }

		#endregion

        #region Trash

        private const int SpellId_Fixate = 157168;
        private const int SpellId_FelStomp = 157173;

        private const uint MobId_SargereiArbiter = 88658;
        private const uint MobId_SargereiWarden = 77935;
        private const uint MobId_FelborneAbyssal = 79508;
        private const uint MobId_Felguard = 76259;

        private const uint AreaTriggerId_RadiantFury_Trash = 6507;
        private const uint AreaTriggerId_WardensHammer = 5631;

        [ObjectHandler(230398, "Holy Barrier", 15)]
        [ObjectHandler(230397, "Holy Barrier", 15)]
        public async Task<bool> HolyBarrierHandler(WoWGameObject gameObject)
        {
            var door = (WoWDoor) gameObject.SubObj;
            if (door.IsClosed && Targeting.Instance.IsEmpty() && BotPoi.Current.Type == PoiType.None && StyxWoW.Me.IsTank())
            {
                TreeRoot.StatusText = "Waiting for door to open";
                await CommonCoroutines.StopMoving();
                return true;
            }
            return false;
        }


        [EncounterHandler(88658, "Sargerei Arbiter")]
        public Func<WoWUnit, Task<bool>> SargereiArbiterEncounter()
        {
            AddAvoidObject(ctx => true, 2, AreaTriggerId_RadiantFury_Trash);
            return async npc => false;
        }

        [EncounterHandler(77935, "Sargerei Warden")]
        public Func<WoWUnit, Task<bool>> SargereiWardenEncounter()
        {
            AddAvoidObject(ctx => true, 3, AreaTriggerId_WardensHammer);
            return async npc => false;
        }

        [EncounterHandler((int)MobId_FelborneAbyssal, "Felborne Abyssal")]
        public Func<WoWUnit, Task<bool>> FelborneAbyssalEncounter()
        {
            // run away when fixated and not tank.
            AddAvoidObject(ctx => !Me.IsTank(), 
                10, 
                o => o.Entry == MobId_FelborneAbyssal && Me.GetAllAuras()
                    .Any(a => a.SpellId == SpellId_Fixate && a.CreatorGuid == o.Guid));

            return async npc => false;
        }


        #endregion

        #region Vigilant Kaathar

        private const int SpellId_ConsecratedLight = 153006;
        private const int MissileSpellId_HolyShield = 153002;
        private const int MissileSpellId_HallowedGround = 155646;

        private const int AreaTriggerSpellId_SanctifiedStrike_Medimum = 165064;
        private const int AreaTriggerSpellId_SanctifiedStrike_Small = 165065;
        private const int AreaTriggerSpellId_SanctifiedStrike_Large = 163559;

        private const float SanctifiedStrike_SmallRadius = 1.25f;
        private const float SanctifiedStrike_MedimumRadius = 1.875f;
        private const float SanctifiedStrike_LargeRadius =  2.5f;

        private const uint MobId_VigilantKaathar = 75839;
        private const uint AreaTriggerId_SanctifiedStrike = 5207;
        private const uint AreaTriggerId_HolyShield = 6197;
        private const uint GameObjectId_HolyShield = 225749;
        private const uint MobId_HolyShield = 76071;

	    private static WoWUnit _kaathar;

        private static WoWGuid _lastHolyShieldGuid;
        private static List<Vector2[]> _sanctifiedStrikePolygons;

		private readonly static PerFrameCachedValue<WoWUnit> HolyShield =
			new PerFrameCachedValue<WoWUnit>(
				() => ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(g => g.Entry == MobId_HolyShield));

		private static readonly Stopwatch LosTimer = new Stopwatch();

	    private static readonly PerFrameCachedValue<WoWPoint> ConsecratedLightMoveToLoc = 
			new PerFrameCachedValue<WoWPoint>(() =>
			{
				if (HolyShield == null || HolyShield.Value == null || !ScriptHelpers.IsViable(_kaathar))
					return WoWPoint.Zero;
				return CalculateConsecratedLightLosPoint(_kaathar, HolyShield);

			});

		private readonly static PerFrameCachedValue<bool> InConsecratedLightPhase =
			new PerFrameCachedValue<bool>(
				() =>
				{
					if (HolyShield == null || HolyShield.Value == null || !ScriptHelpers.IsViable(_kaathar))
					{
						if (LosTimer.IsRunning)
							LosTimer.Reset();
						return false;
					}

					if (_kaathar.CastingSpellId == SpellId_ConsecratedLight || _kaathar.HasAura("Consecrated Light"))
						return true;

					if (LosTimer.ElapsedMilliseconds < 7000)
					{
						var estimateTimeTillCast = 7000 - LosTimer.ElapsedMilliseconds;

						var timeToGetToLoc = (ConsecratedLightMoveToLoc.Value.Distance(Me.Location) / Me.MovementInfo.ForwardSpeed * 1000) + 1000;
						return estimateTimeTillCast < timeToGetToLoc;
					}
					return false;
				});


        [EncounterHandler(75839, "Vigilant Kaathar", Mode = CallBehaviorMode.Proximity)]
        public Func<WoWUnit, Task<bool>> VigilantKaatharEncounter()
        {
	        var losCRCapabilityHandle = ScriptHelpers.CombatRoutineCapabilityManager.CreateNewHandle();

            var roomCenter = new WoWPoint(1911.314, 3183.538, 30.79947);
            
            var movementTimer = WaitTimer.OneSecond;
            AddAvoidLocation(
                ctx => true,
                () => roomCenter,
                38,
                6,
                o => ((WoWMissile) o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_HolyShield));


            AddAvoidLocation(
                ctx => true,
                () => roomCenter,
                38,
                8,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_HallowedGround));
            

            // tank needs to tank boss away from the shield until it is time to hide behind it.
            AddAvoidObject(ctx => true, 1.5f, MobId_HolyShield);

            AddAvoidObject(
                ctx => !InConsecratedLightPhase && (!Me.IsMoving || !movementTimer.IsFinished),
                () => roomCenter,
                38,
                o => SanctifiedStrike_SmallRadius * 2,
                o => o is WoWAreaTrigger && ((WoWAreaTrigger)o).SpellId == AreaTriggerSpellId_SanctifiedStrike_Small,
                o => o.Location.RayCast(o.Rotation, 0.625f),
                true);

            AddAvoidObject(
                ctx => !InConsecratedLightPhase && (!Me.IsMoving || !movementTimer.IsFinished),
                () => roomCenter,
                38,
                o =>SanctifiedStrike_MedimumRadius * 2,
                o => o is WoWAreaTrigger && ((WoWAreaTrigger)o).SpellId == AreaTriggerSpellId_SanctifiedStrike_Medimum,
                 o => o.Location.RayCast(o.Rotation, 0.75f),
                true);

            AddAvoidObject(
                ctx => !InConsecratedLightPhase && (!Me.IsMoving || !movementTimer.IsFinished),
                () => roomCenter,
                38,
                o => SanctifiedStrike_LargeRadius * 2,
                o => o is WoWAreaTrigger && ((WoWAreaTrigger)o).SpellId == AreaTriggerSpellId_SanctifiedStrike_Large,
                o => o.Location.RayCast(o.Rotation, 1.5f),
                true);

			Func<bool> shouldLos = () => InConsecratedLightPhase && ConsecratedLightMoveToLoc != WoWPoint.Zero 
										&& ScriptHelpers.IsBossAlive("Vigilant Kaathar");

            var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(1876.486, 3195.826, 31.41795), 3);

            return async boss =>
			{
				_kaathar = boss;

                if (!Me.IsMoving)
                    movementTimer.Reset();
               
                if (!boss.Combat)
                {
                    if (Me.IsTank() )
                    {
                        if (!boss.CanSelect)
                        {
                            await ScriptHelpers.ClearArea(roomCenter, 40);
                            // do nothing if waiting for boss to activate.
                            return BotPoi.Current.Type == PoiType.None && Targeting.Instance.IsEmpty();
                        }

                        if (boss.DistanceSqr <= 35*35 && !ScriptHelpers.GroupMembers.All(g => g.Location.DistanceSqr(boss.Location) <= 44*44))
                        {
                            TreeRoot.StatusText = "Waiting on party members to move closer";
                            await CommonCoroutines.StopMoving();
                            return true;
                        }
                    }
                    else
                    {
                        var tank = ScriptHelpers.Tank;
                        if (tank != null && tank.Location.DistanceSqr(roomCenter) <= 44*44
                            && roomCenter.DistanceSqr(Me.Location) > 44*44)
                        {
                            await ScriptHelpers.MoveToContinue(() => randomPointInsideRoom);
                        }
                    }
                    return false;
                }

                if (roomCenter.DistanceSqr(Me.Location) > 45*45 && boss.Combat)
                {
                    TreeRoot.StatusText = "Locked out of room. Waiting for encounter to end";
                    return true;
                }

                // LOS the consecrated light at the holy shield.
                if (InConsecratedLightPhase)
                {
					// We need to make sure  CR does not try to move or face while we're LOSing. 
					ScriptHelpers.CombatRoutineCapabilityManager.Update(losCRCapabilityHandle, CapabilityFlags.Movement, shouldLos, "LOSing boss at Holy Shield");
					ScriptHelpers.CombatRoutineCapabilityManager.Update(losCRCapabilityHandle, CapabilityFlags.Facing, shouldLos, "LOSing boss at Holy Shield");

	                if (!ScriptHelpers.AtLocation(Me.Location, ConsecratedLightMoveToLoc, 1))
		                await CommonCoroutines.MoveTo(ConsecratedLightMoveToLoc, "Holy Shield");

					// return 'true' if CR does not have capabilities implemented; otherwise false so behavior can drop down to CR.
	                return !Me.IsHealer() && RoutineManager.Current.SupportedCapabilities == CapabilityFlags.None;
                }

                if (Me.IsFollower() && Me.Location.DistanceSqr(roomCenter) > 24*24)
                    return (await CommonCoroutines.MoveTo(roomCenter)).IsSuccessful();

                if (await ScriptHelpers.TankUnitAtLocation(roomCenter, 8))
                    return true;

                if (Targeting.Instance.IsEmpty() && Me.IsTank())
                    return true;
                return false;
            };
        }

        private static WoWPoint CalculateConsecratedLightLosPoint(WoWUnit boss, WoWUnit shield)
        {
            var vector = boss.Location.GetDirectionTo(shield.Location);
            var shieldLoc = shield.Location;
            var bestLoc = shieldLoc + (vector * 4);

            if (_lastHolyShieldGuid != shield.Guid)
            {
                _sanctifiedStrikePolygons = GetSanctifiedStrikePolygons(bestLoc, 15);
                _lastHolyShieldGuid = shield.Guid;
            }

            // try and find a location that is at least 1 yd away from any Sanctified Strike
            WoWPoint loc = bestLoc;
            for ( float dist=5; dist <=12; dist++)
            {
                if (_sanctifiedStrikePolygons.All(p => PointToPolygonDistance(loc, p) > 1f))
                    return loc;
                loc = shieldLoc + (vector * dist);
            }
            // we didn't find a point outside of any Sanctified Strike. 
            // Just stand in the bad stuff and see if healer can keep us alive
            return bestLoc;
        }

        /// <summary>Returns the distance to the nearest edge on a polygon. A negative value is returned if point is inside polygon</summary>
        private static float PointToPolygonDistance(WoWPoint point, Vector2[] polygon)
        {
            var poly = polygon.Select(v => new WoWPoint(v.X, v.Y, 0)).ToArray();
            float minDistToEdge = float.MaxValue;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var from = poly[j];
                var to = poly[i];
                var distToEdge = WoWMathHelper.GetNearestPointOnLineSegment(point, from, to).Distance2D(point);
                if (distToEdge < minDistToEdge)
                    minDistToEdge = distToEdge;
            }

            if (WoWMathHelper.IsPointInPoly(point, polygon))
                minDistToEdge = -minDistToEdge;
            return minDistToEdge;
        }

        private static List<Vector2[]> GetSanctifiedStrikePolygons(WoWPoint center, float radius)
        {
            var sanctifiedStrikes = ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
                .Where(a => a.Entry == AreaTriggerId_SanctifiedStrike && a.Location.Distance2DSqr(center) < radius * radius);

            var polygons = new List<Vector2[]>();
            foreach (var sanctifiedStrike in sanctifiedStrikes)
            {
                var shape = (AreaTriggerPolygon)sanctifiedStrike.Shape;
                var matrix = sanctifiedStrike.GetWorldMatrix();

                var polygon = shape.Vertices.Select(
                    v =>
                    {
                        var v3 = new Vector3(v, 0);
                        Vector3.Transform(ref v3, ref matrix, out v3);
                        return new Vector2(v3.X, v3.Y);
                    }).ToArray();
                polygons.Add(polygon);
            }
            return polygons;
        }

        #endregion

        #region Soulbinder Nyami

        private const int SpellId_MindSpike = 154415;
        private const int SpellId_ArbitersHammer = 154218;
        private const int SpellId_ArcaneBolt = 154235;

        private const uint MobId_SpitefulArbiter = 76284;
        private const uint MobId_TwistedMagus = 76296;
        private const uint MobId_MaleficDefender = 76283;
        private const uint AreaTriggerId_SoulVessel = 8137;
        private const uint AreaTriggerId_RadiantFury = 6156;
        private const uint AreaTriggerId_ArcaneBomb = 6035;

        [EncounterHandler(76177, "Soulbinder Nyami", Mode = CallBehaviorMode.Proximity)]
        public Func<WoWUnit, Task<bool>> SoulbinderNyamiEncounter()
        {
            var rightDoorEdge = new WoWPoint(1658.859, 3013.908, 35.59517);
            var leftDoorEdge = new WoWPoint(1671.567, 3010.475, 35.75983);

            var pointInsideRoom = new WoWPoint(1663.054, 3003.275, 34.98947);
            var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(pointInsideRoom, 3);

            return async boss =>
            {
                if (!boss.CanSelect || !boss.Attackable)
                    return false;

                if (!boss.Combat)
                {
                    // move inside room to avoid getting locked out.
                    if (Me.IsTank() )
                    {
                        if ( Me.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge) 
                            && !ScriptHelpers.GroupMembers.All(g => g.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge))
                            && WoWMathHelper.GetNearestPointOnLineSegment(Me.Location, leftDoorEdge, rightDoorEdge)
                                .DistanceSqr(Me.Location) > 6 * 6)
                        {
                            TreeRoot.StatusText = "Waiting on group members to enter room";
                            return true;
                        }
                    }
                    else
                    {
                        var tank = ScriptHelpers.Tank;
                        if (tank != null && tank.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge) 
                            && !Me.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge))
                        {
                            await ScriptHelpers.MoveToContinue(() => randomPointInsideRoom);
                        }
                    }
                    return false;
                }

                if (!Me.Location.IsPointLeftOfLine(leftDoorEdge, rightDoorEdge))
                {
                    TreeRoot.StatusText = "Locked out of room. Waiting for encounter to end";
                    return true;
                }

                if (await ScriptHelpers.InterruptCast(boss, SpellId_MindSpike))
                    return true;
                
                if (await ScriptHelpers.DispelGroup("Shadow Word: Pain", ScriptHelpers.PartyDispelType.Magic))
                    return true;

                var soulVesel =
                    ObjectManager.GetObjectsOfType<WoWAreaTrigger>().FirstOrDefault(a => a.Entry == AreaTriggerId_SoulVessel);

                if (soulVesel != null)
                {
                    return await ScriptHelpers.StayAtLocationWhile(
                        () => ScriptHelpers.IsViable(soulVesel),
                        soulVesel.Location,
                        "Soul Vessel",
                        3);
                }

                return false;
            };
        }

        [EncounterHandler((int)MobId_SpitefulArbiter, "SpitefulArbiter")]
        public Func<WoWUnit, Task<bool>> SpitefulArbiterEncounter()
        {
            AddAvoidObject(ctx => true, 2, AreaTriggerId_RadiantFury);
            return async npc =>
            {
                if (await ScriptHelpers.InterruptCast(npc, SpellId_MindSpike))
                    return true;

                return false;
            };
        }

        [EncounterHandler((int)MobId_TwistedMagus, "Twisted Magus")]
        public Func<WoWUnit, Task<bool>> TwistedMagusEncounter()
        {
            AddAvoidObject(ctx => true, 2, AreaTriggerId_ArcaneBomb);
            return async npc =>
            {
                if (await ScriptHelpers.InterruptCast(npc, SpellId_ArcaneBolt))
                    return true;

                return false;
            };
        }

        #endregion

        #region Azzakel

        private const int SpellId_Felblast = 178837;

        private const uint MobId_Azzakel = 75927;
        private const uint MobId_BlazingTrickster = 76220;
        private const uint MobId_CacklingPyromaniac = 76260;

        private const uint AreaTriggerId_FelPool = 6091;

        private const uint AreaTriggerId_Conflagration = 6124;

        [EncounterHandler(75927, "Azzakel")]
        public Func<WoWUnit, Task<bool>> AzzakelEncounter()
        {
            // Curtain of flame spreads to other players so don't stand near any player with it or stay away from other players if bot has it
            AddAvoidObject(ctx => true, 6, o => o is WoWPlayer && !o.IsMe 
                && (o.ToPlayer().HasAura("Curtain of Flame") || Me.HasAura("Curtain of Flame")));

            AddAvoidObject(ctx => true, 10, AreaTriggerId_FelPool);

            return async boss => false;
        }

        [EncounterHandler((int)MobId_CacklingPyromaniac, "Cackling Pyromaniac")]
        public Func<WoWUnit, Task<bool>> CacklingPyromaniacEncounter()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_Felblast);
        }

        [EncounterHandler((int)MobId_BlazingTrickster, "Blazing Trickster")]
        public Func<WoWUnit, Task<bool>> BlazingTricksterEncounter()
        {
            AddAvoidObject(ctx => true, 1.5f, AreaTriggerId_Conflagration);
            return async npc => false;
        }

        [EncounterHandler((int)MobId_Felguard, "Felguard")]
        public Func<WoWUnit, Task<bool>> FelguardEncounter()
        {
            // Run away from the frontal attack that does a knockback.
            AddAvoidObject(
                ctx => true,
                8,
                o => o.Entry == MobId_Felguard && o.ToUnit().CastingSpellId == SpellId_FelStomp,
                o => o.Location.RayCast(o.Rotation, 7));

            return async npc => false;
        }

        #endregion

        #region Teron'gor

        private readonly WoWPoint TerogorRoomCenter = new WoWPoint (1911.661, 2953.059, 16.23632);
        private const float TerogorRoomRadiusSqr = 82 * 82;

        private const int SpellId_ShadowBolt_Terongor = 156829;
        private const int MissileSpellId_ShadowBolt_Gulkosh = 156829;
        private const int SpellId_DrainLife = 156854;
        private const int SpellId_RainofFire = 156857;
        private const int SpellId_ChaosBolt = 156975;
        private const int SpellId_Incinerate = 156963;
        private const int SpellId_Wrathcleave = 159035;
        private const int SpellId_Incinerate_Gromtash = 157051;

        private const uint MobId_Shaadum = 78728;
        private const uint MobId_Gulkosh = 78437;
        private const uint MobId_Zipteq = 78734;
        private const uint MobId_DuragtheDominator = 77890;
        private const uint MobId_Zarshuul = 78735;
        private const uint MobId_GromtashtheDestructor = 77889;

        private const uint AreaTriggerId_ChaosWave = 6438;
        private const uint AreaTriggerId_RainofFire = 6422;

        private const uint GameObjectId_BenefactionsoftheAuchenai = 231241;

        [EncounterHandler((int)MobId_DuragtheDominator, "Durag the Dominator")]
        public Func<WoWUnit, Task<bool>> DuragtheDominatorEncounter()
        {
            // too big, too fast, too little damage to be worth avoiding
            // AddAvoidObject(ctx => true, 12, AreaTriggerId_ChaosWave);
            return async boss => false;
        }

        [EncounterHandler((int)MobId_Zipteq, "Zipteq")]
        public Func<WoWUnit, Task<bool>> ZipteqEncounter()
        {
            return async boss => false;
        }


        [EncounterHandler((int)MobId_Shaadum, "Shaadum")]
        public Func<WoWUnit, Task<bool>> ShaadumEncounter()
        {
            // whirlwind attack
            AddAvoidObject(ctx => !Me.IsTank(), 8, o => o.Entry == MobId_Shaadum && o.ToUnit().HasAura("Wrathstorm"));
            
            // massive frontal cleave.
            AddAvoidObject(
                ctx => true,
                8,
                o => o.Entry == MobId_Felguard && o.ToUnit().CastingSpellId == SpellId_Wrathcleave,
                o => o.Location.RayCast(o.Rotation, 7));

            return async boss => false;
        }

        [EncounterHandler((int)MobId_Gulkosh, "Gul'kosh")]
        public Func<WoWUnit, Task<bool>> GulkoshEncounter()
        {
            return async boss => await ScriptHelpers.DispelGroup("Drain Life", ScriptHelpers.PartyDispelType.Magic)
                || await ScriptHelpers.InterruptCast(boss, MissileSpellId_ShadowBolt_Gulkosh, SpellId_DrainLife);
        }

        [EncounterHandler((int)MobId_Zarshuul, "Zar'shuul")]
        public Func<WoWUnit, Task<bool>> ZarshuulEncounter()
        {
            return async boss => false;
        }

        [EncounterHandler((int)MobId_GromtashtheDestructor, "Grom'tash the Destructor")]
        public Func<WoWUnit, Task<bool>> GromtashtheDestructorEncounter()
        {
            return async boss => await ScriptHelpers.DispelGroup("Immolate", ScriptHelpers.PartyDispelType.Magic)
                || await ScriptHelpers.DispelGroup("Incinerate", ScriptHelpers.PartyDispelType.Magic)
                || await ScriptHelpers.InterruptCast(boss, SpellId_Incinerate_Gromtash);
        }


	    [EncounterHandler(77734, "Teron'gor", Mode = CallBehaviorMode.CurrentBoss)]
	    public async Task<bool> TerongorMoveToLogic()
	    {
			// Force tank to move towards last boss. Without this behavior, if tank dies and ends up at entrance but followers are still at boss that tank will 
			// wait for followers to get to him for several minutes before eventually moving to them. This is because the navigator can't 
			// generate a path to the last boss so tank does not see that the followers are along the path to last boss.
			// We are handling the portals in script because the mesh system can't handle them due start/end locations being too far apart for off-mesh connections.
			if (!Me.IsTank() 
				|| !LootTargeting.Instance.IsEmpty() 
				|| !Targeting.Instance.IsEmpty() 
				|| IsInTerogorsRoom(Me.Location))
			{
				return false;
			}

			await CommonCoroutines.MoveTo(TerogorRoomCenter);
			return true;
	    }

	    [EncounterHandler(77734, "Teron'gor")]
        public Func<WoWUnit, Task<bool>> TerongorEncounter()
        {
            // Seed of Malevolence is a dot that explodes after player dies or debug expires and spreads to those that get hit
            AddAvoidObject(ctx => true, 10, o => o is WoWPlayer && !o.IsMe
                && (o.ToPlayer().GetAllAuras().Any(a => a.Name == "Seed of Malevolence" && a.TimeLeft.TotalMilliseconds < 3000)
                || Me.GetAllAuras().Any(a => a.Name == "Seed of Malevolence" && a.TimeLeft.TotalMilliseconds < 3000)));

            AddAvoidObject(ctx => true, 10, AreaTriggerId_RainofFire);

            return async boss =>
            {
                if ( await ScriptHelpers.InterruptCast(
                            boss,
                            SpellId_ShadowBolt_Terongor,
                            SpellId_DrainLife,
                            SpellId_RainofFire,
                            SpellId_Incinerate,
                            SpellId_ChaosBolt))
                {
                    return true;
                }
                
                if (await ScriptHelpers.DispelGroup("Corruption", ScriptHelpers.PartyDispelType.Magic))
                    return true;

                if (await ScriptHelpers.DispelGroup("Immolate", ScriptHelpers.PartyDispelType.Magic))
                    return true;

                if (await ScriptHelpers.DispelGroup("Conflagrate", ScriptHelpers.PartyDispelType.Magic))
                    return true;

                if (await ScriptHelpers.DispelGroup("Drain Life", ScriptHelpers.PartyDispelType.Magic))
                    return true;

                if (await ScriptHelpers.DispelGroup("Curse of Exhaustion", ScriptHelpers.PartyDispelType.Curse))
                    return true;

                return false;
            };
        }

        [ObjectHandler(231241, "Benefactions of the Auchenai", 100)]
        public async Task<bool> BenefactionsoftheAuchenaiHandler(WoWGameObject chest)
        {
            return await ScriptHelpers.LootChest(chest, true);
        }

		private bool IsInTerogorsRoom(WoWPoint location)
		{
			return location.Z < 22 && location.DistanceSqr(TerogorRoomCenter) < TerogorRoomRadiusSqr;
		}

        #endregion

	}

	#endregion

	#region Heroic Difficulty

    public class AuchindounHeroic : Auchindoun
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 845; }
		}

		#endregion
	}

	#endregion
}