using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Common;

namespace Pokehbuddyplug
{  // blank
    public partial class configForm
    {
        public void blankInit()
        {
            //Logging.Write("init simple level all in team");
            Pokehbuddy.quicksettingsnames.Add("label");
            Pokehbuddy.quicksettingdesc.Add("description"+ System.Environment.NewLine +"");
            Pokehbuddy.quicksettingsfuncs.Add("blankExecute");
        }

        public void blankExecute()
        {
            ProfileManager.LoadEmpty();
            
            EnablePlugin("PetArea");
           // SetBotBase("gather");


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
            Pokehbuddy.MySettings.LevelFactor = -20;
            Pokehbuddy.MySettings.MinPetsAlive = 1;
            Pokehbuddy.MySettings.PVPMaxTime = 1;
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
