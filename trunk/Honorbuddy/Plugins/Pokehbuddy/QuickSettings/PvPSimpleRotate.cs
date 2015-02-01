using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Common;

namespace Pokehbuddyplug
{  // PvPSimpleRotate
    public partial class configForm
    {
        public void PvPSimpleRotateInit()
        {

            Pokehbuddy.quicksettingsnames.Add("PVP - simple rotation");
            Pokehbuddy.quicksettingdesc.Add("PVP - simple rotation (not using the rating system)" + System.Environment.NewLine + "");
            Pokehbuddy.quicksettingsfuncs.Add("PvPSimpleRotateExecute");
        }

        public void PvPSimpleRotateExecute()
        {
            ProfileManager.LoadEmpty();

            //EnablePlugin("PetArea");
           // SetBotBase("combat bot");


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
            Pokehbuddy.MySettings.LevelFactor = -20;
            Pokehbuddy.MySettings.MinPetsAlive = 1;
            Pokehbuddy.MySettings.PVPMaxTime = 0;
            Pokehbuddy.MySettings.PVPMinTime = 0;
            Pokehbuddy.MySettings.UseBandagesToHeal = 0;

            Pokehbuddy.MySettings.UseBlackList = true;
            Pokehbuddy.MySettings.UseHealSkill = 1;
            Pokehbuddy.MySettings.UseRatingSystem = false;
            Pokehbuddy.MySettings.UseWhiteList = false;
            Pokehbuddy.MySettings.PetOrder = "2,3,1";
            closeForm();
        }


    }
}
