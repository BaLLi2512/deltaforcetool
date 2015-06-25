using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Vector2 = Tripper.Tools.Math.Vector2;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Scenarios.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class ProvingGrounds : Dungeon
	{
		#region Overrides of Dungeon
	
		public override uint DungeonId
		{
			get { return 640; }
		}

        public override void RemoveTargetsFilter(List<WoWObject> units)
        {
			units.RemoveAll(
				ret =>
				{
				    var unit = ret as WoWUnit;
				    if (unit == null)
				        return false;

					return false;
				});
		}

	    public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	    {
			var isTank = Me.Specialization.IsTank();

            foreach (var obj in incomingunits)
            {
                var unit = obj as WoWUnit;
                if (unit != null)
                {
	                if (isTank && PartyMembers.Contains(unit.CurrentTarget))
		                outgoingunits.Add(unit);
					else if (unit.Entry == MobId_LargeIllusionaryBananaTosser)
						outgoingunits.Add(unit);
                }
            }
	    }

	    public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
	    {
		    var isTank = Me.Specialization.IsTank();

            foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					if (isTank && PartyMembers.Contains(unit.CurrentTarget))
						priority.Score += 10000;

                    switch (unit.Entry)
                    {
						case MobId_SmallIllusionaryGuardian:
						case MobId_SmallIllusionaryFlamecaller:
							priority.Score += 2000;
							break;
						case MobId_LargeIllusionaryGuardian:
						case MobId_LargeIllusionaryFlamecaller:
							priority.Score += 2500;
							break;
						case MobId_SmallIllusionaryAmberWeaver:
						case MobId_SmallIllusionaryWindGuard:
							priority.Score += 3000;
							break;
						case MobId_LargeIllusionaryAmberWeaver:
						case MobId_LargeIllusionaryWindGuard:
							priority.Score += 3500;
							break;
						case MobId_SmallIllusionaryMystic:
						case MobId_SmallIllusionaryAmbusher:
							priority.Score += 4000;
		                    break;
						case MobId_LargeIllusionaryMystic:
						case MobId_LargeIllusionaryAmbusher:
		                    priority.Score += 4500;
		                    break;
                    }
				}
			}
		}

		public override void IncludeHealTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			// Ensure that the NPC party members are included in Heal targeting.
			PartyMembers.ForEach(p => outgoingObjects.Add(p));
		}

		public override void WeighHealTargetsFilter(List<Targeting.TargetPriority> objPriorities)
		{
			foreach (var priority in objPriorities)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					// Bleed overtime that only gets removed if healed above 90%
					if (unit.HasAura("Chomp"))
						priority.Score += 50;
				}
			}
		}

		#endregion
		
		#region Root

		private LocalPlayer Me { get { return StyxWoW.Me; } }

		private PerFrameCachedValue<List<WoWUnit>> _partyMembers;

		private List<WoWUnit> PartyMembers
		{
			get
			{
				return _partyMembers ??
					   (_partyMembers =
						   new PerFrameCachedValue<List<WoWUnit>>(
							   () =>
							   {
								   return Me.GroupInfo.PartyMemberGuids.Where(g => Me.Guid != g)
									   .Select(ObjectManager.GetObjectByGuid<WoWUnit>).ToList();
							   }));
			}
		}

		private bool CompletedQuest(uint questId)
		{
			return Me.QuestLog.GetCompletedQuests().Contains(questId);
		}

		private const uint MobId_TrialMasterRotun = 61636;
		private readonly WoWPoint RoomCenterLoc = new WoWPoint(3795.656, 533.6826, 639.0075);
		private readonly HashSet<uint> _mobsThatTankingAwayFromGroup = new HashSet<uint>
																	   {
																		   MobId_SmallIllusionaryConqueror,
																		   MobId_LargeIllusionaryConqueror,
																		   MobId_SmallIllusionaryAmbusher,
																		   MobId_LargeIllusionaryAmbusher,
																	   };
		[EncounterHandler(0, "Root")]
		public Func<WoWUnit, Task<bool>> RootLogic()
		{
			AddAvoidObject(
				3.5f,
				o => o.Entry == AreaTriggerId_InvokeLava_Tank || o.Entry == AreaTriggerId_InvokeLava_Healer,
				ignoreIfBlocking: true);

			AddAvoidObject(4, GameObjectId_ProvingGroundsBarricade);
			AddAvoidObject(
				10,
				o =>
					(o.Entry == MobId_LargeIllusionaryConqueror || o.Entry == MobId_SmallIllusionaryConqueror) 
					&& o.ToUnit().CastingSpellId == SpellId_PowerfulSlam,
				o => o.Location.RayCast(o.Rotation, 9));


			AddAvoidObject(
				ctx => Me.Specialization.IsTank() && Targeting.Instance.TargetList.All(t => t.Aggro) 
					&& Targeting.Instance.TargetList.Any(t => _mobsThatTankingAwayFromGroup.Contains(t.Entry)),
				12,
				o => PartyMembers.Contains(o));

			return async npc =>
			{
				if (CurrentWave == 0)
				{
					if (await CheckCompletion())
						return true;

					// start trial
					var stage = ScenarioInfo.Current.CurrentStage;
					if (stage != null && stage.NumberOfCriteria == 2 && !stage.GetCriteria(1).IsComplete && await TalkToTrialMaster())
						return true;
				}
				else
				{
					if (await DamageLogic() || await HealerLogic())
						return true;
				}

				// Move to center of room of doing nothing else.
				if (!Me.Combat && Targeting.Instance.IsEmpty() &&  Me.Location.DistanceSqr(RoomCenterLoc) > 10 * 10)
					return (await CommonCoroutines.MoveTo(RoomCenterLoc)).IsSuccessful();

				return false;
			};
		}

		private async Task<bool> TalkToTrialMaster()
		{
			var trialMaster = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_TrialMasterRotun);
			if (trialMaster == null)
				return false;

			if (!trialMaster.WithinInteractRange)
				return (await CommonCoroutines.MoveTo(trialMaster.Location)).IsSuccessful();

			if (!GossipFrame.Instance.IsVisible)
			{
				trialMaster.Interact();
				await CommonCoroutines.SleepForRandomUiInteractionTime();
				return true;
			}

			// Sometimes no gossip options are shown unless gossip frame is closed and reopened.
			if (!GossipFrame.Instance.GossipOptionEntries.Any())
			{
				GossipFrame.Instance.Close();
				await CommonCoroutines.SleepForRandomUiInteractionTime();
				return true;
			}

			var desiredServerIndex = (int)DesiredTrial;
			foreach (var gossipOption in GossipFrame.Instance.GossipOptionEntries)
			{
				if (gossipOption.ServerIndex == desiredServerIndex)
				{
					Logging.WriteDiagnostic("Selecting gossip option for {0}", DesiredTrial);
					GossipFrame.Instance.SelectGossipOption(gossipOption.Index);
					await CommonCoroutines.SleepForRandomUiInteractionTime();
					return true;
				}
			}

			Logging.WriteDiagnostic("No gossip option for {0} is available", DesiredTrial);
			Logging.WriteDiagnostic("Available gossip options are");
			foreach (var gossipOption in GossipFrame.Instance.GossipOptionEntries)
				Logging.WriteDiagnostic("\t{0}", gossipOption);
			
			return false;
		}


		private async Task<bool> CheckCompletion()
		{
			string reason;
			if (IsComplete(out reason))
			{
				// port outside and stop
				Lua.DoString("LeaveParty()");
				TreeRoot.Stop(reason);
				return true;
			}
			return false;
		}

		private bool IsComplete(out string reason)
		{
			var avgItemLevel = Me.AverageItemLevel;

			if (avgItemLevel < 595)
			{
				reason = "Item level must be >= 595 do proving grounds";
				return true;
			}
			if (avgItemLevel < 610 && IsBronzeTrialDone)
			{
				reason =
					string.Format(
						"All trials that can be done at current item level ({0}) are done. " +
						"Increase your item level to 610 or more to do the next trial.",
						Me.AverageItemLevel);
				return true;
			}

			if (IsSilverTrialDone)
			{
				reason = "All scripted trials are complete";
				return true;
			}
			reason = null;
			return false;
		}

		private enum Trial
		{
			BronzeDamage, SilverDamage, GoldDamage,
			BronzeTank=4, SilverTank, GoldTank,
			BronzeHealer=8, SilverHealer, GoldHealer,
		}

		private const int QuestId_DamageBronze = 37212;
		private const int QuestId_DamageSilver = 37213;
		private const int QuestId_DamageGold = 37214;

		private const int QuestId_TankBronze = 37215;
		private const int QuestId_TankSilver = 37216;
		private const int QuestId_TankGold = 37217;

		private const int QuestId_HealerBronze = 37218;
		private const int QuestId_HealerSilver = 37219;
		private const int QuestId_HealerGold = 37220;

		private bool IsBronzeDamageDone { get { return CompletedQuest(QuestId_DamageBronze); } }
		private bool IsBronzeTankDone { get { return CompletedQuest(QuestId_TankBronze); } }
		private bool IsBronzeHealerDone { get { return CompletedQuest(QuestId_HealerBronze); } }

		private bool IsSilverDamageDone { get { return CompletedQuest(QuestId_DamageSilver); } }
		private bool IsSilverTankDone { get { return CompletedQuest(QuestId_TankSilver); } }
		private bool IsSilverHealerDone { get { return CompletedQuest(QuestId_HealerSilver); } }

		private bool IsGoldDamageDone { get { return CompletedQuest(QuestId_DamageGold); } }
		private bool IsGoldTankDone { get { return CompletedQuest(QuestId_TankGold); } }
		private bool IsGoldHealerDone { get { return CompletedQuest(QuestId_HealerGold); } }

		private bool IsBronzeTrialDone
		{
			get
			{
				return Me.Specialization.IsTank()
					? IsBronzeTankDone
					: (Me.Specialization.IsHealer() ? IsBronzeHealerDone : IsBronzeDamageDone);
			}
		}

		private bool IsSilverTrialDone
		{
			get
			{
				return Me.Specialization.IsTank()
					? IsSilverTankDone
					: (Me.Specialization.IsHealer() ? IsSilverHealerDone : IsSilverDamageDone);
			}
		}

		private bool IsGoldTrialDone
		{
			get
			{
				return Me.Specialization.IsTank() ? IsGoldTankDone : (Me.Specialization.IsHealer() ? IsGoldHealerDone : IsGoldDamageDone);
			}
		}

		private Trial DesiredTrial
		{
			get
			{
				if (Me.Specialization.IsTank())
					return IsSilverTankDone ? Trial.GoldTank : (IsBronzeTankDone ? Trial.SilverTank : Trial.BronzeTank);
				if (Me.Specialization.IsHealer())
					return IsSilverHealerDone ? Trial.GoldHealer : (IsBronzeHealerDone ? Trial.SilverHealer : Trial.BronzeHealer);
				// Damage
				return IsSilverDamageDone ? Trial.GoldDamage : (IsBronzeDamageDone ? Trial.SilverDamage : Trial.BronzeDamage);
			}
		}

		private int _difficulty, _currentWave, _maxWave, _duration;
		private uint _lastFrameCount;

		private int Difficulty { get { return GetScenarioInfo(ScenarioInfoType.Difficulty); } }
		private int CurrentWave { get { return GetScenarioInfo(ScenarioInfoType.CurrentWave); } }
		private int MaxWave { get { return GetScenarioInfo(ScenarioInfoType.MaxWave); } }
		private int Duration { get { return GetScenarioInfo(ScenarioInfoType.Duration); } }

		private enum ScenarioInfoType
		{
			Difficulty,
			CurrentWave,
			MaxWave,
			Duration
		}

		private int GetScenarioInfo(ScenarioInfoType scenarioInfoType)
		{
			if (StyxWoW.Memory.Executor.FrameCount != _lastFrameCount)
			{
				GetScenarioInfo(out _difficulty, out _currentWave, out _maxWave, out _duration);
				_lastFrameCount = StyxWoW.Memory.Executor.FrameCount;
			}

			switch (scenarioInfoType)
			{
				case ScenarioInfoType.Difficulty:
					return _difficulty;
				case ScenarioInfoType.CurrentWave:
					return _currentWave;
				case ScenarioInfoType.MaxWave:
					return _maxWave;
				case ScenarioInfoType.Duration:
					return _duration;
			}
			throw new ArgumentException("scenarioInfoType");
		}

		private static void GetScenarioInfo(out int difficulty, out int currentWave, out int maxWave, out int duration)
		{
			var values = Lua.GetReturnValues("return C_Scenario.GetProvingGroundsInfo()");
			difficulty = int.Parse(values[0]);
			currentWave = int.Parse(values[1]);
			maxWave = int.Parse(values[2]);
			duration = int.Parse(values[3]);
		}

		#endregion


		#region Tank
		private const uint GameObjectId_ProvingGroundsBarricade = 221037;

		private const int SpellId_PowerfulSlam = 144401;
		private const int SpellId_Enrage = 144404;

		private const uint MobId_SmallIllusionaryFlamecaller = 71836;
		private const uint MobId_LargeIllusionaryFlamecaller = 71835;

		private const uint MobId_LargeIllusionaryWindGuard = 71833;
		private const uint MobId_SmallIllusionaryWindGuard = 71834;

		private const uint MobId_SmallIllusionaryConqueror = 71842;
		private const uint MobId_LargeIllusionaryConqueror = 71841;

		private const uint MobId_LargeIllusionaryAmbusher = 71838;
		private const uint MobId_SmallIllusionaryAmbusher = 71839;

		[EncounterHandler((int)MobId_LargeIllusionaryConqueror, "Illusionary Conqueror")]
		[EncounterHandler((int)MobId_SmallIllusionaryConqueror, "Illusionary Conqueror")]
		public Func<WoWUnit, Task<bool>> IllusionaryConquerorLogic()
		{
			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_Enrage);
		}

		#endregion

		#region Damage

		private const uint MobId_LargeIllusionaryBananaTosser = 71414;

		private const uint MobId_SmallIllusionaryMystic = 71076;
		private const uint MobId_LargeIllusionaryMystic = 71069;

		private const uint MobId_SmallIllusionaryAmberWeaver = 71077;
		private const uint MobId_LargeIllusionaryAmberWeaver = 71068;
	
		private const uint MobId_SmallIllusionaryGuardian = 71079;
		private const uint MobId_LargeIllusionaryGuardian = 71064;

		private readonly HashSet<uint> MobIds_IllusionaryGuardian = new HashSet<uint>
																	{
																		MobId_SmallIllusionaryGuardian,
																		MobId_LargeIllusionaryGuardian
																	};
		private const uint MobId_VolatileAmberGlobule = 73332;
		private const int SpellId_AmberGlobule = 142189;
		private const int SpellId_HealIllusion = 142238;
		private const int MissileSpellId_Bananastorm = 142628;

		const uint AreaTriggerId_InvokeLava_Tank = 5267;
		const uint AreaTriggerId_InvokeLava_Healer = 5268;


		readonly PerFrameCachedValue<WoWPoint> GlobuleKiteLocation = new PerFrameCachedValue<WoWPoint>(
			() =>
			{
				var amberGlobule = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_VolatileAmberGlobule);
				WoWPoint amberGlobuleLoc;
				if (amberGlobule != null)
				{
					amberGlobuleLoc = amberGlobule.Location;
				}
				else
				{
					var amberweaver = ObjectManager.GetObjectsOfType<WoWUnit>()
							.FirstOrDefault(u => u.CastingSpellId == SpellId_AmberGlobule && StyxWoW.Me.CurrentTargetGuid != u.Guid);

					if (amberweaver == null)
						return WoWPoint.Zero;

					amberGlobuleLoc = amberweaver.Location.RayCast(amberweaver.Rotation, 5);
				}

				WoWUnit standBehindMob = StyxWoW.Me.IsMelee() 
					? (StyxWoW.Me.CurrentTarget ?? ScriptHelpers.GetUnfriendlyNpsAtLocation(StyxWoW.Me.Location, 50).FirstOrDefault())
					:ScriptHelpers.GetUnfriendlyNpsAtLocation(StyxWoW.Me.Location, 50).FirstOrDefault();

				if (standBehindMob == null)
					return WoWPoint.Zero;

				return WoWMathHelper.CalculatePointFrom(amberGlobuleLoc, standBehindMob.Location, -4);
			});

		private async Task<bool> DamageLogic()
		{
			if (GlobuleKiteLocation != WoWPoint.Zero)
			{
				return await ScriptHelpers.StayAtLocationWhile(
							() => GlobuleKiteLocation != WoWPoint.Zero,
							GlobuleKiteLocation,
							"Globule Kite Location",
							1.5f);
			}

			var currentTarget = Me.CurrentTarget;

			if (currentTarget != null)
			{
				if (MobIds_IllusionaryGuardian.Contains(currentTarget.Entry))
				{
					if (!currentTarget.MeIsSafelyBehind)
					{
						var stayAtLoc = WoWMathHelper.CalculatePointBehind(currentTarget.Location, currentTarget.Rotation, 3.5f);
						return await ScriptHelpers.StayAtLocationWhile(
									() => ScriptHelpers.IsViable(currentTarget) && currentTarget.IsAlive && !currentTarget.MeIsSafelyBehind,
									stayAtLoc,
									"Location behind Guardian",
									1.5f);
					}
				}
				else if (currentTarget.Entry == MobId_LargeIllusionaryBananaTosser)
				{
					//if (currentTarget.DistanceSqr > 17*17)
					//{
					//	return await ScriptHelpers.MoveToContinue(
					//		() => currentTarget.Location,
					//		() =>ScriptHelpers.IsViable(currentTarget)
					//			 && currentTarget.Entry == MobId_LargeIllusionaryBananaTosser && currentTarget.IsAlive,
					//		true);
					//}
				}
			}

			return false;
		}

		private async Task<bool> HealerLogic()
		{
			if (await ScriptHelpers.DispelGroup("Aqua Bomb", ScriptHelpers.PartyDispelType.Magic))
				return true;
			return false;
		}

		[EncounterHandler((int)MobId_LargeIllusionaryMystic, "Large Illusionary Mystic")]
		[EncounterHandler((int)MobId_SmallIllusionaryMystic, "Small Illusionary Mystic")]
		public Func<WoWUnit, Task<bool>> IllusionaryMysticLogic()
		{
			return async npc => npc.Guid == Me.CurrentTargetGuid && await ScriptHelpers.InterruptCast(npc, SpellId_HealIllusion);
		}

		[EncounterHandler((int)MobId_SmallIllusionaryAmberWeaver, "Large Illusionary AmberWeaver")]
		[EncounterHandler((int)MobId_LargeIllusionaryAmberWeaver, "Small Illusionary AmberWeaver")]
		public Func<WoWUnit, Task<bool>> IllusionaryAmberWeaverLogic()
		{
			AddAvoidObject(6, MobId_VolatileAmberGlobule);
			return async npc => npc.Guid == Me.CurrentTargetGuid && await ScriptHelpers.InterruptCast(npc, SpellId_AmberGlobule);
		}

		[EncounterHandler((int)MobId_LargeIllusionaryBananaTosser, "Large Illusionary BananaTosser")]
		[EncounterHandler((int)MobId_LargeIllusionaryBananaTosser, "Small Illusionary BananaTosser")]
		public Func<WoWUnit, Task<bool>> IllusionaryBananaTosserLogic()
		{
			AddAvoidLocation(
				ctx => !Me.IsRange() || !Me.IsCasting,
				o => Me.IsRange() && Me.IsMoving ? 6 : 1,
				o => ((WoWMissile)o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_Bananastorm));

			return async npc => false;
		}
		#endregion


	}

}