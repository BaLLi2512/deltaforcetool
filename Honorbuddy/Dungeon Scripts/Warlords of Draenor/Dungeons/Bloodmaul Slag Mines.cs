using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
    #region Normal Difficulty

    public class BloodmaulSlagMines : Dungeon
    {
        #region Overrides of Dungeon

        public override uint DungeonId
        {
            get { return 787; }
        }

        public override WoWPoint Entrance
        {
            get { return new WoWPoint(7266.906, 4459.15, 129.4387); }
        }

        public override WoWPoint ExitLocation
        {
            get { return new WoWPoint(1819.533, -254.8778, 256.6878); }
        }

        public override void RemoveTargetsFilter(List<WoWObject> units)
        {
            units.RemoveAll(
                ret =>
                {
                    var unit = ret as WoWUnit;
                    if (unit == null)
                        return false;
                    if (unit.Entry == MobId_BloodmaulEarthbreaker && unit.HasAura("Ogre Ogre Burning Bright"))
                        return true;

                    // these guys just run away unless someone attacks them.
                    if (unit.Entry == MobId_FleeingMiner && !unit.IsTargetingMyRaidMember)
                        return true;
                    
                    // Don't attack miners that fight for you.
                    if (MobIds_CapturedMiner.Contains(unit.Entry) && unit.HasAura("Rise of the Miners"))
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
                    if (isDps && (unit.Entry == MobId_Ruination || unit.Entry == MobId_MoltenElemental 
                        ||  unit.Entry == MobId_UnstableSlag || unit.Entry == MobId_Calamity))
                    {
                        outgoingunits.Add(unit);
                    }
                }
            }
        }

        public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            var isDps = Me.IsDps();
	        var isTank = Me.IsTank();
            foreach (var priority in units)
            {
                var unit = priority.Object as WoWUnit;
                if (unit != null)
                {
                    switch (unit.Entry)
                    {
                        case MobId_BloodmaulWarder:
                        case MobId_Ruination:
                        case MobId_MoltenElemental:
						case MobId_MagmaLord:
                        case MobId_UnstableSlag:
                            if (isDps)
                                priority.Score += 4000;
                            break;

                        case MobId_Calamity:
                            if (isDps)
                                priority.Score += 3500;
                            break;
                    }

                    if (MobIds_CapturedMiner.Contains(unit.Entry))
                    {
						if (isDps)
							priority.Score += 5000;
						else if (isTank)
							priority.Score = unit.Aggro ? -5000: 5000 - unit.Distance;
                    }
                }
            }
        }


        #endregion

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        #region Root

        [EncounterHandler(0, "Root")]
        public Func<WoWUnit, Task<bool>> RootHandler()
        {
            return async npc => await ScriptHelpers.CancelCinematicIfPlaying();
        }

        #endregion

        #region Trash

        private const int SpellId_Crush = 151447;
        private const int SpellId_FrighteningRoar = 151545;
        private const int SpellId_Subjugate = 151697;
        private const int SpellId_ChannelFlames = 164615;

        private const int MissileSpellId_LavaSpit = 152183;

        private const uint MobId_BloodmaulSlaver = 75191;
        private const uint MobId_BloodmaulEarthbreaker = 75814;
        private const uint MobId_BloodmaulEnforcer = 84978;
        private const uint MobId_BloodmaulOgreMage = 75272;
        private const uint MobId_BloodmaulWarder = 75210;
        private const uint MobId_BloodmaulOverseer = 75193;
        private const uint MobId_BloodmaulGeomancer = 75198;
	    private const uint MobId_MagmaLord = 75211;
        private const uint MobId_FleeingMiner = 75647;
        private const uint MobId_PillarOfFire = 75327;
        private const uint MobId_BloodmaulFlamespeaker = 81767;

        private const uint AreaTriggerId_SuppressionField = 5785;

        [EncounterHandler(75191, "Bloodmaul Slaver")]
        public Func<WoWUnit, Task<bool>> BloodmaulSlaverEncounter()
        {
            return async npc => await ScriptHelpers.DispelEnemy("Slaver's Rage", ScriptHelpers.EnemyDispelType.Enrage, npc);
        }

        [EncounterHandler(75272, "Bloodmaul Ogre Mage")]
        public Func<WoWUnit, Task<bool>> BloodmaulOgreMageEncounter()
        {
            return async npc => await ScriptHelpers.DispelEnemy("Blood Rage", ScriptHelpers.EnemyDispelType.Magic, npc);
        }

        [EncounterHandler(75193, "Bloodmaul Overseer")]
        public Func<WoWUnit, Task<bool>> BloodmaulOverseerEncounter()
        {
            //Aoe silence
            AddAvoidObject(ctx => Me.IsRange(), 5, AreaTriggerId_SuppressionField);

            // Stone Bulwark is casted by Geomancer 
            return async npc => await ScriptHelpers.DispelEnemy("Stone Bulwark", ScriptHelpers.EnemyDispelType.Magic, npc)
                                 || await ScriptHelpers.InterruptCast(npc, SpellId_Subjugate);
        }

        [EncounterHandler(75210, "Bloodmaul Warder")]
        public Func<WoWUnit, Task<bool>> BloodmaulWarderEncounter()
        {
            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_FrighteningRoar)
                                 ||
                                 await ScriptHelpers.DispelEnemy("Stone Bulwark", ScriptHelpers.EnemyDispelType.Magic, npc);
        }

        [EncounterHandler(84978, "Bloodmaul Enforcer")]
        public Func<WoWUnit, Task<bool>> BloodmaulEnforcerEncounter()
        {
            // avoid the frontal area while this guy is casting Crush
            AddAvoidObject(
                ctx => true,
                6,
                o => o.Entry == MobId_BloodmaulEnforcer && o.ToUnit().CastingSpellId == SpellId_Crush,
                o => o.Location.RayCast(o.Rotation, 5));

            return async npc => false;
        }

        [EncounterHandler(75211, "Magma Lord")]
        public Func<WoWUnit, Task<bool>> MagmaLordEncounter()
        {
            AddAvoidObject(ctx => true,6, MobId_PillarOfFire);

            return async npc => false;
        }

        [EncounterHandler(81767, "Bloodmaul Flamespeaker")]
        public Func<WoWUnit, Task<bool>> BloodmaulFlamespeakerEncounter()
        {
            // Don't get hit by the Channeled flames.
            AddAvoidObject(
                ctx => true,
                4,
                o => o.Entry == MobId_BloodmaulFlamespeaker && o.ToUnit().ChanneledCastingSpellId == SpellId_ChannelFlames,
                o => o.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 15)));

            return async npc => false;
        }

        [EncounterHandler(75406, "Slagna")]
        public Func<WoWUnit, Task<bool>> SlagnaEncounter()
        {
            AddAvoidLocation(
                ctx => true,
                3f,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_LavaSpit));

            return async npc => false;
        }

        #endregion

        #region Magmolatus

        private const int SpellId_RoughSmash = 149941;
        private const int SpellId_SlagSmash = 150023;
        private const int SpellId_Firestorm = 149997;
        private const int SpellId_Scorch = 150290;
        private const int SpellId_MagmaBarrage = 150004;
        private const int SpellId_VolcanicTantrum = 150048;

        private const int MissileSpellId_MagmaBarrage = 150306;

        private const uint MobId_Ruination = 74570;
        private const uint MobId_Calamity = 74571;
        private const uint MobId_Magmolatus = 74475;
        private const uint MobId_MoltenElemental = 74579;

        private const uint AreaTriggerId_MagmaBarrage = 7455;
        private const uint AreaTriggerId_VolcanicTantrum = 5559;

        [EncounterHandler(74366, "Forgemaster Gog'duh")]
        public Func<WoWUnit, Task<bool>> ForgemasterGogduhEncounter()
        {
            return async boss => false;
        }

        [EncounterHandler(74570, "Ruination")]
        public Func<WoWUnit, Task<bool>> RuinationEncounter()
        {
            // avoid the frontal cone aoe stun.
            AddAvoidObject(
                ctx => true,
                6,
                o => o.Entry == MobId_Ruination && o.ToUnit().CastingSpellId == SpellId_RoughSmash,
                o => o.Location.RayCast(o.Rotation, 5));

            return async npc => false;
        }

        [EncounterHandler(74571, "Calamity")]
        public Func<WoWUnit, Task<bool>> CalamityEncounter()
        {
            return async npc => await ScriptHelpers.DispelGroup("Dancing Flames", ScriptHelpers.PartyDispelType.Magic)
                || await ScriptHelpers.InterruptCast(npc, SpellId_Firestorm, SpellId_Scorch);
        }

        [EncounterHandler(74475, "Magmolatus")]
        public Func<WoWUnit, Task<bool>> MagmolatusEncounter()
        {
            AddAvoidObject(ctx => true, 3, AreaTriggerId_MagmaBarrage);

            // avoid the PBaoe stun.
            AddAvoidObject(
                ctx => true,
                9,
                o => o.Entry == MobId_Magmolatus && o.ToUnit().CastingSpellId == SpellId_SlagSmash);

            return async boss =>
            {
                if (await ScriptHelpers.DispelGroup("Withering Flames", ScriptHelpers.PartyDispelType.Magic))
                    return true;
                return false;
            };
        }

        [EncounterHandler(74579, "Molten Element")]
        public Func<WoWUnit, Task<bool>> MoltenElementEncounter()
        {

            AddAvoidLocation(
                ctx => true,
                3f,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_MagmaBarrage));

            AddAvoidObject(ctx => true, 2, AreaTriggerId_VolcanicTantrum);

            return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_MagmaBarrage, SpellId_VolcanicTantrum);
        }

        #endregion

		// http://www.wowhead.com/guide=2664/bloodmaul-slag-mines-dungeon-strategy-guide#slave-watcher-crushto
        #region Slave Watcher Crushto

        private const int SpellId_FerociousYell = 150759;
        private const int SpellId_EarthCrush = 153679;

        private const uint MobId_SlaveWatcherCrush = 74787;
        private readonly uint[] MobIds_CapturedMiner = { 74355, 74356, 74357 };

        [EncounterHandler(74787, "Slave Watcher Crushto")]
        public Func<WoWUnit, Task<bool>> SlaveWatcherCrushtoEncounter()
        {
	        WoWUnit crushto  = null;
			const float earthCrushWidth = 8;
            // avoid the frontal cone aoe stun.

	        AddAvoidLocation(
		        ctx => !Me.IsSwimming && ScriptHelpers.IsViable(crushto) && crushto.CastingSpellId == SpellId_EarthCrush,
		        earthCrushWidth*1.33f,
		        o => (WoWPoint) o,
		        () => ScriptHelpers.GetPointsAlongLineSegment(
			        crushto.Location,
			        crushto.Location.RayCast(crushto.Rotation, 37),
			        earthCrushWidth/2).OfType<object>());

            return async boss =>
				{
					crushto = boss;
                if (await ScriptHelpers.InterruptCast(boss, SpellId_FerociousYell))
                    return true;

                if (await ScriptHelpers.DispelEnemy("Ferocious Yell", ScriptHelpers.EnemyDispelType.Enrage, boss))
                    return true;

                return false;
            };
        }

        #endregion

        #region Roltall

        private const uint AreaTriggerId_FieryBoulder = 6015;
        private const uint AreaTriggerId_BurningSlag = 6022;

        private const int MissileSpellId_BurningSlag = 152918;

        private const int MobId_SLGGenericMoP_LargeAOI = 68553;

       // private readonly uint[] MobIds_FieryBoulder = {75853, 75854, 75828};

        [EncounterHandler(75786, "Roltall")]
        public Func<WoWUnit, Task<bool>> RoltallEncounter()
        {
            // Fiery Boulder impact location
            AddAvoidObject(ctx => true, 8, AreaTriggerId_FieryBoulder);

            AddAvoidObject(ctx => true, 6.5f, AreaTriggerId_BurningSlag);
            AddAvoidLocation(
                ctx => true,
                6.5f,
                o => ((WoWMissile) o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_BurningSlag));

            // run out of the path of the fiery boulders
            AddAvoidObject(
                ctx => true,
                8,
                o => o.Entry == MobId_SLGGenericMoP_LargeAOI && o.ToUnit().HasAura("Fiery Boulder"),
                o => o.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 60)));

            return async boss => false;
        }

        #endregion


        #region Gug'rokk

        private const int SpellId_MoltenBlast = 150677;

        private const uint MobId_MagmaEruption = 74967;
        private const uint MobId_UnstableSlag = 74927;

        [EncounterHandler(74790, "Gug'rokk")]
        public Func<WoWUnit, Task<bool>> GugokkEncounter()
        {
            // stay out of the fire.
            AddAvoidObject(ctx => true, 5, MobId_MagmaEruption);

            return async boss =>
                         {
                             // interupt the boss to prevent him from getting stacks of Molten Core
                             if (await ScriptHelpers.InterruptCast(boss, SpellId_MoltenBlast))
                                 return true;

                             // Dispell Molten Core before it gets to 3 stacks and causes boss to gain the Molten Barrage ability.
                             if (await ScriptHelpers.DispelEnemy("Molten Core", ScriptHelpers.EnemyDispelType.Magic, boss))
                                 return true;

                             return false;
                         };
        }

        #endregion


    }

    #endregion

    #region Heroic Difficulty

    public class BloodmaulSlagMinesHeroic : BloodmaulSlagMines
    {
        #region Overrides of Dungeon

        public override uint DungeonId
        {
            get { return 859; }
        }

        #endregion
    }

    #endregion
}