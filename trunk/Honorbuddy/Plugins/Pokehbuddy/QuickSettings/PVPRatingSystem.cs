using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Common;

namespace Pokehbuddyplug
{  // PVPRatingSystem
    public partial class configForm
    {
        public void PVPRatingSystemInit()
        {
            //Logging.Write("init simple level all in team");
            Pokehbuddy.quicksettingsnames.Add("PVP - rating system");
            Pokehbuddy.quicksettingdesc.Add("PVP - rating system" + System.Environment.NewLine + "Uses the more complex rating system");
            Pokehbuddy.quicksettingsfuncs.Add("PVPRatingSystemExecute");
        }

        public void PVPRatingSystemExecute()
        {
            ProfileManager.LoadEmpty();

            //EnablePlugin("PetArea");
          //  SetBotBase("combat bot");


            Pokehbuddy.MySettings.AboveLevel = 5;
            Pokehbuddy.MySettings.AdFactor = 2;
            Pokehbuddy.MySettings.BelowLevel = 3;
            Pokehbuddy.MySettings.DisFactor = 2;
            Pokehbuddy.MySettings.Distance = 1;
            Pokehbuddy.MySettings.DoPreCombatSwapping = true;
            Pokehbuddy.MySettings.DoPVP = true;
            Pokehbuddy.MySettings.ForfeitIfNotInteresting = false;
            Pokehbuddy.MySettings.GetRarity = 2;
            Pokehbuddy.MySettings.HPFactor = 10;
            Pokehbuddy.MySettings.LevelFactor = 2;
            Pokehbuddy.MySettings.MinPetsAlive = 1;
            Pokehbuddy.MySettings.PVPMaxTime = 0;
            Pokehbuddy.MySettings.PVPMinTime = 0;
            Pokehbuddy.MySettings.UseBandagesToHeal = 0;

            Pokehbuddy.MySettings.UseBlackList = true;
            Pokehbuddy.MySettings.UseHealSkill = 1;
            Pokehbuddy.MySettings.UseRatingSystem = true;
            Pokehbuddy.MySettings.UseWhiteList = false;
            Pokehbuddy.MySettings.PetOrder = "2,3,1";
            closeForm();
        }


    }
}
