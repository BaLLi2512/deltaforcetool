
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.TreeSharp;
using Styx;
using Styx.WoWInternals.WoWObjects;
using System.Collections.Generic;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	public class RazorfenDowns : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 20; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(-4661.441, -2531.453, 82.05106); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(2592.96, 1116.085, 50.44804); }
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			foreach (var unit in incomingObjects.OfType<WoWUnit>())
			{
				if (unit.Entry == MobId_BlazingServitor)
					outgoingObjects.Add(unit);
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> objPriorities)
		{
			var isDps = Me.IsDps();
			foreach (var targetPriority in objPriorities)
			{
				var unit = targetPriority.Object as WoWUnit;
				if (unit == null)
					continue;

				if (unit.Entry == MobId_BlazingServitor)
					targetPriority.Score += 4500;

				// DPS should priortize adds above boss.
				if (unit.Entry == MobId_DeathSpeakerBlackthorn && isDps)
					targetPriority.Score -= 4500;

			}
		}

		#endregion

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}


		#region Aarux

		const uint GameObjectId_Gong = 148917;

		[ObjectHandler((int) GameObjectId_Gong, "Gong", ObjectRange = 100)]
		public async Task<bool> GongHandler(WoWGameObject gong)
		{
			if (!Me.IsTank() || !Targeting.Instance.IsEmpty() || !gong.CanUse())
				return false;
			
			if (await ScriptHelpers.ClearArea(gong.Location, 40) || BotPoi.Current.Type != PoiType.None)
				return false;
			
			return await ScriptHelpers.InteractWithObject(gong);
		}

		private const uint MobId_Aarux = 74412;
		[EncounterHandler((int)MobId_Aarux, "Aarux")]
		public Func<WoWUnit, Task<bool>> AaruxEncounter()
		{
			AddAvoidObject(ctx => Me.HasAura("Web Strand"), 20, MobId_Aarux);

			return async boss =>
			{
				if (Me.HasAura("Web Strand"))
					Navigator.NavigationProvider.StuckHandler.Reset();

				return false;
			};
		}

		#endregion

		#region Mordresh Fire Eye

		private const int SpellId_LavaBurst = 150001;
		const uint MobId_BlazingServitor = 74548;
		private const uint MobId_MordreshFireEye = 74347;

		[EncounterHandler((int)MobId_MordreshFireEye, "Mordresh Fire Eye")]
		public Func<WoWUnit, Task<bool>> MordreshFireEyeEncounter()
		{
			return async boss => await ScriptHelpers.InterruptCast(boss, SpellId_LavaBurst);
		}

		#endregion

		#region Mushlump

		private const int SpellId_TummyAche = 149851;
		private const uint SpellId_PutridFunk = 152136;
		private const uint MobId_Mushlump = 74435;

		[EncounterHandler((int)MobId_Mushlump, "Mushlump")]
		public Func<WoWUnit, Task<bool>> MushlumpEncounter()
		{
			WoWUnit boss = null;
			AddAvoidObject(5, SpellId_PutridFunk);

			// 5 degree cone that does damage up to 40 yds
			AddAvoidLocation(
				ctx => !Me.IsSwimming && ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_TummyAche,
				4 * 1.33f,
				o => (WoWPoint)o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					boss.Location,
					boss.Location.RayCast(boss.Rotation, 40),
					4 / 2).OfType<object>());

			return async npc =>
			{
				boss = npc;
				return false;
			};
		}

		#endregion


		#region Death Speaker Blackthorn

		private const int SpellId_Shockwave = 151962;
		private const int SpellId_SearingShadows = 150616;
		private const int SpellId_Shadowmend = 150550;
		private const uint AreaTriggerId_BubonicPlague = 5584;
		private const uint MobId_DeathSpeakerBlackthorn = 74875;

		[EncounterHandler((int)MobId_DeathSpeakerBlackthorn, "Death Speaker Blackthorn")]
		public Func<WoWUnit, Task<bool>> DeathSpeakerBlackthornEncounter()
		{
			// From one of the trash mobs I think
			AddAvoidObject(4, AreaTriggerId_BubonicPlague);

			#region Shockwave

			AddAvoidObject(
				3,
				o => o.Entry == MobId_DeathSpeakerBlackthorn && o.ToUnit().CastingSpellId == SpellId_Shockwave,
				o => o.Location.RayCast(o.Rotation, 2));
			AddAvoidObject(
				6,
				o => o.Entry == MobId_DeathSpeakerBlackthorn && o.ToUnit().CastingSpellId == SpellId_Shockwave,
				o => o.Location.RayCast(o.Rotation, 6));
			AddAvoidObject(
				8,
				o => o.Entry == MobId_DeathSpeakerBlackthorn && o.ToUnit().CastingSpellId == SpellId_Shockwave,
				o => o.Location.RayCast(o.Rotation, 12));
			AddAvoidObject(
				10,
				o => o.Entry == MobId_DeathSpeakerBlackthorn && o.ToUnit().CastingSpellId == SpellId_Shockwave,
				o => o.Location.RayCast(o.Rotation, 17));

			#endregion


			return async boss => await ScriptHelpers.InterruptCast(boss, SpellId_Shadowmend, SpellId_SearingShadows)
								|| await ScriptHelpers.DispelGroup("Searing Shadows", ScriptHelpers.PartyDispelType.Magic);
		}

		#endregion

		#region Amnennar the Coldbringer

		const uint SpellId_FrozenBomb = 152190;
		private const uint AreaTriggerId_RedeemedSoil = 5634;
		private const uint SpellId_SoulLeech = 150679;

		private const uint MobId_AmnennartheColdbringer = 74434;
		[EncounterHandler((int)MobId_AmnennartheColdbringer, "Amnennar the Coldbringer")]
		public Func<WoWUnit, Task<bool>> AmnennartheColdbringerEncounter()
		{
			WoWUnit boss = null;

			var soulLeech = new PerFrameCachedValue<bool>(
				() => Me.HasAura("Soul Leech") || ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_SoulLeech);

			AddAvoidObject(ctx => !soulLeech, 2.75f, SpellId_FrozenBomb);

			return async npc =>
			{
				boss = null;

				if (soulLeech)
				{
					var redeemdSoil =
						ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
							.Where(a => a.Entry == AreaTriggerId_RedeemedSoil)
							.OrderBy(a => a.DistanceSqr)
							.FirstOrDefault();

					if (redeemdSoil != null)
					{
						return await ScriptHelpers.StayAtLocationWhile(() => soulLeech && ScriptHelpers.IsViable(redeemdSoil),
									redeemdSoil.Location,
									"Redeemed soil",
									3.5f);
					}

				}
				return false;
			};
		}

		#endregion
	}
}