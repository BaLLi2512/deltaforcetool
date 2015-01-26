using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Styx.Helpers;
using Styx.Common;
using Styx;
using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace BuddyCon2{
    public class BuddyConSettings2 : Settings
    {
        private static BuddyConSettings2 _instance;
        public static BuddyConSettings2 Instance { get { return _instance ?? (_instance = new BuddyConSettings2()); } }

        public BuddyConSettings2() : base(Path.Combine(Path.Combine(Styx.Helpers.GlobalSettings.SettingsDirectory, "Settings"), string.Format("BudyConSettings2_{0}.xml", StyxWoW.Me.Name)))
        {

        }

        #region Category: Global
        [Setting]
        [Category("Global")]
        [DisplayName("API Key")]
        public string gl_apikey
        {
            get { return BuddyConGlobalSettings.Instance.gl_apikey; }
            set { BuddyConGlobalSettings.Instance.gl_apikey = value; BuddyConGlobalSettings.Instance.Save(); }
        }
        [Setting]
        [Category("Global")]
        [DisplayName("Profile Path")]
        public string gl_profilepath
        {
            get { return BuddyConGlobalSettings.Instance.gl_profilepath; }
            set { BuddyConGlobalSettings.Instance.gl_profilepath = value; BuddyConGlobalSettings.Instance.Save(); }
        }
        #endregion

        #region Category: General

        [Setting]
        [Category("General")]
        [DisplayName("Profile Path")]
        public string profilepath
        {
            get
            {
                if (_profilepath != null && _profilepath.Length > 6) return _profilepath;
                return BuddyConGlobalSettings.Instance.gl_profilepath;
            }
            set { _profilepath = value; }
        }
        private string _profilepath;

        [Setting]
        [Category("General")]
        [DisplayName("API Key")]
        public string apikey
        {
            get
            {
                if (_apikey != null && _apikey.Length > 6) return _apikey;
                return BuddyConGlobalSettings.Instance.gl_apikey;
            }
            set { _apikey = value;  }
        }
        private string _apikey;


        [Setting]
        [Category("General")]
        [DisplayName("Upload Script")]
        [DefaultValue("")]
        [Description("Url to your upload script on your webhost. If not set the defualt Image hoster imgur will be used")]
        public string scripturl { get; set; }

        [Setting]
        [Category("General")]
        [DefaultValue("")]
        [DisplayName("Prowl Api Key")]
        public string prowlapi { get; set; }

        [Setting]
        [Category("General")]
        [DefaultValue("")]
        [DisplayName("Android Device Key")]
        public string androidapi { get; set; }


        [Setting]
        [Category("General")]
        [DefaultValue(true)]
        [DisplayName("Send Status Messages to Android")]
        [Description("If you are using the newest Android App this will push status messages to your app.")]
        public Boolean androidSendStatus { get; set; }

        [Setting]
        [Category("General")]
        [DefaultValue("")]
        [Description("Url to your own hosting script should be like http://www.buddyc.eu/index.php?r=api")]
        [DisplayName("Own Hosting URL")]
        public string ownurl { get; set; }

        #endregion  
        
        #region Category: Notifications

        [Setting]
        [Category("Screenshot On")]
        [DefaultValue(false)]
        [DisplayName("Chat")]
        public Boolean scChat { get; set; }

        [Setting]
        [Category("Screenshot On")]
        [DefaultValue(false)]
        [DisplayName("Died")]
        public Boolean scDied { get; set; }

        [Setting]
        [Category("Screenshot On")]
        [DefaultValue(false)]
        [DisplayName("Level up")]
        public Boolean scLevel { get; set; }

        [Setting]
        [Category("Notificate On Mobile")]
        [DefaultValue(false)]
        [DisplayName("Level up")]
        public Boolean notfLevel { get; set; }

        [Setting]
        [Category("Notificate On Mobile")]
        [DefaultValue(0)]
        [DisplayName("Status Prowl Minutes")]
        [Description("Send Status report to Prowl every X Minutes")]
        public int notfStatusProwl { get; set; }

        [Setting]
        [Category("Notificate On Mobile")]
        [DefaultValue(false)]
        [DisplayName("Died")]
        public Boolean notfDied { get; set; }

        [Setting]
        [Category("Notificate On Mobile(Chat)")]
        [DefaultValue(true)]
        [DisplayName("Say")]
        public Boolean notfSay { get; set; }

        [Setting]
        [Category("Notificate On Mobile(Chat)")]
        [DefaultValue(true)]
        [DisplayName("Whisper")]
        public Boolean notfWhisper { get; set; }

        [Setting]
        [Category("Notificate On Mobile(Chat)")]
        [DefaultValue(true)]
        [DisplayName("BG")]
        public Boolean notfBG { get; set; }

        [Setting]
        [Category("Notificate On Mobile(Chat)")]
        [DefaultValue(true)]
        [DisplayName("Guild")]
        public Boolean notfGuild { get; set; }

        [Setting]
        [Category("Notificate On Mobile(Chat)")]
        [DefaultValue(true)]
        [DisplayName("Raid")]
        public Boolean notfRaid { get; set; }

        [Setting]
        [Category("Notificate On Mobile")]
        [DefaultValue(true)]
        [DisplayName("Start")]
        public Boolean notfStart { get; set; }
        [Setting]
        [Category("Notificate On Mobile")]
        [DefaultValue(true)]
        [DisplayName("Stop")]
        public Boolean notfStop { get; set; }
        #endregion

        #region Tracking

        [Setting]
        [Category("Tracking")]
        [DefaultValue(0)]
        [Description("")]
        [DisplayName("Watch itemid")]
        public int watchitemid { get; set; }

        [Setting]
        [Category("Tracking")]
        [DefaultValue(0)]
        [Description("")]
        [DisplayName("Watch item amount")]
        public int watchitemamount { get; set; }

        [Setting]
        [Category("Tracking")]
        [DefaultValue(0)]
        [Description("Will send Reputation with this Faaction every 15 Minutes as a Status")]
        [DisplayName("Reputation FactionID")]
        public int watchreputationID { get; set; }

        #endregion
       
        [Setting]
        [Category("General")]
        [DefaultValue(false)]
        [DisplayName("Debug")]
        [Description("ONLY USE THIS WHEN ASKED BY DEV! ONLY FOR THE DEV!")]
        public Boolean debug { get; set; }
 /*
        [Setting]
        [Category("Debug")]
        [DefaultValue(false)]
        [DisplayName("Perfomance Debug")]
        [Description("If you get freezes or lags you can turn this on. It will show more messages if lags or freezes occour everytime X happens give this infomation to the author")]
        public Boolean pdebug { get; set; }
        */
    }

}
