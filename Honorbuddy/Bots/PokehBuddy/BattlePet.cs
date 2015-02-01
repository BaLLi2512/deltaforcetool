using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokehBuddy
{
    public class BattlePet
    {
        private string _speciesID;
        private string _customName;
        private int _level;
        private int _xp;
        private int _maxXp;
        private int _displayID;
        private bool _isFavorite;
        private string _name;
        private string _icon;
        private int _petType;
        private int _creatureID;
        private string _sourceText;
        private string _description;
        private bool _isWild;
        private bool _canBattle;
        private bool _tradeable;
        private bool _unique;
        private bool _obtainable;

        private int _health, _maxHealth, _power, _speed, _rarity;

        //private UInt64 _petID;
        private Styx.WoWInternals.WoWGuid _petID;




        public BattlePet() : this("", "", -1,-1,-1,-1,false,"","",-1,-1,"","",false,false,false,false,false,-1,-1,-1,-1,-1,default(Styx.WoWInternals.WoWGuid))
        {
            
        }

        public BattlePet(string speciesId, string customName, int level, int xp, int maxXP, int displayID, bool isFavorite, string name, string icon, 
            int petType, int creatureID, string sourceText, string description, bool isWild, bool canBattle, bool tradeable, bool unique, bool obtainable,
            int health, int maxHealth, int power, int speed, int rarity, Styx.WoWInternals.WoWGuid petID)
        {
            _speciesID = speciesId;
            _customName = customName;
            _level = level;
            _xp = xp;
            _maxXp = maxXP;
            _displayID = displayID;
            _isFavorite = isFavorite;
            _name = name;
            _icon = icon;
            _petType = petType;
            _creatureID = creatureID;
            _sourceText = sourceText;
            _description = description;
            _isWild = isWild;
            _canBattle = canBattle;
            _tradeable = tradeable;
            _unique = unique;
            _obtainable = obtainable;
            _health = health;
            _maxHealth = maxHealth;
            _power = power;
            _speed = speed;
            _rarity = rarity;
            _petID = petID;
        }

        public void update(string speciesId, string customName, int level, int xp, int maxXP, int displayID, bool isFavorite, string name, string icon,
            int petType, int creatureID, string sourceText, string description, bool isWild, bool canBattle, bool tradeable, bool unique, bool obtainable, 
            int health, int maxHealth, int power, int speed, int rarity, Styx.WoWInternals.WoWGuid petID)
        {
            _speciesID = speciesId;
            _customName = customName;
            _level = level;
            _xp = xp;
            _maxXp = maxXP;
            _displayID = displayID;
            _isFavorite = isFavorite;
            _name = name;
            _icon = icon;
            _petType = petType;
            _creatureID = creatureID;
            _sourceText = sourceText;
            _description = description;
            _isWild = isWild;
            _canBattle = canBattle;
            _tradeable = tradeable;
            _unique = unique;
            _obtainable = obtainable;
            _health = health;
            _maxHealth = maxHealth;
            _power = power;
            _speed = speed;
            _rarity = rarity;
            _petID = petID;
        }
        //speciesID, level, xp, maxXP, displayID, name, icon, petType, isWild, health, maxHealth, power, speed, rarity
        public void updateActive(string speciesId, int level, int xp, int maxXP, int displayID, string name, string icon, int petType, 
            int health, int maxHealth, int power, int speed, int rarity, Styx.WoWInternals.WoWGuid petID)
        {
            _speciesID = speciesId;
            _level = level;
            _xp = xp;
            _maxXp = maxXP;
            _displayID = displayID;
            _name = name;
            _icon = icon;
            _petType = petType;
            _health = health;
            _maxHealth = maxHealth;
            _power = power;
            _speed = speed;
            _rarity = rarity;
            _petID = petID;
        }

        public Styx.WoWInternals.WoWGuid PetID
        {
            get { return _petID; }
        }

        public string SpeciesID
        {
            get { return _speciesID; }
        }

        public string CustomName
        {
            get { return _customName; }
        }

        public int Level
        {
            get { return _level; }
        }

        public int Xp
        {
            get { return _xp; }
        }

        public int MaxXp
        {
            get { return _maxXp; }
        }

        public int DisplayID
        {
            get { return _displayID; }
        }

        public bool IsFavorite
        {
            get { return _isFavorite; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Icon
        {
            get { return _icon; }
        }

        public int PetType
        {
            get { return _petType; }
        }

        public int CreatureID
        {
            get { return _creatureID; }
        }

        public string SourceText
        {
            get { return _sourceText; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool IsWild
        {
            get { return _isWild; }
        }

        public bool CanBattle
        {
            get { return _canBattle; }
        }

        public bool Tradeable
        {
            get { return _tradeable; }
        }

        public bool Unique
        {
            get { return _unique; }
        }

        public bool Obtainable
        {
            get { return _obtainable; }
        }

        public int Rarity
        {
            get { return _rarity; }
        }

        public int Speed
        {
            get { return _speed; }
        }

        public int Power
        {
            get { return _power; }
        }

        public int MaxHealth
        {
            get { return _maxHealth; }
        }

        public int Health
        {
            get { return _health; }
        }

    }
}
