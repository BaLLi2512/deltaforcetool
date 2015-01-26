using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Enums;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
namespace Bots.DungeonBuddy.Dungeon_Scripts.Mists_of_Pandaria
{
	class Coren_Direbrew : Dungeon
	{

		#region Overrides of Dungeon

        public override WoWPoint Entrance
        {
            get { return new WoWPoint(-7178.79, -925.1274, 166.8448); }
        }

        public override WoWPoint ExitLocation
        {
            get { return new WoWPoint(457.0491, 38.14, -68.74); }
        }

		public override uint DungeonId
		{
			get { return 287; }
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
		    var tank = ScriptHelpers.Leader;
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
				    if (unit.Entry == UrsulaDirebrewId)
				    {
                        // dps should try to get aggro on Ursula since she stuns highest threat target and best if that's not the tank.
				        var shouldPull = tank != null && !tank.IsMe &&
				                         unit.GetThreatInfoFor(tank).ThreatStatus >= ThreatStatus.NoobishTank;
				        priority.Score += shouldPull ? 5000 : -5000;
				    }
				}
			}
		}

		#endregion

	    private const uint IlsaDirebrewId = 26764;
        private const uint UrsulaDirebrewId = 26822;

		LocalPlayer Me { get { return StyxWoW.Me; } }

		[EncounterHandler(0)]
		public Func<WoWUnit, Task<bool>> RootBehavior()
		{
			return async npc =>
						{
							return false;
						};
		}

	    [EncounterHandler(23872, "Coren Direbrew", Mode = CallBehaviorMode.Proximity)]
	    public Func<WoWUnit, Task<bool>> CorenDirebrewEncounter()
	    {
            return async boss =>
            {
                if (!boss.Combat && Me.IsLeader() && !Me.Combat)
                {
                    if (boss.DistanceSqr > 15)
                        return (await CommonCoroutines.MoveTo(boss.Location)).IsSuccessful();
                    // wait for group members to gather around.
                    if (! ScriptHelpers.GroupMembers.All(g => g.Distance < 40))
                        return true;
                    return await ScriptHelpers.TalkToNpc(boss, 1, 1);
                }
                return false;
            };
	    } 

	}
}
