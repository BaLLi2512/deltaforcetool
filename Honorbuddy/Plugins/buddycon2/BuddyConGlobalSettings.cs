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
    public class BuddyConGlobalSettings : Settings
    {
        private static BuddyConGlobalSettings _instance;
        public static BuddyConGlobalSettings Instance { get { return _instance ?? (_instance = new BuddyConGlobalSettings()); } }

        public BuddyConGlobalSettings()
            : base(Path.Combine(Path.Combine(Styx.Helpers.GlobalSettings.SettingsDirectory, "Settings"), "BudyConSettings2.xml"))
        {

        }


        [Setting]
        [Category("General")]
        [DisplayName("Global API Key")]
        public string gl_apikey { get; set; }

        [Setting]
        [Category("General")]
        [DisplayName("Global Profile Path")]
        public string gl_profilepath { get; set; }

        

    }

}
