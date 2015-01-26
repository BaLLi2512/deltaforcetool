using Styx.Plugins;
using Styx.WoWInternals;
using Styx.CommonBot;
using Styx.Common;
using System;
using System.IO;
using System.Collections.Generic;
//using System.Timers;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using GreyMagic;

using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Timers;
/*
 - BNET 
 - Profiles
*/
namespace BuddyCon2
{

    public class BuddyCon2 : HBPlugin
    {
        Connection con;
        String wowpath;

        DateTime startTime;

        DateTime lastProwl;

        DateTime send15Interval; 
        DateTime lastSend; 

		Boolean onEventReg;
		
        int watchItemCount = 0;

        public static BuddyCon2 _instance;
        public static BuddyCon2 Instance { get { return _instance; } }
        private static System.Timers.Timer _timer;

        #region Plugin
        public override string Author { get { return "thalord"; } }

        public override string Name { get { return "BuddyCon2"; } }

        public override void Pulse()
        {

            if (!con.connected() && BuddyConSettings2.Instance.apikey.Length > 6 && BuddyCon2._instance != this)
            {
                Util.PostLog(string.Format("[bC2]: Version {0} try connect", Version.ToString()));

                con.connect();
            }
			
			if ((DateTime.Now - lastSend).TotalSeconds >= 15)
            {
				lastSend = DateTime.Now;
				
				Util.PostLog(string.Format("[bc2]: start pulse update"));
				BuddyCon2._instance.con.sendJSON(BuddyCon2._instance.getData());
				Util.PostLog(string.Format("[bc2]: end pulse update"));
				
			}

            if ((DateTime.Now - send15Interval).TotalMinutes >= 15 && BuddyConSettings2.Instance.watchreputationID != 0)
            {
                
                BuddyCon2._instance.data["reputation"] = Convert.ToUInt32(Styx.StyxWoW.Me.GetReputationWith(Convert.ToUInt32(BuddyConSettings2.Instance.watchreputationID))).ToString();
                BuddyCon2._instance.data["reputationlevel"] = Convert.ToUInt32(Styx.StyxWoW.Me.GetReputationLevelWith(Convert.ToUInt32(BuddyConSettings2.Instance.watchreputationID))).ToString();
                
                string repulevel = "";
                switch (BuddyCon2._instance.data["reputationlevel"])
                {
                    case "7":
                        repulevel = "Exalted";
                        break;
                    case "6":
                        repulevel = "Revered";
                        break;
                    case "5":
                        repulevel = "Honored";
                        break;
                    case "4":
                        repulevel = "Friendly";
                        break;
                    case "3":
                        repulevel = "Neutral";
                        break;
                    case "2":
                        repulevel = "Unfriendly";
                        break;
                    case "1":
                        repulevel = "Hostile";
                        break;
                    default:
                        repulevel = "Hated?";
                        break;
                }
                BuddyCon2._instance.data["status"] = string.Format("Reputation for FactionID {0}: {1} of {2}", BuddyConSettings2.Instance.watchreputationID, BuddyCon2._instance.data["reputation"], repulevel);
                
                con.sendJSON(BuddyCon2._instance.data);
                if (BuddyConSettings2.Instance.androidapi.Length > 10)
                {
                    Dictionary<string, string> dataToSend = new Dictionary<string, string>(data);
                    dataToSend.Remove("apikey");
                    dataToSend["server"] = dataToSend["server"].Replace("'", "");
                    String jsonString = Util.MyDictionaryToJson(dataToSend);
                    jsonString = jsonString.Replace(@"""{}""", "{}").Replace(@"""{", "{").Replace(@"}"",", "},");
                    Util.PostLog("[bc2]: send notification : " + jsonString);
                    Util.SendNotification(BuddyConSettings2.Instance.androidapi, jsonString);
                    dataToSend.Clear();
                }
                Util.PostLog(string.Format("[bC2]: {0}", BuddyCon2._instance.data["status"]));
                Util.sendToProwl("Looted",
                   BuddyCon2._instance.data["status"]
                    , data["name"], data["server"]);

                BuddyCon2._instance.data["status"] = "";

                //reset
                send15Interval = DateTime.Now;
            }

            if (BuddyConSettings2.Instance.notfStatusProwl > 0)
            {

                DateTime now = DateTime.Now;
                //Util.PostLog(string.Format("[bc2]: notity on Status Minutes {0} and now {1}", BuddyConSettings2.Instance.notfStatusProwl, (now - lastProwl).TotalMinutes));
                if ((now - lastProwl).TotalMinutes >= BuddyConSettings2.Instance.notfStatusProwl)
                {
                    double xpPro = 0;
                    String nodesString = "  ";
                    String running = "";

                    try
                    {
                        Dictionary<string, int> nodes = Bots.Gatherbuddy.GatherbuddyBot.NodeCollectionCount;
                        foreach (KeyValuePair<string, int> pair in nodes)
                        {
                            nodesString = nodesString + "\n  "+pair.Key+":"+pair.Value;
                        }
                        xpPro = Math.Floor(((double)Styx.StyxWoW.Me.Experience / (double)Styx.StyxWoW.Me.NextLevelExperience) * 100);
                        running = (DateTime.Now - startTime).TotalMinutes.ToString()+" Min";

                    }
                    catch (Exception e) {
                        Util.PostLog(string.Format("[bC2]: Prowl Status error:{0}", e.Message));
                    }
                    String sendString = string.Format("[bc2]: Status:\nLevel: {0} \nXP in %: {1}\nGold: {3}\nRunning Time: {4}\nTime to Level: {5}\nKills Total: {6}\nFree Bag Slots: {7}\nNodes:{2}\n", data["level"], xpPro, nodesString, data["gold"], running, Styx.CommonBot.GameStats.TimeToLevel.TotalMinutes + " Min", data["kills"], data["bagfree"]);
                    Util.PostLog(string.Format("[bC2]: Prowl Status send:{0}", sendString));
                    Util.sendToProwl("Status",
                        sendString
                        , data["name"], data["server"]);
                    lastProwl = DateTime.Now;
                }
            } 
			
			
        }

        public override System.Version Version
        {
            get { return new System.Version(2, 2, 0); }
        }

        public override void OnButtonPress() { new FormSettings().ShowDialog(); }

        public override bool WantButton { get { return true; } }

        public override string ButtonText { get { return "Settings"; } }

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        #endregion

        private Dictionary<string, string> data;

        

        //public BuddyCon2() 
        public override void OnEnable()
        {
            if (BuddyCon2._instance != this)
            {
                BuddyCon2._instance = this;
                Util.ShowToLog(string.Format("[bC2]: Version {0} initalized", Version.ToString()));

                startTime = DateTime.Now;
                lastProwl = DateTime.Now;
                send15Interval = DateTime.Now;
				lastSend = DateTime.Now;

                con = new Connection();
                con.connect();
                Chat.Say += Chat_Say;
                Chat.Yell += Chat_Yell;
                Chat.Whisper += Chat_Whisper;
                Chat.Party += Chat_Party;
                Chat.PartyLeader += Chat_Party;
                Chat.Guild += Chat_Guild;
                Chat.Emote += Chat_Emote;
                Chat.Battleground += Chat_BG;
                Chat.BattlegroundLeader += Chat_BG;
                Chat.Raid += Chat_Raid;
                Chat.RaidLeader += Chat_Raid;
                Chat.Officer += Chat_Officer;

                Styx.CommonBot.BotEvents.Player.OnPlayerDied += onDead;
                Styx.CommonBot.BotEvents.Player.OnLevelUp += onLevel;
                Styx.CommonBot.BotEvents.Player.OnMobKilled += onMobkill;

                Styx.CommonBot.BotEvents.OnBotStopped += onStop;
                Styx.CommonBot.BotEvents.OnBotStarted += onStart;

                Lua.Events.AttachEvent("CHAT_MSG_LOOT", Looted);

                Lua.Events.AttachEvent("GUILDBANKFRAME_OPENED", GbankUpdate);
                Lua.Events.AttachEvent("GUILDBANK_UPDATE_MONEY", GbankUpdate);

                Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER", BNetWhisper);
                Lua.Events.AttachEvent("GMRESPONSE_RECEIVED", GMResponse);

				onEventReg = true;
				
                data = new Dictionary<string, string>();
                data.Add("version", Version.ToString());
                data.Add("name", "");
                data.Add("level", "");
                data.Add("xp", "");
                data.Add("xp_needed", "");
                data.Add("chat_message", "");
                data.Add("chat_type", "");
                data.Add("chat_from", "");
                data.Add("screen", "");
                data.Add("nodeh", "");
                data.Add("runningtime", "");
                data.Add("xph", "");
                data.Add("timeolevel", "");
                data.Add("kills", "");
                data.Add("killsh", "");
                data.Add("honor", "");
                data.Add("honorh", "");
                data.Add("death", "");
                data.Add("deathh", "");
                data.Add("bgwin", "");
                data.Add("bglost", "");
                data.Add("server", "");
                data.Add("gbankmoney", "");
                data.Add("status", "");
                data.Add("faction", "");
                data.Add("bagfree", "");
                data.Add("reputation", "");
                data.Add("reputationlevel", "");

                // init start values
                data["level"] = Convert.ToInt32(Styx.StyxWoW.Me.Level).ToString();
                data["apikey"] = BuddyConSettings2.Instance.apikey;
                data["server"] = Styx.StyxWoW.Me.RealmName.ToString();
                data["faction"] = Styx.StyxWoW.Me.IsAlliance.ToString();


                data["name"] = Styx.StyxWoW.Me.Name.ToString();
                if (Convert.ToInt32(Styx.StyxWoW.Me.Experience) > 0 && Convert.ToInt32(Styx.StyxWoW.Me.Experience) <= Int32.MaxValue) data["xp"] = Convert.ToInt32(Styx.StyxWoW.Me.Experience).ToString();
                if (Convert.ToInt32(Styx.StyxWoW.Me.NextLevelExperience) > 0 && Convert.ToInt32(Styx.StyxWoW.Me.NextLevelExperience) >= Int32.MaxValue) data["xp_needed"] = Convert.ToInt32(Styx.StyxWoW.Me.NextLevelExperience).ToString();
                wowpath = Styx.StyxWoW.Memory.Process.MainModule.FileName.Substring(0, Styx.StyxWoW.Memory.Process.MainModule.FileName.Length - 8);

                // start timer
				/*
                _timer = new System.Timers.Timer(15000);
                _timer.Elapsed += new ElapsedEventHandler(updater);
                _timer.Enabled = true;
				*/
				
                Util.PostLog(string.Format("[bc2]:Settings check-> api key length: {0} , android apikey length: {1}, sendstatus: {2}, notfsay: {3}, notfGuild: {4}, notfBG: {5}, notfRaid: {6}, notfWhisper: {7} ", 
                    BuddyConSettings2.Instance.apikey.Length,
                    BuddyConSettings2.Instance.androidapi.Length,
                    BuddyConSettings2.Instance.androidSendStatus,
                    BuddyConSettings2.Instance.notfSay,
                    BuddyConSettings2.Instance.notfGuild,
                    BuddyConSettings2.Instance.notfBG,
                    BuddyConSettings2.Instance.notfRaid,
                    BuddyConSettings2.Instance.notfWhisper
                    ));

                Util.PostLog(string.Format("[bc2]: Init done for {0} Level and faction: {1}", data["level"], data["faction"]));
            }
            }


        private void updater(object sender, ElapsedEventArgs e)
        {
            Util.PostLog(string.Format("[bc2]: start updater"));
            BuddyCon2._instance.con.sendJSON(BuddyCon2._instance.getData());
            Util.PostLog(string.Format("[bc2]: end updater"));
        }


        public override void OnDisable()
        {

            BuddyCon2._timer.Stop();
            BuddyCon2._timer.Dispose();

            Chat.Say -= Chat_Say;
            Chat.Yell -= Chat_Yell;
            Chat.Whisper -= Chat_Whisper;
            Chat.Party -= Chat_Party;
            Chat.PartyLeader -= Chat_Party;
            Chat.Guild -= Chat_Guild;
            Chat.Emote -= Chat_Emote;
            Chat.Battleground -= Chat_BG;
            Chat.BattlegroundLeader -= Chat_BG;
            Chat.Raid -= Chat_Raid;
            Chat.RaidLeader -= Chat_Raid;
            Chat.Officer -= Chat_Officer;

            Styx.CommonBot.BotEvents.Player.OnPlayerDied -= onDead;
            Styx.CommonBot.BotEvents.Player.OnLevelUp -= onLevel;
            Styx.CommonBot.BotEvents.Player.OnMobKilled -= onMobkill;

            //Lua.Events.DetachEvent("CHAT_MSG_LOOT", Loot);
            //Lua.Events.DetachEvent("CHAT_MSG_COMBAT_XP_GAIN", Xp);
            //Lua.Events.DetachEvent("SCREENSHOT_SUCCEEDED", uploadscreen);


            Lua.Events.DetachEvent("GUILDBANKFRAME_OPENED", GbankUpdate);
            Lua.Events.DetachEvent("GUILDBANK_UPDATE_MONEY", GbankUpdate);

            Lua.Events.DetachEvent("CHAT_MSG_BN_WHISPER", BNetWhisper);
            Lua.Events.DetachEvent("GMRESPONSE_RECEIVED", GMResponse);

            Styx.CommonBot.BotEvents.OnBotStopped -= onStop;
            Styx.CommonBot.BotEvents.OnBotStarted -= onStart;

			onEventReg = false;
			
			
            data = null;

            con.disconnect();

            Util.ShowToLog("[bC2]: Dispose ");

            base.OnDisable();


        }
        public Dictionary<string, string> getData()
        {
		
            //if (Styx.StyxWoW.IsInGame || Styx.StyxWoW.IsInWorld)
            //{
                try
                {
                    Util.PostLog(string.Format("[bc2]: start try getData"));

                    // fill rest data
                    using (Styx.StyxWoW.Memory.AcquireFrame())
                    {
                        BuddyCon2._instance.data["server"] = Styx.StyxWoW.Me.RealmName.ToString();
                        BuddyCon2._instance.data["faction"] = Styx.StyxWoW.Me.IsAlliance.ToString();

						Util.PostLog(string.Format("[bc2]: getData after faction"));

                        BuddyCon2._instance.data["name"] = Styx.StyxWoW.Me.Name.ToString();
                        BuddyCon2._instance.data["level"] = Convert.ToInt32(Styx.StyxWoW.Me.Level).ToString();
						Util.PostLog(string.Format("[bc2]: getData after level"));

                        BuddyCon2._instance.data["apikey"] = BuddyConSettings2.Instance.apikey;
                        BuddyCon2._instance.data["runningtime"] = (DateTime.Now - startTime).TotalSeconds.ToString();
                        BuddyCon2._instance.data["xp"] = Convert.ToUInt32(Styx.StyxWoW.Me.Experience).ToString();
                        BuddyCon2._instance.data["xp_needed"] = Convert.ToUInt32(Styx.StyxWoW.Me.NextLevelExperience).ToString();
                        BuddyCon2._instance.data["xph"] = Convert.ToUInt32(Styx.CommonBot.GameStats.XPPerHour).ToString();
                        BuddyCon2._instance.data["timetolevel"] = Convert.ToUInt32(Styx.CommonBot.GameStats.TimeToLevel.TotalSeconds).ToString();
						Util.PostLog(string.Format("[bc2]: getData after timetolevel"));

                        BuddyCon2._instance.data["kills"] = Convert.ToUInt32(Styx.CommonBot.GameStats.MobsKilled).ToString();
                        BuddyCon2._instance.data["killsh"] = Convert.ToUInt32(Styx.CommonBot.GameStats.MobsPerHour).ToString();
						Util.PostLog(string.Format("[bc2]: getData after kills"));

                        BuddyCon2._instance.data["honor"] = Convert.ToUInt32(Styx.CommonBot.GameStats.HonorGained).ToString();
                        BuddyCon2._instance.data["honorh"] = Convert.ToUInt32(Styx.CommonBot.GameStats.HonorPerHour).ToString();
                        BuddyCon2._instance.data["bgwin"] = Convert.ToUInt32(Styx.CommonBot.GameStats.BGsWon).ToString();
                        BuddyCon2._instance.data["bglost"] = Convert.ToUInt32(Styx.CommonBot.GameStats.BGsLost).ToString();
						Util.PostLog(string.Format("[bc2]: getData after bglost"));

                        BuddyCon2._instance.data["gold"] = Convert.ToUInt32(Styx.StyxWoW.Me.Copper).ToString();
                        BuddyCon2._instance.data["nodeh"] = JSON.JsonEncode(Bots.Gatherbuddy.GatherbuddyBot.NodeCollectionCount);
						Util.PostLog(string.Format("[bc2]: getData after nodeh"));

                        BuddyCon2._instance.data["bagfree"] = Convert.ToUInt32(Styx.StyxWoW.Me.FreeBagSlots).ToString();
						Util.PostLog(string.Format("[bc2]: getData after nodeh"));

                        if (BuddyConSettings2.Instance.watchreputationID != 0)
                        {
                            BuddyCon2._instance.data["reputation"] = Convert.ToUInt32(Styx.StyxWoW.Me.GetReputationWith(Convert.ToUInt32(BuddyConSettings2.Instance.watchreputationID))).ToString();
                            BuddyCon2._instance.data["reputationlevel"] = Convert.ToUInt32(Styx.StyxWoW.Me.GetReputationLevelWith(Convert.ToUInt32(BuddyConSettings2.Instance.watchreputationID))).ToString();
                        }
                        
						Util.PostLog(string.Format("[bc2]: getData after watchrep"));
						
                        Util.PostLog("[bc2]: gbankmoney" + data["gbankmoney"]);
						Util.PostLog(string.Format("[bc2]: getData after gbank"));


                    }
                }
                catch (Exception e)
                {
                    Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
                }
           // }

            return BuddyCon2._instance.data;
        }

        #region Chat
        private void Chatter(string message, string author = "", string type = "")
        {
            if (!message.StartsWith("OQ,"))
            {
                getData();
                data["chat_message"] = message;
                data["chat_type"] = type;
                data["chat_from"] = author;
				
				if(type=="BNet"){
					string friendname = Styx.WoWInternals.Lua.GetReturnVal<string>(string.Format("return BNGetFriendInfoByID({0})", author), 4);
					data["chat_from"] = string.Format("({0}){1}", author,friendname.ToString());
				}
				
                Util.ShowToLog(string.Format("[bc2]: Chat: From: {1}  Message: {0} Type: {2}", message, data["chat_from"], type));

                if (
                    (BuddyConSettings2.Instance.notfSay && type == "CHAT_MSG_SAY") ||
                    (BuddyConSettings2.Instance.notfGuild && type == "CHAT_MSG_GUILD") ||
                    (BuddyConSettings2.Instance.notfBG && (type == "CHAT_MSG_BATTLEGROUND" || type == "CHAT_MSG_BATTLEGROUND_LEADER")) ||
                    (BuddyConSettings2.Instance.notfRaid && (type == "CHAT_MSG_RAID" || type == "CHAT_MSG_RAID_LEADER")) ||
                    (BuddyConSettings2.Instance.notfWhisper && (type == "CHAT_MSG_WHISPER" || type == "BNet"))
                    )
                {
                    try
                    {
                        Util.PostLog("[bc2]: send notification");
                        Util.sendToProwl("Chat", string.Format("[bc2]: Chat: From: {1}  Message: {0} Type: {2}", message, author, type), data["name"], data["server"]);

                        if (BuddyConSettings2.Instance.androidapi.Length > 10)
                        {
                            Dictionary<string, string> dataToSend = new Dictionary<string, string>(data);
                            dataToSend.Remove("apikey");
                            dataToSend["server"] = dataToSend["server"].Replace("'", "");
                            String jsonString = Util.MyDictionaryToJson(dataToSend);
                            jsonString = jsonString.Replace(@"""{}""", "{}").Replace(@"""{", "{").Replace(@"}"",", "},");
                            Util.PostLog("[bc2]: send notification : " + jsonString);
                            Util.SendNotification(BuddyConSettings2.Instance.androidapi, jsonString);
                            dataToSend.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
                    }
                }
                if (BuddyConSettings2.Instance.scChat) screenie();
                con.sendJSON(data);
                data["chat_message"] = "";
                data["chat_type"] = "";
                data["chat_from"] = "";
            }
        }
        private void BNetWhisper(object sender, LuaEventArgs args)
        {
            if (!((string)args.Args[0]).StartsWith("OQ,0Z,")){
                Util.PostLog(string.Format("[bc2]: Chat: From: {1}  Message: {0} Type: {2}", (string)args.Args[0], args.Args[12], args.ToString()));
				Thread thread = new Thread(delegate() { Chatter((string)args.Args[0], (string)args.Args[12].ToString(), "BNet"); });
				thread.Start();
			}
        }

        private void GMResponse(object sender, LuaEventArgs args)
        {
            Util.PostLog(string.Format("[bc2]: Chat: From: {1}  Message: {0} Type: {2}", (string)args.Args[0], args.Args[1], args.ToString()));
        }

        private void Chat_Officer(Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();

        }
        private void Chat_Raid(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_BG(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Emote(Styx.CommonBot.Chat.ChatAuthoredEventArgs e)
        {

            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Party(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Whisper(Styx.CommonBot.Chat.ChatWhisperEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Yell(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Say(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }

        private void Chat_Guild(Styx.CommonBot.Chat.ChatGuildEventArgs e)
        {
            Thread thread = new Thread(delegate() { Chatter(e.Message, e.Author, e.EventName); });
            thread.Start();
        }
        #endregion



        internal void screenie()
        {
            Util.ShowToLog("[bc2]: Screenshot requested");
            try
            {
                Lua.Events.AttachEvent("SCREENSHOT_SUCCEEDED", uploadscreen);
                Lua.DoString("TakeScreenshot()");
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }
        }
        internal void sendChat(string type, string message, string to)
        {
            try
            {
                Lua.DoString("SendChatMessage(\"" + message + "\", \"" + type + "\", nil, \"" + to + "\");");
                Util.ShowToLog(string.Format("[bc2]: send Chat channel: {0} Message: {1} To:{2}", type, message, to));
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }

        }
        internal void sendBNWhisper(string id, string message)
        {
            try
            {
                Lua.DoString("BNSendWhisper(\"" + id + "\", \"" + message + "\");");
                Util.ShowToLog(string.Format("[bc2]: send BNWhisper id: {0} message: {1}", id, message));
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }

        }
        internal void sendMacro(string message)
        {
            try
            {
                Lua.DoString("RunMacroText(\"" + message + "\");");
                Util.ShowToLog(string.Format("[bc2]: send Macro {0} ", message));
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }

        }
        #region onX


        private void Looted(object sender, LuaEventArgs args)
        {
		Util.PostLog(string.Format("[bc2]: getting loot {0} ", (string)args.Args.GetValue(0)));
            try
            {
                Match loots = Regex.Match((string)args.Args.GetValue(0), ".*Hitem:([0-9]*):.*(rx|r)([0-9]*).*");
				if(loots.Success){
					if (BuddyConSettings2.Instance.watchitemid == Convert.ToInt32(loots.Groups[1].Value))
					{
						if(loots.Groups[2].Value == "rx")
							watchItemCount += Convert.ToInt32(loots.Groups[3].Value);
						else
							watchItemCount += 1;
						
						
						if (watchItemCount >= BuddyConSettings2.Instance.watchitemamount && BuddyConSettings2.Instance.watchitemamount>0)
						{
							BuddyCon2._instance.data["status"] =  string.Format("looted ItemID: {0} {1} times", BuddyConSettings2.Instance.watchitemid, watchItemCount);

							con.sendJSON(BuddyCon2._instance.data);
							if (BuddyConSettings2.Instance.androidapi.Length > 10)
							{
								Dictionary<string, string> dataToSend = new Dictionary<string, string>(data);
								dataToSend.Remove("apikey");
								dataToSend["server"] = dataToSend["server"].Replace("'", "");
								String jsonString = Util.MyDictionaryToJson(dataToSend);
								jsonString = jsonString.Replace(@"""{}""", "{}").Replace(@"""{", "{").Replace(@"}"",", "},");
								Util.PostLog("[bc2]: send notification : " + jsonString);
								Util.SendNotification(BuddyConSettings2.Instance.androidapi, jsonString);
								dataToSend.Clear();
							}
							Util.PostLog(string.Format("[bC2]: looted ItemID: {0} {1} times", BuddyConSettings2.Instance.watchitemid, watchItemCount));
							Util.sendToProwl("Looted",
							   BuddyCon2._instance.data["status"]
								, data["name"], data["server"]);

							BuddyCon2._instance.data["status"] = "";
						}
					}
				}
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1} args: {2}", e.Message, e.StackTrace, (string)args.Args.GetValue(0)));
            }
        }


        private void GbankUpdate(object sender, LuaEventArgs args)
        {
            try
            {
                BuddyCon2._instance.data["gbankmoney"] = Convert.ToUInt32(Styx.WoWInternals.Lua.GetReturnVal<Int32>("return GetGuildBankMoney()", 0)).ToString();
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }
        }

        private void onMobkill(BotEvents.Player.MobKilledEventArgs args)
        {
			Thread thread = new Thread(delegate() { con.sendJSON(getData()); });
            thread.Start();
            
        }

        private void onStart(EventArgs args)
        {
            startTime = DateTime.Now;
            // if(times != null)
            //    times.Dispose(); 
            Util.PostLog(string.Format("[bc2]: start onStart"));
			
			if(onEventReg == false){
				//using (Styx.StyxWoW.Memory.AcquireFrame()){
				Styx.CommonBot.BotEvents.Player.OnPlayerDied += onDead;
				Styx.CommonBot.BotEvents.Player.OnLevelUp += onLevel;
				//Styx.CommonBot.BotEvents.Player.OnMobKilled += onMobkill;
				onEventReg = true;
			}

            BuddyCon2._instance.data["name"] = Styx.StyxWoW.Me.Name.ToString();
            BuddyCon2._instance.data["server"] = Styx.StyxWoW.Me.RealmName.ToString();
            BuddyCon2._instance.data["gbankmoney"] = Convert.ToUInt32(Styx.WoWInternals.Lua.GetReturnVal<Int32>("return GetGuildBankMoney()", 0)).ToString();

            //}

            Util.ShowToLog("[bc2]: Bot started");
            if (BuddyConSettings2.Instance.notfStart)
                Util.sendToProwl("Bot", "Bot started", BuddyCon2._instance.data["name"], BuddyCon2._instance.data["server"]);

            BuddyCon2._instance.data["status"] = "Bot started";

            con.sendJSON(BuddyCon2._instance.data);
            BuddyCon2._instance.data["status"] = "";

            foreach (Styx.WoWInternals.WoWObjects.WoWItem item in Styx.StyxWoW.Me.BagItems)
            {
                if (item.ItemInfo.Id == BuddyConSettings2.Instance.watchitemid) watchItemCount += Convert.ToInt32(item.StackCount);
                //Util.PostLog(string.Format("[bc2]: bags {0} x {1}", item.ItemInfo.Id, item.StackCount));
            }
            
            Util.PostLog(string.Format("[bc2]: end onStart"));


        }

        private void onStop(EventArgs args)
        {
            //Thread thread = new Thread(delegate() { 

            Util.PostLog(string.Format("[bc2]: onstop start "));

			if(onEventReg == true){
				//times = new Timer(timesUpdater, data, 0, 15000);
				//using (Styx.StyxWoW.Memory.AcquireFrame()){
				Styx.CommonBot.BotEvents.Player.OnPlayerDied -= onDead;
				Styx.CommonBot.BotEvents.Player.OnLevelUp -= onLevel;
				//Styx.CommonBot.BotEvents.Player.OnMobKilled -= onMobkill;
				onEventReg = false;
			}
            //Lua.Events.DetachEvent("SCREENSHOT_SUCCEEDED", uploadscreen);
            //}
            Util.ShowToLog("[bc2]: Bot stopped");
            if (BuddyConSettings2.Instance.notfStop)
                Util.sendToProwl("Bot", "Bot stopped", BuddyCon2._instance.data["name"], BuddyCon2._instance.data["server"]);

            BuddyCon2._instance.data["status"] = "Bot stopped";
            con.sendJSON(BuddyCon2._instance.data);
            BuddyCon2._instance.data["status"] = "";

            watchItemCount = 0;

            Util.PostLog(string.Format("[bc2]: onstop end "));
            //});
            //thread.Start();

        }

        private void onDead()
        {
            try
            {
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {
                    BuddyCon2._instance.data["death"] = Convert.ToInt32(Styx.CommonBot.GameStats.Deaths).ToString();
                    BuddyCon2._instance.data["deathh"] = Convert.ToInt32(Styx.CommonBot.GameStats.DeathsPerHour).ToString();
                }
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }
            Util.ShowToLog("[bc2]: Died!");
            if (BuddyConSettings2.Instance.notfDied)
                Util.sendToProwl("Died", "Died", data["name"], data["server"]);
            BuddyCon2._instance.data["status"] = "Died!";
            con.sendJSON(BuddyCon2._instance.data);
            BuddyCon2._instance.data["status"] = "";
            if (BuddyConSettings2.Instance.scDied) screenie();

        }

        private void onLevel(BotEvents.Player.LevelUpEventArgs args)
        {
            try
            {
                BuddyCon2._instance.data["level"] = Convert.ToInt32(Styx.StyxWoW.Me.Level).ToString();
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }
            Util.ShowToLog("[bc2]: Level up!");

            if (BuddyConSettings2.Instance.notfLevel)
                Util.sendToProwl("Level", "Level up", BuddyCon2._instance.data["name"], BuddyCon2._instance.data["server"]);

            BuddyCon2._instance.data["status"] = "Level up!";
            con.sendJSON(BuddyCon2._instance.data);
            BuddyCon2._instance.data["status"] = "";

            if (BuddyConSettings2.Instance.scLevel) screenie();
        }
        #endregion

        private void uploadscreen(object sender, LuaEventArgs args)
        {
            try
            {
                Lua.Events.DetachEvent("SCREENSHOT_SUCCEEDED", uploadscreen);
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bc2]: getting error {0} stack: {1}", e.Message, e.StackTrace));
            }
            var directory = new DirectoryInfo(wowpath + "\\Screenshots\\");
            string ret = "";
            if (BuddyConSettings2.Instance.scripturl.Length > 15)
                ret = Util.PostToFtp(directory + Util.GetLatestWritenFileFileInDirectory(directory).ToString());
            else
                ret = Util.PostToImgur(directory + Util.GetLatestWritenFileFileInDirectory(directory).ToString(), "e6b704a473bc5894e03ff99db649e825");

            BuddyCon2._instance.data["screen"] = ret;
            BuddyCon2._instance.data["status"] = "Screenshot sent";
            con.sendJSON(BuddyCon2._instance.data);
            BuddyCon2._instance.data["status"] = "";
            BuddyCon2._instance.data["screen"] = "";
            Util.ShowToLog("[bc2]: Screenshot sent");

        }


    }
}
