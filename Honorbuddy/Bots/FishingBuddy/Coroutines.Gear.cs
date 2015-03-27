using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace Bots.FishingBuddy
{
	static partial class Coroutines
	{
		static void Gear_OnStart()
		{
		}

		static void Gear_OnStop()
		{
			if (Utility.EquipWeapons())
				FishingBuddyBot.Log("Equipping weapons");

			if (Utility.EquipMainHat())
				FishingBuddyBot.Log("Switched to my normal hat");
		}

		public async static Task<bool> EquipGear()
		{
			if (Me.Combat)
				return false;

			return await EquipPole() || await EquipHat();
		}

		public async static Task<bool> EquipPole()
		{
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			WoWItem pole = Me.CarriedItems
				.Where(i => i != null && i.IsValid 
					&& i.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole)
				.OrderByDescending(Utility.GetBonusFishingSkillOnEquip)
				.ThenByDescending(i => i.ItemInfo.Level)
				.FirstOrDefault();

			if (pole == null || pole == mainHand)
				return false;

			return await EquipItem(pole, WoWInventorySlot.MainHand);
		}

		public async static Task<bool> EquipHat()
		{
			var hat = Utility.GetFishingHat();

			if (hat == null || StyxWoW.Me.Inventory.Equipped.Head == hat)
				return false;

			return Utility.EquipItem(hat, WoWInventorySlot.Head) 
				&& await Coroutine.Wait(4000, () => StyxWoW.Me.Inventory.Equipped.Head == hat);
		}

		public async static Task<bool> EquipItem(WoWItem item, WoWInventorySlot slot)
		{
			if (!Utility.EquipItem(item, slot))
				return false;
			await CommonCoroutines.SleepForLagDuration();
			await CommonCoroutines.SleepForRandomUiInteractionTime();
			return true;
		}
	}
}
