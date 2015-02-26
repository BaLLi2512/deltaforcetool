using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor;
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
using Tripper.Tools.Math;
using Vector3 = Tripper.Tools.Math.Vector3;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	#region Normal Difficulty

	public class Skyreach : Dungeon
	{
		#region Overrides of Dungeon

	
		public override uint DungeonId
		{
			get { return 779; }
		}

	    public override WoWPoint Entrance
	    {
            get { return new WoWPoint(28.13581, 2526.396, 103.606); }
	    }

        // Must talk to Shadow-Sage Iskar (Id: 82376) and click the confirmation popup to exit. 
        // Call lua func SelectGossipOption(1) to confirm
	    public override WoWPoint ExitLocation
	    {
            get { return new WoWPoint(1235.361, 1734.913, 177.1658); }
	    }

	    public override void RemoveTargetsFilter(List<WoWObject> units)
	    {
	        var isTank = Me.IsTank();
	        List<WoWUnit> ashes = null;
			units.RemoveAll(
			obj =>
			{
				var unit = obj as WoWUnit;
				if (unit == null)
				    return false;

                if (unit.Entry == MobId_SolarFlare)
			    {
			        if (isTank)
			            return true;

                    if (ashes == null)
                    {
                        ashes = ObjectManager.GetObjectsOfType<WoWUnit>()
                            .Where(u => u.Entry == MobId_PileofAsh && u.HasAura("Dormant")).ToList();
                    }
                    // Killing a Solar Flare by an ash will cause another solar flare to spawn
			        if (ashes.Any(u => u.Location.DistanceSqr(unit.Location) <= 5*5))
			            return true;
			    }
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
                    if ((unit.Entry == MobId_SolarFlare || unit.Entry == MobId_SolarZealot ) && isDps)
                        outgoingunits.Add(unit);
                }
            }
	    }

	    public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
	    {
	        var isDps = Me.IsDps();
	        foreach (var priority in units)
	        {
	            var unit = priority.Object as WoWUnit;
	            if (unit != null)
	            {
	                switch (unit.Entry)
	                {
	                    case MobId_PileofAsh:
	                        if (isDps && !unit.HasAura("Dormant"))
	                            priority.Score += 4500;
	                        break;
                        case MobId_SolarMagnifier:
                        case MobId_SolarFamiliar:
                        case MobId_SunTrinket:
                        case MobId_DefenseConstruct:
	                        if (isDps)
	                            priority.Score += 4500;
	                        break;
                        // These shield High Sage Viryx
                        case  MobId_SkyreachShieldConstruct:
                            priority.Score += 4500;
                            break;
                        // Will carry a palyer off the platform during High Sage Viryx encounter unlessed killed first.
                        case MobId_SolarZealot:
	                        priority.Score += 5000;
                            break;
	                }
	            }
			}
		}

	    public override void IncludeLootTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
	    {
	        foreach (var incomingObject in incomingObjects)
	        {
	            var gObj = incomingObject as WoWGameObject;
	            if (gObj != null)
	            {
					if ((gObj.Entry == GameObjectId_CacheofArakkoanTreasures || gObj.Entry == GameObjectId_CacheofArakkoanTreasures_Heroic)
                        && DungeonBuddySettings.Instance.LootMode != LootMode.Off
                        && gObj.CanLoot)
	                {
	                    outgoingObjects.Add(incomingObject);
	                }
	            }
	        }
	    }

	    public override void OnEnter()
	    {
	        _highSageViryxTrash_Blackspots = new List<DynamicBlackspot>
	                                         {
	                                             new DynamicBlackspot(
	                                                 () => ShouldAvoidLeftSide,
	                                                 () => LeftHighSageViryxTrashLoc,
	                                                 LfgDungeon.MapId,
	                                                 20,
	                                                 20,
	                                                 "Left Entrance Steps"),
	                                             new DynamicBlackspot(
	                                                 () => ShouldAvoidRightSide,
	                                                 () => RightHighSageViryxTrashLoc,
	                                                 LfgDungeon.MapId,
	                                                 7,
	                                                 20,
	                                                 "Right Entrance Steps"),
	                                         };
            DynamicBlackspotManager.AddBlackspots(_highSageViryxTrash_Blackspots);

	    }

        public override void OnExit()
        {
            DynamicBlackspotManager.RemoveBlackspots(_highSageViryxTrash_Blackspots);
            _highSageViryxTrash_Blackspots = null;
        }

	    #endregion

		#region Root

	    private static LocalPlayer Me { get { return StyxWoW.Me; } }

	    private static readonly WoWPoint[] WindPath =
	    {
            new WoWPoint(958.7506, 1899.613, 215.1132),
            new WoWPoint(991.1855, 1915.5, 227.9198),
            new WoWPoint(989.8868, 1928.845, 227.8823),
            new WoWPoint(993.5295, 1935.617, 227.8823),
            new WoWPoint(1000.47, 1941.263, 227.8823),
            new WoWPoint(1013.008, 1944.458, 227.8823),
            new WoWPoint(1021.958, 1941.863, 227.8823),
            new WoWPoint(1027.734, 1935.086, 227.9031),
            new WoWPoint(1031.265, 1923.598, 227.8823),
            new WoWPoint(1028.775, 1919.856, 227.7367),
            new WoWPoint(1023.285, 1923.882, 227.6823),
            new WoWPoint(1020.629, 1932.105, 227.6823),
            new WoWPoint(1014.836, 1936.723, 227.6823),
            new WoWPoint(1007.047, 1937.06, 227.6823),
            new WoWPoint(1000.579, 1933.303, 227.6823),
            new WoWPoint(997.6884, 1927.151, 227.6823),
            new WoWPoint(998.5137, 1919.573, 227.6823),
            new WoWPoint(1001.279, 1903.705, 228.0616),
            new WoWPoint(1000.151, 1889.907, 231.0617),
            new WoWPoint(998.2874, 1882.888, 235.5307),
            new WoWPoint(995.3996, 1870.825, 240.8472),
	    };

	    [EncounterHandler(0, "Root")]
	    public Func<WoWUnit, Task<bool>> RootHandler()
	    {
	        return async npc =>
	        {
	            if (Me.HasAura("Cloak"))
	            {
	                TreeRoot.StatusText = "Taking Dread Raven ride to instance entrace";
	                return true;
	            }
	            if (Me.HasAura("Wind"))
	                await FollowWindPath();
	            return false;
	        };
	    }

	    private async Task FollowWindPath()
	    {
	        var index = 1;
	        var shortestDist = float.MaxValue;
	        var myLoc = Me.Location;
	        var path = WindPath;
            // cycle to the nearest path segment
            for (int i = 1; i < path.Length; i++)
	        {
                var dist = WoWMathHelper.GetNearestPointOnLineSegment(myLoc, path[i - 1], path[i]).Distance(myLoc);
	            if (dist < shortestDist)
	            {
	                index = i;
	                shortestDist = dist;
	            }
	        }

	        var timer = WaitTimer.TenSeconds;
            timer.Reset();
            while (!Navigator.AtLocation(path.Last()) && index < path.Length)
	        {
	            if (!Me.IsAlive)
	            {
                    Logger.Write("Died. Canceling wind path follow");
	                return;
	            }

	            if (timer.IsFinished)
	            {                    
	                if (Me.GroupInfo.IsInParty)
	                {
                        Logger.Write("Got stuck while following wind path. Leaving group.");
                        Lua.DoString("LeaveParty()");
	                    return;
	                }
                    Logger.Write("Got stuck while following wind path. Stopping HB.");
                    TreeRoot.Stop();
                    return;
	            }

                if (Navigator.AtLocation(path[index]))
	            {
                    timer.Reset();
	                index++;
	            }

                var moveTo = path[index];
                WoWMovement.ClickToMove(moveTo);
	            await Coroutine.Yield();
	        }
	    }


	    #endregion

	    #region Ranjit

        #region Trash

        private const int SpellId_ThrowChakram = 169689;
        private const int SpellId_SolarHeal = 152893;
        private const int SpellId_FlashHeal = 152894;
        private const int SpellId_SolarWrath = 157020;
        private const int SpellId_FlashBang = 152953;

        private const uint AreaTriggerId_Storm = 6420;
        private const uint MobId_SolarMagnifier = 77559;
        private const uint MobId_DivingChakramSpinner = 76116;
        private const uint MobId_WhirlingDervish = 77605;
        private const uint MobId_HeraldofSunrise = 78933;
        private const uint MobId_BloodedBladefeather = 76205;
        private const uint MobId_SoaringChrakramMaster = 76132;
        private const uint MobId_BlindingSolarFlare = 79462;
        private const uint MobId_SolarFamiliar = 76097;

        private const uint AreaTriggerId_SpinningBlade = 6038;
        private const uint AreaTriggerId_SolarZone = 6743;

        [LocationHandler(1269.863, 1763.272, 177.196, 20, "Ranjit Trash Handler 1")]
        public Func<WoWPoint, Task<bool>> RanjitTrashHandler1()
        {
            var packLoc = new WoWPoint(1290.827, 1732.28, 177.1701);
            return async loc =>
            {
                if (Me.Combat || !Me.IsTank())
                    return false;
                var trash = ScriptHelpers.GetUnfriendlyNpsAtLocation(packLoc, 14).FirstOrDefault();

                if (await ScriptHelpers.PullNpcToLocation(() => trash != null, trash, loc, 5000, 2))
                    return true;
                return false;
            };
        }

        [LocationHandler(1290.912, 1747.058, 177.1656, 30, "Ranjit Trash Handler 2")]
        public Func<WoWPoint, Task<bool>> RanjitTrashHandler2()
        {
            var packLoc = new WoWPoint(1291.937, 1702.007, 177.2466);
            return async loc =>
            {
                if (Me.Combat || !Me.IsTank())
                    return false;
                var trash = ScriptHelpers.GetUnfriendlyNpsAtLocation(packLoc, 20).ToList();
                // pull the 2nd group when pat isn't around.
                if (trash.Any() && await ScriptHelpers.PullNpcToLocation(trash.Any, () => trash.Count <= 3, trash.First(), loc, loc, 5000, 2))
                    return true;
                return false;
            };
        }

        [EncounterHandler((int)MobId_DivingChakramSpinner, "Diving Chakram Spinner")]
        public Func<WoWUnit, Task<bool>> DivingChakramSpinnerHandler()
        {
            AddAvoidObject(ctx => true, 2, AreaTriggerId_SpinningBlade);
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_ThrowChakram);
        }

        [EncounterHandler((int)MobId_BloodedBladefeather, "Blooded Bladefeather")]
        public Func<WoWUnit, Task<bool>> BloodedBladefeatherHandler()
        {
            // windup attack that causes mob to charge forward, doing damage to anyone in path
            AddAvoidObject(ctx => true, 5,
                o => o.Entry == MobId_BloodedBladefeather && o.ToUnit().HasAura("Piercing Rush"),
                o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 25)));

            return async npc => false;
        }

        [EncounterHandler((int)MobId_WhirlingDervish, "Whirling Dervish")]
        public Func<WoWUnit, Task<bool>> WhirlingDervishSunHandler()
        {
            AddAvoidObject(ctx => true, 3, AreaTriggerId_Storm);

            return async npc => false;
        }

        [EncounterHandler((int)MobId_HeraldofSunrise, "Herald of Sunrise")]
        public Func<WoWUnit, Task<bool>> HeraldofSunriseHandler()
        {
            // Pull mobs out of the effect since it heals them.
            AddAvoidObject(
                ctx => Me.IsTank() && Targeting.Instance.TargetList.All(t => t.Aggro)
                    && Targeting.Instance.TargetList.Any(t => t.HasAura("Solar Zone")),
                15,
                AreaTriggerId_SolarZone);

            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarHeal, SpellId_FlashHeal);
        }

        [EncounterHandler((int)MobId_SoaringChrakramMaster, "Soaring Chrakram Master")]
        public Func<WoWUnit, Task<bool>> SoaringChrakramMasterHandler()
        {
            return async npc => false;
        }

        [EncounterHandler((int)MobId_BlindingSolarFlare, "Blinding Solar Flare")]
        public Func<WoWUnit, Task<bool>> BlindingSolarFlareHandler()
        {
            // player does aoe damage to party members when Solar Detonation expires
            AddAvoidObject(
                ctx => true,
                2,
                o => o is WoWPlayer && !o.IsMe && (o.ToPlayer().HasAura("Solar Detonation") || Me.HasAura("Solar Detonation")));

            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarWrath);
        }

        [EncounterHandler((int)MobId_SolarFamiliar, "Solar Familiar")]
        public Func<WoWUnit, Task<bool>> SolarFamiliarHandler()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_FlashBang);
        }


        #endregion

	    private const int MissileSpellId_PiercingRush = 165732;
	    private readonly int[] MissileSpellIds_Windwall = {153593, 156793};
	    private const int MissileSpellId_FourWinds = 156793;

        private const uint MobId_Ranjit = 75964;
	    private const uint AreaTriggerId_FourWinds = 6385;
	    private const uint AreaTriggerId_Windwall = 6062;
	    private const int AreaTriggerSpellId_WindWall_ClockwiseRotation = 156634;
        private const int AreaTriggerSpellId_WindWall_CounterClockwiseRotation = 156636;

	    private readonly Vector3[] WindWallRelativeAvoidPoints =
	    {
	        new Vector3(-11, 0, 0),
	        new Vector3(-9, 0, 0),
	        new Vector3(-7, 0, 0),
	        new Vector3(-5, 0, 0),
	        new Vector3(-3, 0, 0),
	        new Vector3(-1, 0, 0),
	        new Vector3(1, 0, 0),
	        new Vector3(3, 0, 0),
	        new Vector3(5, 0, 0),
	        new Vector3(7, 0, 0),
	        new Vector3(9, 0, 0),
	        new Vector3(11, 0, 0),
	    };

	    private readonly Vector3[] FourWindsRelativeAvoidPoints =
	    {
	        new Vector3(5.2072f, 34.84442f, 0),
	        new Vector3(6.40078f, 32.64315f, 0),
	        new Vector3(7.41073f, 29.98329f, 0),
	        new Vector3(7.96161f, 27.32342f, 0),
	        new Vector3(8.51249f, 24.11323f, 0),
	        new Vector3(8.51249f, 20.71961f, 0),
	        new Vector3(8.23705f, 17.60114f, 0),
	        new Vector3(7.77798f, 14.75783f, 0),
	        new Vector3(7.04348f, 12.37312f, 0),
	        new Vector3(6.03353f, 9.4381f, 0),
	        new Vector3(5.11539f, 6.96167f, 0),
	        new Vector3(3.73819f, 5.219f, 0),
	        new Vector3(1.90192f, 3.75148f, 0),
	        new Vector3(0.34109f, 2.10053f, 0),
	        new Vector3(-0.24478f, -0.87428f, 0),
	        new Vector3(2.17459f, -0.43484f, 0),
	        new Vector3(4.0441f, -2.4123f, 0),
	        new Vector3(6.02358f, -4.38975f, 0),
	        new Vector3(8.4394f, -5.81845f, 0),
	        new Vector3(11.15196f, -7.07786f, 0),
	        new Vector3(13.72155f, -7.79537f, 0),
	        new Vector3(16.91072f, -8.56438f, 0),
	        new Vector3(20.20985f, -8.56438f, 0),
	        new Vector3(22.95913f, -8.45452f, 0),
	        new Vector3(26.14829f, -8.12494f, 0),
	        new Vector3(29.5574f, -7.24608f, 0),
	        new Vector3(32.30667f, -6.36721f, 0),
	        new Vector3(34.94598f, -5.15876f, 0),
	        new Vector3(-0.24478f, -0.87428f, 0),
	        new Vector3(-1.6744f, -2.74187f, 0),
	        new Vector3(-3.214f, -4.60947f, 0),
	        new Vector3(-4.86357f, -6.36721f, 0),
	        new Vector3(-5.96328f, -8.45452f, 0),
	        new Vector3(-7.28293f, -10.98127f, 0),
	        new Vector3(-8.05273f, -13.94745f, 0),
	        new Vector3(-8.71256f, -17.35306f, 0),
	        new Vector3(-9.15244f, -20.20939f, 0),
	        new Vector3(-8.82253f, -23.06571f, 0),
	        new Vector3(-8.27267f, -25.92203f, 0),
	        new Vector3(-7.61284f, -28.99807f, 0),
	        new Vector3(-6.40316f, -31.85439f, 0),
	        new Vector3(-5.19348f, -34.60086f, 0),
	        new Vector3(-1.31156f, 0.72474f, 0),
	        new Vector3(-3.54391f, 1.98204f, 0),
	        new Vector3(-5.41342f, 3.9595f, 0),
	        new Vector3(-8.27267f, 5.49752f, 0),
	        new Vector3(-11.13192f, 7.1454f, 0),
	        new Vector3(-14.32108f, 7.91441f, 0),
	        new Vector3(-17.2903f, 8.57356f, 0),
	        new Vector3(-20.69941f, 9.01299f, 0),
	        new Vector3(-24.21848f, 8.68342f, 0),
	        new Vector3(-27.40765f, 8.02427f, 0),
	        new Vector3(-29.93698f, 7.36511f, 0),
	        new Vector3(-32.35635f, 6.48625f, 0),
	        new Vector3(-34.99565f, 5.2778f, 0),
	    };

        // Strategy Guide http://www.wowhead.com/guide=2669/skyreach-dungeon-strategy-guide#ranjit
	    [EncounterHandler(75964, "Ranjit", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> RanjitEncounter()
	    {
	        var roomCenter = new WoWPoint(1165.827, 1727.5, 189.4511);
            var rightDoorEdge = new WoWPoint(1066.295, 1778.992, 263.4299);
            var leftDoorEdge = new WoWPoint(1082.896, 1803.643, 263.1978);

            var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(new WoWPoint (1191.229, 1720.492, 189.9772), 2);

	        AddAvoidLocation(
	            ctx => true,
	            () => roomCenter,
	            30f,
	            12,
	            m => ((WoWMissile) m).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => MissileSpellIds_Windwall.Contains(m.SpellId)));

            AddAvoidObject(ctx => true, 12, AreaTriggerId_Windwall);

	        AddAvoidLocation(
	            ctx => true,
	            () => roomCenter,
	            30f,
	            4,
	            m => ((WoWMissile) m).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FourWinds));

	        AddAvoidLocation(
	            ctx => true,
	            () => roomCenter,
	            30f,
	            8,
	            m => ((WoWMissile) m).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_PiercingRush));


            // windup attack that causes mob to charge forward, doing damage to anyone in path
            AddAvoidObject(ctx => true, 4,
                o => o.Entry == MobId_Ranjit && o.ToUnit().HasAura("Piercing Rush"),
                o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 25)));

            // Commented out for now.. Causes too much movement and doesn't work its spinning counter clockwise
            //AddAvoidLocation(
            //    ctx => true,
            //    () => roomCenter,
            //    30f,
            //    o => Me.IsMoving ? 8 : 4,
            //    o => (WoWPoint)o,
            //    GetFourWindsAvoidPoints);

	        return async boss =>
	        {
                if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointInsideRoom))
                    return true;

	            if (await ScriptHelpers.TankUnitAtLocation(roomCenter, 14))
	                return true;

	            return false;
	        };
	    }

	    IEnumerable<object> GetFourWindsAvoidPoints()
	    {
	        var fourWinds = ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
	            .FirstOrDefault(a => a.Entry == AreaTriggerId_FourWinds);

            if (fourWinds == null)
                yield break;

	        var matrix = fourWinds.GetWorldMatrix();
	        foreach (var relativePoint in FourWindsRelativeAvoidPoints)
                yield return (WoWPoint)Vector3.Transform(relativePoint, matrix);
	    }

	    #endregion

        #region Araknath

	    #region Trash

	    private const int SpellId_SolarShower = 160274;
	    private const int SpellId_CraftSunTrinket = 153521;
        private const uint MobId_SunTrinket = 76094;
	    private const int SpellId_SolarStorm = 159215;

        private const uint MobId_InitiateoftheRisingSun = 79466;
        private const uint MobId_AdeptoftheDawn = 79467;
        private const uint MobId_DrivingGaleCaller = 78932;
        private const uint MobId_AdornedBladetalon = 79303;
        private const uint MobId_SkyreachArcanologist = 76376;

        private const uint AreaTriggerId_Dervish = 6117;
        private const uint AreaTriggerId_SolarStorm = 6697;

        //[LocationHandler(1131.825, 1751.89, 194.271, 20, "Araknath Trash Handler")]
        //public Func<WoWPoint, Task<bool>> AraknathTrashHandler()
        //{
        //    var packLoc = new WoWPoint(1115.23, 1781.71, 202.9953);
        //    return async loc =>
        //    {
        //        var trash = ScriptHelpers.GetUnfriendlyNpsAtLocation(packLoc, 14).FirstOrDefault();

        //        if (await ScriptHelpers.PullNpcToLocation(() => trash != null, trash, loc, 5000, 2))
        //            return true;
        //        return false;
        //    };
        //}

        [EncounterHandler((int)MobId_InitiateoftheRisingSun, "Initiate of the Rising Sun")]
        public Func<WoWUnit, Task<bool>> InitiateoftheRisingSunHandler()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarShower, SpellId_SolarHeal, SpellId_FlashHeal);
        }

        [EncounterHandler((int)MobId_AdeptoftheDawn, "Adept of the Dawn")]
        public Func<WoWUnit, Task<bool>> AdeptoftheDawnHandler()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarHeal, SpellId_FlashHeal, SpellId_CraftSunTrinket);
        }


        [EncounterHandler((int)MobId_DrivingGaleCaller, "Driving Gale-Caller")]
        public Func<WoWUnit, Task<bool>> DrivingGaleCallerHandler()
        {
            AddAvoidObject(ctx => true, 1.2f, AreaTriggerId_Dervish);

            return async npc => false;
        }

        [EncounterHandler((int)MobId_SkyreachArcanologist, "Skyreach Arcanologist")]
        public Func<WoWUnit, Task<bool>> SkyreachArcanologistHandler()
        {
            AddAvoidObject(ctx => true, 5, AreaTriggerId_SolarStorm);

            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarStorm);
        }

	    #endregion


        private const uint MobId_SkyreachSunConstructPrototype = 76142;
        private const uint MobId_Araknath = 76141;
        readonly int[] SpellIds_Smash = { 154113, 154110 };

        [EncounterHandler((int)MobId_Araknath, "Araknath", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> AraknathEncounter()
	    {
	        var roomCenter = new WoWPoint(1042.375, 1814.569, 200.1174);
            const float roomRadius = 32;

	        var randomPointsInsideRoom = new[]
	        {
	            WoWMathHelper.GetRandomPointInCircle(new WoWPoint(1057.8, 1790.674, 199.5619), 3),
	            WoWMathHelper.GetRandomPointInCircle(new WoWPoint(1070.234, 1808.342, 199.5619), 3),
	        };

	        AddAvoidObject(
	            ctx => true,
	            9f,
                o => o.Entry == MobId_Araknath && SpellIds_Smash.Contains(o.ToUnit().CastingSpellId),
	            o => o.Location.RayCast(o.Rotation, 7));

	        return async boss =>
	        {
                var randomPoint = randomPointsInsideRoom.OrderBy(l => l.DistanceSqr(Me.Location)).First();
                if (await ScriptHelpers.GetInsideCircularBossRoom(boss, roomCenter, roomRadius, randomPoint))
                    return true;

	            if (!boss.Combat)
	                return false;

                if (boss.HasAura("Energize") && Me.IsTank())
	            {
	                var construct =
	                    ObjectManager.GetObjectsOfType<WoWUnit>()
                            .FirstOrDefault(u => u.Entry == MobId_SkyreachSunConstructPrototype && u.HasAura("Energize"));

	                if (construct != null )
	                {
	                    var beamMoveTo = WoWMathHelper.GetNearestPointOnLineSegment(
	                        Me.Location,
	                        WoWMathHelper.CalculatePointFrom(construct.Location, boss.Location, 3),
	                        construct.Location);

	                    return await ScriptHelpers.StayAtLocationWhile(
	                        () => ScriptHelpers.IsViable(boss) && boss.HasAura("Energize"),
	                        beamMoveTo,
	                        null,
	                        1);
	                }
	            }

	            return false;
	        };
	    }

	    #endregion

        #region Rukhran

        #region Trash

        private const uint MobId_SkyreachSunTalon = 79093;

        [LocationHandler(978.3587, 1858.442, 218.511, 30, "Rukhkran Trash Handler")]
        public Func<WoWPoint, Task<bool>> RukhkranTrashHandler()
        {
            var leftPackLoc = new WoWPoint(930.3639, 1873.723, 213.8664);
            var rightPackLoc = new WoWPoint(952.46, 1896.429, 213.8671);
            return async loc =>
            {
                var rightPack = ScriptHelpers.GetUnfriendlyNpsAtLocation(
                    rightPackLoc,
                    12,
                    o => o.Entry == MobId_SkyreachSunTalon && o.IsAlive).FirstOrDefault();

                if (await ScriptHelpers.PullNpcToLocation(() => rightPack != null, rightPack, loc, 5000, 2))
                    return true;

                var leftPack = ScriptHelpers.GetUnfriendlyNpsAtLocation(
                    leftPackLoc,
                    12,
                    o => o.Entry == MobId_SkyreachSunTalon && o.IsAlive).FirstOrDefault();

                if (await ScriptHelpers.PullNpcToLocation(() => leftPack != null, leftPack, loc, 5000, 2))
                    return true;
                return false;
            };
        }

        #endregion

        private const uint MobId_PileofAsh = 79505;
        private const uint MobId_SolarFlare = 79505;
		private const int SpellId_Quills = 159382;
        const uint GameObjectId_CacheofArakkoanTreasures = 234164;
		const uint GameObjectId_CacheofArakkoanTreasures_Heroic = 234165;
	    private const int MissileSpellId_SummonSolarFlare = 153810;

        [EncounterHandler(76143, "Rukhran", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> RukhranEncounter()
        {
	        WoWUnit rukhran = null;

            AddAvoidLocation(
                ctx => true,
                5,
                m => ((WoWMissile) m).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_SummonSolarFlare));

            AddAvoidObject(ctx => Me.HasAura("Fixate"), o => Me.IsMoving ? 15 : 8, o => o.Entry == MobId_SolarFlare && !o.ToUnit().HasAura("Dormant"));

	        var gabcloserCapabilityHandler = CapabilityManager.Instance.CreateNewHandle();
			var pillarLosLoc = new WoWPoint (946.8605, 1881.408, 213.8669);
	        var castingQuills = new PerFrameCachedValue<bool>(() =>
				        ScriptHelpers.IsViable(rukhran) && (rukhran.CastingSpellId == SpellId_Quills || rukhran.HasAura(SpellId_Quills)));

	        return async boss =>
			{
				rukhran = boss;
				CapabilityManager.Instance.Update(
					gabcloserCapabilityHandler,
					CapabilityFlags.GapCloser,
					() => ScriptHelpers.IsViable(boss) && Targeting.Instance.FirstUnit == boss);

				if (castingQuills)
					return await ScriptHelpers.StayAtLocationWhile(() => castingQuills, pillarLosLoc, "LOS location", 3);
				return false;
			};
	    }

	    #endregion

        #region High Sage Viryx

	    #region Trash

	    private const int SpellId_ProtectiveBarrier = 152973;
	    private const int SpellId_Empower = 152917;
	    private const uint MobId_RadiantSupernova = 79463;
        private const uint MobId_DefenseConstruct = 76087;

        private static readonly WoWPoint RightHighSageViryxTrashLoc = new WoWPoint(1025.469, 1777.403, 250.2384);
        private static readonly WoWPoint LeftHighSageViryxTrashLoc = new WoWPoint(1077.552, 1849.872, 250.3618);

        // We need followers to take the ramp that trash has been cleared if running back from a wipe.
        private List<DynamicBlackspot> _highSageViryxTrash_Blackspots;

	    private readonly TimeCachedValue<bool> ShouldAvoidLeftSide = new TimeCachedValue<bool>(
	        TimeSpan.FromSeconds(5),
            () => ScriptHelpers.GetUnfriendlyNpsAtLocation(LeftHighSageViryxTrashLoc, 20, unit => unit.IsHostile).Any());

	    private readonly TimeCachedValue<bool> ShouldAvoidRightSide = new TimeCachedValue<bool>(
	        TimeSpan.FromSeconds(5),
	        () => ScriptHelpers.GetUnfriendlyNpsAtLocation(RightHighSageViryxTrashLoc, 20, unit => unit.IsHostile).Any());

        [EncounterHandler((int)MobId_DefenseConstruct, "Defense Construct")]
        public Func<WoWUnit, Task<bool>> DefenseConstructHandler()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_ProtectiveBarrier);
        }

        [EncounterHandler((int)MobId_RadiantSupernova, "Radiant Supernova")]
        public Func<WoWUnit, Task<bool>> RadiantSupernovaHandler()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_SolarWrath);
        }

        [EncounterHandler((int)MobId_SolarMagnifier, "Solar Magnifier")]
        public Func<WoWUnit, Task<bool>> SolarMagnifierHandler()
        {
            return async npc => false;
        }

	    #endregion

	    private const int SpellId_SolarBurst = 154396;
	    private const int SpellId_Shielding = 158641;
	    private const int MissileSpellId_CastDown = 156789;

        private const uint MobId_SolarZealot = 76267;
        private const uint MobId_SkyreachShieldConstruct = 76292;
        private const uint MobId_ArakkoaMagnifyingGlassFocus = 76083;
        private const uint MobId_ArakkoaMagnifyingGlass = 76285;

        private const uint AreaTriggerId_LensFlare = 6127;

        // Strategy Guide http://www.wowhead.com/guide=2669/skyreach-dungeon-strategy-guide#high-sage-viryx
        [EncounterHandler(76266, "High Sage Viryx", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> HighSageViryxEncounter()
	    {
            var platformCenter = new WoWPoint(1086.119, 1783.944, 262.1719);

            var rightDoorEdge = new WoWPoint(1066.295, 1778.992, 263.4299);
            var leftDoorEdge = new WoWPoint(1082.896, 1803.643, 263.1978);

            var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(platformCenter, 3);

            // increased avoidance on lens flare for tank to ensure boss is not tanked in them.
            AddAvoidObject(ctx => true,  6, AreaTriggerId_LensFlare);
            AddAvoidObject(ctx => true, 2, MobId_ArakkoaMagnifyingGlassFocus);

            var leftFlareRunToStart = new WoWPoint(1101.015, 1819.824, 262.1719);
            var leftFlareRunToend = new WoWPoint(1127.635, 1807.427, 262.1719);
            var rightFlareRunToStart = new WoWPoint(1063.045, 1764.506, 262.1719);
            var rightFlareRunToend = new WoWPoint(1088.384, 1737.376, 262.1719);

	        return async boss =>
	        {
                if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointInsideRoom))
                    return true;

	            if (!boss.Combat)
	                return false;

                // Move to platform center if targeted by 'CastDown'
	            if (WoWMissile.InFlightMissiles.Any(m => m.SpellId == MissileSpellId_CastDown && m.TargetGuid == Me.Guid))
	                return (await CommonCoroutines.MoveTo(platformCenter)).IsSuccessful();

                // kite the flare away from center of platform.
                var magnifyingGlass = ObjectManager.GetObjectsOfType<WoWUnit>()
                    .FirstOrDefault(u => u.Entry == MobId_ArakkoaMagnifyingGlass);

                // Unable to figure out
	            if (magnifyingGlass != null && magnifyingGlass.CurrentTargetGuid == Me.Guid)
                {
                    TreeRoot.StatusText = "Running Lens Focus away from group";
                    var nearestLeftPoint = Me.Location.GetNearestPointOnSegment(leftFlareRunToStart, leftFlareRunToend);
                    var nearestRightPoint = Me.Location.GetNearestPointOnSegment(rightFlareRunToStart, rightFlareRunToend);
                    // either go left or right, which ever is closer.
                    var moveTo = Me.Location.DistanceSqr(nearestLeftPoint) < Me.Location.DistanceSqr(nearestRightPoint)
                        ? nearestLeftPoint
                        : nearestRightPoint;

                    if (!Navigator.AtLocation(moveTo))
                    {
                        return (await CommonCoroutines.MoveTo(moveTo)).IsSuccessful();
                    }
                }

                if (await ScriptHelpers.TankUnitAtLocation(platformCenter, 15))
                    return true;

	            if (await ScriptHelpers.InterruptCast(boss, SpellId_SolarBurst))
	                return true;

	            return false;
	        };
	    }

        // Mob that spawns during encounter. Need to interrupt the 'Shielding' spell since it places a 
        // shield on boss that reduces damage by 50%.
	    [EncounterHandler((int) MobId_SkyreachShieldConstruct, "Skyreach Shield Construct")]
	    public Func<WoWUnit, Task<bool>> SkyreachShieldConstructEncounter()
	    {
	        return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_Shielding);
	    }

	    #endregion

	}

	#endregion

	#region Heroic Difficulty

    public class SkyreachHeroic : Skyreach
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 780; }
		}

		#endregion
	}

	#endregion
}