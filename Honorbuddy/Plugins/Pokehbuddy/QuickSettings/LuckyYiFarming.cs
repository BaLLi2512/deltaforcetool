using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Common;
using System.Windows.Forms;

namespace Pokehbuddyplug
{  // LuckyYiFarming
    public partial class configForm
    {
        public void LuckyYiFarmingInit()
        {

            Pokehbuddy.quicksettingsnames.Add("Lucky Yi farming");
            Pokehbuddy.quicksettingdesc.Add("Enables Combat Bot, puts Lucky Yi on the whitelist" +System.Environment.NewLine+"Start this close to lucky yi");

            Pokehbuddy.quicksettingsfuncs.Add("LuckyYiFarmingExecute");
        }

        public void LuckyYiFarmingExecute()
        {
            ProfileManager.LoadEmpty();

            //EnablePlugin("PetArea");
           // SetBotBase("combat bot");


            Pokehbuddy.MySettings.AboveLevel = 5;
            Pokehbuddy.MySettings.AdFactor = 2;
            Pokehbuddy.MySettings.BelowLevel = 3;
            Pokehbuddy.MySettings.DisFactor = 2;
            Pokehbuddy.MySettings.Distance = 300;
            Pokehbuddy.MySettings.DoPreCombatSwapping = true;
            Pokehbuddy.MySettings.DoPVP = false;
            Pokehbuddy.MySettings.ForfeitIfNotInteresting = false;
            Pokehbuddy.MySettings.GetRarity = 2;
            Pokehbuddy.MySettings.HPFactor = 10;
            Pokehbuddy.MySettings.LevelFactor = 2;
            Pokehbuddy.MySettings.MinPetsAlive = 1;
            Pokehbuddy.MySettings.PVPMaxTime = 0;
            Pokehbuddy.MySettings.PVPMinTime = 0;
            Pokehbuddy.MySettings.UseBandagesToHeal = 0;

            Pokehbuddy.MySettings.UseBlackList = false;
            Pokehbuddy.MySettings.UseHealSkill = 1;
            Pokehbuddy.MySettings.UseRatingSystem = true;
            Pokehbuddy.MySettings.UseWhiteList = true;
            Pokehbuddy.MySettings.PetOrder = "2,3,1";


            string addwhitelist = "lucky yi";
            var item = listBox4.FindString(addwhitelist);
            if (item == ListBox.NoMatches)
            {
                listBox4.Items.Add(addwhitelist);
            }
            WhitelistSave();


            closeForm();
        }


    }
}
