using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Plugins;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Coroutines;
using Buddy.Coroutines;
using Styx.TreeSharp;
using CommonBehaviors.Actions;
using Bots.Grind;


namespace ItsATrap
{
    class ItsATrap : HBPlugin
    {
		// change this on non-english client
		private const string TRAPPED_AURA_NAME = "Snap Shut";
	
        // standard plugin overrides
        public override string Name { get { return "It's a Trap - Barn Auto-Trapper"; } }
        public override string Author { get { return "Opply"; } }
        public override Version Version { get { return new Version(1, 0, 0, 2); } }
        public override string ButtonText { get { return "Settings"; } }
        public override bool WantButton { get { return true; } }
        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        private GUI gui = null;
        private bool hasTrap = false;
		

        public override void OnButtonPress()
        {
            gui.ShowDialog();
        }

        public ItsATrap()
        {
            gui = new GUI();
        }

        public override void OnEnable()
        {
            Log("Enabled()");
            base.OnEnable();

			BotEvents.OnBotStarted += OnBotStarted;
			
            gui.getMobHandler().SettingsFilePath = "Settings\\ItsATrap.txt";
            gui.getMobHandler().readFile();
        }

        public override void OnDisable()
        {
            Log("Disabled()");
            gui.getMobHandler().saveFile();

            base.OnDisable();
        }
		
		public void OnBotStarted(EventArgs args)
        {
			Log("Bot started");
			
			Log("Trapping targets:");
            foreach (KeyValuePair<string, int> pair in gui.getMobHandler().trappableMobs)
            {
                Log(String.Format("{0} = {1}", pair.Value, pair.Key));
            }
		}

		//get trap as WoWItem object from inventory
        public WoWItem GetTrap()
        {
            hasTrap = true;
            WoWItem deadlyTrap = Me.BagItems.FirstOrDefault(h => h.Entry == 115010);
            if(deadlyTrap != null)
            {
                return deadlyTrap;
            }

            WoWItem improvedTrap = Me.BagItems.FirstOrDefault(h => h.Entry == 115009);
            if (improvedTrap != null)
            {
                return improvedTrap;
            }

            WoWItem basicTrap = Me.BagItems.FirstOrDefault(h => h.Entry == 113991);
            if (basicTrap != null)
            {
                return basicTrap;
            }
            hasTrap = false;

            Log("Could not find a Trap in your inventory");
            return null;
        }
		
        public override void Pulse()
        {
			if( !Me.Combat )
			{
				enableCombat();
			}
		
            if (!Me.Combat || !Me.IsAlive || Me.IsGhost || Me.IsOnTransport || Me.OnTaxi || Me.Stunned || Me.Mounted )
                return;
				
            WoWUnit target = Me.CurrentTarget;
			if(target==null)
			{
				TryHeal();
				return;
			}
			
			//should we trap this target?
			List<int> targetList = gui.getMobHandler().getList();
			bool shalWeTrapCurrentTarget = targetList.Contains((int)target.Entry);
            if (shalWeTrapCurrentTarget && Me.Combat)
            {
				if (!target.IsDead && !target.IsPet)
				{
					preTrappingLogic();
					handleTrap();
					if (target.HealthPercent <= 49) 
					{
						disableCombat();
						kiteTarget();
					}
					else
					{
						//if target is over 49%, make sure we got combat enabled
						enableCombat();
					}
				}
            }
			else
			{
				enableCombat();
			}
        }
		
		//any logic to execute before trapping logic start
		private void preTrappingLogic()
		{
			//move hunters or warlocks close to the mob
			if( Me.Class == WoWClass.Hunter || Me.Class == WoWClass.Warlock )
			{
				WoWUnit target = Me.CurrentTarget;
				if( target.Distance > 15 )
				{
					WoWMovement.ClickToMove(target.Location, 10);
					Thread.Sleep(1800);
				}
			}
		}
		
		//any logic to execute after trapping logic is done
		private void postTrappingLogic()
		{
		}
		
		//logic to "kite" mob over trap
		private void kiteTarget()
		{
			WoWUnit target = Me.CurrentTarget;

			//is target trapped all ready?
			bool hasTargetBeenTrapped = target.HasAura(TRAPPED_AURA_NAME);
			if(!hasTargetBeenTrapped)
			{
				if(hunterKiteLogic())
				{
					petFollow();
					return;
				}
				petFollow();
				Log("Moving backwards to trap!");
				WoWMovement.Move(WoWMovement.MovementDirection.Backwards, new TimeSpan(30000));
				Thread.Sleep(3000);
				WoWMovement.MoveStop();
			}
			else
			{
				Log("Target successfully trapped!!!");
				Me.ClearTarget();
				postTrappingLogic();
			}
		}
		
		//for hunters, use distracting shot to taunt mob from pet
		private bool hunterKiteLogic()
		{
			//if not hunter, do nothing
			if( Me.Class != WoWClass.Hunter )
				return false;
			
			WoWUnit target = Me.CurrentTarget;
			int distractingShotSpellName = 20736;
			if (Styx.CommonBot.SpellManager.CanCast(distractingShotSpellName, target))
			{
				Log("Hunter logic: Casting Distracting Shot");
				Styx.CommonBot.SpellManager.Cast(distractingShotSpellName, target);
				Log("Hunter logic: /stopattack");
				Lua.DoString("StopAttack();");
				Log("Hunter logic: /petfollow");
				Lua.DoString("PetFollow();");
				return false;
			}
			else
			{
				return true;
			}
		}
		
		//do /petfollow for hunters and warlocks
		private void petFollow()
		{
			if(Me.Class == WoWClass.Warlock)
			{
				Log("Warlock logic: /petfollow");
				Lua.DoString("PetFollow();");
			}
		}
		
		//handles trap deployment
		private void handleTrap()
		{
			WoWUnit target = Me.CurrentTarget;
			WoWItem trapItem = GetTrap();
			bool hasTargetBeenTrapped = target.HasAura(TRAPPED_AURA_NAME);
			if (trapItem != null && (trapItem.Cooldown == 0) && (target.HealthPercent <= 85) && !hasTargetBeenTrapped)
			{
				target.Face();
				trapItem.UseContainerItem();
				Log(trapItem.Name + "thrown at " + target.Name + "!");
			}
		}
		
		//get healing spell based on class
		private int GetHealingSpellId()
		{
			switch(Me.Class)
			{
				case WoWClass.Priest: 
					return 2061;   //flash heal
				case WoWClass.Shaman: 
					return 8004;   //healing surge
				case WoWClass.Druid: 
					return 5185;   //healing touch
				case WoWClass.Monk: 
					return 116694; //surging mist
				case WoWClass.Paladin: 
					return 19750;  //flash of light
				default:
					return 0;
			}
		}

		//try to cast healing spell
        private void TryHeal()
        {
			if(Me.HealthPercent < 96)
			{
				int healingSpell = GetHealingSpellId();
				if(healingSpell != 0 && Styx.CommonBot.SpellManager.CanCast(healingSpell, Me))
				{
					Log("Casting heal on self");
					Styx.CommonBot.SpellManager.Cast(healingSpell, Me);
				}
			}
        }
		
		//enable combat
		private void enableCombat()
		{
			LevelBot.BehaviorFlags = BehaviorFlags.All;
		}
		
		//disable combat
		private void disableCombat()
		{
			LevelBot.BehaviorFlags = BehaviorFlags.Death;
		}

		//log to honorbuddy gui
        internal void Log(string text)
        {
            Logging.Write(String.Format("ItsATrap {0}: {1}", Version, text));
        }
    }
}

