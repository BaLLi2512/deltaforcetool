using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.DB;
using Styx.WoWInternals.WoWObjects;

namespace Bots.FishingBuddy
{
	static partial class Coroutines
	{
		private readonly static WaitTimer LureRecastTimer = WaitTimer.TenSeconds;


		private static readonly Dictionary<uint, string> Lures = new Dictionary<uint, string>
																{
																	{68049, "Heat-Treated Spinning Lure"},
																	{62673, "Feathered Lure"},
																	{34861, "Sharpened Fish Hook"},
																	{46006, "Glow Worm"},
																	{6533, "Aquadynamic Fish Attractor"},
																	{7307, "Flesh Eating Worm"},
																	{6532, "Bright Baubles"},
																	{6530, "Nightcrawlers"},
																	{6811, "Aquadynamic Fish Lens"},
																	{6529, "Shiny Bauble"},
																	{67404, "Glass Fishing Bobber"},
																	{118391, "Worm Supreme"}
																};

		// does nothing if no lures are in bag
		public async static Task<bool> Applylure()
		{
			if (FishingBuddySettings.Instance.Poolfishing )
				return false;

			if (StyxWoW.Me.IsCasting || HasLureOnPole)
				return false;

			if (!LureRecastTimer.IsFinished)
				return false;
			
			LureRecastTimer.Reset();
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (mainHand == null || mainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.FishingPole)
				return false;

			// use any item with a lure effect
			WoWItem item = GetItemWithLureEffect();
			if (item != null)
			{
				FishingBuddyBot.Log("Appling lure from {0} to fishing pole", item.SafeName);
				item.Use();
				await CommonCoroutines.SleepForLagDuration();
				return true;
			}

			foreach (var kv in Lures)
			{
				WoWItem lureInBag = Utility.GetItemInBag(kv.Key);
				if (lureInBag != null && lureInBag.Use())
				{
					FishingBuddyBot.Log("Appling {0} to fishing pole", kv.Value);
					await CommonCoroutines.SleepForLagDuration();
					return true;
				}
			}
			return false;
		}

		private static WoWItem GetItemWithLureEffect()
		{
			var item = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (item != null && item.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole && HasUsableEffect(item))
				return item;

			item = StyxWoW.Me.Inventory.Equipped.Head;

			if (item != null && Utility.FishingHatIds.Contains(item.Entry) && HasUsableEffect(item))
				return item;

			return null;
		}

		private static bool HasUsableEffect(WoWItem item)
		{
			return item.Effects != null 
				&& item.Effects.Any(e => e.TriggerType == ItemEffectTriggerType.OnUse && e.Spell != null && !e.Spell.Cooldown);
		}

		public static IEnumerable<WoWItem> GetLures()
		{
			return StyxWoW.Me.BagItems.Where(
			i => Utility.FishingHatIds.Contains(i.Entry)
				|| Lures.ContainsKey(i.Entry));
		}

		public static bool HasLureOnPole
		{
			get
			{
				var ret = Lua.GetReturnValues("return GetWeaponEnchantInfo()");
				return ret != null && ret.Count > 0 && ret[0] == "1";
			}
		}

	}
}
