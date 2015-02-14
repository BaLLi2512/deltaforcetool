using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class ImperatorsRise : WoDLfr
	{
		#region Overrides of Dungeon
	
		public override uint DungeonId
		{
			get { return 851; }
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
	        var isMelee = Me.IsMelee();
			var imperatorFinalPhase = ScriptHelpers.IsViable(_imperator) && _imperator.Combat
				&& _imperator.HealthPercent <= 25 && !_imperator.HasAura("Arcane Protection");

			units.RemoveAll(
				ret =>
				{
				    var unit = ret as WoWUnit;
				    if (unit == null)
				        return false;

					if (unit.Entry == MobId_GuardCaptainThag && unit.CastingSpellId == SpellId_GroundStomp && isMelee)
						return true;

					if (unit.Entry == MobId_CouncilorDaglat && unit.CastingSpellId == SpellId_ArcaneDestruction && isMelee)
						return true;

					if (unit.Entry == MobId_ImperatorMargok && unit.HasAura("Arcane Protection"))
						return true;

					// Ignore these mobs in final phase of Imperator.
					if ((unit.Entry == MobId_GorianWarmage || unit.Entry == MobId_VolatileAnomaly) && imperatorFinalPhase)
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
            foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
                    switch (unit.Entry)
                    {
						case MobId_CouncilorNouk:
							priority.Score += 5500;
							break;
						case MobId_HighCouncilorMalgris:
						case MobId_OgronMauler:
						case MobId_ArcaneAberration:
						case MobId_FortifiedArcaneAberration:
						case MobId_DisplacingArcaneAberration:
						case MobId_ReplicatingArcaneAberration:
							priority.Score += 5000;
							break;
						case MobId_CouncilorMagknor:
							priority.Score += 4500;
							break;
						case MobId_CouncilorDaglat:
							priority.Score += 4000;
							break;
						case MobId_CouncilorGorluk:
							priority.Score += 3500;
							break;
						case MobId_VolatileAnomaly:
		                    priority.Score += (isMelee ? 3500 : 4500);
							break;
						case MobId_GorianWarmage:
		                    priority.Score += (isMelee ? 4500 : 3500);
							break;
							
                    }
				}
			}
		}

        #endregion
		#region Root


		#endregion

		#region Imperator Mar'gok

	    #region Trash

	    private const int SpellId_DeafeningRoar = 174477;
		private const int SpellId_GroundStomp = 174495;
	    private const int SpellId_BrutalCleave = 174491;
	    private const int SpellId_ArcaneDestruction = 174541;

	    private const int MissileSpellId_ArcaneTorrent = 174573;

		private const uint MobId_GuardCaptainThag = 81780;
	    private const uint MobId_OgronMauler = 81779;
		private const uint MobId_HighCouncilorMalgris = 81811;
		private const uint MobId_CouncilorDaglat = 81810;
		private const uint MobId_CouncilorGorluk = 81809;
		private const uint MobId_CouncilorMagknor = 81808;
		private const uint MobId_CouncilorNouk = 81807;

		[EncounterHandler((int)MobId_OgronMauler, "OgronMauler")]
		public Func<WoWUnit, Task<bool>> OgronMauleEncounter()
		{
			return async npc =>
			{
				// Stop casting inorder to avoid getting interrupted and spell locked
				if (npc.CastingSpellId == SpellId_DeafeningRoar && Me.IsCasting 
					&& npc.CurrentCastTimeLeft <= TimeSpan.FromSeconds(1))
				{
					await Coroutine.Wait(2, () => npc.CastingSpellId != SpellId_DeafeningRoar);
				}
				return false;
			};
		}

		[EncounterHandler((int)MobId_GuardCaptainThag, "Guard Captain Thag")]
		public Func<WoWUnit, Task<bool>> GuardCaptainThagEncounter()
		{
			// PBAoE damage
			AddAvoidObject(12, o => o.Entry == MobId_GuardCaptainThag && o.ToUnit().CastingSpellId == SpellId_GroundStomp);
			// Avoid getting cleaved
			AddAvoidObject(8, o => o.Entry == MobId_GuardCaptainThag, o => o.Location.RayCast(o.Rotation, 7));
			return async npc =>
			{
				return false;
			};
		}

		[EncounterHandler((int)MobId_CouncilorNouk, "Councilor Nouk")]
		public Func<WoWUnit, Task<bool>> CouncilorsEncounter()
		{
			AddAvoidObject(10, o => o.Entry == MobId_CouncilorDaglat && o.ToUnit().CastingSpellId == SpellId_ArcaneDestruction);

			AddAvoidLocation(
				ctx => true,
				6,
				m => ((WoWMissile) m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_ArcaneTorrent));

			return async npc => await ScriptHelpers.DispelGroup("Time Stop", ScriptHelpers.PartyDispelType.Magic);
		}
	    #endregion

	    private const int SpellId_NetherBlast = 157769;
		private const uint MobId_ImperatorMargok = 77428;
		private const uint MobId_GorianWarmage = 78121;
		private const uint MobId_VolatileAnomaly = 78077;

		private const uint MobId_ArcaneAberration = 77809;
		private const uint MobId_FortifiedArcaneAberration = 77878;
		private const uint MobId_DisplacingArcaneAberration = 77879;
		private const uint MobId_ReplicatingArcaneAberration = 77877;

		private const uint MobId_DestructiveResonance = 77681;
	    private const uint MobId_DestructiveResonance2 = 77637;
		private const uint AreaTriggerId_OrbsofChaos = 6644;
		private const int SpellId_DestructiveResonance_Growable = 156941;

		private readonly int[] _forceNovaIds = { 157349, 164232, 164235, 164240 };

		private WoWUnit _imperator;

		// http://www.wowhead.com/guide=2798/imperator-margok-highmaul-raid-strategy-guide
		[EncounterHandler((int)MobId_ImperatorMargok, "Imperator Mar'gok")]
		public Func<WoWUnit, Task<bool>> ImperatorMargokEncounter()
		{
			Func<WoWPlayer, bool> shouldDropBranded = player => player.HasAura("Branded", aura => aura.StackCount >= 5)
				|| player.HasAura("Branded: Displacement", aura => aura.StackCount >= 5)
				|| player.HasAura("Branded: Fortification", aura => aura.StackCount >= 5)
				|| player.HasAura("Branded: Replication", aura => aura.StackCount >= 5);

			var iShouldDropBranded = new PerFrameCachedValue<bool>(() => shouldDropBranded(Me));

			AddAvoidObject(7, o => o is WoWPlayer && !o.IsMe && (shouldDropBranded(o.ToPlayer()) || iShouldDropBranded));
			AddAvoidObject(
				ctx => true,
				o => o.ToUnit().HasAura(SpellId_DestructiveResonance_Growable) ? 8 : 4,
				MobId_DestructiveResonance,
				MobId_DestructiveResonance2);

			AddAvoidObject(6, AreaTriggerId_OrbsofChaos);

			return async boss =>
						 {
							 _imperator = null;
							 return false;
						 };
		}

		[EncounterHandler((int)MobId_GorianWarmage, "GorianWarmage")]
		public Func<WoWUnit, Task<bool>> GorianWarmageEncounter()
		{
			return async boss => !Me.IsHealer() && boss == Me.CurrentTarget &&  await ScriptHelpers.InterruptCast(boss, SpellId_NetherBlast);
		}

		#endregion
	}


}