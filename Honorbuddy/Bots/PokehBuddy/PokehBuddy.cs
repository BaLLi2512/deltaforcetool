using System;
using Styx.Common.Helpers;
using Styx.CommonBot.Inventory;
using Styx.Helpers;
using Styx.Plugins;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using Styx.Common;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx;

using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.POI;
/***************************************************************
TODO

-PetsAlive EnemyPetsAlive [Dunno what this is, was here before]
 * WildPetBattleTarget (or w/e) is called twice, should only be called once if possible.
 * 
 * Allow groundmount only movement [Flightor defaults to Navigator?]
 * clean up output [Should be pretty clean]
 * Sometimes lags when starting flight? [Flightor?]
 * Fights pets any time it sees them (If within level range), I like this feature however.
 * Cannot travel cross continent.
 * I don't think this will try to use flight-paths
 * Logics can continually swap pets in and out if all 3 pets are below their swapout threshold.
 * Swapping to second/swapping to third happens sometimes. Fix logic.
 * For some reason, always wants to switch when below 30 HP or something?
 * Some sort of memory leak in WoW. No idea what.
 * 
 * Optimize and update legacy code
***************************************************************/


using Styx.WoWInternals.World;
using Styx.WoWInternals.Misc;

using Bots.BGBuddy.Helpers;

using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Levelbot.Decorators.Death;
using Levelbot.Actions.Death;
using Bots.Grind;
using CommonBehaviors.Actions;
using System.Windows.Forms;
using NewMixedMode;

namespace PokehBuddy
{
    public partial class PokehBuddy : BotBase
    {

        public static MyPets myPets = new MyPets();

        public PokehBuddy()
        {
            MySettings = new BBSettings(Application.StartupPath + "\\Bots\\PokehBuddy\\Pokehbuddy.xml");
        }

        #region BotBaseOverrides

        private Composite _root;
        public override Composite Root
        {
            get
            {
                return _root ?? (_root = createBotBehavior());
            }
        }

        public override Form ConfigurationForm
        {
            get { return new configForm(); }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All; }
        }

        public override string Name
        {
            get { return "PokehBuddy"; }
        }

        public override void Initialize()
        {
            try
            {
                LoadDefaultLogic("Default Logic");
                BlacklistLoad();
                WhitelistLoad();
            }

            catch (Exception ex) { Log(ex.ToString()); }
            if (MySettings.AdFormula.Contains("DisFactor"))
            {
                MySettings.HPFormula = "petHP * HPFactor";
                MySettings.AdFormula = "advantage * 50 * AdFactor";
                MySettings.DisFormula = "disadvantage * 50 * DisFactor";
                MySettings.LevelFormula = "(petLevel - enemylevel) * 4 * LevelFactor";
            }
            battleCount = 0;
        }

        public override void Pulse()
        {
        }

        public override void Start()
        {
            BotPoi.Clear();
            AttachLuaEvents();
            myPets.updateMyPets();
        }

        public override void Stop()
        {
            //battleCount updated twice per battle
            Log("Fought " + battleCount/2 + " battles.");
            battleCount = 0;
            DetachLuaEvents();
        }

        public override bool RequiresProfile
        {
            get
            {
                return true;
            }
        }

        public override bool IsPrimaryType
        {
            get
            {
                return MySettings.isPrimaryType;
            }
        }

        public override bool RequirementsMet
        {
            get
            {
                return wildPetTargetNearby() || inPetCombat();
            }
        }

#endregion
        #region Behaviors
        public Composite createBotBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !hasThreePetsEquiped(),
                    NeedThreePetsAction()),
                new Decorator(ret => !StyxWoW.Me.IsAlive,
                    new PrioritySelector(
                            new DecoratorNeedToRelease(new ActionReleaseFromCorpse()),
                            new DecoratorNeedToMoveToCorpse(LevelBot.CreateDeathBehavior()),
                            new DecoratorNeedToTakeCorpse(LevelBot.CreateDeathBehavior()),
                            new ActionSuceedIfDeadOrGhost()
                            )),
                new Decorator(
                        c => StyxWoW.Me.Combat && !StyxWoW.Me.IsFlying,
                        LevelBot.CreateCombatBehavior()),
                new Decorator(ret => inPetCombat(),
                    createPetCombatBehavior()),
                new Decorator(ret => numberOfPetsDead() >= PokehBuddy.MySettings.UseHealSkill,
                    new PrioritySelector(
                        new Decorator(ret => !isRezPetOnCooldown(),
                            healPetsAction()),
                        new Decorator(ret => shouldUsePetBandage(),
                            usePetBandageAction()),
                        new Decorator(ret =>3 - numberOfPetsDead() <= PokehBuddy.MySettings.MinPetsAlive,
                            waitToRezBattlePetsAction())
                        )),
                new Decorator(ret => PokehBuddy.MySettings.DoPVP,
                    createPvpBehavior()),
                createMovementBehavior());

        }

        public Composite createPetCombatBehavior()
        {
            return new PrioritySelector(
                    new Decorator(ret => mustSelectNew(),
                        SelectNewAction()),
                    new Decorator(ret => !CanFight(),
                        new ActionAlwaysSucceed()),
                    new Decorator(ret => shouldTrap(),
                        TrapPetAction()),
                    new Decorator(ret => shouldForfeit(),
                        forfeitBattleAction()),
                    PetBattleAttackAction()
                );
        }

        public Composite createPvpBehavior()
        {
            return new PrioritySelector(
                    new Decorator(ret => !inPetBattleQueue(),
                        queueForPetBattleAction()),
                    new Decorator(ret => queuePrompted(),
                        acceptQueuePromptAction()),
                    new ActionAlwaysSucceed()
                );
        }

        public Composite createMovementBehavior()
        {
            return new PrioritySelector(
                        new Decorator(ret => poiIsWildPet(),
                            new PrioritySelector(
                                new Decorator(ret => !isWildPetValidTarget(),
                                    clearPoiAction()),
                                new Decorator(ret => shouldBlacklistTarget(),
                                    new Sequence(
                                        blacklistPoiAction(),
                                        clearPoiAction())),
                                new Decorator(ret => canInteract(),
                                    new Sequence(
                                        preCombatSwappingAction(),
                                        interactWildPetAction())),
                                moveToPoiAction(),
                                new ActionAlwaysSucceed())),
                        new Decorator(ret => wildPetTargetNearby(),
                            moveToWildPetAction()),
                        new Decorator(ret => !havePoi(),
                            setPoiToNextHotspotAction()),
                        moveToPoiAction(),
                        new ActionAlwaysSucceed()
                );
        }

        #endregion

        #region Actions
        private Composite NeedThreePetsAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("You must equip three pets to use this bot.");
                    Stop();
                    return RunStatus.Success;
                });
        }

        private Composite PetBattleAttackAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    //myPets.updateMyPets();
                    LoadPetSettings(myPets.ActivePet.PetID.ToString(), myPets.ActivePet.Name);
                    string dumdum = DefaultLogicz.Logic + "@" + PetSettings.Logic;
                    string[] PetLogics = dumdum.Split('@');
                    foreach (string alogic in PetLogics)
                    {
                        bool gelukt = ParseLogic(alogic);
                        if (gelukt)
                        {
                            if ((String.Compare(alogic.Substring(0, 7), "SWAPOUT") == 0))
                            {
                                doLogicTimer.Reset();
                                WantSwapping();
                            }
                            if ((String.Compare(alogic.Substring(0, 12), "CASTSPELL(1)") == 0))
                            {
                                doLogicTimer.Reset();
                                Lua.DoString("C_PetBattles.UseAbility(1)");
                            }
                            if ((String.Compare(alogic.Substring(0, 12), "CASTSPELL(2)") == 0))
                            {

                                doLogicTimer.Reset();
                                Lua.DoString("C_PetBattles.UseAbility(2)");
                            }
                            if ((String.Compare(alogic.Substring(0, 12), "CASTSPELL(3)") == 0))
                            {

                                doLogicTimer.Reset();
                                Lua.DoString("C_PetBattles.UseAbility(3)");
                            }
                            if ((String.Compare(alogic.Substring(0, 8), "PASSTURN") == 0))
                            {

                                doLogicTimer.Reset();
                                Lua.DoString("C_PetBattles.SkipTurn()");
                            }
                            if ((String.Compare(alogic.Substring(0, 7), "FORFEIT") == 0)) 
                            {
                                doLogicTimer.Reset(); 
                                Lua.DoString("C_PetBattles.ForfeitGame()");
                            }
                            /*else
                            {
                                Log("Don't think I can do anything. Skipping turn. 1");
                                Lua.DoString("C_PetBattles.SkipTurn()");
                            }*/
                        }
                        else if (!doLogicTimer.IsRunning)
                        {
                            doLogicTimer.Start();
                        }
                        else if (doLogicTimer.ElapsedMilliseconds > 15000)
                        {
                            Log("Don't think I can do anything. Skipping turn.");
                            Lua.DoString("C_PetBattles.SkipTurn()");
                        }

                    }
                    return RunStatus.Success;
                });
        }

        private Composite SelectNewAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    //myPets.updateMyPets();
                    //myPets.updateEnemyActivePet();
                    int slot1rating = 0;
                    int slot2rating = 0;
                    int slot3rating = 0;

                    if (CanSelect(1)) slot1rating = BattleRating(myPets[0].Level, myPets[0].Health, myPets[0].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);

                    if (CanSelect(2)) slot2rating = BattleRating(myPets[1].Level, myPets[1].Health, myPets[1].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);
                    if (CanSelect(3)) slot3rating = BattleRating(myPets[2].Level, myPets[2].Health, myPets[2].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);

                    if (!CanSelect(1)) slot1rating = slot1rating - 100000;
                    if (!CanSelect(2)) slot2rating = slot2rating - 100000;
                    if (!CanSelect(3)) slot3rating = slot3rating - 100000;
                    if (slot1rating < slot2rating || slot1rating < slot3rating)
                    {
                        //swap pet 
                        Log("Swapping pets");
                        if (slot2rating >= slot3rating) CombatCallPet(2);
                        if (slot2rating < slot3rating) CombatCallPet(3);
                        //Thread.Sleep(1000);

                    }
                    else
                    {
                        CombatCallPet(1);
                    }
                    return RunStatus.Success;
                });
        }

        private Composite TrapPetAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Trap Pet Action");
                    Lua.DoString("C_PetBattles.UseTrap()");
                    return RunStatus.Success;
                });
        }

        private Composite forfeitBattleAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Forfeit Battle Action");
                    int getrari = PokehBuddy.MySettings.GetRarity;
                    if (getrari > 4) getrari = 2;
                    List<string> forfeit = Lua.GetReturnValues("if C_PetBattles.GetBreedQuality(2,1) <= " + getrari + " and C_PetBattles.GetBreedQuality(2,2) <= " + getrari + " and C_PetBattles.GetBreedQuality(2,3) <= " + getrari + " and  C_PetBattles.IsWildBattle() == true then C_PetBattles.ForfeitGame() return 1 end return 0");
                    return RunStatus.Success;
                });
        }

        private Composite queueForPetBattleAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Lua.DoString("C_PetBattles.StartPVPMatchmaking()");
                    Log("Queued for PVP match");
                    return RunStatus.Success;
                });
        }

        private Composite acceptQueuePromptAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Lua.DoString("if C_PetBattles.GetPVPMatchmakingInfo()=='proposal' then C_PetBattles.AcceptQueuedPVPMatch() end");
                    Log("Accepting queue for PVP match");
                    return RunStatus.Success;
                });
        }

        private Composite interactWildPetAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    //Log("interactWildPetAction");
                    //if (Styx.StyxWoW.Me.Mounted) Styx.CommonBot.Mount.Dismount();
                    if (Styx.StyxWoW.Me.Mounted) Styx.CommonBot.Coroutines.CommonCoroutines.LandAndDismount();
                    //if (Styx.StyxWoW.Me.Mounted) Styx.CommonBot.Mount.ActionLandAndDismount();
                    BotPoi.Current.AsObject.Interact();
                    return RunStatus.Success;
                });
        }

        private Composite preCombatSwappingAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    //myPets.updateMyPets();
                    BotPoi.Current.AsObject.ToUnit().Target();
                    int tartype = GetTypeByTarget();
                    int tarlevel = GetLevelByTarget();
                    int slot1rating = BattleRating(myPets[0].Level, myPets[0].Health, myPets[0].PetID.ToString(), tartype, tarlevel);
                    int slot2rating = BattleRating(myPets[1].Level, myPets[1].Health, myPets[1].PetID.ToString(), tartype, tarlevel);
                    int slot3rating = BattleRating(myPets[2].Level, myPets[2].Health, myPets[2].PetID.ToString(), tartype, tarlevel);
                    //Log("Pet Ratings - Slot 1 : " + slot1rating + " " + "Slot 2 : " + slot2rating + " " + "Slot 3 : " + slot3rating);

                    //Try and level pets equally, regardless of rating. Also, no way the logic needs to be this complicated T.T
                    if (myPets[1].Level < myPets[0].Level && tarlevel - myPets[1].Level <= PokehBuddy.MySettings.BelowLevel + 1)
                    {
                        Log("Swapping to second pet. He needs some experience too!");
                        SetSlot(1, 2);
                    }
                    else if (myPets[2].Level < myPets[0].Level && tarlevel - myPets[2].Level <= PokehBuddy.MySettings.BelowLevel + 1)
                    {
                        Log("Swapping to third pet. He needs some experience too!");
                        SetSlot(1, 3);
                    }
                    else if (slot1rating < slot2rating && (myPets[1].Level == myPets[0].Level || tarlevel - myPets[0].Level > PokehBuddy.MySettings.BelowLevel + 1) && (slot2rating > slot3rating || myPets[2].Level != myPets[0].Level))
                    {
                        Log("Swapping to second pet for higher rating.");
                        SetSlot(1, 2);
                    }
                    else if (slot1rating < slot3rating && (myPets[2].Level == myPets[0].Level || tarlevel - myPets[0].Level > PokehBuddy.MySettings.BelowLevel + 1))
                    {
                        Log("Swapping to third pet for higher rating");
                        SetSlot(1, 3);
                    }
                    myPets.updateMyPets();
                    return RunStatus.Success;
                });
        }

        private Composite moveToWildPetAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    WoWUnit tar = WildBattleTarget();
                    if (tar == null) return RunStatus.Failure;
                    BotPoi.Current = new BotPoi(tar, PoiType.Interact, NavType.Fly);
                    blacklistTimer.Reset();
                    blacklistTimer.Start();
                    return RunStatus.Success;
                });
        }

        private Composite setPoiToNextHotspotAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    //Move to next hotspot

                    //Log("moveToNextHotspotAction");
                    Profile p = ProfileManager.CurrentProfile;
                    WoWPoint hsLoc = WoWPoint.Empty;
                    if (BotPoi.Current.Type == PoiType.Interact || BotPoi.Current.Type == PoiType.None)
                    {
                        p.GrindArea.CycleToNearest();
                        hsLoc = p.GrindArea.CurrentHotSpot.Position;
                    }
                    else hsLoc = p.GrindArea.GetNextHotspot();
                    BotPoi.Current = new BotPoi(hsLoc, PoiType.Hotspot, NavType.Fly);

                    return RunStatus.Success;
                });
        }

        private Composite moveToPoiAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    
                    if (canFly())
                        Flightor.MoveTo(BotPoi.Current.Location, false);
                    else
                    {
                        WoWPoint p = BotPoi.Current.Location;
                        Navigator.FindHeight(p.X,p.Y, out p.Z);
                        Navigator.MoveTo(p);

                    }
                    return RunStatus.Success;
                });
        }

        private Composite healPetsAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    SpellManager.Cast("Revive Battle Pets");
                    return RunStatus.Success;
                });
        }

        private Composite clearPoiAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Pet is dead or uninteractable, clearing POI.");
                    BotPoi.Clear();
                    return RunStatus.Success;
                });
        }

        private Composite waitToRezBattlePetsAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Waiting to revive pets.");
                    return RunStatus.Success;
                });
        }

        
        private Composite blacklistPoiAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Took longer than 30 seconds to enter battle with pet. Blacklisting.");
                    Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Interact, new TimeSpan(0, 1, 0), "Unable to battle.");
                    blacklistTimer.Reset();
                    return RunStatus.Success;
                });
        }

        private Composite usePetBandageAction()
        {
            return new Styx.TreeSharp.Action(
                ctx =>
                {
                    Log("Enough pets injured, Using Bandages");
                    Lua.DoString("RunMacroText(\"/use Battle Pet Bandage\");");
                    return RunStatus.Success;
                });
        }
        #endregion


        #region decoratorHelpers
        public bool hasThreePetsEquiped()
        {
           //Log("HasThreePetsEquiped");
           List<string> petsequipped = Lua.GetReturnValues("local dummy=3 if C_PetJournal.GetPetLoadOutInfo(1)==nil then dummy=dummy-1 end if C_PetJournal.GetPetLoadOutInfo(2)==nil then dummy=dummy-1 end if C_PetJournal.GetPetLoadOutInfo(3)==nil then dummy=dummy-1 end return dummy");
           return petsequipped[0] == "3";
        }

        static public bool inPetCombat()
        {
            //Log("InPetCombat");
            List<string> cnt = Lua.GetReturnValues("dummy,reason=C_PetBattles.IsTrapAvailable() return dummy,reason");

            if (cnt != null) { if (cnt[1] != "0") return true; }
            return false;
        }

        public bool doPvp()
        {
            Log("Do PVP");
            return PokehBuddy.MySettings.DoPVP;
        }

        public bool mustSelectNew()
        {
            List<string> cnt = Lua.GetReturnValues("return C_PetBattles.ShouldShowPetSelect()");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }

        public bool shouldTrap()
        {
            //Log("ShouldTrap");
            //myPets.updateEnemyActivePet();
            int minrarity = PokehBuddy.MySettings.GetRarity;
            if (minrarity > 4)
            {


                minrarity = GetRarityBySpeciesID(myPets.EnemeyActivePet.SpeciesID.ToString());

                //Not sure minrarity will ever be below 0, but w/e
                if (minrarity < 1) minrarity = 0;


            }

            if (myPets.EnemeyActivePet.Rarity > minrarity && CanTrap())
            {
                if (CanFight()) return true;

            }
            return false;
        }

        public bool shouldForfeit()
        {
            //Log("ShouldForfeit");
            int getrari = PokehBuddy.MySettings.GetRarity;
            if (getrari > 4) getrari = 2;
            if (PokehBuddy.MySettings.ForfeitIfNotInteresting)
            {
                List<string> forfeit = Lua.GetReturnValues("if C_PetBattles.GetBreedQuality(2,1) <= " + getrari + " and C_PetBattles.GetBreedQuality(2,2) <= " + getrari + " and C_PetBattles.GetBreedQuality(2,3) <= " + getrari + " and  C_PetBattles.IsWildBattle() == true then return 1 end return 0");
                if (forfeit != null && forfeit[0] == "1") return true;
            }
            return false;
        }

        public bool inPetBattleQueue()
        {
            Log("InPetBattleQueue");
            List<string> cnt = Lua.GetReturnValues("if C_PetBattles.GetPVPMatchmakingInfo()=='queued' then return true end return false");
            return cnt[0] == "1";
        }

        public bool queuePrompted()
        {
            Log("QueuePrompted");
            List<string> cnt = Lua.GetReturnValues("if C_PetBattles.GetPVPMatchmakingInfo()=='proposal' then return true end return false");
            return cnt[0] == "1";
        }

        public bool poiIsWildPet()
        {
            //Log("poiIsWildPet");
            if(BotPoi.Current.AsObject != null)
                return BotPoi.Current.AsObject.ToUnit().IsPetBattleCritter;
            return false;
        }

        public bool canInteract()
        {
            //Log("CanInteract");
            return BotPoi.Current.AsObject.WithinInteractRange;
        }

        public bool wildPetTargetNearby()
        {
            //Log("WildPetTargetNearby");
            return WildBattleTarget() != null;
        }

        public bool havePoi()
        {
            return BotPoi.Current.Type != PoiType.None && BotPoi.Current.Location.Distance2D(StyxWoW.Me.Location) > 5.0;
        }

        public int numberOfPetsDead()
        {
            int count = 0;
            //myPets.updateMyPets();
            if (myPets[0].Health < 40) count++;
            if (myPets[1].Health < 40) count++;
            if (myPets[2].Health < 40) count++;
            return count;
        }

        public bool isWildPetValidTarget()
        {
            var pet = BotPoi.Current.AsObject.ToUnit();
            return pet.IsAlive && !pet.IsTagged;
            
        }

        public bool isRezPetOnCooldown()
        {
            return SpellManager.Spells["Revive Battle Pets"].Cooldown;
        }

        public bool shouldBlacklistTarget()
        {
            return blacklistTimer.ElapsedMilliseconds >= 30000;
        }

        public bool shouldUsePetBandage()
        {
            
            return PokehBuddy.MySettings.UseBandagesToHeal != 0 && numberOfPetsDead() >= PokehBuddy.MySettings.UseBandagesToHeal;
        }

        #endregion

        #region EventHandlers
        private void AttachLuaEvents()
        {
            Lua.Events.AttachEvent("PET_BATTLE_CLOSE", LuaEndOfBattle);
            /*Lua.Events.AttachEvent("PET_BATTLE_OPENING_START", LuaPetBattleStarted);
            Lua.Events.AttachEvent("PET_BATTLE_PET_ROUND_PLAYBACK_COMPLETE", LuaEndOfRound);
            Lua.Events.AttachEvent("PET_BATTLE_TURN_STARTED", LuaRoundStart);
            Lua.Events.AttachEvent("CHAT_MSG_PET_BATTLE_COMBAT_LOG", PetChatter);*/

            Lua.Events.AttachEvent("PET_BATTLE_PET_CHANGED", LuaBattlePetChanged);
            Lua.Events.AttachEvent("PET_JOURNAL_LIST_UPDATE", LuaPetJournalListUpdate);
        }
        private void DetachLuaEvents()
        {

            Lua.Events.DetachEvent("PET_BATTLE_CLOSE", LuaEndOfBattle);
            /*Lua.Events.DetachEvent("PET_BATTLE_OPENING_START", LuaPetBattleStarted);
            Lua.Events.DetachEvent("PET_BATTLE_PET_ROUND_PLAYBACK_COMPLETE", LuaEndOfRound);
            Lua.Events.DetachEvent("PET_BATTLE_TURN_STARTED", LuaRoundStart);
            Lua.Events.DetachEvent("CHAT_MSG_PET_BATTLE_COMBAT_LOG", PetChatter);*/
            Lua.Events.DetachEvent("PET_BATTLE_PET_CHANGED", LuaBattlePetChanged);
            Lua.Events.DetachEvent("PET_JOURNAL_LIST_UPDATE", LuaPetJournalListUpdate);
        }

        private void LuaEndOfBattle(object sender, LuaEventArgs args)
        {
            //Fires twice.
            battleCount++;
            doLogicTimer.Reset();
        }
        private void LuaBattlePetChanged(object sender, LuaEventArgs args)
        {
            //Not sure if this and below are needed
            myPets.updateMyPets();
            myPets.updateEnemyActivePet();
        }
        private void LuaPetJournalListUpdate(object sender, LuaEventArgs args)
        {
            myPets.updateMyPets();
            myPets.updateEnemyActivePet();
        }
        #endregion

        public static void Log(string s)
        {
            Logging.Write(s);
        }

        private bool canFly()
        {
            bool license;
            switch (StyxWoW.Me.MapId) {
                case 0:
                case 1:
                    license = SpellManager.HasSpell("Flight Master's License");
                    break;
                case 571: //NR
                    license = SpellManager.HasSpell("Cold Weather Flying");
                    break;
                case 870: //Pandaria
                    license = SpellManager.HasSpell("Wisdom of the Four Winds");
                    break;
                default:
                    license = true;
                    break;
            }

            return Flightor.MountHelper.CanMount && Mount.FlyingMounts.Count > 0 && license;
        }

        /*********** 99% LEGACY CODE ****************/
        #region LegacyCode

        public static BBSettings MySettings;
        public int pvptimer;
        public int battleCount;
        public static PetBattleSettings PetSettings;
        public static PetBattleSettings DefaultLogicz;
        private static Stopwatch blacklistTimer = new Stopwatch();
        private static Stopwatch doLogicTimer = new Stopwatch();

        //private static readonly Form1 Gui2 = new Form1();
        public static List<string> theblacklist = new List<string>();
        public static List<string> thewhitelist = new List<string>();
        private string[] PetDefaultLogics = { "SWAPOUT Health(THISPET) ISLESSTHAN 30", "PASSTURN HASBUFF(822) EQUALS true", "PASSTURN HASBUFF(498) EQUALS true", "CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false" };
        public static void BlacklistLoad()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\blacklist.txt";

            theblacklist.Clear();

            try
            {
                StreamReader Read = new StreamReader(Convert.ToString(filename));
                while (Read.Peek() >= 0)
                {
                    string pline = Read.ReadLine();
                    if (pline != null)
                    {
                        theblacklist.Add(pline.ToLower());

                    }
                }
                Read.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }

        }

        public static void WhitelistLoad()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\whitelist.txt";

            thewhitelist.Clear();

            try
            {
                StreamReader Read = new StreamReader(Convert.ToString(filename));
                while (Read.Peek() >= 0)
                {
                    string pline = Read.ReadLine();
                    if (pline != null)
                    {
                        thewhitelist.Add(pline.ToLower());

                    }
                }
                Read.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }

        }

        public static void LoadPetSettings(string petID, string name)
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + petID + ".xml";
            if (!File.Exists(filename))
            {
                string filename2 = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + name + ".xml";
                if (File.Exists(filename2))
                {
                    File.Copy(filename2, filename);
                }
            }

            try
            {
                PetSettings = new PetBattleSettings(filename);


            }
            catch (Exception ex) { Log(ex.ToString()); }
            PetSettings.Logic = ConvertFromOldFile(PetSettings.Logic);
            PetSettings.Save();
        }
        public static void LoadPetSettingsBN(string petID)
        {

            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + petID + ".xml";


            try
            {
                PetSettings = new PetBattleSettings(filename);


            }
            catch (Exception ex) { Log(ex.ToString()); }
            PetSettings.Logic = ConvertFromOldFile(PetSettings.Logic);
            PetSettings.Save();
        }

        public static void LoadDefaultLogic(string filez)
        {

            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\" + filez + ".xml";


            try
            {
                DefaultLogicz = new PetBattleSettings(filename);


            }
            catch (Exception exc) { Log(exc.Message.ToString()); }


        }
        public bool ActualCalc(string s)
        {
            if (s == "" || s == null) return false;
            var ce = new CalcEngine.CalcEngine();

            var x = ce.Parse(s);
            //Logging.Write("Evaluating");
            var value = (bool)x.Evaluate();
            return value;


        }
        public bool Calc(string s)
        {

            char[] delimit = new char[] { '&' };
            string s10 = s;
            foreach (string substr in s10.Split(delimit))
            {
                if (!ActualCalc(substr)) return false;
            }
            //Log("returning true");
            return true;


        }


        public void WantSwapping()
        {
            int slot1rating = 0;
            int slot2rating = 0;
            int slot3rating = 0;
            //myPets.updateMyPets();
            //myPets.updateEnemyActivePet();
            slot1rating = BattleRating(myPets[0].Level, myPets[0].Health, myPets[0].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);

            slot2rating = BattleRating(myPets[1].Level, myPets[1].Health, myPets[1].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);
            slot3rating = BattleRating(myPets[2].Level, myPets[2].Health, myPets[2].PetID.ToString(), myPets.EnemeyActivePet.PetType, myPets.EnemeyActivePet.Level);
            if (!CanSelect(1)) slot1rating = slot1rating - 100000;
            if (!CanSelect(2)) slot2rating = slot2rating - 100000;
            if (!CanSelect(3)) slot3rating = slot3rating - 100000;

            if (slot1rating < slot2rating || slot1rating < slot3rating)
            {
                //swap pet 
                Log("Swapping Pets");
                if (slot2rating >= slot3rating) CombatCallPet(2);
                if (slot2rating < slot3rating) CombatCallPet(3);
                //Thread.Sleep(1000);

            }
            else
            {
                CombatCallPet(1);
            }
        }

        public int GetTypeByTarget()
        {
            List<string> cnt = Lua.GetReturnValues("local dummy=UnitBattlePetType('target') if dummy==nil then dummy=0 end return dummy");
            int getal = 0;
            try
            {
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }

            return getal;
        }

        public int GetLevelByTarget()
        {
            List<string> cnt = Lua.GetReturnValues("local dummy=UnitBattlePetLevel('target') if dummy==nil then dummy=0 end return dummy");
            int getal = 0;
            try
            {
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }
            return getal;
        }


        public void SetSlot(int slot, int fromslot)
        {
            Lua.DoString("local petID= C_PetJournal.GetPetLoadOutInfo(" + fromslot + ") C_PetJournal.SetPetLoadOutInfo(" + slot + ", petID)");

        }

        public void CombatCallPet(int petnum)
        {
            Lua.DoString("if C_PetBattles.GetActivePet(1) ~= " + petnum + " then C_PetBattles.ChangePet(" + petnum + ") end");
        }


        public void SetPetAbilities()
        {
            //myPets.updateMyPets();
            //For each pet
            for (int i = 1; i < 4; i++)
            {
                LoadPetSettings(myPets[i].PetID.ToString(), myPets[i].Name);
                if (PetSettings.SpellLayout == "") PetSettings.SpellLayout = "ASSIGNABILITY1(0)@ASSIGNABILITY2(0)@ASSIGNABILITY3(0)";
                //parse settings looking for AssignAbility1, AssignAbility2, AssignAbility3
                string dumdum = PetSettings.SpellLayout;

                string[] PetLogics = dumdum.Split('@');

                foreach (string alogic in PetLogics)
                {
                    if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY1(") == 0))
                    {
                        int FirstChr = alogic.IndexOf("ASSIGNABILITY1(") + 15;
                        int SecondChr = alogic.IndexOf(")", FirstChr);
                        string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                        if (strTemp.ToInt32() > 0) SetAbility(i, 1, strTemp.ToInt32());
                    }
                    if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY2(") == 0))
                    {
                        int FirstChr = alogic.IndexOf("ASSIGNABILITY2(") + 15;
                        int SecondChr = alogic.IndexOf(")", FirstChr);
                        string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                        if (strTemp.ToInt32() > 0) SetAbility(i, 2, strTemp.ToInt32());
                    }
                    if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY3(") == 0))
                    {
                        int FirstChr = alogic.IndexOf("ASSIGNABILITY3(") + 15;
                        int SecondChr = alogic.IndexOf(")", FirstChr);
                        string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                        if (strTemp.ToInt32() > 0) SetAbility(i, 3, strTemp.ToInt32());
                    }
                }
            }
        }

        public void SetAbility(int petSlot, int abilitySlot, int spellID)
        {
            Lua.DoString("petGUID = C_PetJournal.GetPetLoadOutInfo(" + petSlot + ") speciesID, customName, level = C_PetJournal.GetPetInfoByPetID(petGUID) if (" + abilitySlot + " == 1 and level > 9) then C_PetJournal.SetAbility(" + petSlot + "," + abilitySlot + "," + spellID + ") end if (" + abilitySlot + " == 2 and level > 14) then C_PetJournal.SetAbility(" + petSlot + "," + abilitySlot + "," + spellID + ") end if (" + abilitySlot + " == 3 and level > 19) then C_PetJournal.SetAbility(" + petSlot + "," + abilitySlot + "," + spellID + ") end");
        }

        public bool ParseLogic(string theLogic)
        {
            //myPets.updateMyPets();
            //myPets.updateEnemyActivePet();
            //if (String.Compare(theLogic.Substring(0, 13), "ASSIGNABILITY") == 0) return false;

            //private string PetDefaultLogics[] = {"SWAPOUT Health(THISPET) ISLESSTHAN 50","CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false",""};
            theLogic = theLogic.Replace("ISLESSTHAN", "<");
            theLogic = theLogic.Replace("ISGREATERTHAN", ">");
            theLogic = theLogic.Replace("EQUALS", "=");
            theLogic = theLogic.Replace("ISNOT", "<>");

            theLogic = theLogic.Replace("$", "&");
            string oldlogic = theLogic;


            theLogic = theLogic.Replace("SWAPOUT", "");
            theLogic = theLogic.Replace("FORFEIT", "");
            theLogic = theLogic.Replace("CASTSPELL(1)", "");
            theLogic = theLogic.Replace("CASTSPELL(2)", "");
            theLogic = theLogic.Replace("CASTSPELL(3)", "");
            theLogic = theLogic.Replace("PASSTURN", "");






            theLogic = theLogic.Replace("COOLDOWN(SKILL(1))", (!PetCanCast(1)).ToString());
            theLogic = theLogic.Replace("COOLDOWN(SKILL(2))", (!PetCanCast(2)).ToString());
            theLogic = theLogic.Replace("COOLDOWN(SKILL(3))", (!PetCanCast(3)).ToString());
            //Logging.Write("Ok so far!!!!!!!!!");
            theLogic = theLogic.Replace("Health(THISPET)", (100*(1.0 * myPets.ActivePet.Health / myPets.ActivePet.MaxHealth)).ToString());
            theLogic = theLogic.Replace("Health(ENEMYPET)", (100*(1.0 * myPets.EnemeyActivePet.Health * 100 / myPets.EnemeyActivePet.MaxHealth)).ToString());

            theLogic = theLogic.Replace("MyPetLevel", (myPets.ActivePet.Level).ToString());
            theLogic = theLogic.Replace("EnemyPetLevel", (myPets.EnemeyActivePet.Level).ToString());

            theLogic = theLogic.Replace("MyPetsAlive", (GetPetsAlive()).ToString());
            theLogic = theLogic.Replace("EnemyPetsAlive", (GetEnemyPetsAlive()).ToString());





            theLogic = theLogic.Replace("ENEMYSPEED", (myPets.EnemeyActivePet.Speed).ToString());
            theLogic = theLogic.Replace("MYPETSPEED", (myPets.ActivePet.Speed).ToString());

            //



            theLogic = theLogic.Replace("ENEMYTYPE", (myPets.EnemeyActivePet.PetType).ToString());
            theLogic = theLogic.Replace("HUMANOID", "1");
            theLogic = theLogic.Replace("DRAGONKIN", "2");
            theLogic = theLogic.Replace("FLYING", "3");
            theLogic = theLogic.Replace("UNDEAD", "4");
            theLogic = theLogic.Replace("CRITTER", "5");
            theLogic = theLogic.Replace("MAGIC", "6");
            theLogic = theLogic.Replace("ELEMENTAL", "7");
            theLogic = theLogic.Replace("BEAST", "8");
            theLogic = theLogic.Replace("AQUATIC", "9");
            theLogic = theLogic.Replace("MECHANICAL", "10");

            //id = ["hasbuff", "wheatherbuff", ...]
            //action = [checkforbuff, checkwheaterbuff,...]
            //action[id.indexof("hasbuff)]();

            while (theLogic.IndexOf("HASBUFF(") > -1)
            {
                int FirstChr = theLogic.IndexOf("HASBUFF(") + 8;
                int SecondChr = theLogic.IndexOf(")", FirstChr);
                string dumdumdum = theLogic.Substring(FirstChr, SecondChr - FirstChr);
                //Log(dumdumdum+" "+FirstChr+" "+SecondChr+""+CheckForBuff(dumdumdum));
                theLogic = theLogic.Replace("HASBUFF(" + dumdumdum + ")", (CheckForBuff(dumdumdum)).ToString());
            }
            while (theLogic.IndexOf("WEATHERBUFF(") > -1)
            {
                int FirstChr = theLogic.IndexOf("WEATHERBUFF(") + 12;
                int SecondChr = theLogic.IndexOf(")", FirstChr);
                string dumdumdum = theLogic.Substring(FirstChr, SecondChr - FirstChr);
                //Log(dumdumdum+" "+FirstChr+" "+SecondChr+""+CheckForBuff(dumdumdum));
                theLogic = theLogic.Replace("WEATHERBUFF(" + dumdumdum + ")", (CheckWeatherBuff(dumdumdum)).ToString());
            }
            while (theLogic.IndexOf("HASENEMYBUFF(") > -1)
            {
                int FirstChr = theLogic.IndexOf("HASENEMYBUFF(") + 13;
                int SecondChr = theLogic.IndexOf(")", FirstChr);
                string dumdumdum = theLogic.Substring(FirstChr, SecondChr - FirstChr);
                //Log(dumdumdum+" "+FirstChr+" "+SecondChr+""+CheckForBuff(dumdumdum));
                theLogic = theLogic.Replace("HASENEMYBUFF(" + dumdumdum + ")", (CheckEnemyForBuff(dumdumdum)).ToString());
            }
            while (theLogic.IndexOf("HASTEAMBUFF(") > -1)
            {
                int FirstChr = theLogic.IndexOf("HASTEAMBUFF(") + 12;
                int SecondChr = theLogic.IndexOf(")", FirstChr);
                string dumdumdum = theLogic.Substring(FirstChr, SecondChr - FirstChr);
                theLogic = theLogic.Replace("HASTEAMBUFF(" + dumdumdum + ")", (CheckTeamBuff(dumdumdum, 1)).ToString());
            } //HASTEAMBUFF ENEMYTEAMBUFF
            while (theLogic.IndexOf("ENEMYTEAMBUFF(") > -1)
            {
                int FirstChr = theLogic.IndexOf("ENEMYTEAMBUFF(") + 14;
                int SecondChr = theLogic.IndexOf(")", FirstChr);
                string dumdumdum = theLogic.Substring(FirstChr, SecondChr - FirstChr);
                theLogic = theLogic.Replace("ENEMYTEAMBUFF(" + dumdumdum + ")", (CheckTeamBuff(dumdumdum, 2)).ToString());
            }

            bool dumdum = Calc(theLogic);
            
            return dumdum;
        }

        public int BattleRating(int petLevel, int petHP, string petID, int enemytype, int enemylevel)
        {
            int advantage = 0;
            int disadvantage = 0;
            // (petHP * PokehBuddy.MySettings.HPFactor) * 10 + (advantage * 100 * PokehBuddy.MySettings.AdFactor) * 10 + (disadvantage *100 * PokehBuddy.MySettings.DisFactor)*10 + ((petLevel - enemylevel) * 4 * PokehBuddy.MySettings.LevelFactor) *10
            //PokehBuddy.MySettings.HPFactor
            //Logging.Write("Pet ID : " + petID);
            int mypet = GetTypeByID(petID);
            //Logging.Write("Pet Type : " + mypet);
            int rating = 0;
            //Logging.Write("target type " + enemytype);
            if (mypet == DumbChoiceTakeMoreDMG(enemytype)) disadvantage = -2;
            if (mypet == DumbChoiceDealLessDMG(enemytype)) disadvantage = disadvantage - 1;//rating -1;
            if (mypet == SmartChoiceTakeLessDMG(enemytype)) advantage = 1;
            if (mypet == SmartChoiceDealMoreDMG(enemytype)) advantage = advantage + 2;

            /*****************************************/
            var ce = new CalcEngine.CalcEngine();
            string s = "1 + 1 * 3";
            var x = ce.Parse(s);

            var value = x.Evaluate();
            var total = 0;

            //pet 1


            s = MySettings.HPFormula;
            s = s.Replace("petHP", petHP.ToString()).Replace("HPFactor", PokehBuddy.MySettings.HPFactor.ToString());
            x = ce.Parse(s);
            var HPresult = x.Evaluate();
            total = int.Parse(HPresult.ToString());



            s = MySettings.AdFormula;
            s = s.Replace("advantage", advantage.ToString()).Replace("AdFactor", MySettings.AdFactor.ToString());
            x = ce.Parse(s);
            var Adresult = x.Evaluate();
            total = total + int.Parse(Adresult.ToString());


            s = MySettings.DisFormula;
            s = s.Replace("disadvantage", disadvantage.ToString()).Replace("DisFactor", MySettings.DisFactor.ToString());
            x = ce.Parse(s);
            var Disresult = x.Evaluate();
            total = total + int.Parse(Disresult.ToString());

            s = MySettings.LevelFormula;
            s = s.Replace("petLevel", petLevel.ToString()).Replace("enemylevel", enemylevel.ToString()).Replace("LevelFactor", MySettings.LevelFactor.ToString());
            x = ce.Parse(s);
            var Levelresult = x.Evaluate();
            total = total + int.Parse(Levelresult.ToString());


            /***************************************/

            //do more rating stuff with health percentages, level difference
            rating = total; /*(petHP * PokehBuddy.MySettings.HPFactor) +
                     (advantage * 50 * PokehBuddy.MySettings.AdFactor) +
                     (disadvantage * 50 * PokehBuddy.MySettings.DisFactor) +
                     ((petLevel - enemylevel) * 4 * PokehBuddy.MySettings.LevelFactor);*/
            int oldrating = rating;
            if (petHP < 30) rating = rating - 10000;
            if (petHP < 15) rating = rating - 40000;
            if (petHP < 5) rating = rating - 50000;
            if (!CanSummon(petID)) rating = -100000000;
            
            return rating;
        }


        public bool CanSelect(int petnum)
        {
            List<string> cnt = Lua.GetReturnValues("return C_PetBattles.CanPetSwapIn(" + petnum + ")");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }



        public bool CanTrap()
        {
            List<string> cnt = Lua.GetReturnValues("dummy,reason=C_PetBattles.IsTrapAvailable() return dummy,reason");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }


        public bool CanSummon(string petID)
        {
            List<string> cnt = Lua.GetReturnValues("return C_PetJournal.PetIsSummonable(" + petID + ");");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }


        public bool PetCanCast(int skillnum)
        {
            List<string> cnt = Lua.GetReturnValues("local isUsable, currentCooldown = C_PetBattles.GetAbilityState(LE_BATTLE_PET_ALLY, C_PetBattles.GetActivePet(LE_BATTLE_PET_ALLY), " + skillnum + "); if isUsable == nil then isUsable=0 end return isUsable,currentCooldown");
            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }

        public bool CanFight()
        {
            List<string> cnt = Lua.GetReturnValues("return C_PetBattles.IsWaitingOnOpponent()");

            if (cnt != null) { if (cnt[0] == "0") return true; }
            return false;
        }


        public static int SmartChoiceTakeLessDMG(int enemytype)
        {
            if (enemytype > 10 || enemytype < 0) enemytype = 1;
            int[] smart = new int[] { 0, 8, 4, 2, 9, 1, 10, 5, 3, 6, 7 };
            return smart[enemytype];
        }
        //return {0,8,4,2,9,1,10,5,3,6,7}[enemytype]
        public static int SmartChoiceDealMoreDMG(int enemytype)
        {
            if (enemytype > 10 || enemytype < 0) enemytype = 1;
            int[] smart = new int[] { 0, 4, 1, 6, 5, 8, 2, 9, 10, 3, 7 };
            return smart[enemytype];
        }
        public static int DumbChoiceDealLessDMG(int enemytype)
        {
            if (enemytype > 10 || enemytype < 0) enemytype = 1;
            int[] smart = new int[] { 0, 5, 3, 8, 2, 7, 9, 10, 1, 4, 6 };
            return smart[enemytype];
        }
        public static int DumbChoiceTakeMoreDMG(int enemytype)
        {
            if (enemytype > 10 || enemytype < 0) enemytype = 1;
            int[] smart = new int[] { 0, 2, 6, 9, 1, 4, 3, 10, 5, 7, 8 };
            return smart[enemytype];
        }

        //for i=1, C_PetBattles.GetNumAuras(0,0) do local auraID = C_PetBattles.GetAuraInfo(LE_BATTLE_PET_WEATHER, PET_BATTLE_PAD_INDEX, i) if (auraID == 596) then return true end end return false
        //
        public bool CheckWeatherBuff(string buffnum)
        {

            List<string> cnt = Lua.GetReturnValues("for i=1, C_PetBattles.GetNumAuras(0,0) do local auraID = C_PetBattles.GetAuraInfo(LE_BATTLE_PET_WEATHER, PET_BATTLE_PAD_INDEX, i) if (auraID == " + buffnum + ") then return true end end return false");

            if (cnt[0] == "1") return true;
            return false;
        }

        public bool CheckTeamBuff(string buffnum, int teamnum)
        {

            List<string> cnt = Lua.GetReturnValues("for i=1, C_PetBattles.GetNumAuras(" + teamnum + ",0) do local auraID = C_PetBattles.GetAuraInfo(" + teamnum + ", PET_BATTLE_PAD_INDEX, i) if (auraID == " + buffnum + ") then return true end end return false");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }

        ///for i=1, C_PetBattles.GetNumAuras("+teamnum+",0) do local auraID = C_PetBattles.GetAuraInfo("+teamnum+", PET_BATTLE_PAD_INDEX, i) if (auraID == "+buffnum+") then return true end end return false
        public bool CheckForBuff(string buffnum)
        {

            List<string> cnt = Lua.GetReturnValues("for j=1,C_PetBattles.GetNumAuras(1,C_PetBattles.GetActivePet(1)) do  local buffid = C_PetBattles.GetAuraInfo(1,C_PetBattles.GetActivePet(1),j)  if buffid == " + buffnum + " then return (true) end end return( false) ");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }

        public bool CheckEnemyForBuff(string buffnum)
        {

            List<string> cnt = Lua.GetReturnValues("for j=1,C_PetBattles.GetNumAuras(2,C_PetBattles.GetActivePet(2)) do  local buffid = C_PetBattles.GetAuraInfo(2,C_PetBattles.GetActivePet(2),j)  if buffid == " + buffnum + " then return (true) end end return( false) ");

            if (cnt != null) { if (cnt[0] == "1") return true; }
            return false;
        }


        public int GetRarityBySpeciesID(string speciesid)
        {
            Lua.DoString("C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_FAVORITES, false) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_COLLECTED, true) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_NOT_COLLECTED, true) ");
            Lua.DoString("C_PetJournal.ClearSearchFilter() C_PetJournal.AddAllPetSourcesFilter() C_PetJournal.AddAllPetTypesFilter() ");
            ////Lua.DoString("C_PetJournal.SetSearchFilter('" + petname + "')");

            string lustring = "local dummy=-1 for i=1,C_PetJournal.GetNumPets(false) do     local petID, speciesID, isOwned, customName, level, favorite, isRevoked, name, icon, petType  = C_PetJournal.GetPetInfoByIndex(i, false)  if isOwned then local _, _, _, _, rarity = C_PetJournal.GetPetStats(petID) if (speciesID == " + speciesid + ")then if (rarity > dummy) then dummy=rarity ; end end end end return dummy;";

            List<string> cnt = Lua.GetReturnValues(lustring);


            if (cnt[0] != null && cnt[0] != "-1")
            {

                int numValue = -1;
                try
                {
                    numValue = Convert.ToInt32(cnt[0]);
                }
                catch (Exception exc)
                {
                    Log(exc.Message.ToString());
                }






                return numValue;

            }
            return -1;

        }

        public static int GetTypeByID(string thepetID)
        {
            Lua.DoString("C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_FAVORITES, false) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_COLLECTED, true) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_NOT_COLLECTED, true) ");
            Lua.DoString("C_PetJournal.ClearSearchFilter() C_PetJournal.AddAllPetSourcesFilter() C_PetJournal.AddAllPetTypesFilter() ");
            ////Lua.DoString("C_PetJournal.SetSearchFilter('" + petname + "')");

            List<string> cnt = Lua.GetReturnValues("local dummy=-1 for i=1,C_PetJournal.GetNumPets(false) do     local petID, speciesID, isOwned, customName, level, favorite, isRevoked, name, icon, petType  = C_PetJournal.GetPetInfoByIndex(i, false) if tonumber(petID,16) == " + thepetID + " then return petType end end  print('finished') return dummy;");
            //Logging.Write("Returned : " + cnt[0]);
            if (cnt != null)
            {
                if (cnt[0] != null && cnt[0] != "-1")
                {


                    int numValue = -1;
                    try
                    {
                        numValue = Convert.ToInt32(cnt[0]);
                    }
                    catch (Exception exc)
                    {
                        Log(exc.Message.ToString());
                    }


                    return numValue;

                }
            }
            return -1;
        }

        public static string SlotIcon(int slotnr)
        {
            List<string> cnt = Lua.GetReturnValues("local petID = C_PetJournal.GetPetLoadOutInfo(" + slotnr + "); local speciesID = C_PetJournal.GetPetInfoByPetID(petID) return speciesID");
            //Logging.Write(cnt[0]);
            //int decAgain = int.Parse(, System.Globalization.NumberStyles.HexNumber);
            //return cnt[0];//decAgain.ToString();
            for (int i = 0; i < allpetz.Length; i++)
            {
                if (allpetz[i] == cnt[0]) return allpetz[i + 2];
            }
            return "";

        }

        public static string GetSpeciesByName(string speciesID)
        {

            for (int i = 0; i < allpetz.Length; i++)
            {
                if (allpetz[i] == speciesID) return allpetz[i - 1];

            }
            return "Error";
        }

        public static string GetNameBySpeciesID(string speciesID)
        {

            for (int i = 0; i < allpetz.Length; i++)
            {
                if (allpetz[i] == speciesID) return allpetz[i + 1];

            }
            return "Error";
        }

        public static WoWUnit WildBattleTarget()
        {

            WoWUnit ret = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>(true, true)
                           orderby unit.Distance ascending

                           where !Blacklist.Contains(unit.Guid, BlacklistFlags.All)
                           //where unit.CreatureType.ToString() == "14"
                           where unit.IsPetBattleCritter
                           where !unit.IsDead
                           where (MySettings.UseWhiteList && thewhitelist.Contains(unit.Name.ToLower()) || !MySettings.UseWhiteList)
                           where (MySettings.UseBlackList && !theblacklist.Contains(unit.Name.ToLower()) || !MySettings.UseBlackList)




                           where unit.Distance < PokehBuddy.MySettings.Distance
                           select unit).FirstOrDefault();
            if (ret != null)
            {
                ret.Target();
                int dumlevel = GetWildLevel();
                if (dumlevel >= GetLowLevel() - (PokehBuddy.MySettings.BelowLevel + 1) && dumlevel <= GetHighLevel() + PokehBuddy.MySettings.AboveLevel + 1)
                {
                    Logging.Write("Attacking " + ret.Name + "(" + ret.Guid + ")");
                    return ret;
                }
                else
                {
                    if (dumlevel > 0)
                    {
                        Blacklist.Add(ret.Guid, BlacklistFlags.All, TimeSpan.FromMinutes(1));
                    }
                }

                //Logging.Write(""+ret.Name+" range " + ret.Guid);


            }
            return null;
        }

        public static int GetLowLevel()
        {
            int getal = 0;
            List<string> cnt = Lua.GetReturnValues("local dummy = 99 for j=1,3 do local petID= C_PetJournal.GetPetLoadOutInfo(j) local speciesID, customName, level = C_PetJournal.GetPetInfoByPetID(petID) if level < dummy then dummy=level end end return dummy");
            try
            {
                //Log(""+cnt[0]);
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }


            return getal;
        }
        public static int GetPetsAlive()
        {
            int getal = 0;

            List<string> cnt = Lua.GetReturnValues("dummy=0 for i = 1,C_PetBattles.GetNumPets(1)    do health, maxhealth = C_PetBattles.GetHealth(1, i) if health > 0 then dummy=dummy+1 end end return dummy");
            try
            {
                //Log(""+cnt[0]);
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }


            return getal;
        }
        public static int GetEnemyPetsAlive()
        {
            int getal = 0;

            List<string> cnt = Lua.GetReturnValues("dummy=0 for i = 1,C_PetBattles.GetNumPets(2)    do health, maxhealth = C_PetBattles.GetHealth(2, i) if health > 0 then dummy=dummy+1 end end return dummy");
            try
            {
                //Log(""+cnt[0]);
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }


            return getal;
        }





        public static int GetHighLevel()
        {
            int getal = 0;
            List<string> cnt = Lua.GetReturnValues("local dummy = 0 for j=1,3 do local petID= C_PetJournal.GetPetLoadOutInfo(j) local speciesID, customName, level = C_PetJournal.GetPetInfoByPetID(petID) if level > dummy then dummy=level end end return dummy");
            try
            {
                //Log(""+cnt[0]);
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }


            return getal;
        }
        public static int GetWildLevel()
        {
            //Logging.Write(GUID);
            int getal = 0;
            List<string> cnt = Lua.GetReturnValues("return UnitBattlePetLevel('target')");
            try
            {
                //Log(""+cnt[0]);
                getal = Convert.ToInt32(cnt[0]);
            }
            catch (Exception exc)
            {
                Log(exc.Message.ToString());
            }


            return getal;
        }

    }

}



/***********************************************************\
*															*
*				Settings part!!								*
*															*
\***********************************************************/

namespace PokehBuddy
{  // credits to Apoc for showing how to use the HB settings. & also credits from me, the guy that stole this from the AutoAngler Addon
    //DefaultLogic[0]={0,32000,""};


    public class BBSettings : Settings
    {

        public BBSettings(string settingsPath)
            : base(settingsPath)
        {
            Load();
        }

        [Setting, DefaultValue(1)]
        public int HPFactor { get; set; }

        [Setting, DefaultValue(1)]
        public int LevelFactor { get; set; }

        [Setting, DefaultValue(1)]
        public int AdFactor { get; set; }

        [Setting, DefaultValue(1)]
        public int DisFactor { get; set; }

        [Setting, DefaultValue(250)]
        public int Distance { get; set; }

        [Setting, DefaultValue(3)]
        public int GetRarity { get; set; }

        [Setting, DefaultValue(5)]
        public int BlacklistCounterLimit { get; set; }

        [Setting, DefaultValue(5)]
        public int SkipCounterLimit { get; set; }

        [Setting, DefaultValue(false)]
        public bool DoPVP { get; set; }

        [Setting, DefaultValue(5)]
        public int PVPMinTime { get; set; }

        [Setting, DefaultValue(10)]
        public int PVPMaxTime { get; set; }

        [Setting, DefaultValue(1)]
        public int MinPetsAlive { get; set; }

        [Setting, DefaultValue(3)]
        public int BelowLevel { get; set; }

        [Setting, DefaultValue(3)]
        public int AboveLevel { get; set; }

        [Setting, DefaultValue(0)]
        public int UseBandagesToHeal { get; set; }

        [Setting, DefaultValue(1)]
        public int UseHealSkill { get; set; }

        [Setting, DefaultValue(false)]
        public bool ForfeitIfNotInteresting { get; set; }

        [Setting, DefaultValue(false)]
        public bool UseWhiteList { get; set; }

        [Setting, DefaultValue(false)]
        public bool UseBlackList { get; set; }

        [Setting, DefaultValue(true)]
        public bool AmHorde { get; set; }

        [Setting, DefaultValue("petHP * HPFactor")]
        public string HPFormula { get; set; }

        [Setting, DefaultValue("advantage * 50 * AdFactor")]
        public string AdFormula { get; set; }

        [Setting, DefaultValue("disadvantage * 50 * DisFactor")]
        public string DisFormula { get; set; }

        [Setting, DefaultValue("(petLevel - enemylevel) * 4 * LevelFactor")]
        public string LevelFormula { get; set; }

        [Setting, DefaultValue(true)]
        public bool CheckAllowUsageTracking { get; set; }

        [Setting, DefaultValue(true)]
        public bool isPrimaryType { get; set; }



        [Setting, DefaultValue(false)]
        public bool IBSupport { get; set; }

    }

    public class PetBattleSettings : Settings
    {

        public PetBattleSettings(string settingsPath)
            : base(settingsPath)
        {
            Load();
        }

        [Setting, DefaultValue("SWAPOUT Health(THISPET) ISLESSTHAN 30@CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false")]
        public string Logic { get; set; }

        [Setting, DefaultValue("ASSIGNABILITY1(0)@ASSIGNABILITY2(0)@ASSIGNABILITY3(0)")]
        public string SpellLayout { get; set; }



    }
#endregion
}