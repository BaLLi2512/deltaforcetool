using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Bars;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals.WoWObjects.AreaTriggerShapes;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	#region Normal Difficulty

	public class IronDocks : WoDDungeon
    {
        #region Overrides of Dungeon


        public override uint DungeonId { get { return 821; } }

        public override WoWPoint Entrance { get { return new WoWPoint(8849.521, 1352.264, 98.26431); } }

        public override WoWPoint ExitLocation { get { return new WoWPoint(6749.356, -538.567, 4.925448); } }

        public override void RemoveTargetsFilter(List<WoWObject> units)
        {
            units.RemoveAll(
                ret =>
                {
                    var unit = ret as WoWUnit;
                    if (unit == null)
                        return false;

                    // Attacks while Reckless Provocations is active will cause player to get feared.
                    if (unit.Entry == MobId_FleshrenderNokgar
                        && (unit.HasAura("Reckless Provocation") || unit.CastingSpellId == SpellId_RecklessProvocation))
                    {
                        return true;
                    }

                    // damaging units with this aura will trigger an AOE heal
                    if (unit.HasAura("Sanguine Sphere"))
                        return true;

                    if (unit.Entry == MobId_Bombsquad)
                        return true;

                    return false;
                });
        }

        public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
        {
            foreach (var obj in incomingunits)
            {
                var unit = obj as WoWUnit;
                if (unit != null) {}
            }
        }

        public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            var isDps = Me.IsDps();
            var isRangeDps = isDps && Me.IsRange();
            var isMeleeDps = isDps && !isRangeDps;

            foreach (var priority in units)
            {
                var unit = priority.Object as WoWUnit;
                if (unit != null)
                {
                    switch (unit.Entry)
                    {
                        case MobId_Oshir:
                            if (unit.HasAura("Feeding Frenzy"))
                                priority.Score += 5000;
                            break;
                        case MobId_RavenousWolf:
                            if (isDps)
                                priority.Score += isMeleeDps ? 4000 : 3500;
                            break;
                        case MobId_RylakSkyterror:
                            if (isDps)
                                priority.Score += isRangeDps ? 4000 : 3500;
                            break;
                        case MobId_Koramar:
                            if (isDps)
                                priority.Score += 4000;
                            break;
                        case MobId_Zoggosh:
                            if (isDps)
                                priority.Score += 3500;
                            break;
                        case MobId_Skulloc:
                            if (InCannonBarragePhase(unit))
                                priority.Score += 5000;
                            break;
                    }
                }
            }
        }


        #endregion

        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        #region Root


        #endregion

		#region Garrison Inn Quests

		// Budd's Gambit 
		[ObjectHandler(237478, "Very Shiny Thing", ObjectRange = 30)]
		public async Task<bool> VeryShinyThingHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 40);
		}

		// Feeling A Bit Morose
		[ObjectHandler(237462, "Horribly Acidic Solution", ObjectRange = 100)]
		public async Task<bool> HorriblyAcidicSolutionHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 120);
		}

		// The Brass Compass
		[ObjectHandler(237463, "Strange Brass Compass", ObjectRange = 100)]
		public async Task<bool> StrangeBrassCompassHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 120);
		}

		#endregion

        #region Trash

        private const int SpellId_Bladestorm = 167232;
        private const int MissileSpellId_HighExplosiveGrenade = 178298;
        private const int MissileSpellId_HatchetToss = 173112;
        private const int MissileSpellId_ThrowGatecrasher = 172952;
        private const int MissileSpellId_FlamingArrows = 173148;
        private const int MissileSpellId_LavaBarrage = 173482;

        private readonly int[] MissileSpellIds_LavaBlast =
        {
            173516,
            173529,
            173530
        };

        private const uint MobId_GromkarTechnician = 81432;
        private const uint MobId_GromkarBattlemaster = 83025;
        private const uint MobId_GromkarFlameslinger = 81279;
        private const uint MobId_GromkarDeckhand = 83762;
        private const uint MobId_WhirlingChains = 86565;
        private const uint MobId_IronwingFlamespitter = 83389;
        private const uint MobId_SiegemasterOlugar = 83026;
        private const uint MobId_SiegemasterRokra = 84028;
        private const uint MobId_PitwardenGwarnok = 84520;

        private const uint AreaTriggerId_LavaBarrage = 7898;
        private const uint AreaTriggerId_LavaBlast = 7899;
        private const uint AreaTriggerId_ThrowGatecrasher = 7858;
        private const uint AreaTriggerId_FlamingArrows = 7870;
        private const uint AreaTriggerId_GreaseVial = 7847;
        [EncounterHandler(81432, "Gromkar Technician")]
        public Func<WoWUnit, Task<bool>> GromkarTechnicianEncounter()
        {
            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_HighExplosiveGrenade));

            AddAvoidObject(ctx => true, 5, MobId_WhirlingChains, AreaTriggerId_GreaseVial);
            return async npc => false;
        }

        [EncounterHandler(81279, "Gromkar Flameslinger")]
        public Func<WoWUnit, Task<bool>> GromkarFlameslingerEncounter()
        {
            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FlamingArrows));

            AddAvoidObject(ctx => true, 1.5f, AreaTriggerId_FlamingArrows);
            return async npc => false;
        }

        [EncounterHandler(83025, "Gromkar Battlemaster")]
        [EncounterHandler(84520, "Pitwarden Gwarnok")]
        public Func<WoWUnit, Task<bool>> GromkarBattlemasterEncounter()
        {
            AddAvoidObject(
                ctx => Me.IsFollower(),
                8,
                o =>( o.Entry == MobId_GromkarBattlemaster || o.Entry == MobId_PitwardenGwarnok) && o.ToUnit().CastingSpellId == SpellId_Bladestorm);

            return async npc => false;
        }

        [EncounterHandler(83762, "Gromkar Deckhand")]
        public Func<WoWUnit, Task<bool>> GromkarDeckhandEncounter()
        {
            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_HatchetToss));

            return async npc => false;
        }

	    [EncounterHandler(81247, "Iron Star", Mode = CallBehaviorMode.Proximity, BossRange = 100)]
	    public Func<WoWUnit, Task<bool>> IronStarEncounter()
	    {
	        return async ironStar =>
	        {
	            if (Me.Transport == ironStar && ActionBar.Active.Type == ActionBarType.Vehicle)
	            {
	                var button = ActionBar.Active.Buttons.FirstOrDefault();
                    if (button !=null && button.CanUse)
                    {
                        button.Use();
                        await CommonCoroutines.SleepForRandomUiInteractionTime();
                        return true;
                    }
	                return false;
	            }

	            if (!Me.IsTank())
	                return false;

	            if (ironStar.DistanceSqr < 40*40 && await ScriptHelpers.InteractWithObject(ironStar))
	                return true;
                ScriptHelpers.SetLeaderMoveToPoi(ironStar.Location);

	            return false;
	        };
	    }

        [EncounterHandler(83389, "Ironwing Flamespitter")]
        public Func<WoWUnit, Task<bool>> IronwingFlamespitterEncounter()
        {
            AddAvoidObject(ctx => true, 5, AreaTriggerId_LavaBarrage, AreaTriggerId_LavaBlast);

            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_LavaBarrage 
                    || MissileSpellIds_LavaBlast.Contains(m.SpellId)));

            return async npc => false;
        }

        [EncounterHandler(84028, "Siegemaster Rokra")]
        [EncounterHandler(83026, "Siegemaster Olugar")]
        public Func<WoWUnit, Task<bool>> SiegemasterEncounter()
        {
            AddAvoidObject(ctx => true, 5, AreaTriggerId_ThrowGatecrasher);

            return async npc => false;
        }

        #endregion

        #region Fleshrender Nok'gar

	    private const int SpellId_RecklessProvocation = 164426;
        private const uint AreaTriggerId_BurningArrows = 7224; 
        private const uint AreaTriggerId_BarbedArrowBarrage = 7198;
        private const uint MobId_FleshrenderNokgar = 81305;
	    private const int MissileSpellId_BurningArrows = 164234;
	    private const int MissileSpellId_BarbedArrowBarrage = 166914;

	    [EncounterHandler(81305, "Fleshrender Nok'gar")]
	    public Func<WoWUnit, Task<bool>> FleshrenderNokgarEncounter()
	    {
	        AddAvoidObject(ctx => true, 3, AreaTriggerId_BurningArrows);
	        AddAvoidObject(ctx => true, 7.5f, AreaTriggerId_BarbedArrowBarrage);

	        return async boss =>
	        {
	            if ((boss.HasAura("Reckless Provocation") || boss.CastingSpellId == SpellId_RecklessProvocation)
	                && Me.IsAutoAttacking)
	            {
	                Lua.DoString("StopAttack()");
	                return true;
	            }
	            return false;
	        };
	    }

	    #endregion

	    #region Grimrail Enforcers

	    private readonly int[] MissileSpellId_OgreTraps =
	    {
	        163275,
	        163303,
	        163304,
	        163278,
	        163279,
	        163305,
	        163307,
	        163306,
	    };

	    private const int SpellId_FlamingSlash = 163665;

	    private const uint MobId_OgreTrap = 88758;
	    private const uint MobId_Bombsquad = 80875;
	    private const uint MobId_MakoggEmberblade = 80805;
        private const uint MobId_SpinningSlash = 81040;

	    private const uint AreaTriggerId_FlamingSlash = 7089;
	    private const uint AreaTriggerId_LavaSwipe = 7276;

	    [EncounterHandler(80805, "Makogg Emberblade")]
	    public Func<WoWUnit, Task<bool>> MakoggEmberbladeEncounter()
	    {
	        AddAvoidObject(
	            ctx => true,
	            10,
	            o => o.Entry == MobId_MakoggEmberblade && o.ToUnit().CastingSpellId == SpellId_FlamingSlash,
	            o =>
	            {
                    // boss runs towards this NPC when using this ability.
	                var spinningSlash =
	                    ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_SpinningSlash);
	                return spinningSlash == null 
                        ? o.Location 
                        : Me.Location.GetNearestPointOnSegment(o.Location, spinningSlash.Location);
	            });

            //AddAvoidObject(
            //    ctx => true,
            //    o =>
            //    {
            //        var box = (AreaTriggerBox) ((WoWAreaTrigger) o).Shape;
            //        var currentExtends = box.CurrentExtents;
            //        return (float) Math.Sqrt((currentExtends.X*currentExtends.X) + (currentExtends.Y*currentExtends.Y) + 1);
            //    },
            //    o => o.Entry == AreaTriggerId_LavaSwipe);

	        AddAvoidObject(ctx => true, o => 6, AreaTriggerId_LavaSwipe);

	        return async boss => false;
	    }

	    [EncounterHandler(80816, "Ahri'ok Dugru")]
	    public Func<WoWUnit, Task<bool>> AhriokDugruEncounter()
	    {
	        return async boss => false;
	    }

	    [EncounterHandler(80808, "Neesa Nox")]
	    public Func<WoWUnit, Task<bool>> NeesaNoxEncounter()
	    {
	        AddAvoidLocation(
	            ctx => true,
	            3,
	            o => ((WoWMissile) o).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => MissileSpellId_OgreTraps.Contains(m.SpellId)));

	        AddAvoidObject(ctx => true, 4, MobId_OgreTrap);
	        AddAvoidObject(ctx => true, 7, o => o.Entry == MobId_Bombsquad);

	        return async boss => false;
	    }

	    #endregion

        #region Oshir

	    private const int MissileSpellId_AcidSpit = 178155;
        private const uint AreaTriggerId_AcidSpit = 8200;

        private const uint MobId_RavenousWolf = 89012;
        private const uint MobId_RylakSkyterror = 89011;
        private const uint MobId_Oshir = 79852;
        private const uint MobId_RendingSlashes = 79889;

        [EncounterHandler(79852, "Oshir")]
	    public Func<WoWUnit, Task<bool>> OshirEncounter()
	    {
            AddAvoidLocation(
                ctx => true,
                6,
                o => ((WoWMissile) o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_AcidSpit));


            AddAvoidObject(ctx => true, 6, AreaTriggerId_AcidSpit);

            // boss charges forward, doing damage to all in path when he has this aura.
            AddAvoidObject(
                ctx => true,
                6,
                o => o.Entry == MobId_Oshir && (o.ToUnit().HasAura("Hamstring Backflip") || o.ToUnit().HasAura("Rending Slashes")),
                o =>
                {
                    // boss runs towards this NPC when using this ability.
                    var rendingSlashes =
                        ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_RendingSlashes);
                    return rendingSlashes == null
                        ? o.Location
                        : Me.Location.GetNearestPointOnSegment(o.Location, rendingSlashes.Location);
                });

	        return async boss => false;
	    }

	    #endregion

        #region Skulloc

	    private const int SpellId_CannonBarrage = 168929;
        private const int MissileSpellId_CannonBarrage_Trash = 168539;
        private const int MissileSpellId_RapidFire = 168348;

        private const uint MobId_Skulloc = 83612;
        private const uint MobId_Zoggosh = 83616;
        private const uint MobId_Koramar = 83613;
        private const uint MobId_BlackhandsMightTurret = 84215;

	    private readonly int[] MissileSpellId_CannonBarrage = {168539, 168384, 168385};

	    [EncounterHandler(83612, "Skulloc", BossRange = 200)]
	    public Func<WoWUnit, Task<bool>> SkullocEncounter()
	    {
            AddAvoidLocation(
                ctx => true,
                10,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_CannonBarrage_Trash && m.ImpactPosition.DistanceSqr(Me.Location) < 35 * 35));

			var northLosLoc = new WoWPoint(6848.57, -969.3016, 23.04621);
	        var midLosLoc = new WoWPoint(6817.913, -1004.176, 23.06598);
	        var southLosLoc = new WoWPoint(6785.69, -1005.73, 23.04617);

			WaitTimer startBarrageTimer = null;

	        Func<WoWUnit, WoWPoint> getCannonBarrageLosLoc = boss =>
	        {
	            if (!ScriptHelpers.IsViable(boss))
	                return WoWPoint.Zero;
                var isChanneling = boss.HasAura("Cannon Barrage");
	            var myX = Me.X;

				if (myX > northLosLoc.X + 5 || isChanneling && myX > northLosLoc.X - 5 
					|| startBarrageTimer != null && !startBarrageTimer.IsFinished)
				{
					return northLosLoc;
				}

	            if (myX > midLosLoc.X + 5 || isChanneling && myX > midLosLoc.X - 5)
	                return midLosLoc;

	            if (myX > southLosLoc.X + 5 || isChanneling && myX > southLosLoc.X - 5)
	                return southLosLoc;
	            return WoWPoint.Zero;
	        };

	        return async boss =>
	        {
				// Healer can sometimes stay alive forver by LOSing boss when all others are dead. Don't let that happen.
				if (Me.IsHealer() && ScriptHelpers.GroupMembers.All(g => g.Guid == Me.Guid || g.IsDead))
				{
					TreeRoot.StatusText = "All my friends died, joining them";
					return Navigator.AtLocation(boss.Location)
						|| (await CommonCoroutines.MoveTo(boss.Location)).IsSuccessful();
				}

                if (InCannonBarragePhase(boss))
                {
					if (startBarrageTimer == null)
					{
						startBarrageTimer = new WaitTimer(TimeSpan.FromSeconds(6));
						startBarrageTimer.Reset();
					}

                    return await ScriptHelpers.StayAtLocationWhile(
                        () => getCannonBarrageLosLoc(boss) != WoWPoint.Zero,
                        getCannonBarrageLosLoc(boss),
                        "LOSing Cannon Barrage",
                        3);
                }
                else
                {
	                startBarrageTimer = null;
                }
	            return false;
	        };
	    }

        private bool InCannonBarragePhase(WoWUnit skuloc)
        {
            return ScriptHelpers.IsViable(skuloc) 
                && (skuloc.CastingSpellId == SpellId_CannonBarrage || skuloc.HasAura("Cannon Barrage")
                    || skuloc.HasAura("Check for Players"));
        }

	    [EncounterHandler(83616, "Zoggosh")]
	    public Func<WoWUnit, Task<bool>> ZoggoshEncounter()
        {
            AddAvoidLocation(ctx => true, 4,
                o =>
                {
                    var m = (WoWMissile) o;
                    return Me.Location.GetNearestPointOnLine(m.FirePosition, m.ImpactPosition);
                }, 
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_RapidFire && m.TargetGuid != Me.Guid));

	        return async boss => false;
	    }

	    [EncounterHandler(83613, "Koramar")]
	    public Func<WoWUnit, Task<bool>> KoramarEncounter()
	    {
            AddAvoidObject(ctx => Me.IsFollower(), 10, o => o.Entry == MobId_Koramar && o.ToUnit().HasAura("Bladestorm"));

	        return async boss => false;
	    }  	   
        
	    #endregion


    }

	#endregion

	#region Heroic Difficulty

    public class IronDocksHeroic : IronDocks
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 857; }
		}

		#endregion
	}

	#endregion
}