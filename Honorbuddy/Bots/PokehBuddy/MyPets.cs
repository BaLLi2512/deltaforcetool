using Styx.WoWInternals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokehBuddy
{
    public class MyPets
    {
        public MyPets()
        {
            _pets = new BattlePet[3];
            _pets[0] = new BattlePet();
            _pets[1] = new BattlePet();
            _pets[2] = new BattlePet();
            _enemeyActivePet = new BattlePet();
            _activePet = -1;
            //updatePets();                 --Removed because we might not always have 3 pets.
        }

        private BattlePet[] _pets;
        private int _activePet;

        private BattlePet _enemeyActivePet;

        public BattlePet EnemeyActivePet
        {
            get { return _enemeyActivePet; }
        }

        public BattlePet ActivePet
        {
            get { return _pets[_activePet]; }
        }

        public BattlePet this[int index]
        {
            get { return _pets[index]; }
        }

        public void updateMyPets() {
            //Can probably be combined into one single (HUGE) Lua statement
            List<string> l;
            l = Lua.GetReturnValues("return C_PetBattles.GetActivePet(1)");
            _activePet = Convert.ToInt32(l[0]) - 1;

			Styx.WoWInternals.WoWGuid newGUID = default(Styx.WoWInternals.WoWGuid);
			
            for (int i = 0; i < 3; i++)
            {
                if (PokehBuddy.inPetCombat())
                {
                    l = Lua.GetReturnValues("local speciesID = C_PetBattles.GetPetSpeciesID(1, " + (i + 1) + ") local level = C_PetBattles.GetLevel(1, " + (i + 1) + ") local xp, maxXP = C_PetBattles.GetXP(1, " + (i + 1) + ") local displayID = C_PetBattles.GetDisplayID(1, " + (i + 1) + ") local name = C_PetBattles.GetName(1, " + (i + 1) + ") local icon = C_PetBattles.GetIcon(1, " + (i + 1) + ") local petType = C_PetBattles.GetPetType(1, " + (i + 1) + ") local health = C_PetBattles.GetHealth(1, " + (i + 1) + ") local maxHealth = C_PetBattles.GetMaxHealth(1, " + (i + 1) + ") local power = C_PetBattles.GetPower(1, " + (i + 1) + ") local speed = C_PetBattles.GetSpeed(1, " + (i + 1) + ") local rarity = C_PetBattles.GetBreedQuality(1, " + (i + 1) + ") local petID = C_PetJournal.GetPetLoadOutInfo(" + (i + 1) + ") return speciesID, level, xp, maxXP, displayID, name, icon, petType, health, maxHealth, power, speed, rarity, petID");
					if(Styx.WoWInternals.WoWGuid.TryParseFriendly(l[13], out newGUID)) {
						_pets[i].updateActive(l[0], Convert.ToInt32(l[1]), Convert.ToInt32(l[2]), Convert.ToInt32(l[3]), Convert.ToInt32(l[4]), l[5], l[6], Convert.ToInt32(l[7]), Convert.ToInt32(l[8]), Convert.ToInt32(l[9]), Convert.ToInt32(l[10]), Convert.ToInt32(l[11]), Convert.ToInt32(l[12]), newGUID);
					}
                }
                else
                {
                    l = Lua.GetReturnValues("local petID = C_PetJournal.GetPetLoadOutInfo(" + (i + 1) + ") local speciesID, customName, level, xp, maxXp, displayID, isFavorite, name, icon, petType, creatureID, sourceText, description, isWild, canBattle, tradable, unique, obtainable = C_PetJournal.GetPetInfoByPetID(petID) local health, maxHealth, power, speed, rarity = C_PetJournal.GetPetStats(petID) return speciesID, customName, level, xp, maxXp, displayID, isFavorite, name, icon, petType, creatureID, sourceText, description, isWild, canBattle, tradable, unique, obtainable, health, maxHealth, power, speed, rarity, petID");
					if(Styx.WoWInternals.WoWGuid.TryParseFriendly(l[23], out newGUID)) {
						_pets[i].update(l[0]
							, l[1], 
							Convert.ToInt32(l[2]), 
							Convert.ToInt32(l[3]), 
							Convert.ToInt32(l[4]), 
							Convert.ToInt32(l[5]), 
							convertLuaBool(l[6]), 
							l[7], 
							l[8], 
							Convert.ToInt32(l[9]), 
							Convert.ToInt32(l[10]), 
							l[11], 
							l[12], 
							convertLuaBool(l[13]), 
							convertLuaBool(l[14]), 
							convertLuaBool(l[15]), 
							convertLuaBool(l[16]), 
							convertLuaBool(l[17]), 
							Convert.ToInt32(l[18]), 
							Convert.ToInt32(l[19]), 
							Convert.ToInt32(l[20]), 
							Convert.ToInt32(l[21]), 
							Convert.ToInt32(l[22]), 
							newGUID);
							//Convert.ToUInt64(l[23], 16));
					}
                }
            }
        }

        public void updateEnemyActivePet()
        {
            List<string> l = Lua.GetReturnValues("local petIndex = C_PetBattles.GetActivePet(2) local speciesID = C_PetBattles.GetPetSpeciesID(2, petIndex) local level = C_PetBattles.GetLevel(2, petIndex) local xp, maxXP = C_PetBattles.GetXP(2, petIndex) local displayID = C_PetBattles.GetDisplayID(2, petIndex) local name = C_PetBattles.GetName(2, petIndex) local icon = C_PetBattles.GetIcon(2, petIndex) local petType = C_PetBattles.GetPetType(2, petIndex) local health = C_PetBattles.GetHealth(2, petIndex) local maxHealth = C_PetBattles.GetMaxHealth(2, petIndex) local power = C_PetBattles.GetPower(2, petIndex) local speed = C_PetBattles.GetSpeed(2, petIndex) local rarity = C_PetBattles.GetBreedQuality(2, petIndex) return speciesID, level, xp, maxXP, displayID, name, icon, petType, health, maxHealth, power, speed, rarity");
            _enemeyActivePet.updateActive(l[0], 
                Convert.ToInt32(l[1]), 
                Convert.ToInt32(l[2]), 
                Convert.ToInt32(l[3]), 
                Convert.ToInt32(l[4]), 
                l[5], 
                l[6], 
                Convert.ToInt32(l[7]), 
                Convert.ToInt32(l[8]),
                Convert.ToInt32(l[9]), 
                Convert.ToInt32(l[10]), 
                Convert.ToInt32(l[11]), 
                Convert.ToInt32(l[12]), 
                default(Styx.WoWInternals.WoWGuid));
        }

        public bool convertLuaBool(string s)
        {
            return s == "nil" ? false : true;
        }
    }
}
