using Styx.Common;
using System;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using Styx;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
//using HighVoltz.HBRelog.Remoting;

namespace BuddyCon2
{
    public class Connection
    {
        TcpClient tcpClient;
        NetworkStream serverStream = default(NetworkStream);
        string readData = string.Empty;

        Dictionary<string, string> queue = new Dictionary<string,string>();

        public bool connected()
        {
            if (tcpClient == null) return false;
            try
            {
                return tcpClient.Connected;
            }
            catch (Exception e)
            {
                return false;
            }
        }  
        public void sendString(String js, String hash)
        {
            Util.PostLog(string.Format("[bC2]: start send string"));
            //if(hash != "") queue.Add(hash,js);
            Util.PostLog(string.Format("[bC2]: string to send: {0}", js));
            Task taskOpenEndpoint = Task.Factory.StartNew(() =>
                {
                    //int count = 0;
                    //do{
                        try
                        {
                            Byte[] byteDateLine = Encoding.ASCII.GetBytes(js);
                            serverStream.Write(byteDateLine, 0, byteDateLine.Length);
                            serverStream.Flush();
                            
                        }
                        catch (Exception e) {
                            Util.PostLog(string.Format("[bC2]: send exception: {0}", e.StackTrace));
                            serverStream = default(NetworkStream);
                            Util.PostLog(string.Format("[bC2]: send exception serverstream", e.StackTrace));
                            disconnect();
                            Util.PostLog(string.Format("[bC2]: send exception disc", e.StackTrace));
                            connect();
                            Util.PostLog(string.Format("[bC2]: send exception reconnecting", e.StackTrace));
                            //queue.Remove(hash);
                        }
                        //count++;
                        //if (count>=10) break;

                        //Thread.Sleep(1000);
                    //}  while(queue.ContainsKey(hash));
                });
            Util.PostLog(string.Format("[bC2]: end send string"));

        }
        public void sendJSON(Dictionary<string, string> input)
        {
            if (BuddyConSettings2.Instance.androidapi.Length > 10 && BuddyConSettings2.Instance.androidSendStatus && input["chat_type"]=="")
            {
                Dictionary<string, string> dataToSend = new Dictionary<string, string>(input);
                dataToSend.Remove("apikey");
                dataToSend["server"] = dataToSend["server"].Replace("'", "");
                String jsonString = Util.MyDictionaryToJson(dataToSend);
                jsonString = jsonString.Replace(@"""{}""", "{}").Replace(@"""{", "{").Replace(@"}"",", "},");
                Util.PostLog("[bc2]: send notification : " + jsonString);
                Util.SendNotification(BuddyConSettings2.Instance.androidapi, jsonString);
                dataToSend.Clear();
            }

            Dictionary<string, string> js = new Dictionary<string, string>(input);

            String hash = js.GetHashCode().ToString();
            js.Add("hash",hash);

            String json = JSON.JsonEncode(js);

            if(js["chat_message"] != "") sendString(json,hash);
            else sendString(json,"");
        }
        public Connection()
        {
        }
        public void disconnect()
        {
            try
            {
                serverStream.Close();
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bC2]: close exception: {0}", e.StackTrace));

            }
        }
        public void connect()
        {

            try
            {
                tcpClient = null;
                tcpClient = new TcpClient();
                // Socket 
                tcpClient.Connect("buddycon.eu", 1337);

                serverStream = tcpClient.GetStream();
                String conString = "{\"apikey\":\"" + BuddyConSettings2.Instance.apikey + "\", \"name\":\"" + Styx.StyxWoW.Me.Name.ToString() + "\", \"server\":\"" + Styx.StyxWoW.Me.RealmName.ToString() + "\", \"android\":\"" + BuddyConSettings2.Instance.androidapi + "\"}";
                byte[] outStream = Encoding.ASCII.GetBytes(conString);
                Util.PostLog(string.Format("[bC]: socket connect with {0}",conString.Replace(Styx.StyxWoW.Me.Name.ToString(),"BOTNAME")));
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                // upload as javascript blob
                Task taskOpenEndpoint = Task.Factory.StartNew(() =>
                {
                    while (tcpClient.Connected)
                    {
                        // Read bytes
                        serverStream = tcpClient.GetStream();
                        byte[] message = new byte[4096];
                        int bytesRead;
                        bytesRead = 0;

                        try
                        {
                            // Read up to 4096 bytes
                            bytesRead = serverStream.Read(message, 0, 4096);
                        }
                        catch
                        {
                            /*a socket error has occured*/
                            Util.PostLog(string.Format("[bC]: socket error"));
                        }

                        //We have rad the message.
                        //ASCIIEncoding encoder = new ASCIIEncoding();
                        // Update main window
                        AddMessage(Encoding.UTF8.GetString(message, 0, bytesRead));
                        Thread.Sleep(500);
                    }
                });
            }
            catch (Exception e)
            {
                Util.PostLog(string.Format("[bC]: socket error {0}", e.Message));
                Util.PostLog(string.Format("[bC]: socket data {0}", e.Data));
                Util.PostLog(string.Format("[bC]: socket src {0}", e.Source));
                Util.PostLog(string.Format("[bC]: socket helplink {0}", e.HelpLink));
                Util.PostLog(string.Format("[bC]: socket stack {0}", e.StackTrace));

            }
        }

        private void AddMessage(string msg)
        {
            /*Dispatcher.BeginInvoke(DispatcherPriority.Input, (ThreadStart)(
             () =>
             {*/
            Util.PostLog(string.Format("[bC]: socket >> {0}", msg));
            if (msg == "") disconnect();
            else
            {
                Hashtable resJson = (Hashtable)JSON.JsonDecode(msg);

                switch ((string)resJson["action"])
                {
                    case "":
                        break;
                    case "killwow":
                        Util.ShowToLog(string.Format("[bC2]:  {0}", "Kill Wow requested!"));
                        Styx.StyxWoW.Memory.Process.Kill();
                        break;
                    case "switchprofile":
                        Util.ShowToLog(string.Format("[bC2]:  {0}: {1}", "Switch Profile requested!", (string)resJson["profile"]));
                        Styx.CommonBot.TreeRoot.Stop();
                        Styx.CommonBot.Profiles.ProfileManager.LoadNew((string)resJson["profile"]);
                        Thread.Sleep(500);
                        Styx.CommonBot.TreeRoot.Start();
                        break;
                    case "killhbrelog":
                        Util.ShowToLog(string.Format("[bC2]:  {0}", "Kill HBRelog&ARelog requested!"));
                        Process[] processes = Process.GetProcessesByName("HBRelog");
                        foreach (Process process in processes)
                        {
                            process.Kill();
                        } 
                        processes = Process.GetProcessesByName("ARelog");
                        foreach (Process process in processes)
                        {
                            process.Kill();
                        }                        
                        break;
                    case "getprofiles":
                        Util.ShowToLog(string.Format("[bC2]:  {0}", "Get Profile List requested!"));
						
						try{
							string path = BuddyConSettings2.Instance.profilepath;
							string searchPattern = "*.xml";

							DirectoryInfo di = new DirectoryInfo(path);

							FileInfo[] files =
								di.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

							string returnFiles = "{\"files\":[";
							bool first = true;
							foreach (FileInfo file in files)
							{
								if (!first) returnFiles += ",";
								if (first) first = false;
								returnFiles += "{\"name\":\"" + file.FullName + "\"}";
								 
							}
							returnFiles += "]}";
							string Result = System.Text.RegularExpressions.Regex.Replace(returnFiles, @"(\\)([\000\010\011\012\015\032\042\047\134\140])", "$2");
							Util.ShowToLog(string.Format("[bC]:  {0} :{1}", "Get Profile List done!", Result));
							string[] sendParam = new string[] { "files","apikey","name","server" };
							string[] sendVal = new string[] { Result, BuddyConSettings2.Instance.apikey, Styx.StyxWoW.Me.Name.ToString(), Styx.StyxWoW.Me.RealmName.ToString() };
							Util.HttpPost("http://buddycon.eu/index.php?r=api/profiles", sendParam, sendVal);
                        }catch(Exception ex){
							Util.ShowToLog(string.Format("[bC2]:  {0}", "Error or No Profile path set! Please set a path in the plugin settings."));

						}
                        break;
                    case "screen":
                        BuddyCon2.Instance.screenie();
                        break;
                    case "chat":
                        Util.ShowToLog(string.Format("[bC]:  {0} :{1}", "Get Chat  ", (string)resJson["message"]));
                        BuddyCon2.Instance.sendChat((string)resJson["type"], (string)resJson["message"], (string)resJson["to"]);
                        break;
                    case "bnet":
                        Util.ShowToLog(string.Format("[bC]:  {0} :{1}", "Get Chat  ", (string)resJson["message"]));
                        BuddyCon2.Instance.sendBNWhisper((string)resJson["to"], (string)resJson["message"]);
                        break;
                    case "macro":
                        BuddyCon2.Instance.sendMacro((string)resJson["message"]);
                        break;
                    case "stophb":
                        Styx.CommonBot.TreeRoot.Stop();
                        break;
                    case "starthb":
                        Styx.CommonBot.TreeRoot.Start();
                        break;
                    case "returnhash":
                        queue.Remove((string)resJson["hash"]);
                        break;
                    case "android":
                        //Styx.CommonBot.TreeRoot.Start();
                        Util.ShowToLog(string.Format("[bC2]: ANDROID GET {0}", msg));
                        BuddyConSettings2.Instance.androidapi = (string)resJson["key"];
                        BuddyConSettings2.Instance.Save();
                        break;
                    case "version":
                        //Styx.CommonBot.TreeRoot.Start();
                        Util.ShowToLog(string.Format("[bC2]: Version message: ", (string)resJson["message"]));
                        break;
                    default:
                        Util.PostLog(string.Format("[bC2]:  {0}", msg));
                        break;

                }
            }
            // }));
        }
    }
}