using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Styx.Common;

namespace Pokehbuddyplug
{
    internal static class Updater
    {
        private const string PbSvnUrl = "http://team-random.googlecode.com/svn/trunk/PokeBuddy/Pokehbuddy/"; // oude svn
        //private const string PbSvnUrl = "http://subversion.assembla.com/svn/team-random/trunk/dev-priv/plugins/Pokehbuddy/";  // private beta
        //private const string PbSvnUrl = "http://subversion.assembla.com/svn/team-random/trunk/release/plugins/Pokehbuddy/";  // public beta
        

        private static readonly Regex _linkPattern = new Regex(@"<li><a href="".+"">(?<ln>.+(?:..))</a></li>",
                                                               RegexOptions.CultureInvariant);
        public static void CheckForUpdate(string path)
        {
            CheckForUpdate(path, true);
        }

        public static void CheckForUpdate(string path, bool checkallow)
        {
            try
            {
                Logging.Write("Checking for new Pokehbuddy version - To disable the autoupdate, see settings");
                int remoteRev = GetRevision();
				//Logging.Write("remote version " + remoteRev);
                if (Pokehbuddy.MySettings.CurrentRevision != remoteRev)
                {
                    string logwrt = "";
                    if (Pokehbuddy.MySettings.AllowAutoUpdate){logwrt= "Downloading Update";} else {logwrt= "Please update!";};
                    Logging.Write("A new version was found. "+logwrt);
                    if (!Pokehbuddy.MySettings.AllowAutoUpdate && checkallow) return;

                    DownloadFilesFromSvn(new WebClient(), PbSvnUrl, path);
                    //Logging.Write("Download complete.");
                    Pokehbuddy.MySettings.CurrentRevision = remoteRev;
                    Pokehbuddy.MySettings.Save();

                    Logging.Write(Colors.Red, "A new version of Pokehbuddy was installed. Please restart Honorbuddy");
                    
                }
                else
                {
                    Logging.Write("No updates found");
                }
            }
            catch (Exception ex)
            {
                Logging.Write(ex.ToString());
            }
        }

        private static int GetRevision()
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            string webData = wc.DownloadString(PbSvnUrl + "version");


            return int.Parse(webData);
            throw new Exception("Unable to retreive revision");
        }



        private static void DownloadFilesFromSvn(WebClient client, string url, string path)
        {
            string html = client.DownloadString(url);
			//Logging.Write(url);
            MatchCollection results = _linkPattern.Matches(html);

            IEnumerable<Match> matches = from match in results.OfType<Match>()
                                         where match.Success && match.Groups["ln"].Success
                                         select match;
            foreach (Match match in matches)
            {
                string file = RemoveXmlEscapes(match.Groups["ln"].Value);
                string newUrl = url + file;
				//Logging.Write(url);
                if (newUrl[newUrl.Length - 1] == '/') // it's a directory...
                {
                    if (!newUrl.Contains("PetSettings") && !newUrl.Contains("CalcEngine")) DownloadFilesFromSvn(client, newUrl, path);
                }
                else // its a file.
                {
                    string filePath, dirPath;
                    if (url.Length > PbSvnUrl.Length)
                    {
                        string relativePath = url.Substring(PbSvnUrl.Length);
                        dirPath = Path.Combine(path, relativePath);
                        filePath = Path.Combine(dirPath, file);
                    }
                    else
                    {
                        dirPath = Environment.CurrentDirectory;
                        filePath = Path.Combine(path, file);
                    }
                    Logging.Write("Downloading {0}", file);
					try {
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                    client.DownloadFile(newUrl, filePath);
					//Logging.Write("Download {0} done", file);
					}
					catch (Exception ex)
            {
			}
					
                }
            }
        }

        private static string RemoveXmlEscapes(string xml)
        {
            return
                xml.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace(
                    "&apos;", "'");
        }
    }
}