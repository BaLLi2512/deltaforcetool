using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Enums;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Holidays
{
	class TheHeadlessHorseman : Dungeon
	{

		#region Overrides of Dungeon

        public override WoWPoint Entrance
        {
            get { return new WoWPoint(2920.317, -799.8921, 160.3323); }
        }

        public override WoWPoint ExitLocation
        {
            get { return new WoWPoint(1124.471, 504.2796, 0.9892024); }
        }

		public override uint DungeonId
		{
            get { return 285; }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
					var unit = ret.ToUnit();
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
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
				    if (unit.Entry == HeadoftheHorsemanId)
				        priority.Score += 5000;
				}
			}
		}

		#endregion

        const uint HeadoftheHorsemanId = 23775;
        const uint PulsingPumpkinId = 23694;

        private readonly WaitTimer _lootFilledPumpkinTimer = new WaitTimer(TimeSpan.FromSeconds(30));

		LocalPlayer Me { get { return StyxWoW.Me; } }

		[EncounterHandler(0)]
		public Func<WoWUnit, Task<bool>> RootBehavior()
		{
            return async npc =>
			{
                // open the Loot filled pumpkin container item
                if (_lootFilledPumpkinTimer.IsFinished && BotPoi.Current.Type == PoiType.None)
                {
                    var lootFilledPumpkin = Me.CarriedItems.FirstOrDefault(i => i.Entry == ItemId_LootFieldPumpkin);
                    if (lootFilledPumpkin != null)
                    {
                        lootFilledPumpkin.UseContainerItem();
                        if (await Coroutine.Wait(3000, () => LootFrame.Instance.IsVisible))
                        {
                            LootFrame.Instance.LootAll();
                            await Coroutine.Wait(3000, () => !lootFilledPumpkin.IsValid);
                        _lootFilledPumpkinTimer.Reset();
                            return true;
                        }
                    }
                }
			    return false;
			};
		}

	    private const uint ItemId_LootFieldPumpkin = 117392;

	    [ObjectHandler(186267, "Pumpkin Shrine")]
	    public async Task<bool> PumpkinShrineHandler(WoWGameObject pumpkin)
	    {
	        if (BotPoi.Current.Type != PoiType.None || !pumpkin.CanUse())
	            return false;

	        if (!pumpkin.WithinInteractRange)
	            return (await CommonCoroutines.MoveTo(pumpkin.Location)).IsSuccessful();

	        await CommonCoroutines.StopMoving();
	        if (!GossipFrame.Instance.IsVisible)
	        {
                pumpkin.Interact();
	            await ScriptHelpers.SleepForRandomUiInteractionTime();
	            return true;
	        }
            
            GossipFrame.Instance.SelectGossipOption(0);
	        return true;
	    }

	    private const uint HeadlessHorsemanId = 23682;

        [EncounterHandler(23682, "Headless Horseman", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> HeadlessHorsemanEncounter()
	    {
            // don't get cleaved..
            AddAvoidObject(ctx => Me.IsFollower(), 6, o => o.Entry == HeadlessHorsemanId && o.ToUnit().CurrentTargetGuid != Me.Guid,
                o => o.Location.RayCast(o.Rotation, 5));

            return async boss =>
            {

                return false;
            };
	    } 

	}
}
