using System;
using System.Linq;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.Plugins;
using Styx.WoWInternals.WoWObjects;

namespace OraliusWhisperingCrystal {
    public class OraliusWhisperingCrystalPlugin : HBPlugin {

        // ===========================================================
        // Constants
        // ===========================================================

        // ===========================================================
        // Fields
        // ===========================================================

        public static LocalPlayer Me = StyxWoW.Me;

        public static WoWItem OraliusWhisperingCrystalItem;

        // ===========================================================
        // Constructors
        // ===========================================================

        // ===========================================================
        // Getter & Setter
        // ===========================================================

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        public override string Name {
            get { return "Oralius' Whispering Crystal"; }
        }

        public override string Author {
            get { return "Morgrel"; }
        }

        public override Version Version {
            get { return new Version(1, 0); }
        }


        public override void Pulse() {
            if(!HasOraliusWhisperingCrystalItem()) {
                return;
            }

            if(!CanUseOraliusWhisperingCrystal()) {
                return;
            }

            if(HasOraliusWhisperingCrystalBuff()) {
                return;
            }

            if(IsOraliusWhisperingCrystalOnCooldown()) {
                return;
            }

            UseOraliusWhisperingCrystal();

            CustomNormalLog("Oralius' Whispering Crystal buff is now active.");
        }

        // ===========================================================
        // Methods
        // ===========================================================

        public void CustomNormalLog(string message, params object[] args) {
            Logging.Write(Colors.DeepSkyBlue, "[Oralius' Whispering Crystal]: " + message, args);
        }

        public static bool IsViable(WoWObject pWoWObject) {
            return (pWoWObject != null) && pWoWObject.IsValid;
        }

        public static bool HasOraliusWhisperingCrystalItem() {
            OraliusWhisperingCrystalItem = Me.BagItems.FirstOrDefault(item => item.Entry == 118922);

            return OraliusWhisperingCrystalItem != null;
        }

        public static bool CanUseOraliusWhisperingCrystal() {
            return IsViable(Me) && !Me.Mounted && !Me.IsDead && !Me.InVehicle && !Me.IsChanneling && !Me.IsFlying && !Me.IsCasting && !Me.OnTaxi;
        }

        public static bool HasOraliusWhisperingCrystalBuff() {
            return Me.HasAura(176151);
        }

        public static void UseOraliusWhisperingCrystal() {
            OraliusWhisperingCrystalItem.Use();
        }

        public static bool IsOraliusWhisperingCrystalOnCooldown() {
            return OraliusWhisperingCrystalItem.Cooldown > 0;
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================


    }
}