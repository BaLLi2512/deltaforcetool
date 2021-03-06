﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Styx.CommonBot.Coroutines;

namespace Bots.DungeonBuddy.Dungeon_Scripts.Classic
{
	public class WailingCaverns : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 1; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(-743.4651f, -2215.294f, 15.46369f); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(-168.255, 136.5673, -72.73431); }
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (WoWUnit unit in incomingunits.Select(obj => obj.ToUnit()))
			{
				// need to add Kresh manually since he's a neutral.
				if (unit.Entry == KreshId && !Me.Combat && Me.IsTank())
				{
					var pathDist = unit.Location.PathDistance(Me.Location, 40f);
					if (pathDist.HasValue && pathDist < 40f)
						outgoingunits.Add(unit);
				}
			}
		}


		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (Targeting.TargetPriority t in units)
			{
				WoWObject prioObject = t.Object;
			}
		}

		public override void OnEnter()
		{
			_muyohLoc.CycleTo(_muyohLoc.First);
		}

        public override void IncludeLootTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingunits)
        {
            foreach (var obj in incomingObjects)
            {
                var gObj = obj as WoWGameObject;
                if (gObj != null && gObj.Entry == SerpentbloomId
                    && ScriptHelpers.SupportsQuesting && gObj.DistanceSqr < 25 * 25 && gObj.ZDiff < 10
                    && !ScriptHelpers.WillPullAggroAtLocation(gObj.Location) && gObj.CanUse())
                {
                    outgoingunits.Add(gObj);
                }
            }
        }

		#endregion

		#region Root

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}


		[EncounterHandler(0)]
		public Composite RootBehavior()
		{
			return new PrioritySelector();
		}

		[EncounterHandler(5767, "Nalpak", Mode = CallBehaviorMode.Proximity, BossRange = 40)]
		[EncounterHandler(5768, "Ebru", Mode = CallBehaviorMode.Proximity, BossRange = 40)]
		public Composite QuestGiversBehavior()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				new Decorator(ctx => !Me.Combat && unit.QuestGiverStatus == QuestGiverStatus.Available, ScriptHelpers.CreatePickupQuest(ctx => unit)),
				new Decorator(ctx => !Me.Combat && unit.QuestGiverStatus == QuestGiverStatus.TurnIn, ScriptHelpers.CreateTurninQuest(ctx => unit)));
		}

	    private const uint SerpentbloomId = 13891;

		#endregion

		private const uint KreshId = 3653;

		[EncounterHandler(3669, "Lord Cobrahn")]
		[EncounterHandler(3670, "Lord Pythas")]
		[EncounterHandler(3671, "Lady Anacondra")]
		[EncounterHandler(3673, "Lord Serpentis")]
		public Composite DruidsEncounter()
		{
			const int healingTouchId = 23381;
			const int druidsSlumberId = 8040;
			WoWUnit boss = null;
			return new PrioritySelector(ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => boss, healingTouchId, druidsSlumberId),
				ScriptHelpers.CreateDispelGroup("Druid's Slumber", ScriptHelpers.PartyDispelType.Magic),
				// Lord Cobrahn only.
				ScriptHelpers.CreateDispelGroup("Poison", ScriptHelpers.PartyDispelType.Poison));
		}

		[EncounterHandler(3653, "Kresh")]
		public Composite KreshEncounter()
		{
			return new PrioritySelector();
		}


		[EncounterHandler(3674, "Skum")]
		public Composite SkumEncounter()
		{
			return new PrioritySelector();
		}

		#region Mutanus the Devourer

		private readonly CircularQueue<WoWPoint> _muyohLoc = new CircularQueue<WoWPoint>()
		{
			new WoWPoint(-134.965, 125.402, -78.17783),
			new WoWPoint(114.9415, 237.7185, -96.02783)
		};

		[EncounterHandler(3654, "Mutanus the Devourer", Mode = CallBehaviorMode.CurrentBoss)]
		public Func<WoWUnit, Task<bool>> MutanustheDevourerSpawnHandler()
		{
			const int muyohId = 3678;
			var muyohStartLoc = new WoWPoint(-134.965, 125.402, -78.17783);
			var muyohEndLoc = new WoWPoint(114.9415, 237.7185, -96.02783);
			return async boss =>
			{
				if (ScenarioInfo.Current.IsComplete)
					return false;

				if (boss == null && Me.IsTank())
				{
					var muyoh = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == muyohId);
					if (muyoh == null)
					{
						if (Me.Location.Distance2DSqr(_muyohLoc.Peek()) <= 5*5)
							_muyohLoc.Dequeue();
						TreeRoot.StatusText = "Searching for Muyoh";
                        return (await CommonCoroutines.MoveTo(_muyohLoc.Peek())).IsSuccessful();
					}

					if (muyoh.Location.DistanceSqr(muyohEndLoc) > 5 * 5)
						return await ScriptHelpers.TankTalkToAndEscortNpc(muyoh, muyohStartLoc);

					var moveToEncounter = !ScriptHelpers.GetUnfriendlyNpsAtLocation(muyohEndLoc, 80).Any() ||
					                       (Me.Location.DistanceSqr(muyohEndLoc) > 10*10 && Targeting.Instance.IsEmpty());

					if (moveToEncounter)
						return (await CommonCoroutines.MoveTo(_muyohLoc.Peek(), "Muyoh Encounter")).IsSuccessful();

					TreeRoot.StatusText = "Clearing area to cause Mutanus to spawn";
                    return await ScriptHelpers.ClearArea(muyohEndLoc, 80);
				}

				return false;
			};
		}

		[EncounterHandler(3654, "Mutanus the Devourer")]
		public Composite MutanustheDevourerEncounter()
		{
			const int naralexsNightmare = 7967;
			WoWUnit boss = null;
			return new PrioritySelector(
				ctx => boss = ctx as WoWUnit,
				ScriptHelpers.CreateInterruptCast(ctx => boss, naralexsNightmare),
				ScriptHelpers.CreateDispelGroup("Terrify", ScriptHelpers.PartyDispelType.Magic),
				ScriptHelpers.CreateDispelGroup("Naralex's Nightmare", ScriptHelpers.PartyDispelType.Magic));
		}

		#endregion
	}
}