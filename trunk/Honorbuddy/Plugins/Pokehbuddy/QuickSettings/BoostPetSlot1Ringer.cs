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
        public void BoostPetSlot1RingerInit()
        {
            //Logging.Write("init simple level all in team");
            Pokehbuddy.quicksettingsnames.Add("Boost pet in slot 1 " + System.Environment.NewLine + "(aka ringer mode)");
            Pokehbuddy.quicksettingdesc.Add("Enables GatherBuddy2, PetArea and manages the settings."+ System.Environment.NewLine + "You might wanna change the PetArea profiles in PetArea Settings");
            Pokehbuddy.quicksettingsfuncs.Add("BoostPetSlot1RingerExecute");
        }

        public void BoostPetSlot1RingerExecute()
        {
            ProfileManager.LoadEmpty();

            EnablePlugin("PetArea");
            //SetBotBase("gather");




            /*string addtodefault = "SWAPOUT EnemyPetLevel ISGREATERTHAN MyPetLevel + 3 $ Health(THISPET) ISLESSTHAN 70 $ MyPetsAlive ISGREATERTHAN 1";
            var item = listBox6.FindString(addtodefault);
            if (item == ListBox.NoMatches)
            {
                listBox6.Items.Add(addtodefault);
            }
            SaveDefault();*/

            Pokehbuddy.MySettings.AboveLevel = 8;
            Pokehbuddy.MySettings.AdFactor = 2;
            Pokehbuddy.MySettings.BelowLevel = 3;
            Pokehbuddy.MySettings.DisFactor = 2;
            Pokehbuddy.MySettings.Distance = 300;
            Pokehbuddy.MySettings.DoPreCombatSwapping = false;
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
            Pokehbuddy.MySettings.UseRatingSystem = false;
            Pokehbuddy.MySettings.UseWhiteList = false;
            Pokehbuddy.MySettings.PetOrder = "2,3,1";
            closeForm();
        }


    }
}
