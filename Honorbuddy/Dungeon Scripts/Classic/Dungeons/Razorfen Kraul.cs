using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	public class RazorfenKraul : Dungeon
	{
        #region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 16; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(-4459.123, -1659.543, 81.59359); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(1939.186, 1538.528, 82.28346); }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit != null) { }
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
                    if (unit.Entry == MobId_CrystalfireTotem || unit.Entry == MobId_SolarshardTotem)
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
                        case MobId_CrystalfireTotem:
                        case MobId_SolarshardTotem:
				            if (isDps)
				                priority.Score += 5000;
                            break;
                        case MobId_RazorfenHuntmaster:
                        case MobId_AggemThorncurse:
                            if (isDps)
                                priority.Score += 4500;
                            break;
                        case MobId_BloodBrandedRazorfen:
                        case MobId_DeathSpeakerJargba:
                            if (isDps)
                                priority.Score += 4000;
                            break;
                        case MobId_RazorfenBeastStalker:
                            if (isDps)
                                priority.Score += 4300;
                            break;
				    }
				}
			}
		}

		#endregion

		#region Root

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}


		[EncounterHandler(44402, "Auld Stonespire", Mode = CallBehaviorMode.Proximity)]
		[EncounterHandler(44415, "Spirit of Agamaggan", Mode = CallBehaviorMode.Proximity)]
		public Composite AuldStonespireEncounter()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				new Decorator(ctx => unit.QuestGiverStatus == QuestGiverStatus.Available, ScriptHelpers.CreatePickupQuest(ctx => unit)),
				new Decorator(ctx => unit.QuestGiverStatus == QuestGiverStatus.TurnIn, ScriptHelpers.CreateTurninQuest(ctx => unit)));
		}

		#endregion

        #region Hunter Bonetusk

        // Easy tank/spank boss
        private const uint MobId_HunterBonetusk = 75001;
        [EncounterHandler((int)MobId_HunterBonetusk, "Hunter Bonetusk")]
        public Func<WoWUnit, Task<bool>> HunterBonetuskEncounter()
        {
            return async boss => false;
        }

        #endregion


        #region Roogug

	    private const int SpellId_WovenElements = 150774;
	    private const int SpellId_BleakStrike = 150848;
        const uint AreaTriggerSpellId_SolarshardBeam = 153551;

	    private const uint MobId_SolarshardTotem = 76107;
	    private const uint MobId_CrystalfireTotem = 76105;
	    private const uint MobId_RazorfenHuntmaster = 76088;
	    private const uint MobId_BloodBrandedRazorfen = 76090;
	    private const uint MobId_RazorfenBeastStalker = 76089;
	    private const uint MobId_Roogug = 74948;

        [EncounterHandler((int)MobId_Roogug, "Roogug")]
        public Func<WoWUnit, Task<bool>> RoogugEncounter()
        {
            // Beam the silences
            AddAvoidObject(ctx => true, 2.4f, o => o is WoWAreaTrigger && ((WoWAreaTrigger)o).SpellId == AreaTriggerSpellId_SolarshardBeam);
            // The CrystalfireT totem shoots missiles or something. they should be killed by DPS as soon as they pop.

            return async boss => await ScriptHelpers.InterruptCast(boss, SpellId_WovenElements);
        }

        [EncounterHandler((int)MobId_RazorfenHuntmaster, "Razorfen Huntmaster")]
        public Func<WoWUnit, Task<bool>> RazorfenHuntmasterEncounter()
        {
            // he does a poison volley. not sure what the debuf is called. probably safe to ignore or let CR take care of it.
            return async boss => false;
        }

        [EncounterHandler((int)MobId_BloodBrandedRazorfen, "BloodBranded Razorfen")]
        public Func<WoWUnit, Task<bool>> BloodBrandedRazorfenEncounter()
        {
            // frontal attack
            AddAvoidObject(
                ctx => true,
                6,
                o => o.Entry == MobId_BloodBrandedRazorfen && o.ToUnit().CastingSpellId == SpellId_BleakStrike,
                o => o.Location.RayCast(o.Rotation, 5));
            return async boss => false;
        }

        #endregion

        #region Warlord Ramtusk

	    private const int SpellId_SpiritBolt = 151253;
	    private const int MissileSpellId_SpiritAxe = 151312;

        private const uint MobId_DeathSpeakerJargba = 75152;
        private const uint MobId_WarlordRamtusk = 74462;
        private const uint MobId_AggemThorncurse = 75149;
	    private const uint AreaTriggerId_ArcaneShot = 6046;


        [EncounterHandler((int)MobId_WarlordRamtusk, "Warlord Ramtusk")]
        public Func<WoWUnit, Task<bool>> WarlordRamtuskEncounter()
        {
            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile) o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_SpiritAxe));

            return async boss => false;
        }

        [EncounterHandler((int)MobId_DeathSpeakerJargba, "Death Speaker Jargba")]
        public Func<WoWUnit, Task<bool>> DeathSpeakerJargbaEncounter()
        {
            return async boss => await ScriptHelpers.InterruptCast(boss, SpellId_SpiritBolt);
        }

        [EncounterHandler((int)MobId_AggemThorncurse, "Aggem Thorncurse")]
        public Func<WoWUnit, Task<bool>> AggemThorncurseEncounter()
        {
            AddAvoidObject(ctx => true, 2.5f, AreaTriggerId_ArcaneShot);
            return async boss => false;
        }

        #endregion

	    #region Groyat, the Blind Hunter

	    private const int MissileSpellId_SonicField = 151431;

        private const uint AreaTriggerId_SonicField = 5750;
        private const uint AreaTriggerId_SonicCharge = 5833;

	    private const uint MobId_GroyattheBlindHunter = 75247;

	    [EncounterHandler((int) MobId_GroyattheBlindHunter, "Groyat, the Blind Hunter")]
	    public Func<WoWUnit, Task<bool>> GroyattheBlindHunterEncounter()
	    {
            AddAvoidObject(ctx => true, 3, AreaTriggerId_SonicField, AreaTriggerId_SonicCharge);

            AddAvoidLocation(
                ctx => true,
                3,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_SonicField));

	        return async boss => await ScriptHelpers.DispelGroup("Drain Life", ScriptHelpers.PartyDispelType.Magic);
	    }

	    #endregion

        #region Charlga Razorflank

        private const uint MobId_CharlgaRazorflank = 4421;
        [EncounterHandler((int)MobId_CharlgaRazorflank, "Charlga Razorflank")]
        public Func<WoWUnit, Task<bool>> CharlgaRazorflankEncounter()
        {
            // boss only used the Elemental Binding ability when I did this.
            return async boss => await ScriptHelpers.DispelGroup("Elemental Binding", ScriptHelpers.PartyDispelType.Magic);
        }

        #endregion
	}
}