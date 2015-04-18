// Originally contributed by Chinajade
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 4.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/4.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Usings
using System.Windows;

using BuddyControlPanel.Resources.Localization;
using Styx.CommonBot;

// ReSharper disable InconsistentNaming
#endregion


namespace BuddyControlPanel
{
    internal class MenuItem_BotSettings : MenuItemBase
    {
        public MenuItem_BotSettings()
			: base(BCPGlobalization.Item_BotConfiguration_Label,
					BCPGlobalization.Item_BotConfiguration_ToolTip)
        {
			// empty
        }


	    protected override void OnClick()
	    {
			if (BotManager.Current == null)
				return;

			var startLocation = Utility.GetStartLocation(this);

			Utility.BeginInvokeOnSpecificDispatcher(Dispatcher, () =>
			{
				var configForm = BotManager.Current.ConfigurationForm;
				var configWindow = BotManager.Current.ConfigurationWindow;

				if (configWindow != null)
					configWindow.ShowAtFront(startLocation);

				else if ((configForm != null) && !configForm.IsDisposed && !configForm.Disposing)
					configForm.ShowAtFront(startLocation);
			});

		    base.OnClick();
	    }


	    public override void NotifyBotChanged(BotBase newBot)
	    {
			Utility.BeginInvokeOnSpecificDispatcher(Dispatcher, () =>
			{
				if (newBot == null)
				{
					Header = BCPGlobalization.Item_BotConfiguration_Label;
					IsEnabled = false;
					ToolTip = BCPGlobalization.Item_BotConfigurationUnavailable_ToolTip;
					return;
				}

				var configForm = newBot.ConfigurationForm;
				var configWindow = newBot.ConfigurationWindow;

				var hasConfiguration = (configForm != null) && !configForm.IsDisposed && !configForm.Disposing;
				hasConfiguration |= (configWindow != null);

				Header = string.Format(BCPGlobalization.Item_BotConfigurationSpecific_LabelFormat, newBot.Name);
				IsEnabled = hasConfiguration;
				ToolTip = IsEnabled
					? string.Format(BCPGlobalization.Item_BotConfigurationSpecific_ToolTipFormat, newBot.Name)
					: string.Format(BCPGlobalization.Item_BotConfigurationSpecificUnavailable_ToolTipFormat, newBot.Name);
			});
	    }
    }
}