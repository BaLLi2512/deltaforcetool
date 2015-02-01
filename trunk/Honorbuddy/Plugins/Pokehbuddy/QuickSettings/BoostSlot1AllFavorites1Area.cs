using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Common;
using System.Windows.Forms;

namespace Pokehbuddyplug
{  // BoostPetSlot1Ringer
    public partial class configForm
    {
        public void BoostSlot1AllFavorites1AreaInit()
        {
            //Logging.Write("init simple level all in team");
            Pokehbuddy.quicksettingsnames.Add("Auto Boost pets in slot 1 " + System.Environment.NewLine + "(aka ringer mode) swaps out at 25");
            Pokehbuddy.quicksettingdesc.Add("Enables GatherBuddy2, and manages the settings." + System.Environment.NewLine + System.Environment.NewLine + "YOU have to load a profile to your liking. " + System.Environment.NewLine + System.Environment.NewLine + "Will swapout the lowlevel when its level 25 and swap in a new FAVORITE pet of level 1+");
            Pokehbuddy.quicksettingsfuncs.Add("BoostSlot1AllFavorites1AreaExecute");
        }

        public void BoostSlot1AllFavorites1AreaExecute()
        {
            ProfileManager.LoadEmpty();

            SetBotBase("gather");

            Pokehbuddy.MySettings.AboveLevel = 20;
            Pokehbuddy.MySettings.AdFactor = 5;
            Pokehbuddy.MySettings.BelowLevel = 20;
            Pokehbuddy.MySettings.DisFactor = 2;
            Pokehbuddy.MySettings.Distance = 300;
            Pokehbuddy.MySettings.DoPreCombatSwapping = false;
            Pokehbuddy.MySettings.DoPVP = false;
            Pokehbuddy.MySettings.ForfeitIfNotInteresting = false;
            Pokehbuddy.MySettings.GetRarity = 2;
            Pokehbuddy.MySettings.HPFactor = 10;
            Pokehbuddy.MySettings.LevelFactor = 20;
            Pokehbuddy.MySettings.MinPetsAlive = 1;
            Pokehbuddy.MySettings.PVPMaxTime = 1;
            Pokehbuddy.MySettings.PVPMinTime = 0;
            Pokehbuddy.MySettings.UseBandagesToHeal = 3;
            
            
            Pokehbuddy.MySettings.Slot1SwapEnabled = true;
            Pokehbuddy.MySettings.Slot1AllowWild = true;
            Pokehbuddy.MySettings.Slot1TradeableOnly = false;

            Pokehbuddy.MySettings.Slot1SwapFavoriteOnly = true;
            Pokehbuddy.MySettings.Slot1SwapMaxLevel = 25;
            Pokehbuddy.MySettings.Slot1SwapMinLevel = 1;
            


            Pokehbuddy.MySettings.UseBlackList = true;
            Pokehbuddy.MySettings.UseHealSkill = 1;
            Pokehbuddy.MySettings.UseRatingSystem = true;
            Pokehbuddy.MySettings.UseWhiteList = false;
            Pokehbuddy.MySettings.PetOrder = "2,3,1";
            closeForm();
            MessageBox.Show("Load a profile to your liking and press start!");
        }


    }
}
