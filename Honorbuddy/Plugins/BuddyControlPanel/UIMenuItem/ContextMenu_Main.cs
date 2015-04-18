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

using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using BuddyControlPanel.Resources.Localization;
using Honorbuddy;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;

// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable InconsistentNaming
#endregion


namespace BuddyControlPanel
{
    public class ContextMenu_Main : Assets.ThemedContextMenu, IBotChangeListener, IBotStateChangeListener, IProfileChangeListener
	{
		#region Creation & Destruction
		public ContextMenu_Main()
		{
			BorderBrush = Assets.BrushTransparent;

			Items.Add(new MenuItem_Bots());
			Items.Add(new MenuItem_BotSettings());
			_menuItem_BotActions = new MenuItem_BotActions();
			Items.Add(_menuItem_BotActions);

			Items.Add(new Assets.ThemedSeparator());	
			Items.Add(new MenuItem_LoadProfile());
			Items.Add(new MenuItem_ProfileSettings());
			
			Items.Add(new Assets.ThemedSeparator());	
			var combatRoutineName = ((RoutineManager.Current != null) && !string.IsNullOrEmpty(RoutineManager.Current.Name))
				? RoutineManager.Current.Name
				: BCPGlobalization.GeneralText_Unknown;
			Items.Add(new MenuItem_ButtonProxy(
				string.Format(BCPGlobalization.Item_CombatRoutineConfiguration_LabelFormat, combatRoutineName),
				string.Format(BCPGlobalization.Item_CombatRoutineConfiguration_ToolTipFormat, combatRoutineName),
				"btnClassConfig",
				false));

			Items.Add(new MenuItem_SettingsAndTools());
			Items.Add(new MenuItem_Plugins());

			Items.Add(new Assets.ThemedSeparator());
			Items.Add(new MenuItem_DeveloperTools());

			Items.Add(new Assets.ThemedSeparator());
			Items.Add(new MenuItem_Help());
			Items.Add(new MenuItem_Preferences());

			Items.Add(new Assets.ThemedSeparator());
			Items.Add(new MenuItem_Cancel(this));
			Items.Add(new MenuItem_Exit());
        }
		#endregion

	    private readonly MenuItem_BotActions _menuItem_BotActions;


	    public void NotifyBotChanged(BotBase newBot)
	    {
		    foreach (var menuItem in Items.OfType<MenuItemBase>())
			    menuItem.NotifyBotChanged(newBot);
	    }


		public void NotifyProfileChanged()
		{
			foreach (var menuItem in Items.OfType<MenuItemBase>())
				menuItem.NotifyProfileChanged();
		}


		public void NotifyStateChanged(CurrentBotStateNotification currentBotStateNotification)
	    {
			foreach (var menuItem in Items.OfType<MenuItemBase>())
				menuItem.NotifyStateChanged(currentBotStateNotification);		    
	    }

	    protected override void OnOpened(RoutedEventArgs evt)
	    {
		    _menuItem_BotActions.EvaluateBotActionsAvailable();
	    }
    }


	internal class MenuItem_Cancel : MenuItemBase
	{
		public MenuItem_Cancel(ContextMenu contextMenuToClose)
			: base(BCPGlobalization.Item_ContextMenuCancel_Label, BCPGlobalization.Item_ContextMenuCancel_ToolTip)
		{
			_contextMenuToClose = contextMenuToClose;
		}

		private readonly ContextMenu _contextMenuToClose;

		protected override void OnClick()
		{
			_contextMenuToClose.IsOpen = false; 
			base.OnClick();
		}
	}


	internal class MenuItem_DeveloperTools : MenuItemBase
	{
		public MenuItem_DeveloperTools()
			: base(BCPGlobalization.Item_DeveloperTools_Label, BCPGlobalization.Item_DeveloperTools_ToolTip)
		{
			// empty
		}

		protected override void OnClick()
		{
			var startLocation = Utility.GetStartLocation(this);

			var instance = DevToolsWindow.Instance;
			if (instance == null)
			{
				Utility.InvokeOnSpecificDispatcher(Application.Current.Dispatcher, () =>
				{
					// TODO: We'd really like the Overlay window to be 'owner', but DevToolsWindow is a singleton.
					// E.g., Owner = Window.GetWindow(this),
					// This will not work correctly in a C# environment where thread access is checked,
					// and the MainWindow, and the Overlay run in distinct threads.
					instance = new DevToolsWindow() { Owner = Application.Current.MainWindow, };
				});
			}

			Utility.InvokeOnSpecificDispatcher(instance.Dispatcher, () =>
			{
				if (!instance.IsLoaded)
				{
					// TODO: We'd really like the Overlay window to be 'owner', but DevToolsWindow is a singleton.
					// E.g., Owner = Window.GetWindow(this),
					// This will not work correctly in a C# environment where thread access is checked,
					// and the MainWindow, and the Overlay run in distinct threads.
					instance = new DevToolsWindow() { Owner = Application.Current.MainWindow, };
				}

				instance.ShowAtFront(startLocation);
			});

			base.OnClick();
		}
	}


	internal class MenuItem_Exit : MenuItemBase
	{
		public MenuItem_Exit()
			: base(BCPGlobalization.Item_Exit_Label, 
					string.Format(BCPGlobalization.Item_Exit_ToolTipFormat, BCPGlobalization.BuddyBotName))
		{
			// empty
		}

		protected override void OnClick()
		{
			var mousePosition = Utility.GetStartLocation(this);

			Utility.InvokeOnSpecificDispatcher(Application.Current.Dispatcher, () =>
			{
				var exitConfirmationDialog = new Dialog_General(SystemIcons.Question,
					BCPGlobalization.Title_ExitConfirmation,
					string.Format(BCPGlobalization.GeneralText_ExitConfirmationFormat, BCPGlobalization.BuddyBotName),
					MessageBoxButton.OKCancel)
				{
					WindowStartupLocation = WindowStartupLocation.Manual,
					Left = mousePosition.X,
					Top = mousePosition.Y
				};

				var messageBoxResult = exitConfirmationDialog.ShowDialog();
				if (messageBoxResult.HasValue && messageBoxResult.Value)
					Application.Current.Shutdown(0);
			});

			base.OnClick();
		}
	}


	internal class MenuItem_Preferences : MenuItemBase
	{
		public MenuItem_Preferences()
			: base(BCPGlobalization.Item_BuddyControlPanelPreferences_Label,
			string.Format(BCPGlobalization.Item_BuddyControlPanelPreferences_ToolTipFormat, BCPGlobalization.PluginName))
		{
			// empty
		}

		protected override void OnClick()
		{
			Dialog_Preferences.Instance.ShowAtFront(Utility.GetStartLocation(this));
			base.OnClick();
		}
	}
	
	
	internal class MenuItem_ProfileSettings : MenuItemBase
	{
		public MenuItem_ProfileSettings()
			: base(BCPGlobalization.Item_ProfileConfiguration_Label,
				   BCPGlobalization.Item_ProfileConfiguration_ToolTip)
		{
			// empty
		}

		protected override void OnClick()
		{
			Dialog_ProfileSettings.Instance.ReevaluateState();
			Dialog_ProfileSettings.Instance.ShowAtFront(Utility.GetStartLocation(this));
			base.OnClick();
		}

		public override void NotifyBotChanged(BotBase newBot)
		{
			Utility.InvokeOnSpecificDispatcher(Dispatcher, () => HandleStateChanges());
		}

		public override void NotifyProfileChanged()
		{
			Utility.InvokeOnSpecificDispatcher(Dispatcher, () => HandleStateChanges());
		}

		public override void NotifyStateChanged(CurrentBotStateNotification currentBotState)
		{
			Utility.InvokeOnSpecificDispatcher(Dispatcher, () =>
			{
				if (currentBotState.IsLoadProfileInProgress.HasValue)
				{
					IsEnabled = !currentBotState.IsLoadProfileInProgress.Value;
					ToolTip = IsEnabled ? DefaultToolTip : BCPGlobalization.GeneralText_DisabledWhileProfileIsLoading;
					if (!IsEnabled)
						return;
				}

				HandleStateChanges();
			});
		}

		private void HandleStateChanges()
		{
			IsEnabled = true;
			ToolTip = DefaultToolTip;
		}
	}


	internal class MenuItem_SettingsAndTools : MenuItemBase
	{
		public MenuItem_SettingsAndTools()
			: base(string.Format(BCPGlobalization.Item_BuddyBotConfiguration_LabelFormat, BCPGlobalization.BuddyBotName),
					string.Format(BCPGlobalization.Item_BuddyBotConfiguration_ToolTipFormat, BCPGlobalization.BuddyBotName))
		{
			// empty
		}

		protected override void OnClick()
		{
			(new SettingsWindow()).ShowAtFront(Utility.GetStartLocation(this));
			base.OnClick();
		}


		public override void NotifyStateChanged(CurrentBotStateNotification currentBotStateNotification)
		{
			HandleNotifyOfStateChange(currentBotStateNotification);
		}
	}
}
