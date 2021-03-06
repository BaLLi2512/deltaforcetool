﻿// Behavior originally contributed by BarryDurex
// Credits: [Bot]MrFishIt by Nesox | [Bot]PoolFishingBuddy by Iggi66
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//   

#region Summary and Documentation
// QUICK DOX:
// MRFISHIT was designed to fulfill quests where you needed a certain amount
// of a particular item acquired by fishing.
//
// BEHAVIOR ATTRIBUTES:
//
// Basic Attributes:
// *** ALSO see the documentation in QuestBehaviorBase.cs.  All of the attributes it provides
// *** are available here, also.  The documentation on the attributes QuestBehaviorBase provides
// *** is _not_ repeated here, to prevent documentation inconsistencies.
//
//      CollectItemId [REQUIRED]
//          Specifies the Id of the item we want to collect.
//      PoolId [REQUIRED, if X/Y/Z is not provided;  Default: none]
//          This Pool will must  faced for each fishing cast,
//          such that the bobber always falls into the water.
//      X/Y/Z [REQUIRED, if PoolId is not provided;  Default: none]
//          This specifies the location that should be faced.  This point
//          is somewhere on the surface of the water.
//
// Tunables:
//      BaitId [optional;  Default: none]
//          Specifies the bait item id. If not specified then any bait in bags is used. 
//      CollectItemCount [optional;  Default: 1]
//          Specifies the number of items that must be collected.
//          The behavior terminates when we have this number of CollectItemId
//          or more in our inventory.
//			This can be a math expression e.g. CollectItemCount="10 - GetItemCount(1234)"
//      MaxCastRange [optional;  Default: 20]
//          [Only meaningful if PoolId is specified.]
//          Specifies the maximum cast range to the pool.  If the toon is not within
//          this range of PoolId, the behavior will move the toon closer.
//      MinCastRange [optional;  Default: 15]
//          [Only meaningful if PoolId is specified.]
//          Specifies the minimum cast range to the pool.  If the toon is closer than
//          this range of PoolId, the behavior will move the toon further away.
//      MoveToPool [optional;  Default: true]
//          [Only meaningful if PoolId is specified.]
//          If true, the behavior should find the place to fish.
//      QuestId [optional; Default: none]
//          Specifies the QuestId, if the item is the only thing to complete this quest.
//      UseFishingGear [optional; Default: false]
//          Equips fishing poles and hats using highest stats
//      
//
// THINGS TO KNOW:
// * The original documenation can be found here:
//       https://www.thebuddyforum.com/honorbuddy-forum/honorbuddy-profiles/neutral/96244-quest-behavior-mrfishit-fishing-questitems.html
//
#endregion


#region Examples
//    <CustomBehavior File="MrFishIt"
//                    <!-- one or more of the following attributes must be specified -->
//                    X="-1972.388" Y="6277.719" Z="56.86252"   <!-- X/Y/Z and PoolId are mutually exclusive -->
//                    PoolId="212169"                           <!-- X/Y/Z and PoolId are mutually exclusive -->
//                    CollectItemId="195492"
//
//                   <!-- may/may not be optional  -->
//                    CollectItemCount="6"
//                    QuestId="14069"  
//                    MoveToPool="true"
//                    MaxCastRange="20"
//                    MinCastRange="15"
//     />
#endregion


#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bots.Grind;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DB;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;
#endregion


namespace Honorbuddy.Quest_Behaviors.MrFishIt
{
	[CustomBehaviorFileName(@"MrFishIt")]
	class MrFishIt : QuestBehaviorBase
	{
		public MrFishIt(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;

			try
			{
				var collectItemCountExpression = GetAttributeAs("CollectItemCount", false, ConstrainAs.StringNonEmpty, null);
				CollectItemCountCompiledExpression = Utility.ProduceParameterlessCompiledExpression<int>(collectItemCountExpression);
				CollectItemCount = Utility.ProduceCachedValueFromCompiledExpression(CollectItemCountCompiledExpression, 1);

				CollectItemId = GetAttributeAsNullable<int>("CollectItemId", true, ConstrainAs.ItemId, null) ?? 0;
				PoolFishingBuddy.MaxCastRange = GetAttributeAsNullable<int>("MaxCastRange", false, ConstrainAs.ItemId, null) ?? 20;
				PoolFishingBuddy.MinCastRange = GetAttributeAsNullable<int>("MinCastRange", false, ConstrainAs.ItemId, null) ?? 15;
				MoveToPool = GetAttributeAsNullable<bool>("MoveToPool", false, null, null) ?? true;
				PoolId = GetAttributeAsNullable<int>("PoolId", false, ConstrainAs.ItemId, null) ?? 0;
				TestFishing = GetAttributeAsNullable<bool>("TestFishing", false, null, null) ?? false;
				WaterPoint = GetAttributeAsNullable<WoWPoint>("", false, ConstrainAs.WoWPointNonEmpty, null) ?? WoWPoint.Empty;
				UseFishingGear = GetAttributeAsNullable<bool>("UseFishingGear", false, null, null) ?? false;
				BaitId = GetAttributeAsNullable<int>("BaitId", false, null, null);
			}

			catch (Exception except)
			{
				// Maintenance problems occur for a number of reasons.  The primary two are...
				// * Changes were made to the behavior, and boundary conditions weren't properly tested.
				// * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
				// In any case, we pinpoint the source of the problem area here, and hopefully it
				// can be quickly resolved.
				QBCLog.Exception(except);
				IsAttributeProblem = true;
			}
		}


		// Attributes provided by caller
		private PerFrameCachedValue<int> CollectItemCount { get; set; }
		
		[CompileExpression]
		public DelayCompiledExpression<Func<int>> CollectItemCountCompiledExpression { get; private set; }

		public int CollectItemId { get; private set; }
		public bool MoveToPool { get; private set; }
		public static int PoolId { get; private set; }
		public bool TestFishing { get; private set; }
		public WoWPoint WaterPoint { get; private set; }
		private bool UseFishingGear { get; set; }
		private int? BaitId { get; set; }

		// Private variables for internal state
		private readonly ProfileHelperFunctionsBase _helperFuncs = new ProfileHelperFunctionsBase();
		public static WoWGuid _PoolGUID;
		private Composite _root;
		private Composite _moveToPoolBehavior = PoolFishingBuddy.CreateMoveToPoolBehavior();
		private WoWGuid? _mainHandItemGuid;
		private WoWGuid? _offHandItemGuid;
		readonly WaitTimer _lureTimer = WaitTimer.FiveSeconds;
		readonly WaitTimer _baitTimer = WaitTimer.FiveSeconds;
		readonly WaitTimer _buffTimer = WaitTimer.FiveSeconds;

		private const uint ItemId_SavageFishingPool = 116825;
		private const uint ItemId_DraenicFishingPool = 116826;
		private const uint ItemId_BladeboneHook = 122742;

		private static readonly List<Lure> Lures = new List<Lure>
													{
														new Lure(6529, "Shiny Bauble", 25),
														new Lure(6530, "Nightcrawlers", 50),
														new Lure(6532, "Bright Baubles", 75),
														new Lure(6811, "Aquadynamic Fish Lens", 50),
														new Lure(6533, "Aquadynamic Fish Attractor", 100),
														new Lure(7307, "Flesh Eating Worm", 75),
														new Lure(34861, "Sharpened Fish Hook", 100),
														new Lure(46006, "Glow Worm", 100),
														new Lure(62673, "Feathered Lure", 100),
														new Lure(67404, "Glass Fishing Bobber", 15),
														new Lure(68049, "Heat-Treated Spinning Lure", 150),
														new Lure(118391, "Worm Supreme", 200),
													};

		private static readonly Dictionary<uint, string> Baits = new Dictionary<uint, string>
																   {
																	   {110293, "Abyssal Gulper Eel Bait"},
																	   {110294, "Blackwater Whiptail Bait"},
																	   {110290, "Blind Lake Sturgeon Bait"},
																	   {110289, "Fat Sleeper Bait"},
																	   {110291, "Fire Ammonite Bait"},
																	   {110274, "Jawless Skulker Bait"},
																	   {110292, "Sea Scorpion Bait"},
																	   {116755, "Nat's Hookshot Bait"},
																	   {128229, "Felmouth Frenzy Bait"},
																   };

		private static readonly Dictionary<uint, string> BuffItems = new Dictionary<uint, string>
																   {
																	   {122742, "Bladebone Hook"},
																   };

		// Hats are listed in best to worst order.
		internal static readonly uint[] FishingHatIds =
		{
			117405, // Nat's Drinking Hat
			118380, // Hightfish Cap
			118393, // Tentacled Hat
			88710, // Nat's Hat
			33820, // Weather-Beaten Fishing Hat
		};

		#region Overrides of CustomForcedBehavior

		protected override void EvaluateUsage_DeprecatedAttributes(XElement xElement)
		{
			//// EXAMPLE: 
			//UsageCheck_DeprecatedAttribute(xElement,
			//    Args.Keys.Contains("Nav"),
			//    "Nav",
			//    context => string.Format("Automatically converted Nav=\"{0}\" attribute into MovementBy=\"{1}\"."
			//                              + "  Please update profile to use MovementBy, instead.",
			//                              Args["Nav"], MovementBy));
		}

		protected override void EvaluateUsage_SemanticCoherency(XElement xElement)
		{
			//// EXAMPLE:
			//UsageCheck_SemanticCoherency(xElement,
			//    (!MobIds.Any() && !FactionIds.Any()),
			//    context => "You must specify one or more MobIdN, one or more FactionIdN, or both.");
			//
			//const double rangeEpsilon = 3.0;
			//UsageCheck_SemanticCoherency(xElement,
			//    ((RangeMax - RangeMin) < rangeEpsilon),
			//    context => string.Format("Range({0}) must be at least {1} greater than MinRange({2}).",
			//                  RangeMax, rangeEpsilon, RangeMin)); 
		}

        public override void OnFinished()
        {
	        try
	        {
		        Lua.Events.DetachEvent("LOOT_OPENED", HandleLootOpened);
		        Lua.Events.DetachEvent("LOOT_CLOSED", HandleLootClosed);

		        if (Fishing.IsFishing)
			        SpellManager.StopCasting();

		        // Make sure our weapons are equipped when done. 
		        EquipWeapons();
		        _root = null;
	        }
	        finally
	        {
				base.OnFinished();
			}
		}

		public override void OnStart()
		{
			// Let QuestBehaviorBase do basic initialization of the behavior, deal with bad or deprecated attributes,
			// capture configuration state, install BT hooks, etc.  This will also update the goal text.
			var isBehaviorShouldRun = OnStart_QuestBehaviorCore();

			// If the quest is complete, this behavior is already done...
			// So we don't want to falsely inform the user of things that will be skipped.
			if (isBehaviorShouldRun)
			{
				Lua.Events.AttachEvent("LOOT_OPENED", HandleLootOpened);
				Lua.Events.AttachEvent("LOOT_CLOSED", HandleLootClosed);
				LootOpen = false;

				// Disable any settings that may cause us to dismount --
				// When we mount for travel via FlyTo, we don't want to be distracted by other things.
				// NOTE: these settings are restored to their normal values when the behavior completes
				// or the bot is stopped.
				LevelBot.BehaviorFlags  &= ~(BehaviorFlags.Loot | BehaviorFlags.Pull);

				// Make sure we don't get logged out
				GlobalSettings.Instance.LogoutForInactivity = false;

				PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
				if (quest == null)
					this.UpdateGoalText(QuestId, "Fishing Item [" + CollectItemId + "]");
				else
					this.UpdateGoalText(QuestId, "Fishing Item for [" + quest.Name + "]");

                QBCLog.DeveloperInfo("Fishing Item (for QuestId {0}): {1}({2}) x{3}", QuestId, Utility.GetItemNameFromId(CollectItemId), CollectItemId, CollectItemCount.Value);

				if (UseFishingGear)
				{
					// Ensure that Autoequip doesn't interfere when we equip fishing gear. 
					CharacterSettings.Instance.AutoEquip = false;

					bool using2Hand;
					_mainHandItemGuid = FindMainHandGuid(out using2Hand);
					if (!using2Hand)
						_offHandItemGuid = FindOffhandGuid();
				}
			}
		}

		public static bool hasPoolFound
		{
			get
			{
				ObjectManager.Update();
				WoWGameObject _pool = (from unit in ObjectManager.GetObjectsOfType<WoWGameObject>(true, true)
									   orderby unit.Distance ascending

									   where !Blacklist.Contains(unit, BlacklistFlags.All)
									   where unit.IsValid
									   where unit.Entry == PoolId
									   select unit).FirstOrDefault();

				if (_pool != null)
				{
					//QBCLog.DeveloperInfo(DateTime.Now.ToLongTimeString() + " - hasPoolFound - set " + _pool.Guid.ToString() + " - " + _pool.Name + " - " + _pool.Distance2D);
					if (_PoolGUID != _pool.Guid)
					{
						PoolFishingBuddy.looking4NewLoc = true;
						_PoolGUID = _pool.Guid;
					}
					return true;
				}
				_PoolGUID = WoWGuid.Empty;
				return false;
			}
		}

		protected override Composite CreateBehavior_QuestbotMain()
		{
			return _root ?? (_root = new ActionRunCoroutine(ctx => MainCoroutine()));
		}

		private async Task<bool> MainCoroutine()
		{
			if (IsDone)
				return false;

			if (_helperFuncs.GetItemCount(CollectItemId) >= CollectItemCount)
			{
				BehaviorDone();
				return true;
			}

			// Have we a facing waterpoint or a PoolId and PoolGUID? No, then cancel this behavior!
			if ((!TestFishing && WaterPoint == WoWPoint.Empty && (PoolId == 0 || !_PoolGUID.IsValid)) 
				|| Me.Combat || Me.IsDead || Me.IsGhost)
			{
				BehaviorDone();
				return true;
			}

			if ((!Flightor.MountHelper.Mounted || !StyxWoW.Me.IsFlying) 
				&& (TestFishing || (WaterPoint != WoWPoint.Empty 
				|| (PoolId != 0 && hasPoolFound && PoolFishingBuddy.saveLocation.Count > 0 
				&& StyxWoW.Me.Location.Distance(PoolFishingBuddy.saveLocation[0]) <= 2.5
				&& !PoolFishingBuddy.looking4NewLoc))))
			{
				return await FishLogic();
			}

			return await _moveToPoolBehavior.ExecuteCoroutine();
		}

		private bool LootOpen { get; set; }
		private void HandleLootOpened(object sender, LuaEventArgs args)
		{
			LootOpen = true;
			QBCLog.DeveloperInfo("looting..");

			object[] arg = args.Args;
			if ((double)arg[0] == 0)
			{
				QBCLog.DeveloperInfo("no autoloot");
				Lua.DoString("for i=1, GetNumLootItems() do LootSlot(i) ConfirmBindOnUse() end CloseLoot()");
			}
			QBCLog.DeveloperInfo("looting done.");
		}
		private void HandleLootClosed(object sender, LuaEventArgs args) { QBCLog.DeveloperInfo("(hook)looting done."); LootOpen = false; }


	    private async Task<bool> FishLogic()
	    {
	        // Don't do anything if the global cooldown timer is running
	        if (SpellManager.GlobalCooldown)
	            return true;

	        // Do we need to interact with the bobber?
	        if (Fishing.IsBobberBobbing)
	        {
	            // Interact with the bobber
	            QBCLog.Info("[MrFishIt] Got a bite!");
	            WoWGameObject bobber = Fishing.FishingBobber;

	            if (bobber == null)
	                return false;

	            bobber.Interact();

	            // Wait for the lootframe
	            if (!await Coroutine.Wait(5000, () => LootFrame.Instance.IsVisible))
	            {
	                QBCLog.Warning("[MrFishIt] Did not see lootframe");
	                return false;
	            }

	            QBCLog.Info("[MrFishIt] Looting ...");
	            LootFrame.Instance.LootAll();
	            await Coroutine.Sleep(Delay.AfterInteraction);
	            return true;
	        }

            if (Fishing.FishingBobber == null 
                || !Fishing.IsFishing 
                || (Fishing.IsFishing && PoolId != 0 && !PoolFishingBuddy.BobberIsInTheHole))
	        {
				if (Fishing.FishingBobber == null)
				{
					QBCLog.DeveloperInfo("no FishingBobber found!?");

					if (await ApplyLure() || await ApplyBait() || await ApplyItemBuff())
						return true;

					if (UseFishingGear && (await EquipHat() || await EquipPole()))
						return true;
				}

				await CommonCoroutines.StopMoving();
				QBCLog.Info("Casting...");

	            if (WaterPoint != WoWPoint.Empty)
	            {
	                StyxWoW.Me.SetFacing(WaterPoint);
	                await Coroutine.Sleep(200);
	            }

                if (PoolId != 0)
	            {
	                StyxWoW.Me.SetFacing(_PoolGUID.asWoWGameObject());
	                await Coroutine.Sleep(200);
	            }

	            SpellManager.Cast("Fishing");

				// Wait until the bobber appears...
				await Coroutine.Wait(2000, () => Fishing.FishingBobber != null);

	            return true;
	        }

	        TreeRoot.StatusText = "[MrFishIt] Waiting for bobber to splash ...";
	        return true;
        }
		#endregion

		private async Task<bool> ApplyLure()
		{
			if (StyxWoW.Me.IsCasting || HasLureOnPole)
				return false;

			if (!_lureTimer.IsFinished)
				return false;

			_lureTimer.Reset();
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (mainHand == null || mainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.FishingPole)
				return false;

			// use any item with a lure effect
			WoWItem wearableItem = GetWearableItemWithLureEffect();
			if (wearableItem != null)
			{
				QBCLog.Info("Applying lure from {0} to fishing pole", wearableItem.SafeName);
				wearableItem.Use();
				await CommonCoroutines.SleepForLagDuration();
				return true;
			}

			var bestLure = GetBestLure();

			if (bestLure == null || !bestLure.Use())
				return false;

			QBCLog.Info("Applying {0} to fishing pole", bestLure.SafeName);
			await CommonCoroutines.SleepForLagDuration();
			return true;
		}

		private async Task<bool> ApplyBait()
		{
			if (!_baitTimer.IsFinished)
				return false;

			if (StyxWoW.Me.IsCasting || GotBaitOnPole)
				return false;

			_baitTimer.Reset();

			var bait = StyxWoW.Me.BagItems.FirstOrDefault(
					i => HasRequiredSkillLevel(i.ItemInfo) && (BaitId.HasValue ? BaitId.Value == i.Entry : Baits.ContainsKey(i.Entry)));

			if (bait == null)
				return false;
			QBCLog.Info("Attaching bait: {0} to pole", bait.SafeName);
			bait.Use();
			await Coroutine.Wait(2000, () => GotBaitOnPole);
			return true;
		}

		private async Task<bool> ApplyItemBuff()
		{
			if (!_buffTimer.IsFinished)
				return false;

			if (StyxWoW.Me.IsCasting)
				return false;

			_baitTimer.Reset();

			string buffName = null;
			var buffItem = Me.BagItems.FirstOrDefault(i =>
			{
				if (!HasRequiredSkillLevel(i.ItemInfo))
					return false;

				if (!BuffItems.TryGetValue(i.Entry, out buffName))
					return false;

				return !Me.HasAura(buffName);
			});

			if (buffItem == null)
				return false;

			QBCLog.Info($"Activating item buff: {buffItem.SafeName}");
			buffItem.Use();
			await Coroutine.Wait(2000, () => Me.HasAura(buffName));
			return true;
		}

		#region Utilities

		private async Task<bool> EquipPole()
		{
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			WoWItem pole = Me.CarriedItems
				.Where(i => i != null && i.IsValid
				   && i.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole && HasRequiredSkillLevel(i.ItemInfo))
				.OrderByDescending(GetBonusFishingSkillOnEquip)
				.ThenByDescending(i => i.ItemInfo.Level)
				// If these match then make sure we get poles in a consistent order; previously the order of poles
				// could be swapped after we equipped one, in which case we would swap equipped poles endlessly
				.ThenBy(i => i.Entry)
				.FirstOrDefault();

			// We are comparing Entry rather then the WoWItems instance since there can be multiple of the same item
			if (pole == null || (mainHand != null && pole.Entry == mainHand.Entry))
				return false;

			EquipItem(pole, WoWInventorySlot.MainHand);
			await CommonCoroutines.SleepForLagDuration();
			return true;
		}

		private async Task<bool> EquipHat()
		{
			WoWItem hat = null;
			foreach (var fishingHatId in FishingHatIds)
			{
				hat = StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == fishingHatId && HasRequiredSkillLevel(i.ItemInfo));

				if (hat != null)
					break;
			}
			if (hat == null)
				return false;
			var equippedHat = StyxWoW.Me.Inventory.Equipped.Head;

			// We are comparing Entry rather then the WoWItems instance since there can be multiple of the same item
			if (equippedHat != null && equippedHat.Entry == hat.Entry)
				return false;

			await CommonCoroutines.SleepForLagDuration();
			EquipItem(hat, WoWInventorySlot.Head);
			return true;
		}

		private static WoWItem GetBestLure()
		{
			return (from item in StyxWoW.Me.BagItems
					where HasRequiredSkillLevel(item.ItemInfo)
					let lure = Lures.FirstOrDefault(l => l.ItemId == item.Entry)
					where lure != null
					orderby lure.BonusSkill descending
					select item).FirstOrDefault();
		}

		private static bool GotBaitOnPole
		{
			get { return StyxWoW.Me.GetAllAuras().Any(a => Baits.ContainsValue(a.Name)); }
		}

		private static int GetBonusFishingSkillOnEquip(WoWItem item)
		{
			if (item.Effects == null)
				return 0;

			// These 2 two poles have a lure use ability that can only be used if pole is equipped.
			// If we have no better lure item in bags then we will return the total Fishing skill that these
			// pole give from 'Equip' bonus and Lure effect.
			if (item.Entry == ItemId_DraenicFishingPool || item.Entry == ItemId_SavageFishingPool)
			{
				var bestLure = GetBestLure();
				var bestLureBonus = bestLure != null ? ((int?)Lures.First(l => l.ItemId == bestLure.Entry).BonusSkill) : null;
				// Lure gives 200 Fishing skill and Equip bonus gives 30.
				if (!bestLureBonus.HasValue || bestLureBonus.Value <= 200)
					return 230;
			}

			return (from effect in item.Effects
				where effect.TriggerType == ItemEffectTriggerType.OnEquip && effect.Spell != null
				from spellEffect in effect.Spell.SpellEffects
				where spellEffect.AuraType == WoWApplyAuraType.ModSkill || spellEffect.EffectType == WoWSpellEffectType.ApplyAura
				where (SkillLine) spellEffect.MiscValueA == SkillLine.Fishing
				select spellEffect.BasePoints).FirstOrDefault();
		}

		private static bool HasRequiredSkillLevel(ItemInfo itemInfo)
		{
			return itemInfo.RequiredSkillId == 0
				   || itemInfo.RequiredSkillLevel <= StyxWoW.Me.GetSkill(itemInfo.RequiredSkillId).CurrentValue;
		}

		private static WoWItem GetWearableItemWithLureEffect()
		{
			var item = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (item != null && item.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole && HasUsableEffect(item))
				return item;

			item = StyxWoW.Me.Inventory.Equipped.Head;

			if (item != null && FishingHatIds.Contains(item.Entry) && HasUsableEffect(item))
				return item;

			return null;
		}

		private static bool HasUsableEffect(WoWItem item)
		{
			return item.Usable && item.CooldownTimeLeft.TotalSeconds <= 0 && item.Effects != null
			       && item.Effects.Any(e => e.TriggerType == ItemEffectTriggerType.OnUse && e.Spell != null && !e.Spell.Cooldown);
		}

		public static bool HasLureOnPole
		{
			get
			{
				var ret = Lua.GetReturnValues("return GetWeaponEnchantInfo()");
				return ret != null && ret.Count > 0 && ret[0] == "1";
			}
		}

		public void EquipWeapons()
		{
			if (_mainHandItemGuid.HasValue)
			{
				var equippedWeapon = StyxWoW.Me.Inventory.Equipped.MainHand;
				FindAndEquipWeaponInBags(equippedWeapon, _mainHandItemGuid.Value, WoWInventorySlot.MainHand);
			}

			// equip left hand weapon
			if (_offHandItemGuid.HasValue)
			{
				var equippedWeapon = StyxWoW.Me.Inventory.Equipped.OffHand;
				FindAndEquipWeaponInBags(equippedWeapon, _offHandItemGuid.Value, WoWInventorySlot.OffHand);
			}
		}

		private bool FindAndEquipWeaponInBags(WoWItem equippedItem, WoWGuid weaponGuid, WoWInventorySlot slot)
		{
			if (equippedItem != null && equippedItem.Guid == weaponGuid)
				return false;

			var wantedWeapon = ObjectManager.GetObjectByGuid<WoWItem>(weaponGuid);

			if (wantedWeapon == null)
			{
				Logging.Write("Could not find a weapon to equip");
				return false;
			}

			if (equippedItem != null && equippedItem.Guid == wantedWeapon.Guid)
				return false;

			EquipItem(wantedWeapon, slot);
			return true;
		}

		private WoWGuid? FindMainHandGuid(out bool is2Hand)
		{
			is2Hand = false;
			WoWItem mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;
			if (mainHand == null || mainHand.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole)
			{
				mainHand = StyxWoW.Me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
					FirstOrDefault(
						i => i.IsSoulbound 
						&& CharacterManager.AutoEquip.GetInventorySlots(i.ItemInfo).Contains(WoWInventorySlot.MainHand)
						&& StyxWoW.Me.CanEquipItem(i));

				if (mainHand == null)
				{
					QBCLog.Warning("Unable to find an mainhand weapon to swap to when in combat");
					return null;
				}
			}
			is2Hand = mainHand.ItemInfo.InventoryType == InventoryType.TwoHandWeapon
					|| mainHand.ItemInfo.InventoryType == InventoryType.Ranged;

			QBCLog.Info($"Mainhand: {mainHand.SafeName} will be swapped to when in combat.");
			return mainHand.Guid;
		}

		private WoWGuid? FindOffhandGuid()
		{
			WoWItem offHand = StyxWoW.Me.Inventory.Equipped.OffHand;
			if (offHand == null)
			{
				offHand = StyxWoW.Me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
					FirstOrDefault(
						i => i.IsSoulbound
						&& CharacterManager.AutoEquip.GetInventorySlots(i.ItemInfo).Contains(WoWInventorySlot.OffHand)
						&& StyxWoW.Me.CanEquipItem(i));

				if (offHand == null)
				{
					QBCLog.Warning("Unable to find an offhand weapon to swap to when in combat");
					return null;
				}
			}

			QBCLog.Info($"Offhand: {offHand.SafeName} will be swapped to when in combat.");
			return offHand.Guid;
		}

		//private static async Task EquipItem(WoWItem item, WoWInventorySlot slot)
		//{
		//	QBCLog.Info($"Equipping {item.SafeName} in {slot} slot");
		//	Lua.DoString("ClearCursor()");
		//	item.PickUp();
		//	Lua.DoString($"PickupInventoryItem({(int) slot + 1})");
		//	await CommonCoroutines.SleepForLagDuration();
		//}

		private static void EquipItem(WoWItem item, WoWInventorySlot slot)
		{
			QBCLog.Info($"Equipping {item.SafeName} in {slot} slot");
			Lua.DoString("ClearCursor()");
			item.PickUp();
			Lua.DoString($"PickupInventoryItem({(int)slot + 1})");
		}

		#endregion Utilities


		private class Lure
		{
			public Lure(int itemId, string name, int bonusSkill)
			{
				ItemId = itemId;
				Name = name;
				BonusSkill = bonusSkill;
			}

			public int ItemId { get; private set; }
			public string Name { get; private set; }
			public int BonusSkill { get; private set; }
		}
	}


	static class Fishing
	{
		//static readonly List<int> FishingIds = new List<int> { 131474, 7620, 7731, 7732, 18248, 33095, 51294, 88868 };
		/// <summary>
		/// Returns true if you are fishing
		/// </summary>
		public static bool IsFishing { get { return /*FishingIds.Contains(StyxWoW.Me.ChanneledCastingSpellId);*/ (StyxWoW.Me.IsCasting | StyxWoW.Me.HasAura("Fishing")); } }

		/// <summary>
		/// Returns your fishing pole
		/// </summary>
		public static WoWItem FishingPole
		{ get { return ObjectManager.GetObjectsOfType<WoWItem>().Where(b => b.IsFishingPole()).FirstOrDefault(); } }

		/// <summary>
		/// Returns true if you have a temp-enchantm on your pole
		/// </summary>
		public static bool GotLure { get { return Lua.GetReturnVal<bool>("return GetWeaponEnchantInfo()", 0); } }

		/// <summary>
		/// Returns true if the fishing bobber is bobbing
		/// </summary>
		public static bool IsBobberBobbing { get { return FishingBobber != null && FishingBobber.IsBobbing(); } }

		/// <summary>
		/// Returns the current fishing bobber in use, null otherwise
		/// </summary>
		public static WoWGameObject FishingBobber
		{
			get
			{
				ObjectManager.Update();
				var d = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o != null && o.IsValid && o.CreatedByGuid == StyxWoW.Me.Guid);
				//if (d != null)
				//    QBCLog.DeveloperInfo("[FishingBobber]Name: {0} - AnimationState: {1}", d.Name, d.AnimationState);

				return ObjectManager.GetObjectsOfType<WoWGameObject>()
					.FirstOrDefault(o => o != null && o.IsValid && o.CreatedByGuid == StyxWoW.Me.Guid &&
						o.SubType == WoWGameObjectType.FishingNode);
			}
		}
	}

	static class Extensions
	{
		static readonly List<uint> PoleIds = new List<uint> { 44050, 19970, 45991, 45992, 45858, 19022, 25978, 6367, 12225, 6366, 6256, 6365 };

		public static bool IsFishingPole(this WoWItem value)
		{
			if (value == null)
				return false;

			return PoleIds.Contains(value.Entry);
		}

		public static bool IsBobbing(this WoWGameObject value)
		{
			if (value == null)
				return false;

			return ((WoWFishingBobber)value.SubObj).IsBobbing;
			//return null != Fishing.FishingBobber ? 1 == Fishing.FishingBobber.AnimationState : false;

			//return value.AnimationState == 1;
		}

		public static WoWGameObject asWoWGameObject(this WoWGuid GUID)
		{
			ObjectManager.Update();
			WoWGameObject _o = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Guid == GUID);
			return _o;
		}
	}

	static class PoolFishingBuddy
	{
		private static Stopwatch movetopoolTimer = new Stopwatch();
		public static bool looking4NewLoc;
		private static WoWGameObject Pool { get { if (MrFishIt.hasPoolFound) { } return MrFishIt._PoolGUID.asWoWGameObject(); } }
		static public List<WoWPoint> saveLocation = new List<WoWPoint>(100);
		static public List<WoWPoint> badLocations = new List<WoWPoint>(100);
		static public int newLocAttempts = 0;

		private static int MaxNewLocAttempts = 5;
		public static int MaxCastRange { get; set; }
		public static int MinCastRange { get; set; }

		/// <summary>
		/// fixed by BarryDurex
		/// </summary>
		static public WoWPoint getSaveLocation(WoWPoint Location, int minDist, int maxDist, int traceStep, int traceStep2)
		{
			QBCLog.DeveloperInfo("Navigation: Looking for save Location around {0}.", Location);

			WoWPoint point = WoWPoint.Empty;
			float _PIx2 = 3.14159f * 2f;

			for (int i = 0, x = minDist; i < traceStep && x < maxDist && looking4NewLoc == true; i++)
			{
				WoWPoint p = Location.RayCast((i * _PIx2) / traceStep, x);

				p.Z = getGroundZ(p);
				WoWPoint pLoS = p;
				pLoS.Z = p.Z + 0.5f;

				if (p.Z != float.MinValue && !badLocations.Contains(p) && StyxWoW.Me.Location.Distance(p) > 1)
				{
					if (getHighestSurroundingSlope(p) < 1.2f && GameWorld.IsInLineOfSight(pLoS, Location))
					{
						point = p;
						break;
					}
				}

				if (i == (traceStep - 1))
				{
					i = 0;
					x++;
				}
			}

			for (int i = 0, x = 10; i < traceStep2 && x < maxDist && looking4NewLoc == true; i++)
			{
				WoWPoint p2 = point.RayCast((i * _PIx2) / traceStep2, x);

				p2.Z = getGroundZ(p2);
				WoWPoint pLoS = p2;
				pLoS.Z = p2.Z + 0.5f;

				if (p2.Z != float.MinValue && !badLocations.Contains(p2) && StyxWoW.Me.Location.Distance(p2) > 1)
				{
					if (getHighestSurroundingSlope(p2) < 1.2f && GameWorld.IsInLineOfSight(pLoS, Location) && p2.Distance2D(Location) <= maxDist)
					{
						looking4NewLoc = false;
						QBCLog.DeveloperInfo("Navigation: Moving to {0}. Distance: {1}", p2, Location.Distance(p2));
						return p2;
					}
				}

				if (i == (traceStep2 - 1))
				{
					i = 0;
					x++;
				}
			}

			QBCLog.Warning("{0} - No valid points returned by RayCast, blacklisting for 2 minutes.", DateTime.Now.ToLongTimeString());
			Blacklist.Add(Pool, BlacklistFlags.All, TimeSpan.FromMinutes(2));
			return WoWPoint.Empty;
		}

		static public WoWPoint getSaveLocation(WoWPoint Location, int minDist, int maxDist, int traceStep)
		{
			QBCLog.DeveloperInfo("Navigation: Looking for save Location around {0}.", Location);

			float _PIx2 = 3.14159f * 2f;

			for (int i = 0, x = minDist; i < traceStep && x < maxDist && looking4NewLoc == true; i++)
			{
				WoWPoint p = Location.RayCast((i * _PIx2) / traceStep, x);

				p.Z = getGroundZ(p);
				WoWPoint pLoS = p;
				pLoS.Z = p.Z + 0.5f;

				if (p.Z != float.MinValue && !badLocations.Contains(p) && StyxWoW.Me.Location.Distance(p) > 1)
				{
					if (getHighestSurroundingSlope(p) < 1.2f && GameWorld.IsInLineOfSight(pLoS, Location) /*&& Navigator.CanNavigateFully(StyxWoW.Me.Location, Location)*/)
					{
						looking4NewLoc = false;
						QBCLog.DeveloperInfo("Navigation: Moving to {0}. Distance: {1}", p, Location.Distance(p));
						return p;
					}
				}

				if (i == (traceStep - 1))
				{
					i = 0;
					x++;
				}
			}

			if (Pool != null)
			{
				QBCLog.Warning("No valid points returned by RayCast, blacklisting for 2 minutes.");
				Blacklist.Add(Pool, BlacklistFlags.All, TimeSpan.FromMinutes(2));
				return WoWPoint.Empty;
			}
			else
			{
				QBCLog.Warning("No valid points returned by RayCast, can't navigate without user interaction. Stopping!");
				TreeRoot.Stop();
				return WoWPoint.Empty;
			}
			
		}

		/// <summary>
		/// Credits to funkescott.
		/// </summary>
		/// <returns>Highest slope of surrounding terrain, returns 100 if the slope can't be determined</returns>
		public static float getHighestSurroundingSlope(WoWPoint p)
		{
			QBCLog.DeveloperInfo("Navigation: Sloapcheck on Point: {0}", p);
			float _PIx2 = 3.14159f * 2f;
			float highestSlope = -100;
			float slope = 0;
			int traceStep = 15;
			float range = 0.5f;
			WoWPoint p2;
			for (int i = 0; i < traceStep; i++)
			{
				p2 = p.RayCast((i * _PIx2) / traceStep, range);
				p2.Z = getGroundZ(p2);
				slope = Math.Abs( getSlope(p, p2) );
				if( slope > highestSlope )
				{
					highestSlope = (float)slope;
				}
			}
			QBCLog.DeveloperInfo("Navigation: Highslope {0}", highestSlope);
			return Math.Abs( highestSlope );
		}

		/// <summary>
		/// Credits to funkescott.
		/// </summary>
		/// <param name="p1">from WoWPoint</param>
		/// <param name="p2">to WoWPoint</param>
		/// <returns>Return slope from WoWPoint to WoWPoint.</returns>
		public static float getSlope(WoWPoint p1, WoWPoint p2)
		{
			float rise = p2.Z - p1.Z;
			float run = (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

			return rise / run;
		}

		/// <summary>
		/// Credits to exemplar.
		/// </summary>
		/// <returns>Z-Coordinates for PoolPoints so we don't jump into the water.</returns>
		public static float getGroundZ(WoWPoint p)
		{
			WoWPoint ground = WoWPoint.Empty;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + MaxCastRange)), new WoWPoint(p.X, p.Y, (p.Z - 0.8f)), TraceLineHitFlags.Collision, out ground);

			if (ground != WoWPoint.Empty)
			{
				QBCLog.DeveloperInfo("Ground Z: {0}.", ground.Z);
				return ground.Z;
			}
			QBCLog.DeveloperInfo("Ground Z returned float.MinValue.");
			return float.MinValue;
		}

		public static  Composite CreateMoveToPoolBehavior()
		{
			return new Decorator(ret => Pool != null && !Blacklist.Contains(Pool, BlacklistFlags.All),
				new Sequence(
					new Action(ret => QBCLog.DeveloperInfo("Composit: CreateMoveToPoolBehaviour")),
					new Action(ret => movetopoolTimer.Start()),

					new PrioritySelector(

						// Timer
						new Decorator(ret => movetopoolTimer.ElapsedMilliseconds > 30000,
							new Sequence(
								new Action(ret => QBCLog.Warning("Timer for moving to ground elapsed, blacklisting for 2 minutes.")),
								new Action(ret => Blacklist.Add(MrFishIt._PoolGUID.asWoWGameObject(), BlacklistFlags.All, TimeSpan.FromMinutes(2)))                                
						)),

						//// Blacklist if other Player is detected
						//new Decorator(ret => Helpers.PlayerDetected && !PoolFisherSettings.Instance.NinjaPools,
						//    new Sequence(
						//            new Action(ret => QBCLog.Warning("Detected another player in pool range, blacklisting for 2 minutes.")),
						//            new Action(ret => Helpers.BlackListPool(Pool)),
						//            new Action(delegate { return RunStatus.Success; })
						//)),

						// Get PoolPoint
						new Decorator(ret => looking4NewLoc,
							new Sequence(
								new ActionSetActivity(ret => "[MrFishIt] Looking for valid Location"),
								new Action(ret => WoWMovement.MoveStop()),
								new PrioritySelector(

									// Pool ist Feuerteich 
									new Decorator(ret => Pool.Entry == 207734,
										new Sequence(
										new Action(ret => saveLocation.Add(getSaveLocation(Pool.Location, MinCastRange, 
											MaxCastRange, 50, 60))),
										new Action(ret => QBCLog.DeveloperInfo("Added {0} to saveLocations.", saveLocation[0]))
									)),

									// Pool ist kein Feuerteich 
									new Decorator(ret => Pool.Entry != 207734,
										new Sequence(
										new Action(ret => saveLocation.Add(getSaveLocation(Pool.Location, MinCastRange, 
											MaxCastRange, 50))),
										new Action(ret => QBCLog.DeveloperInfo("Added {0} to saveLocations.", saveLocation[0]))
									))                                
						))),

						// Move to PoolPoint
						new Decorator(pool => Pool != null && saveLocation.Count > 0 && !looking4NewLoc,
							new PrioritySelector(

								// Pool still there?
								new Decorator(ret => MrFishIt._PoolGUID.asWoWGameObject() == null,
									new Sequence(
										new Action(ret => QBCLog.Info("Fishing Pool is gone, moving on."))
								)),

								// reached max attempts for new locations?
								new Decorator(ret => newLocAttempts == MaxNewLocAttempts + 1,
									new Sequence(
									new Action(ret => QBCLog.Warning("Reached max. attempts for new locations, blacklisting for 2 minutes.")),
									new Action(ret => Blacklist.Add(Pool, BlacklistFlags.All, TimeSpan.FromMinutes(2)))
								)),

								// tries++
								new Decorator(ret => StyxWoW.Me.Location.Distance(saveLocation[0]) <= 2 && !Flightor.MountHelper.Mounted && !StyxWoW.Me.IsMoving,
									new Sequence(
										new Wait(2, ret => StyxWoW.Me.IsCasting, new ActionIdle()),
										new Action(ret => newLocAttempts++),
										new Action(ret => QBCLog.Warning("Moving to new Location.. Attempt: {0} of {1}.", newLocAttempts, MaxNewLocAttempts))
								)),
											

								// Dismount
								new Decorator(ret => StyxWoW.Me.Location.Distance(new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2)) <= 1.5 && Flightor.MountHelper.Mounted, //&& !StyxWoW.Me.IsMoving,
									//new PrioritySelector(

										//new Decorator(ret => Helpers.CanWaterWalk && !Helpers.hasWaterWalking,
											new Sequence(
												new Action(ret => StyxWoW.Me.SetFacing(Pool.Location)),
												//new Action(ret => Helpers.WaterWalk()),
												//new SleepForLagDuration(),
												new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.Descend)),
												new Action(ret => Flightor.MountHelper.Dismount()),
												new Action(ret => WoWMovement.MoveStop()),
												new Action(ret => QBCLog.DeveloperInfo("Navigation: Dismount. Current Location {0}, PoolPoint: {1}, Distance: {2}", StyxWoW.Me.Location, new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2), StyxWoW.Me.Location.Distance(new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2)))),
												new Wait(3, ret => Flightor.MountHelper.Mounted, new ActionIdle()),
												new SleepForLagDuration()

										//new Decorator(ret => !Helpers.CanWaterWalk || (Helpers.CanWaterWalk && Helpers.hasWaterWalking),
										//    new Sequence(
										//        new Action(ret => StyxWoW.Me.SetFacing(Pool.Location)),
										//        new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.Descend)),
										//        new Action(ret => Mount.Dismount()),
										//        new Action(ret => WoWMovement.MoveStop()),
										//        new Action(ret => QBCLog.DeveloperInfo("Navigation: Dismount. Current Location {1}, PoolPoint: {2}, Distance: {3}", StyxWoW.Me.Location, new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2), StyxWoW.Me.Location.Distance(new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2)))),
										//        new Wait(3, ret => Flightor.MountHelper.Mounted, new ActionIdle()),
                                        //        new SleepForLagDuration()))
										
								)),

								// in Line Line of sight?
								new Decorator(ret => StyxWoW.Me.Location.Distance(saveLocation[0]) <= 2 && !Pool.InLineOfSight && !StyxWoW.Me.IsMoving && !Flightor.MountHelper.Mounted,
									new Sequence(
									new Action(ret => QBCLog.Warning("Pool is not in Line of Sight!")),
									new Action(ret => badLocations.Add(saveLocation[0])),
									new Action(ret => saveLocation.Clear()),
									new Action(ret => newLocAttempts++),
									new Action(ret => QBCLog.Warning("Moving to new Location.. Attempt: {0} of {1}.", newLocAttempts, MaxNewLocAttempts)),
									new Action(ret => looking4NewLoc = true)
								)),

								// Move without Mount
								new Decorator(ret => (StyxWoW.Me.Location.Distance(saveLocation[0]) > 1 && StyxWoW.Me.Location.Distance(saveLocation[0]) <= 10 && !Flightor.MountHelper.Mounted && GameWorld.IsInLineOfSight(StyxWoW.Me.Location, saveLocation[0])) && !StyxWoW.Me.IsSwimming,
									new PrioritySelector(

										// Mount if not mounted and Navigator is not able to generate a path
										new Decorator(ret => !Navigator.CanNavigateFully(StyxWoW.Me.Location, saveLocation[0]),
											new Action(ret => Flightor.MountHelper.MountUp())),

										new Sequence(
											new ActionSetActivity(ret => "Moving to new Location"),
											new Action(ret => QBCLog.DeveloperInfo("Navigation: Moving to Pool: " + saveLocation[0] + ", Location: " + StyxWoW.Me.Location + ", distance: " + saveLocation[0].Distance(StyxWoW.Me.Location) + " (Not Mounted)")),
											//new Action(ret => QBCLog.Info("{0} - Moving to Pool: {1}, Location: {2}, Distance: {3}. (Not Mounted)", Helpers.TimeNow, PoolPoints[0], StyxWoW.Me.Location, PoolPoints[0].Distance(StyxWoW.Me.Location))),
											// Move
											new Action(ret => Navigator.MoveTo(saveLocation[0]))
										)
								)),

								// Move with Mount
								new Decorator(ret => (StyxWoW.Me.Location.Distance(saveLocation[0]) > 10 || Flightor.MountHelper.Mounted || (StyxWoW.Me.Location.Distance(saveLocation[0]) <= 10 && !GameWorld.IsInLineOfSight(StyxWoW.Me.Location, saveLocation[0])) && !StyxWoW.Me.IsSwimming),
									new PrioritySelector(

										// Mount if not mounted
										new Decorator(ret => !Flightor.MountHelper.Mounted && !StyxWoW.Me.Combat,
											new Action(ret => Flightor.MountHelper.MountUp())),

										// Move
										new Sequence(
											new ActionSetActivity(ret => "[MrFishIt] Moving to Ground"),
											new Action(ret => QBCLog.DeveloperInfo("Navigation: Moving to Pool: " + saveLocation[0] + ", Location: " + StyxWoW.Me.Location + ", distance: " + StyxWoW.Me.Location.Distance(new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2)) + " (Mounted)")),
											//new Action(ret => QBCLog.Info("{0} - Moving to Pool: {1}, Location: {2}, Distance: {3}. (Mounted)", Helpers.TimeNow, PoolPoints[0], StyxWoW.Me.Location, PoolPoints[0].Distance(StyxWoW.Me.Location)),
											new Action(ret => Flightor.MoveTo(new WoWPoint(saveLocation[0].X, saveLocation[0].Y, saveLocation[0].Z + 2), true))
										)
								))
							))
				)));
		}

		/// <summary>
		/// Checks if the bobber is in distance of 3.6 to location of the pool.
		/// </summary>
		static public bool BobberIsInTheHole
		{
	        get
            {
	            if (Fishing.FishingBobber != null && Pool != null)
	            {
	                if (Fishing.FishingBobber.Location.Distance2D(Pool.Location) <= 3.6f)
	                {
	                    return true;
	                }
	            }
	            return false;
	        }
		}

		
	}
}
