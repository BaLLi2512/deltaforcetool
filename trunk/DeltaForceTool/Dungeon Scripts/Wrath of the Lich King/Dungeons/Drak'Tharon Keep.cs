
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Wrath_of_the_Lich_King
{
	public class DrakTharonKeep : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 214; }
		}

		public override WoWPoint Entrance
		{
			get { return new WoWPoint(4774.611, -2023.276, 229.3549); }
		}

		public override WoWPoint ExitLocation
		{
			get { return new WoWPoint(-518.0573, -480.0317, 10.97494); }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(ret => { return false; });
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			foreach (var obj in incomingunits)
			{
				var unit = obj as WoWUnit;
				if (unit != null) { }
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null) { }
			}
		}


        public override void IncludeLootTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
        {
            var pickupEnduringMojos = ScriptHelpers.HasQuest(QuestId_CleansingDrakTharon)
                                      && !ScriptHelpers.IsQuestInLogComplete(QuestId_CleansingDrakTharon)
                                      && Me.GetCarriedItemCount(ItemId_EnduringMojo) < 5;

            foreach (var obj in incomingunits)
            {
                var unit = obj as WoWUnit;
                if (unit != null)
                {
                    // make sure quest object gets looted regardless of loot settings.
                    if (unit.Entry == MobId_KingDred
                        && ScriptHelpers.HasQuest(QuestId_WhatTheScourgeDred)
                        && !ScriptHelpers.IsQuestInLogComplete(QuestId_WhatTheScourgeDred))
                    {
                        outgoingunits.Add(obj);
                    }
                    else if (MobIds_EnduringMojoMobId.Contains((int)unit.Entry) && pickupEnduringMojos)
                    {
                        outgoingunits.Add(obj);
                    }
                }
            }
        }
		#endregion

		private const string ZombieDpsRotation =
@"local _,s = GetActionInfo(120 + 4) 
if s then  print(s) CastSpellByID(s) end
s = GetActionInfo(120 + 3) 
if s then CastSpellByID(s) end 
s = GetActionInfo(120 + 1) 
if s then CastSpellByID(s) end 
";

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

	    #region Quests

        private const uint QuestId_HeadGames = 13129;
        private const uint QuestId_CleansingDrakTharon = 30120;
        private const uint QuestId_WhatTheScourgeDred = 29828;
        private const uint ItemId_EnduringMojo = 38303;
        private const uint ItemId_DrakurusElixir = 35797;
	    private const uint ItemId_KurzelsBlouseScrap = 43214;
	    private const uint MobId_Drakuru = 28016;
	    readonly WoWPoint _cleansingDrakTharonObjectiveLoc = new WoWPoint(-236.821f, -618.6074f, 116.4761f);

	    private readonly int[] MobIds_EnduringMojoMobId = {
	                                                           26623, // Scourge Brute
                                                               26620, // Drakkari Guardian
                                                               26830, // Risen Drakkari Death Knigh
                                                               26635, // Risen Drakkari Warrior
                                                               26639, // Drakkari Shaman"
                                                               27431, // Drakkari Commander
	                                                        };

	    private static readonly List<uint> QuestsAtEntrance = new List<uint> 
	    {
	        QuestId_HeadGames,
	        QuestId_WhatTheScourgeDred
	    };

	    [EncounterHandler(58149, "Image of Drakuru", Mode = CallBehaviorMode.Proximity, BossRange = 35)]
        [EncounterHandler(28016, "Drakuru", Mode = CallBehaviorMode.Proximity, BossRange = 35)]
	    [EncounterHandler(55677, "Kurzel", Mode = CallBehaviorMode.Proximity, BossRange = 35)]
	    public async Task<bool> QuestPickupTurninHandler(WoWUnit npc)
	    {
	        if (Me.Combat || ScriptHelpers.WillPullAggroAtLocation(npc.Location))
	            return false;
	        // pickup or turnin quests if any are available.
	        return npc.HasQuestAvailable(true)
	            ? await ScriptHelpers.PickupQuest(npc)
	            : npc.HasQuestTurnin() && await ScriptHelpers.TurninQuest(npc);
	    }

        [LocationHandler(-379.2697, -737.7283, 27.22988, 50, "Head Games")]
	    public async Task<bool> HeadGamesHandler(WoWPoint location)
	    {
            if (BotPoi.Current.Type != PoiType.None
                || !ScriptHelpers.HasQuest(QuestId_HeadGames)
                || ScriptHelpers.IsQuestInLogComplete(QuestId_HeadGames))
            {
                return false;
            }

            var unit = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_NovosTheSummoner);
            if (unit == null || !unit.IsDead)
                return false;

            var scrap = Me.CarriedItems.FirstOrDefault(i => i.Entry == ItemId_KurzelsBlouseScrap);
            if (scrap == null)
                return false;

            if (!unit.WithinInteractRange)
                return (await CommonCoroutines.MoveTo(unit.Location)).IsSuccessful();

            await CommonCoroutines.StopMoving();
            scrap.UseContainerItem();
            await Coroutine.Wait(3000, () => ScriptHelpers.IsQuestInLogComplete(QuestId_HeadGames));
            return true;
	    }

	    private async Task<bool> CleansingDrakTharondHandler()
	    {
	        if (!ScriptHelpers.HasQuest(QuestId_CleansingDrakTharon))
	            return false;

            var elixir = Me.CarriedItems.FirstOrDefault(i => i.Entry == ItemId_DrakurusElixir);
	        if (elixir == null )
	            return false;

	        var drakuru = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_Drakuru);
	        if (drakuru == null)
	        {
                if (Me.GetCarriedItemCount(ItemId_EnduringMojo) < 5)
	                return false;

	            if (!Navigator.AtLocation(_cleansingDrakTharonObjectiveLoc))
	                return (await CommonCoroutines.MoveTo(_cleansingDrakTharonObjectiveLoc)).IsSuccessful();

	            elixir.UseContainerItem();
	            await Coroutine.Sleep(2000);
	            return true;
	        }

	        if (drakuru.HasQuestTurnin())
	            return false;

	        if (drakuru.Distance > 25)
	            return (await CommonCoroutines.MoveTo(drakuru.Location)).IsSuccessful();

	        TreeRoot.StatusText = "Waiting for quest to complete";
	        return true;
	    }

	    #endregion

        readonly WoWPoint _lastBossLocation = new WoWPoint(-236.8264,-675.4053,131.8646);

        [EncounterHandler(0, "Root")]
        public System.Func<WoWUnit,Task<bool>> RootLogic()
        {
            var shadowVoidIds = new uint[] { 55847, 59014 };

            AddAvoidObject(ctx => true, 4, shadowVoidIds);
            // port outside and back in to hand in quests once dungeon is complete.
            // QuestPickupTurninHandler will handle the turnin
            return async npc =>
            {              
                if (IsComplete && BotPoi.Current.Type == PoiType.None)
                {
                    if (await CleansingDrakTharondHandler())
                        return true;

                    return QuestsAtEntrance.Any(ScriptHelpers.IsQuestInLogComplete)
                        && Me.Location.DistanceSqr(_lastBossLocation) < 50*50
                        && await ScriptHelpers.PortOutsideAndBackIn();
                }
                return false;
            };
        }

		[EncounterHandler(26624, "Wretched Belcher")]
		public Composite WretchedBelcherEncounter()
		{
			WoWUnit unit = null;
			return new PrioritySelector(
				ctx => unit = ctx as WoWUnit,
				ScriptHelpers.CreateAvoidUnitAnglesBehavior(
					ctx => Me.IsFollower() && unit.CurrentTargetGuid != Me.Guid && !unit.IsMoving && unit.Distance < 15, ctx => unit, new ScriptHelpers.AngleSpan(0, 180)),
				new Decorator(ctx => StyxWoW.Me.CurrentTargetGuid == unit.Guid && unit.CurrentTargetGuid == Me.Guid, ScriptHelpers.CreateTankFaceAwayGroupUnit(15)));
		}


		[EncounterHandler(26630, "Trollgore", Mode = CallBehaviorMode.Proximity, BossRange = 110)]
		public Composite TrollgoreEncounter()
		{
			WoWUnit boss = null;
			List<WoWUnit> belchers = null;
			const uint TrollgoreId = 26630;
			const uint wretchedBelcherId = 26624;
			var corpseExposionIds = new[] { 49555, 59807 };

			var trashTankLoc = new WoWPoint(-355.6056, -624.7963, 11.02102);
			var followerWaitLoc = new WoWPoint(-347.375, -614.9806, 11.01204);
			var pullLoc = new WoWPoint(-338.9121, -630.8854, 11.38);
			var roomCenterLoc = new WoWPoint(-312.3778, -659.7048, 10.28416);

			AddAvoidObject(
				ctx => !Me.IsCasting,
				5,
				o => o.Entry == TrollgoreId && corpseExposionIds.Contains(o.ToUnit().CastingSpellId) && o.ToUnit().CurrentTargetGuid.IsValid,
				o => o.ToUnit().CurrentTarget.Location);

			return new PrioritySelector(
				ctx =>
				{
					belchers = ScriptHelpers.GetUnfriendlyNpsAtLocation(roomCenterLoc, 30, u => u.Entry == wretchedBelcherId);
					return boss = ctx as WoWUnit;
				},
				ScriptHelpers.CreatePullNpcToLocation(
					ctx => belchers.Any(),
					ctx => belchers[0].DistanceSqr <= 40 * 40 && (belchers.Count == 1 || belchers[0].Location.DistanceSqr(belchers[1].Location) > 25 * 25),
					ctx => belchers[0],
					ctx => trashTankLoc,
					ctx => StyxWoW.Me.IsTank() ? pullLoc : followerWaitLoc,
					10));
		}

	    private const uint MobId_NovosTheSummoner = 26631;

		[EncounterHandler(26631, "Novos the Summoner")]
		public Composite NovosTheSummonerEncounter()
		{
			const uint arcaneFieldId = 47346;
			const uint blizardId = 49034;
			AddAvoidObject(ctx => true, 12, arcaneFieldId);
			AddAvoidObject(ctx => !Me.IsCasting, 8, blizardId);

			return new PrioritySelector();
		}

	    private const uint MobId_KingDred = 27483;

		[EncounterHandler(27483, "King Dred", Mode = CallBehaviorMode.Proximity, BossRange = 120)]
		public Composite KingDredEncounter()
		{
			WoWUnit boss = null;
			var tankLoc = new WoWPoint(-494.3439, -721.4702, 30.24773);
			var dredSafeLoc = new WoWPoint(-535.8426, -664.3137, 30.2464);
			var trashLoc = new WoWPoint(-525.6827, -714.9271, 30.24642);
			WoWUnit trash = null;
			return new PrioritySelector(
				ctx =>
				{
					boss = ctx as WoWUnit;
					trash = ScriptHelpers.GetUnfriendlyNpsAtLocation(trashLoc, 30, u => u != boss).FirstOrDefault();
					return boss;
				},
				new Decorator(
					ctx => !boss.Combat,
					new PrioritySelector(
						new Decorator(
							ctx => StyxWoW.Me.IsTank() && Targeting.Instance.FirstUnit == null && StyxWoW.Me.Location.DistanceSqr(tankLoc) > 25 * 25,
							new Action(ctx => ScriptHelpers.SetLeaderMoveToPoiPS(tankLoc))),

						ScriptHelpers.CreatePullNpcToLocation(
							ctx => trash != null && Me.Location.DistanceSqr(tankLoc) < 40 * 40 && !Me.Combat, 
							ctx => boss.Location.DistanceSqr(dredSafeLoc) <= 20 * 20 ,
							ctx => trash, 
							ctx => tankLoc, 
							ctx => tankLoc, 
							10),

						ScriptHelpers.CreatePullNpcToLocation(
							ctx => trash == null && Me.Location.DistanceSqr(tankLoc) < 40 * 40 && !Me.Combat, 
							ctx => boss.Location.DistanceSqr(trashLoc) <= 25 * 25 , 
							ctx => boss, 
							ctx => tankLoc, 
							ctx => tankLoc, 
							10))));
		}

		[EncounterHandler(26632, "The Prophet Tharon'ja")]
		public Composite TheProphetTharonjaEncounter()
		{
			var badstuffIds = new uint[] { 49548, 59969, 49518, 59971 };
			AddAvoidObject(ctx => true, 7, badstuffIds);

			return new PrioritySelector(new Decorator(ctx => StyxWoW.Me.HasAura("Gift of Tharon'ja"), new Action(ctx => Lua.DoString(ZombieDpsRotation))));
		}
	}
}