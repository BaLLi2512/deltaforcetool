using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

// ReSharper disable CheckNamespace

namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public abstract class BlackrockFoundry : WoDLfr
	{

		#region Root

		private const uint MobId_SpinningBlade = 88008;
		private const uint AreaTriggerId_AcidbackPuddle = 6689;

		[EncounterHandler(0, "Blackrock Foundry Root Handler")]
		public Func<WoWUnit, Task<bool>> BlackrockFoundryRootHandler()
		{
			AddAvoidObject(5, MobId_SpinningBlade);
			AddAvoidObject(3, o => o.Entry == AreaTriggerId_AcidbackPuddle, ignoreIfBlocking: true);

			return async boss => false;
		}

		#endregion

	}

	public class Slagworks : BlackrockFoundry
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 847; }
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
			var isMelee = StyxWoW.Me.IsMelee();
			units.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit == null)
						return false;

					if (unit.Entry == MobId_Oregorger && isMelee && unit.HasAura("Rolling Fury") )
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
					if (unit.Entry == MobId_OreCrate && unit.HasAura("Crate Glow") && unit.Attackable)
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
						case MobId_OreCrate:
							priority.Score += 5500;
							break;

						case MobId_Oregorger:
							if (unit.HasAura("Rolling Fury"))
								priority.Score -= 5000;
							break;
					}
				}
			}
		}

		#endregion

		#region Root

		#endregion

		#region Gruul

		private const uint MobId_Gruul = 76877;
		private const uint MobId_CaveIn = 86596;

		private const int SpellId_InfernoSlice = 155080;
		private const int SpellId_OverheadSmash = 155301;

		[EncounterHandler((int) MobId_Gruul, "Gruul")]
		public Func<WoWUnit, Task<bool>> GruulEncounter()
		{
			const float overheadSmashLineWidth = 10;
			WoWUnit boss = null;

			// Don't stand in front of Gruul because of frontal cone attack, Inferno Slice. 
			AddAvoidObject(25, o => o.Entry == MobId_Gruul && o.ToUnit().Combat, o => o.Location.RayCast(o.Rotation, 20));

			var hasPetrify = new PerFrameCachedValue<bool>(() => Me.HasAura("Petrify"));

			// Petrify snares all nearby group members within 8yds, turning them to stone.
			// Run away from group if has the debuf and move from group members that have it
			AddAvoidObject(8, o =>
			{
				var player = o as WoWPlayer;
				return player != null && !player.IsMe && (player.HasAura("Petrify") || hasPetrify);
			});

			// Overhead Smash does damage in a line.
			AddAvoidLine(ctx => ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_OverheadSmash,
				() => overheadSmashLineWidth,
				() => boss.Location.RayCast(boss.Rotation, 10),
				() => boss.Location.RayCast(boss.Rotation, 80));

			AddAvoidObject(8, o => o.Entry == MobId_CaveIn && o.ToUnit().HasAura("Cave In"));

			return async npc =>
			{
				boss = npc;
				return false;
			};
		}

		#endregion

		#region Oregorger

		private const int MissileSpellId_ExplosiveShard = 156390;

		private const uint MobId_Oregorger = 77182;

		private const uint AreaTriggerId_RetchedBlackrock = 6349;
		private const uint MobId_OreCrate = 77252;
		private const int SpellId_BlackrockBarrage = 173461;

		[EncounterHandler((int) MobId_Oregorger, "Oregorger")]
		public Func<WoWUnit, Task<bool>> OregorgerEncounter()
		{
			const float rollLineWidth = 12;

			WoWUnit boss = null;
			// Explosive Shade stuns and does damage on impact.
			AddAvoidLocation(ctx => true, 7, o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_ExplosiveShard));

			AddAvoidObject(7, o => o.Entry == AreaTriggerId_RetchedBlackrock,ignoreIfBlocking: true);

			// Overhead Smash does damage in a line.
			AddAvoidLine(ctx => ScriptHelpers.IsViable(boss) && boss.HasAura("Rolling Fury"),
				() => rollLineWidth,
				() => boss.Location,
				() => boss.Location.RayCast(boss.Rotation, 60));


			return async npc => { return await ScriptHelpers.InterruptCast(npc, SpellId_BlackrockBarrage); };
		}

		#endregion

		#region The Blast Furnace

		private const uint AreaTriggerId_SlagPool = 6269;
		private const uint MobId_HeatRegulator = 76808;
		private const uint AreaTriggerId_Melt = 6221;
		private const uint MobId_ForemanFeldspar = 76809;

		[EncounterHandler((int) MobId_ForemanFeldspar, "ForemanFeldspar")]
		public Func<WoWUnit, Task<bool>> ForemanFeldsparEncounter()
		{
			AddAvoidObject(5, AreaTriggerId_Melt);
			AddAvoidObject(23, AreaTriggerId_SlagPool);


			var hasVolatileFire = new PerFrameCachedValue<bool>(() => Me.HasAura("Volatile Fire"));
			AddAvoidObject(8, o =>
			{
				var player = o as WoWPlayer;
				return player != null && !player.IsMe && (player.HasAura("Volatile Fire") || hasVolatileFire);
			});

			return async boss => { return false; };
		}

		private const uint MobId_HeartoftheMountain = 76806;

		[EncounterHandler((int) MobId_HeartoftheMountain, "Heart of the Mountain")]
		public Func<WoWUnit, Task<bool>> HeartoftheMountainEncounter()
		{
			return async boss => { return false; };
		}

		#endregion
	}
}