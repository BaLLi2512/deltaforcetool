using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{

	// Class that contains common behavior for all WOD LFRs
	public abstract class WoDLfr : Dungeon
	{
		protected static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		[EncounterHandler(0, "Root Handler")]
		public virtual Func<WoWUnit, Task<bool>> RootBehavior()
		{
			return async npc =>
			{
				if (await ScriptHelpers.CancelCinematicIfPlaying())
					return true;

				if (await UseAugmentedRunes())
					return true; 

				return false;
			};
		}

		#region Augment Runes

		private readonly List<Tuple<int, uint>> _augmentedRunes = new List<Tuple<int, uint>>
																  {
																	  new Tuple<int, uint>(175456, 118630), // Hyper Augment Rune
																	  new Tuple<int, uint>(175439, 118631), // Focus Augment Rune
																	  new Tuple<int, uint>(175457, 118632), // Focus Augment Rune
																  };

		private readonly WaitTimer UseAugmentedRuneTimer = WaitTimer.TenSeconds;

		private async Task<bool> UseAugmentedRunes()
		{
			if (!UseAugmentedRuneTimer.IsFinished)
				return false;

			UseAugmentedRuneTimer.Reset();

			if (Me.GetAllAuras().Any(a => _augmentedRunes.Any(t => t.Item1 == a.SpellId)))
				return false;

			var rune = Me.BagItems.FirstOrDefault(item => _augmentedRunes.Any(t => t.Item2 == item.Entry));

			if (rune == null)
				return false;

			Logger.WriteDebug("Using {0}", rune.SafeName);
			rune.UseContainerItem();
			return true;
		}
		#endregion
	}

	public class WalledCity : WoDLfr
	{
		#region Overrides of Dungeon
	
		public override uint DungeonId
		{
			get { return 849; }
		}

        public override WoWPoint Entrance
        {
            get { return new WoWPoint(1491.402, 3071.209, 110.1424); }
        }

        public override WoWPoint ExitLocation
        {
            get { return new WoWPoint(1491.654, 2952.643, 35.23913); }
        }

        public override void RemoveTargetsFilter(List<WoWObject> units)
        {
	        var inArenaStands = Me.HasAura("Monster's Brawl");

			var onKargarthEncounter = ScriptHelpers.IsViable(_kargath) && _kargath.Combat;
	        var tanks = ScriptHelpers.GroupMembers.Where(g => g.IsTank).ToList();

			units.RemoveAll(
				ret =>
				{
				    var unit = ret as WoWUnit;
				    if (unit == null)
				        return false;


					if (onKargarthEncounter)
					{
						if (_arenaStandsMobIds.Contains(unit.Entry) && !inArenaStands)
							return true;

						if (unit.Entry == MobId_KargathBladefist && inArenaStands)
							return true;
					}

					// Some noobs will sometimes solo pull trash. Just ignore any mob if no tank is around
					if (unit.Combat && !tanks.Any(t => t.Location.DistanceSqr(unit.Location) < 90*90))
						return true;

					return false;
				});
		}

		public override void RemoveHealTargetsFilter(List<WoWObject> objects)
		{
	        var inArenaStands = Me.HasAura("Monster's Brawl");

			var onKargarthEncounter = ScriptHelpers.IsViable(_kargath) && _kargath.Combat;

			objects.RemoveAll(
				obj =>
				{
					var unit = obj as WoWUnit;
					if (unit == null)
						return false;

					if (onKargarthEncounter)
					{
						var targetIsInStands = unit.HasAura("Monster's Brawl");
						if (inArenaStands != targetIsInStands)
							return true;
					}

					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	    {
            foreach (var obj in incomingunits)
            {
                var unit = obj as WoWUnit;
                if (unit != null)
                {

                }
            }
	    }

	    public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
	    {
		    var isMelee = Me.IsMelee();
		    var israngedDps = !isMelee && Me.IsDps();
            foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
                    switch (unit.Entry)
                    {
						case MobId_BladespireSorcerer:
						case MobId_DrunkenBileslinger:
						case MobId_IronWarmaster:
						case MobId_FungalFleshEater:
							priority.Score += 4500;
							break;
						case MobId_SporeShooter:
							if (israngedDps)
								priority.Score += 4000;
							break;
						case MobId_IronBomber:
						case MobId_MindFungus:
							priority.Score += 4000;
							break;
						case MobId_IronFlameTechnician:
		                    if (isMelee && unit.HasAura("Flamethrower"))
			                    priority.Score -= 4500;
							break;
                    }
				}
			}
		}


		public override void IncludeHealTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			// Some heal targets don't include NPCs so we need to add them directly. 
			// Healers need to heal mushrooms to full to gain special abilities that help with encounter.
			// Living Mushrooms are ignored since it's LFR... 

			var npcHealTargets = ObjectManager.GetObjectsOfType<WoWUnit>()
				.Where(u => u.Entry == MobId_RejuvenatingMushroom && !u.HasAura(SpellId_WhitheringRapidly));

			foreach (var healTarget in npcHealTargets)
				outgoingObjects.Add(healTarget);
		}

	    public override async Task<bool> HandleMovement(WoWPoint location)
	    {
			// Talk to NPC at entrance to get teleported to top.
			if (IsAtEntrance(Me.Location) && !IsAtEntrance(location))
			{
				var gharg = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_Gharg);
				if (gharg != null && gharg.CanGossip && await ScriptHelpers.TalkToNpc(gharg))
					return true;
			}

			// See if we need to take shortcut.
		    if (IsAtAreaByFirstBoss(Me.Location) && IsAtTheButcherArea(location))
		    {
				if (!Navigator.AtLocation(TheButcherShortcutStart))
					return (await CommonCoroutines.MoveTo(TheButcherShortcutStart, "The Butcher shortcut")).IsSuccessful();

				var timer = Stopwatch.StartNew();
				while (timer.ElapsedMilliseconds < 5000 && !IsAtTheButcherArea(Me.Location))
				{
					WoWMovement.ClickToMove(TheButcherShortcutEnd);
					WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
					await Coroutine.Sleep(120);
					WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
					if (await Coroutine.Wait(1000, () => IsAtTheButcherArea(Me.Location)))
						break;
				}
			    return true;
		    }

		    return false;
	    }

	    #endregion

		#region Root

		private const uint GameObjectId_ArenaElevator = 233098;

		private const uint MobId_Gharg = 84971;
		private static readonly WoWPoint EntranceAreaLoc = new WoWPoint(3482.574, 7597.803, 11.36744);

		private static bool IsAtEntrance(WoWPoint location)
		{
			return Math.Abs(location.Z - EntranceAreaLoc.Z) < 4 && location.Distance2DSqr(EntranceAreaLoc) < 50 * 50;
		}

		[LocationHandler(3482.574, 7597.803, 11.36744, 60, "Elevator Handler")]
		public async Task<bool> ElevatorHandler(WoWPoint location)
		{
			var transport = Me.Transport as WoWGameObject;
			// Do nothing while waiting on elevator.
			if (transport != null && transport.Entry == GameObjectId_ArenaElevator)
				return true;

			if (!IsAtEntrance(Me.Location))
				return false;

			var elevator = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_ArenaElevator);
			if (elevator == null || elevator.ZDiff > 4)
				return false;

			var randomPoint = WoWMathHelper.GetRandomPointInCircle(elevator.Location, 5);
			await ScriptHelpers.MoveToContinue(
					() => randomPoint,
					() => ScriptHelpers.IsViable(elevator) && elevator.ZDiff <= 4,
					name: "Elevator");

			return true;
		}

		#endregion

		#region Kargath Bladefist

	    #region Trash

	    private const uint MobId_Vulgor = 80048;
		private const uint MobId_BladespireSorcerer = 81224;
		private const uint MobId_SmolderingStoneguard = 80051;

	    private const int SpellId_FlameBolt = 162351;
	    private const int SpellId_EarthBreaker = 162271;
	    private const int MissileSpellId_MoltenBomb = 161631;
		private const uint AreaTriggerId_MoltenBomb = 6867;

		private static readonly Vector2[] AreaByFirstBoss =
	    {
			new Vector2(3649.214f, 7736.967f), new Vector2(3551.274f, 7466.804f),
			new Vector2(3266.306f, 7510.375f), new Vector2(3564.712f, 7821.45f),
		};

		[EncounterHandler((int)MobId_Vulgor, "Vul'gor")]
		public Func<WoWUnit, Task<bool>> VulgorEncounter()
		{
			WoWUnit vulgor = null;
			// Don't stand in front because of cleave.
			AddAvoidObject(7, o => o.Entry == MobId_Vulgor && o.ToUnit().IsAlive, o => o.Location.RayCast(o.Rotation, 6));

			const float earthBreakerLineWidth = 7;

			AddAvoidLocation( ctx => ScriptHelpers.IsViable(vulgor) && vulgor.CastingSpellId == SpellId_EarthBreaker,
				earthBreakerLineWidth * 1.33f,
				o => (WoWPoint)o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					vulgor.Location,
					vulgor.Location.RayCast(vulgor.Rotation, 40),
					earthBreakerLineWidth / 2).OfType<object>());

			return async npc =>
			{
				vulgor = npc;
				return false;
			};
		}

		[EncounterHandler((int)MobId_BladespireSorcerer, "Bladespire Sorcerer")]
		public Func<WoWUnit, Task<bool>> BladespireSorcererEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				4,
				m => ((WoWMissile)m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_MoltenBomb));

			AddAvoidObject(4, AreaTriggerId_MoltenBomb);

			return async npc => await ScriptHelpers.InterruptCast(npc, SpellId_FlameBolt);
		}

		private static bool IsAtAreaByFirstBoss(WoWPoint location)
		{
			return location.Z > 40 && WoWMathHelper.IsPointInPoly(location, AreaByFirstBoss);
		}

	    #endregion

		private readonly uint[] _arenaStandsMobIds = { MobId_IronFlameTechnician, MobId_IronWarmaster, MobId_IronBloodMage, MobId_DrunkenBileslinger, MobId_IronBomber };

	    private const int MissileSpellId_MaulingBrew = 159414;
		private const uint AreaTriggerId_MaulingBrew = 6706;

		private const uint MobId_KargathBladefist = 78714;
		private const uint MobId_IronBomber = 78926;
		private const uint MobId_DrunkenBileslinger = 78954;
		private const uint MobId_FirePillar = 78757;

		private const uint AreaTriggerId_FlameJet = 6701;
	    private WoWUnit _kargath;

		// http://www.wowhead.com/guide=2784/kargath-bladefist-highmaul-raid-strategy-guide
		[EncounterHandler((int)MobId_KargathBladefist, "Kargath Bladefist")]
		public Func<WoWUnit, Task<bool>> KargathBladefistEncounter()
		{
			AddAvoidObject(10, AreaTriggerId_FlameJet);

			AddAvoidObject(4, AreaTriggerId_MaulingBrew);
			AddAvoidLocation(
				ctx => true,
				4,
				m => ((WoWMissile)m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_MaulingBrew));

			// Run out of the boss's path while he's casting Bersuerker Rage.
			AddAvoidObject(8, o => o.Entry == MobId_KargathBladefist && o.ToUnit().HasAura("Berserker Rush"), 
				o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 24)));

			var berserkerRushKiteToLoc = new PerFrameCachedValue<WoWPoint>(
			() =>
			{
				if (!ScriptHelpers.IsViable(_kargath))
					return WoWPoint.Zero;

				var flameJet = ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
					.Where(a => a.Entry == AreaTriggerId_FlameJet)
					.OrderBy(o => o.DistanceSqr)
					.FirstOrDefault();

				if (flameJet == null)
					return WoWPoint.Zero;

				return WoWMathHelper.CalculatePointFrom(_kargath.Location, flameJet.Location, -12);
			});

			return async boss =>
			{
				_kargath = boss;
				if (Me.HasAura("Berserker Rush") && berserkerRushKiteToLoc.Value != WoWPoint.Zero)
				{
					return await ScriptHelpers.StayAtLocationWhile(
						() => Me.HasAura("Berserker Rush") && berserkerRushKiteToLoc.Value != WoWPoint.Zero,
						berserkerRushKiteToLoc.Value,
						"Berserker Rush Kite Location");
				}
				return false;
			};
		}

		#endregion

		#region The Butcher

	    #region Trash

	    private const uint MobId_Krush = 82532;
	    private const int MissileSpellId_BoarsRush = 166225;

		[EncounterHandler((int)MobId_Krush, "Krush")]
		public Func<WoWUnit, Task<bool>> KrushEncounter()
		{
			WoWUnit krush = null;

			const float boarsRushLineWidth = 7;

			var boarsRushMissile =
				new PerFrameCachedValue<WoWMissile>(
					() => WoWMissile.InFlightMissiles.FirstOrDefault(m => m.SpellId == MissileSpellId_BoarsRush));

			AddAvoidLocation(ctx => ScriptHelpers.IsViable(krush) && krush.HasAura("Boar's Rush") && boarsRushMissile.Value != null,
				boarsRushLineWidth * 1.33f,
				o => (WoWPoint)o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					krush.Location,
					boarsRushMissile.Value.ImpactPosition,
					boarsRushLineWidth / 2).OfType<object>());

			return async npc => false;
		}


	    #endregion

		private  const uint MobId_TheButcher = 77404;
		private static readonly WoWPoint TheButcherShortcutStart = new WoWPoint(3607.386, 7690.8, 49.68718);
		private static readonly WoWPoint TheButcherShortcutEnd = new WoWPoint(3625.253,7694.636,24.75327);

		private static readonly WoWPoint TheButcherAreaCenter = new WoWPoint(3739.223,7658.993,25.68915);

		// http://www.wowhead.com/guide=2783/the-butcher-highmaul-raid-strategy-guide
		[EncounterHandler((int)MobId_TheButcher, "The Butcher")]
		public Func<WoWUnit, Task<bool>> TheButcherEncounter()
		{
			// 10 stacks of Gushing Wounds causes instant death in LFR.
			AddAvoidObject(
				ctx => Me.IsRange() ||  Me.IsMeleeDps() && Me.HasAura("Gushing Wounds", aura => aura.StackCount >= 5),
				o => ((WoWUnit) o).MeleeRange() + 3,
				o => o.Entry == MobId_TheButcher && o.ToUnit().IsAlive);

			return async boss =>
			{
				return false;
			};
		}

	    private static bool IsAtTheButcherArea(WoWPoint location)
	    {
		    return location.Z < 30 && location.Distance2DSqr(TheButcherAreaCenter) < 200*200;
	    }

		#endregion

		#region Brackenspore

	    #region Trash

	    private const uint MobId_IronFlameTechnician = 86607;
		private const uint MobId_IronWarmaster = 86609;
		private const uint MobId_IronBloodMage = 87229;
	    private const int SpellId_BloodBolt = 174574;

		[EncounterHandler((int)MobId_IronFlameTechnician, "IronFlame Technician")]
		[EncounterHandler((int)MobId_IronWarmaster, "Iron Warmaster")]
		[EncounterHandler((int)MobId_IronBloodMage, "Iron BloodMage")]
		public Func<WoWUnit, Task<bool>> BrackensporeTrashEncounter()
		{
			AddAvoidObject(6, o => o.Entry == MobId_IronFlameTechnician && o.ToUnit().HasAura("Flamethrower"));
			return async npc =>
			{
				if (await ScriptHelpers.DispelGroup("Corrupted Blood", ScriptHelpers.PartyDispelType.Magic))
					return true;

				if (await ScriptHelpers.DispelEnemy("Iron Battle-Rage", ScriptHelpers.EnemyDispelType.Enrage, npc))
					return true;

				return await ScriptHelpers.InterruptCast(npc, SpellId_BloodBolt);
			};
		}


	    #endregion

	    private const int SpellId_Decay = 160013;
	    private const int SpellId_WhitheringRapidly = 163124;
	    private const int SpellId_InfestingSpores = 159996;

		private const uint MobId_SporeShooter = 79183;
		private const uint MobId_Brackenspore = 78491;
		private const uint MobId_FungalFleshEater = 79092;
		private const uint MobId_MindFungus = 79082;
		private const uint MobId_LivingMushroom = 78884;
		private const uint MobId_RejuvenatingMushroom = 78868;

		private const uint AreaTriggerId_SporeShot = 7882;
		private const uint AreaTriggerId_MindFungus = 6733;

		[EncounterHandler((int)MobId_FungalFleshEater, "Fungal FleshEater")]
		public Func<WoWUnit, Task<bool>> FungalFleshEaterEncounter()
		{
			return async boss =>
			{
				return await ScriptHelpers.InterruptCast(boss, SpellId_Decay);
			};
		}

		[EncounterHandler((int)MobId_Brackenspore, "Brackenspore", BossRange = 200)]
		public Func<WoWUnit, Task<bool>> BrackensporeEncounter()
		{
			AddAvoidObject(5, AreaTriggerId_SporeShot);
			// pool that reduces casting speed.
			AddAvoidObject(ctx => Me.IsRange(), 15, AreaTriggerId_MindFungus);

			WoWUnit rejuvinationgMushroom = null, brackenspore = null;

			var standAtRejuvenatingMushroom = new PerFrameCachedValue<bool>(
				() => ScriptHelpers.IsViable(rejuvinationgMushroom) && rejuvinationgMushroom.HasAura("Rejuvenating Spores")
					&& ScriptHelpers.IsViable(brackenspore) && brackenspore.Combat
					&& (Targeting.Instance.IsEmpty() || ScriptHelpers.CanReachUnitFromCircle(Targeting.Instance.FirstUnit, rejuvinationgMushroom.Location, 20)));

			var leftDoorEdge = new WoWPoint(4015.272, 7759.365, 4.900909);
			var rightDoorEdge = new WoWPoint(4017.969, 7740.247, 1.72341);
			var randomPointInsideArea = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(4026.877, 7748.938, 0.96492), 3);
			return async boss =>
			{
				brackenspore = boss;

				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointInsideArea))
					return true;

				if (!ScriptHelpers.IsViable(rejuvinationgMushroom))
				{
					rejuvinationgMushroom =
						ObjectManager.GetObjectsOfType<WoWUnit>()
							.Where(u => u.Entry == MobId_RejuvenatingMushroom && u.HasAura("Rejuvenating Spores"))
							.OrderBy(u => u.DistanceSqr)
							.FirstOrDefault();
				}

				if (standAtRejuvenatingMushroom)
				{
					return await ScriptHelpers.StayAtLocationWhile(
								() => standAtRejuvenatingMushroom,
								rejuvinationgMushroom.Location,
								"Rejuvenating Mushroom",
								20);
				}


				return false;
			};
		}

		#endregion
	}


}