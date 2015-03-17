using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Navigation;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Vector2 = Tripper.Tools.Math.Vector2;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	#region Normal Difficulty

	public class TheEverbloom : WoDDungeon
	{
		#region Overrides of Dungeon

	    public override WoWPoint Entrance
	    {
            get { return new WoWPoint(7111.703, 195.8656, 144.6757); }
	    }

	    public override WoWPoint ExitLocation
	    {
            get { return new WoWPoint(425.5091, 1319.822, 125.0202); }
	    }

	    public override uint DungeonId
		{
			get { return 824; }
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{

			units.RemoveAll(
				obj =>
				{
					var unit = obj as WoWUnit;
					if (unit == null)
						return false;

					if (unit.Entry == MobId_AqueousGlobule)
						return true;
					return false;
				});
		}

	    public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	    {
		    var isDps = Me.IsDps();
            foreach (var obj in incomingunits)
            {
                var unit = obj as WoWUnit;
                if (unit != null)
                {
					if (isDps && unit.Entry == MobId_Entanglement)
		                outgoingunits.Add(obj);

                }
            }
	    }

	    public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
	    {
		    var isDps = Me.IsDps();
		    var isRangedDps = isDps && Me.IsRange();

			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
					if (unit.Entry == MobId_LifeWardenGola || unit.Entry == MobId_Dulhu || unit.Entry == MobId_EarthshaperTelu)
					{
						if (unit.HasAura("Briarskin"))
						{
							priority.Score += -5000;
						}
						else
						{
							if (unit.Entry == MobId_LifeWardenGola)
								priority.Score += isDps ? 4500 : -4000;
							else if (unit.Entry == MobId_EarthshaperTelu)
								priority.Score += isDps ? 3500 : -4000;
							else if (isDps)
								priority.Score += 4000;
						}
						continue;
					}

					if (_sporeImageIds.Contains(unit.Entry))
					{
						priority.Score -= 4000;
						continue;
					}

					switch (unit.Entry)
					{
						case MobId_VenomCrazedPaleOne:
							if (isDps)
								priority.Score += unit.HasAura("Toxic Blood") ?  5500: 3500;
							break;
						case MobId_GorgedBusters:
							priority.Score += 5000;
							break;
						case MobId_ToxicSpiderling:
						case MobId_AqueousGlobule:
							if (isDps)
								priority.Score += 4500;
							break;
						case MobId_VenomSprayer:
							if (isDps)
								priority.Score += 4000;
							break;
					}

					if (isRangedDps && unit.Entry == MobId_Entanglement)
						priority.Score += 4500;

					// DPS should priorite the adds that spawn during Yalknu encounter
					if (isDps && _yalnuAdds.Contains(unit.Entry))
					{
						priority.Score += 4000;
						continue;
					}

				}
			}
		}

        public override void OnEnter()
        {
	        _pathToFirstBossBlackspot = new DynamicBlackspot(
		        () => _shouldBlackspotMainPathToFirstBoss,
			        () => _pathToFirstBossBlackspotLoc,
			        LfgDungeon.MapId,
			        50,
			        20,
			        "Main path to first boss");

			DynamicBlackspotManager.AddBlackspot(_pathToFirstBossBlackspot);
        }

		public override void OnExit()
		{
			DynamicBlackspotManager.RemoveBlackspot(_pathToFirstBossBlackspot);
			_pathToFirstBossBlackspot = null;
		}

		public override async Task<bool> HandleMovement(WoWPoint location)
		{
			var myLoc = Me.Location;

			if (ShouldHandleShortcut1ToFirstBoss(myLoc, location))
				return await TakeShortcut1ToFirstBoss();

			if (ShouldHandleShortcutToSecondBoss(myLoc, location))
				return await TakeShortcutToSecondBoss();

			// move through portal to final boss.
			if (AtYalnuEncounter(location) && !AtYalnuEncounter(Me.Location))
				return (await CommonCoroutines.MoveTo(_portalToValnuLoc)).IsSuccessful();
			return false;
		}

		public override void IncludeLootTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			var doSubversiveInfestationQuest = ScriptHelpers.SupportsQuesting && ScriptHelpers.HasQuest(QuestId_SubversiveInfestation)
				&& !ScriptHelpers.IsQuestInLogComplete(QuestId_SubversiveInfestation);

			foreach (var incomingObject in incomingObjects)
			{
				var unit = incomingObject as WoWUnit;
				if (unit != null)
				{
					// ensure mobs required for Subversive Infestation get looted regardless of loot settings.
					if (doSubversiveInfestationQuest && _subversiveInfestationMobIds.Contains(unit.Entry))
						outgoingObjects.Add(unit);
				}
			}
		}

		#endregion
		
		private static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root


		#endregion

		#region Garrison Inn Quests

		private const int QuestId_SubversiveInfestation = 36813;

		private HashSet<uint> _subversiveInfestationMobIds = new HashSet<uint>
												   {
													   MobId_Dreadpetal,
													   MobId_Gnarlroot,
													   MobId_Witherbark,
													   MobId_VerdantMandragora,
													   MobId_Xeritac
												   };

		// Titanic Evolution
		[ObjectHandler(237473, "Overgrown Artifact", ObjectRange = 100)]
		public async Task<bool> OvergrownArtifactHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 120);
		}

		// For the Birds
		[ObjectHandler(237483, "Rustling Peachick Nest", ObjectRange = 45)]
		public async Task<bool> RustlingPeachickNestHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 55);
		}

		// Subversive Infestation
		[ObjectHandler(236462, "Phylarch's Research", ObjectRange = 100)]
		public async Task<bool> PhylarchsResearchHandler(WoWGameObject gObj)
		{
			return !ScriptHelpers.IsBossAlive("Archimage Sol") && await SafeInteractWithGameObject(gObj, 120);
		}

		// Cenarion Concerns
		[ObjectHandler(237472, "Strangely-Glowing Frond", ObjectRange = 60)]
		public async Task<bool> StrangelyGlowingFrondHandler(WoWGameObject gObj)
		{
			return await SafeInteractWithGameObject(gObj, 70);
		}

		#endregion
		// guide http://www.wowhead.com/guide=2665/everbloom-dungeon-strategy-guide#witherbark
		#region Witherbark

		#region Trash

		private const uint MobId_Dreadpetal = 81864;
		private const uint MobId_VerdantMandragora = 81983;
		private const uint MobId_Gnarlroot = 81984;

		private const uint AreaTriggerId_LivingLeaves = 7607;

		[EncounterHandler((int) MobId_Dreadpetal, "Dreadpetal")]
		public Func<WoWUnit, Task<bool>> DreadpetalEncounter()
		{
			return async boss => Me.PartyMembers.Any(p => p.HasAura("Dread", aura => aura.StackCount >= 3))
					&& await ScriptHelpers.DispelGroup("Dreadpetal Toxin", ScriptHelpers.PartyDispelType.Poison);
		}

		[EncounterHandler((int)MobId_Gnarlroot, "Gnarlroot")]
		public Func<WoWUnit, Task<bool>> GnarlrootEncounter()
		{
			AddAvoidObject(4, AreaTriggerId_LivingLeaves);
			return async boss => false;
		}

		#endregion

		#region Shortcut

		private readonly Vector2[] _boss1Area =
		{
			new Vector2(490.8828f, 1453.919f),
			new Vector2(500.1278f, 1445.825f),
			new Vector2(517.169f, 1445.405f),
			new Vector2(566.2526f, 1563.096f),
			new Vector2(465.5888f, 1633.071f),
			new Vector2(392.0648f, 1644.537f),
			new Vector2(371.7909f, 1597.915f),
		};

		readonly WoWPoint _trashPackAtShortcutToFirstBoss = new WoWPoint(458.3217, 1463.744, 104.2181);
		readonly WoWPoint _topOfShortcut1ToFirstBoss= new WoWPoint(516.7039, 1448.762, 109.6434);
		readonly WoWPoint _bottomOfShortcut1ToFirstBoss = new WoWPoint(510.2285, 1438.621, 103.0686);

		readonly WoWPoint _shortcut1ToFirstBossMidpointMoveTo = new WoWPoint(528.7039, 1448.762, 109.6434);
		readonly WoWPoint _shortcut1ToFirstBossTopMoveTo = new WoWPoint(516.0667, 1448.829, 109.7493);
		readonly WoWPoint _shortcut1ToFristBossPassengerMountLoc = new WoWPoint(516.0903f, 1448.145f, 109.5488f);
		readonly WoWPoint _pathToFirstBossBlackspotLoc = new WoWPoint(384.6688, 1498.319, 94.07461);

		private readonly TimeCachedValue<bool> _shouldBlackspotMainPathToFirstBoss = new TimeCachedValue<bool>(
			TimeSpan.FromSeconds(2),
			() => ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => u.Entry == MobId_VerdantMandragora && u.IsAlive));

		private DynamicBlackspot _pathToFirstBossBlackspot;

		private bool ShouldHandleShortcut1ToFirstBoss(WoWPoint myLoc, WoWPoint destination)
		{
			if (!LootTargeting.Instance.IsEmpty() || BotPoi.Current.Type != PoiType.None)
				return false;

			if (Me.IsActuallyInCombat)
				return false;

			if (WoWMathHelper.IsPointInPoly(myLoc, _boss1Area))
				return false;

			var tank = ScriptHelpers.GroupMembers.FirstOrDefault(g => g.IsTank);

			if (tank == null)
				return false;

			// try take shortuct if tank is a fool and attempting it
			if (tank.Guid != Me.Guid && tank.Location.Distance2DSqr(_topOfShortcut1ToFirstBoss) < 15 * 15)
				return true;

			if (!WoWMathHelper.IsPointInPoly(destination, _boss1Area))
				return false;


			if (Me.IsTank())
				return false;

			// if this trash pack is pulled then we use the other easier shortcut.
			if (ScriptHelpers.UnfriendlyNpcsArePulledOrGone(_trashPackAtShortcutToFirstBoss, 10))
				return false;

			return _shouldBlackspotMainPathToFirstBoss;
		}

		private readonly int[] _passengerMountSpellId = {
												   121820, // Obsidian Nightwing
													75973, // X-53 Touring Rocket
												};

		[LocationHandler(492.4296, 1422.413, 103.2917, 40)]
		public async Task<bool> HandleShortcut1ToFirstBoss(WoWPoint loc)
		{
			if (!ScriptHelpers.IsBossAlive("Witherbark") || Me.IsTank())
				return false;

			if (!LootTargeting.Instance.IsEmpty() || BotPoi.Current.Type != PoiType.None)
				return false;

			if (Me.IsActuallyInCombat)
				return false;

			if (WoWMathHelper.IsPointInPoly(Me.Location, _boss1Area))
			{
				var partyMembersNotAtTop = Me.PartyMembers
					.FirstOrDefault(p => !p.IsMe && p.DistanceSqr < 40 * 40 && !WoWMathHelper.IsPointInPoly(p.Location, _boss1Area));

				if (partyMembersNotAtTop == null)
					return false;

				if (SpellManager.CanCast("Leap of Faith"))
				{
					partyMembersNotAtTop.Target();
					await Coroutine.Wait(2000, () => Me.CurrentTarget == partyMembersNotAtTop);
					SpellManager.Cast("Leap of Faith");
					return true;
				}

				var currentMount = Mount.Current;
				if (currentMount == null || !_passengerMountSpellId.Contains(currentMount.CreatureSpellId))
				{
					var passengerMount = Mount.Mounts.FirstOrDefault(m => _passengerMountSpellId.Contains(m.CreatureSpellId));
					if (passengerMount != null)
					{
						if (Me.Mounted)
							await CommonCoroutines.Dismount();
						Mount.SummonMount(passengerMount.CreatureSpellId);
						await Coroutine.Wait(2000, () => Me.Mounted);
						return true;
					}
				}
				else if (_passengerMountSpellId.Contains(currentMount.CreatureSpellId))
				{
					await CommonCoroutines.MoveTo(_shortcut1ToFristBossPassengerMountLoc);
					return true;
				}

				return false;
			}

			var tank = ScriptHelpers.GroupMembers.FirstOrDefault(g => g.IsTank);

			if (tank == null)
				return false;

			// try take shortcut if tank is a fool and attempting it
			if (tank.Location.Distance2DSqr(_topOfShortcut1ToFirstBoss) > 10 * 10)
				return false;

			return await TakeShortcut1ToFirstBoss();
		}

		private async Task<bool> TakeShortcut1ToFirstBoss()
		{
			if (Me.Location.DistanceSqr(_bottomOfShortcut1ToFirstBoss) > 4*4)
				return (await CommonCoroutines.MoveTo(_bottomOfShortcut1ToFirstBoss)).IsSuccessful();

			var maxTryTimer = Stopwatch.StartNew();

			while (true)
			{
				var myLoc = Me.Location;

				var partyMemberWithPassengerMount =
					Me.PartyMembers.FirstOrDefault(
						p => p.CanInteractNow && p.WithinInteractRange && WoWMathHelper.IsPointInPoly(p.Location, _boss1Area));

				if (partyMemberWithPassengerMount != null)
				{
					partyMemberWithPassengerMount.Interact();
					await Coroutine.Sleep(3000);
					Lua.DoString("VehicleExit()");
				}

				if (WoWMathHelper.IsPointInPoly(Me.Location, _boss1Area))
					break;

				await CommonCoroutines.SummonGroundMount(WoWPoint.Zero);
				var moveTo = myLoc.Z < 105.4 ? _shortcut1ToFirstBossMidpointMoveTo : _shortcut1ToFirstBossTopMoveTo;
				Navigator.PlayerMover.MoveTowards(moveTo);
				try
				{
					WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
					await Coroutine.Sleep(120);
				}
				finally
				{
					WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
				}

				var tank = ScriptHelpers.GroupMembers.FirstOrDefault(g => g.IsTank);
				if (tank == null || !ShouldHandleShortcut1ToFirstBoss(myLoc, tank.Location))
					return false;

				if (maxTryTimer.ElapsedMilliseconds > 600000)
				{
					Logger.Write("Failed to take shortcut, leaving group.");
					Lua.DoString("LeaveParty()");
					return false;
				}
				await Coroutine.Yield();
			}

			return true;
		}

		#endregion

		private const int SpellId_ParchedGasp = 164357;

		private const uint MobId_Witherbark = 81522;
		private const uint MobId_AqueousGlobule = 81638;
		private const uint AreaTriggerId_NoxiousVines = 7200;

		[EncounterHandler((int)MobId_Witherbark, "Witherbark")]
		public Func<WoWUnit, Task<bool>> WitherbarkEncounter()
		{
			var tankLoc = new WoWPoint(417.7986, 1615.377, 89.29382);

			var uncheckedGrowthDropStart = new WoWPoint(398.6838, 1593.925, 88.45592);
			var uncheckedGrowthDropEnd = new WoWPoint(437.8936, 1604.33, 88.48502);

			// Left by Unchecked Growth
			AddAvoidObject(5, AreaTriggerId_NoxiousVines);

			// Deadly frontal 60deg cone AOE attack
			AddAvoidObject(
				ctx => !Me.IsTank(),
				12,
				o => o.Entry == MobId_Witherbark && o.ToUnit().CastingSpellId == SpellId_ParchedGasp,
				o => o.Location.RayCast(o.Rotation, 10));


			return async boss =>
			{
				if (Me.HasAura("Noxious Vines"))
				{
					var stayAtLoc = Me.Location.GetNearestPointOnSegment(uncheckedGrowthDropStart, uncheckedGrowthDropEnd);
					return await ScriptHelpers.StayAtLocationWhile(() => Me.HasAura("Noxious Vines"), stayAtLoc, "Unchecked Growth dropoff");
				}

				if (await ScriptHelpers.TankUnitAtLocation(tankLoc, 15))
					return true;

				return false;
			};
		}

		#endregion

		// guide http://www.wowhead.com/guide=2665/everbloom-dungeon-strategy-guide#ancient-protectors
		#region Ancient Protectors

		#region Trash

		private const int SpellId_TripleAttack = 169418;
		private const int SpellId_NoxiousEruption_Trash = 169445;

		private const uint MobId_TwistedAbomination = 84767;
		private const uint MobId_EverbloomNaturalist = 81819;

		[EncounterHandler((int)MobId_TwistedAbomination, "Twisted Abomination")]
		public Func<WoWUnit, Task<bool>> TwistedAbominationEncounter()
		{
			AddAvoidObject(
				ctx => !Me.IsTank(),
				7,
				o => o.Entry == MobId_TwistedAbomination && o.ToUnit().CastingSpellId == SpellId_TripleAttack,
				o => o.Location.RayCast(o.Rotation, 6));

			AddAvoidObject(8, o => o.Entry == MobId_TwistedAbomination && o.ToUnit().CastingSpellId == SpellId_NoxiousEruption_Trash);

			return async boss => false;
		}

		#endregion

		#region Shortcut

		private readonly WoWPoint _packAtShortcutToSecondBossLoc = new WoWPoint(644.4979, 1352.761, 80.03185);
		private readonly WoWPoint _packInHutAtShortcutToSecondBossLoc = new WoWPoint(625.0367, 1399.632, 89.00101);
		readonly WoWPoint _bottomOfShortcutToSecondBossLoc = new WoWPoint(637.825, 1420.862, 86.09888);

		private readonly Vector2[] _boss2To4Area =
		{
			new Vector2(553.9598f, 1443.522f),
			new Vector2(674.1066f, 1437.386f),
			new Vector2(714.3946f, 1383.961f),
			new Vector2(785.665f, 1354.546f),
			new Vector2(1014.405f, 1399.983f),
			new Vector2(943.2585f, 1663.478f),
			new Vector2(537.8443f, 1779.033f),
		};

		private readonly Vector2[] _shortcut2ToBoss2Area = 
		{
			new Vector2(549.2135f, 1537.049f),
			new Vector2(554.2559f, 1532.708f),
			new Vector2(563.2415f, 1514.594f),
			new Vector2(555.1516f, 1490.635f),
			new Vector2(529.9198f, 1481.171f),
			new Vector2(524.8267f, 1487.626f),
			new Vector2(529.188f, 1508.215f),
			new Vector2(545.1208f, 1523.572f),
		};

		private readonly static WoWPoint ShortcutToSecondBossMoveTo1 = new WoWPoint(641.5132f, 1430.823f, 95.36393f);
		private readonly static WoWPoint ShortcutToSecondBossMoveTo2 = new WoWPoint(650.1648f, 1429.35f, 103.0789f);
		private readonly static WoWPoint ShortcutToSecondBossMoveTo3 = new WoWPoint(655.723f, 1434.124f, 109.476f);
		private readonly static WoWPoint ShortcutToSecondBossMoveTo4 = new WoWPoint(655.353, 1444.739, 117.0462);

		private static readonly WoWPoint[] ShortcutToSecondBossPath = 
		{
			ShortcutToSecondBossMoveTo1,ShortcutToSecondBossMoveTo2,ShortcutToSecondBossMoveTo3,
		};

		private bool ShouldHandleShortcutToSecondBoss(WoWPoint myLoc, WoWPoint destination)
		{
			if (!LootTargeting.Instance.IsEmpty() || BotPoi.Current.Type != PoiType.None)
				return false;

			if (Me.IsActuallyInCombat)
				return false;

			if (WoWMathHelper.IsPointInPoly(myLoc, _boss2To4Area) || AtYalnuEncounter(myLoc))
				return false;

			var tank = ScriptHelpers.GroupMembers.FirstOrDefault(g => g.IsTank);

			if (tank == null)
				return false;

			if (tank.Guid != Me.Guid && (IsAlongShortcutToSecondBoss(tank.Location) || IsAlongShortcutToSecondBoss(Me.Location)))
				return true;

			if (!WoWMathHelper.IsPointInPoly(destination, _boss2To4Area) && !AtYalnuEncounter(destination))
				return false;

			if (Me.IsTank() && !ScriptHelpers.IsBossAlive("Witherbark"))
				return true;

			if (ScriptHelpers.UnfriendlyNpcsArePulledOrGone(_packAtShortcutToSecondBossLoc, 10)
				&& !ScriptHelpers.UnfriendlyNpcsArePulledOrGone(_packInHutAtShortcutToSecondBossLoc, 10))
			{
				return false;
			}

			return true;
		}

		// handles the shortcut that goes through a hut and up a slope near Archmage Sol
		[LocationHandler(637.825, 1420.862, 86.09888, 40)]
		public async Task<bool> HandleShortcutToSecondBoss(WoWPoint loc)
		{
			if ( Me.IsTank())
				return false;

			if (!LootTargeting.Instance.IsEmpty() || BotPoi.Current.Type != PoiType.None)
				return false;

			if (Me.IsActuallyInCombat)
				return false;

			if (WoWMathHelper.IsPointInPoly(Me.Location, _boss1Area))
				return false;

			var tank = ScriptHelpers.GroupMembers.FirstOrDefault(g => g.IsTank);
			if (tank == null)
				return false;

			if (!IsAlongShortcutToSecondBoss(tank.Location) && !IsAlongShortcutToSecondBoss(Me.Location))
				return false;

			return await TakeShortcutToSecondBoss();
		}

		private bool IsAlongShortcutToSecondBoss(WoWPoint loc)
		{
			for (int i = 0, j = ShortcutToSecondBossPath.Length - 1; i < ShortcutToSecondBossPath.Length; j = i++)
			{
				var start = ShortcutToSecondBossPath[j];
				var end = ShortcutToSecondBossPath[i];
				if (loc.GetNearestPointOnSegment(start, end).DistanceSqr(loc) < 6*6)
					return true;
			}
			return false;
		}


		private async Task<bool> TakeShortcutToSecondBoss()
		{
			if (Me.Location.DistanceSqr(_bottomOfShortcutToSecondBossLoc) > 4 * 4 && !IsAlongShortcutToSecondBoss(Me.Location))
				return (await CommonCoroutines.MoveTo(_bottomOfShortcutToSecondBossLoc)).IsSuccessful();

			var maxTryTimer = Stopwatch.StartNew();

			while (true)
			{
				var myLoc = Me.Location;

				if (myLoc.Z < 88 && myLoc.DistanceSqr(_bottomOfShortcutToSecondBossLoc) > 4*4)
				{
					await CommonCoroutines.MoveTo(_bottomOfShortcutToSecondBossLoc);
				}
				else if (myLoc.Z < 94f)
				{
					Navigator.PlayerMover.MoveTowards(ShortcutToSecondBossMoveTo1);
					if (WoWMathHelper.IsFacing(myLoc, Me.Rotation, ShortcutToSecondBossMoveTo1, WoWMathHelper.DegreesToRadians(30)))
					{
						try
						{
							WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
						}
						finally
						{
							WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
						}
					}
				}
				else if (myLoc.Z < 102.5)
				{
					Navigator.PlayerMover.MoveTowards(ShortcutToSecondBossMoveTo2);
				}
				else if (myLoc.Z < 109)
				{
					Navigator.PlayerMover.MoveTowards(ShortcutToSecondBossMoveTo3);
				}
				else
				{
					Navigator.PlayerMover.MoveTowards(ShortcutToSecondBossMoveTo4); 
				}

				if (WoWMathHelper.IsPointInPoly(Me.Location, _boss2To4Area))
					break;


				if (maxTryTimer.ElapsedMilliseconds > 300000)
				{
					Logger.Write("Failed to take shortcut, leaving group.");
					Lua.DoString("LeaveParty()");
					return false;
				}
				await Coroutine.Yield();
			}

			return true;
		}


		// handles the shortcut that starts by jumping up a bank with tree root sticking out, well it handles it by leaving group
		[LocationHandler(521.1617, 1518.794, 97.18973, 60)]
		public Func<WoWPoint, Task<bool>> HandleShortcut2ToSecondBoss()
		{
			var showedAlert = false;

			return async loc =>
			{
				if (showedAlert || Me.IsTank())
					return false;

				if (!LootTargeting.Instance.IsEmpty() || BotPoi.Current.Type != PoiType.None)
					return false;

				if (Me.IsActuallyInCombat)
					return false;

				if (!Me.PartyMembers.Any(p => WoWMathHelper.IsPointInPoly(p.Location, _shortcut2ToBoss2Area)))
					return false;

				var showTimeSec = StyxWoW.Random.Next(20, 30);

				Alert.Show(
					"Dungeonbuddy: Unsupported shortcut",
					"Party members are attempting to take a shortcut that the dungeon script does not support. If you wish to stay in group and manually take the shortcut then press 'Cancel'. Otherwise Dungeonbuddy will automatically leave group.",
					showTimeSec,
					true,
					true,
					() => Lua.DoString("LeaveParty()"),
					null,
					"Leave",
					"Cancel");
				showedAlert = true;
				return true;
			};
		}

		#endregion


		private const int SpellId_NoxiousEruption = 175997;
		private const int SpellId_Slash = 168383;
		private const int SpellId_RevitalizingWaters = 168082;
		private const int SpellId_RapidTides = 168105;
		private const int SpellId_BramblePatch = 167966;
		private const int SpellId_Briarskin = 168041;

		private const uint MobId_Dulhu = 83894;
		private const uint MobId_LifeWardenGola = 83892;
		private const uint MobId_EarthshaperTelu = 83893;

		private const uint AreaTriggerId_BramblePatch = 7499;

		private WoWUnit _gola, _telu;

		[EncounterHandler((int) MobId_Dulhu, "Dulhu")]
		public Func<WoWUnit, Task<bool>> DulhuEncounter()
		{
			// Noxious Eruption - 10yd PBAOE centered on boss
			AddAvoidObject(
				ctx => true, 
				10, 
				o => o.Entry == MobId_Dulhu && o.ToUnit().CastingSpellId == SpellId_NoxiousEruption);

			// Slash - Ability does damage to players in front of boss within 8 yds
			AddAvoidObject(
				ctx => true,
				9,
				o => o.Entry == MobId_Dulhu && o.ToUnit().CastingSpellId == SpellId_Slash,
				o => o.Location.RayCast(o.Rotation, 8));

			var golaIsALive = new PerFrameCachedValue<bool>(() => ScriptHelpers.IsViable(_gola) && _gola.IsAlive);
			var teluIsAlive = new PerFrameCachedValue<bool>(
					() => ScriptHelpers.IsViable(_telu) && _telu.IsAlive && (!ScriptHelpers.IsViable(_gola) || !_gola.IsAlive));

			var disableCapabilityHandler = ScriptHelpers.CombatRoutineCapabilityManager.CreateNewHandle();

			return async boss =>
			{
				ScriptHelpers.CombatRoutineCapabilityManager.Update(
					disableCapabilityHandler,
					CapabilityFlags.GapCloser,
					() => ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_NoxiousEruption,
					"Dulhu is casting Noxious Eruption");

				// cast hero at start if available. 
				if (boss.HealthPercent <= 97 && teluIsAlive && await ScriptHelpers.CastHeroism())
					return true;

				// Casted by Telu, reduces damage taken by 75% and inflicts damage to attacker
				if (await ScriptHelpers.DispelEnemy("Briarskin", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				// Casted by Gola, removes all cooldowns from abilites
				if (await ScriptHelpers.DispelEnemy("Rapid Tides", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				// tank the Gola by the current kill target.
				if (Me.IsTank())
				{
					if (golaIsALive)
						return await ScriptHelpers.StayAtLocationWhile(() => golaIsALive, _gola.Location, "Dulhu");
					if (teluIsAlive)
						return await ScriptHelpers.StayAtLocationWhile(() => teluIsAlive, _telu.Location, "Telu");
				}
				return false;
			};
		}


		[EncounterHandler((int) MobId_LifeWardenGola, "Life Warden Gola")]
		public Func<WoWUnit, Task<bool>> LifeWardenGolaEncounter()
		{
			return async boss =>
			{
				_gola = boss;

				if (await ScriptHelpers.InterruptCast(boss, SpellId_RevitalizingWaters, SpellId_RapidTides))
					return true;

				// Casted by Telu, reduces damage taken by 75% and inflicts damage to attacker
				if (await ScriptHelpers.DispelEnemy("Briarskin", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				// Casted by Gola, removes all cooldowns from abilites
				if (await ScriptHelpers.DispelEnemy("Rapid Tides", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				return false;
			};
		}

		[EncounterHandler((int) MobId_EarthshaperTelu, "Earthshaper Telu")]
		public Func<WoWUnit, Task<bool>> EarthshaperTeluEncounter()
		{
			AddAvoidObject(ctx => true, 4.5f, AreaTriggerId_BramblePatch);
			return async boss =>
			{
				_telu = boss;
				if (await ScriptHelpers.InterruptCast(boss, SpellId_BramblePatch, SpellId_Briarskin))
					return true;

				// Casted by Telu, reduces damage taken by 75% and inflicts damage to attacker
				if (await ScriptHelpers.DispelEnemy("Briarskin", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				// Casted by Gola, removes all cooldowns from abilites
				if (await ScriptHelpers.DispelEnemy("Rapid Tides", ScriptHelpers.EnemyDispelType.Magic, boss))
					return true;

				return false;
			};
		}

		#endregion

		// guide http://www.wowhead.com/guide=2665/everbloom-dungeon-strategy-guide#xeritac-optional
		#region Xeri'tac


		#region Trash
		private const int SpellId_SporeBreath = 170411;

		private const uint MobId_InfestedVenomfang = 85232;
		[EncounterHandler((int)MobId_InfestedVenomfang, "Infested Venomfang")]
		public Func<WoWUnit, Task<bool>> InfestedVenomfangEncounter()
		{
			AddAvoidObject(
				3,
				o => o.Entry == MobId_InfestedVenomfang && o.ToUnit().CastingSpellId == SpellId_SporeBreath,
				o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 25)));

			return async boss => false;
		}

		#endregion

		private const int SpellId_Swipe = 169371;
		private const int SpellId_ToxicBolt = 169375;
		private const int MissileSpellId_GaseousVolley = 169383;

		private const uint MobId_Xeritac = 84550;
		private const uint MobId_GorgedBusters = 86552;
		private const uint MobId_ToxicSpiderling = 84552;
		private const uint MobId_VenomCrazedPaleOne = 84554;
		private const uint MobId_VenomSprayer = 86547;

		private const uint AreaTriggerId_ToxicGas = 7582;
		private const uint DynamicObjectId_Descend = 169322;
		private const uint GameObjectId_ToxicEggs = 234113;

		[ObjectHandler((int)GameObjectId_ToxicEggs, "Toxic Eggs", ObjectRange = 40)]
		public async Task<bool> ToxicEggsHandler(WoWGameObject eggs)
		{
			if (!Me.IsTank() || !Targeting.Instance.IsEmpty() || !LootTargeting.Instance.IsEmpty() )
				return false;

			return await ScriptHelpers.InteractWithObject(eggs, 0, true);
		}

		[EncounterHandler((int)MobId_Xeritac, "Xeri'tac")]
		public Func<WoWUnit, Task<bool>> XeritacEncounter()
		{
			WoWUnit boss = null;

			AddAvoidLocation(
				ctx => ScriptHelpers.IsViable(boss) && boss.Combat,
				2,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_GaseousVolley));

			AddAvoidObject(ctx => true, 2, AreaTriggerId_ToxicGas);

			// These mobs exploded when they reach their target.
			AddAvoidObject(ctx => true, 10, o => o.Entry == MobId_GorgedBusters && o.ToUnit().CurrentTargetGuid == Me.Guid);

			AddAvoidObject(ctx => true, 5, DynamicObjectId_Descend);

			// Swipe does AOE damage in front of the caster
			AddAvoidObject(
				ctx => Me.IsFollower() ,
				6,
				o => o.Entry == MobId_VenomCrazedPaleOne && o.ToUnit().HasAura("Toxic Blood"),
				o => o.Location.RayCast(o.Rotation, 5));

			return async npc =>
			{
				boss = npc;

				if (await ScriptHelpers.InterruptCast(boss, SpellId_ToxicBolt))
					return true;

				if (await ScriptHelpers.DispelGroup("Venomous Sting", ScriptHelpers.PartyDispelType.Poison))
					return true;

				return false;
			};
		}

		#endregion

		// guide http://www.wowhead.com/guide=2665/everbloom-dungeon-strategy-guide#archmage-sol
		#region Archmage Sol

		#region Trash

		private const int SpellId_DragonsBreath = 169843;
		private const int MissileSpellId_FrozenSnap = 169848;
		private const uint MobId_PutridPyromancer = 84957;
		private const uint MobId_InfestedIcecaller = 84989;

		[EncounterHandler((int)MobId_PutridPyromancer, "Putrid Pyromancer")]
		public async Task<bool> PutridPyromancerEncounter(WoWUnit npc)
		{
			return await ScriptHelpers.InterruptCast(npc, SpellId_DragonsBreath);
		}


		[EncounterHandler((int)MobId_InfestedIcecaller, "Infested Icecaller")]
		public Func<WoWUnit, Task<bool>> InfestedIcecallerEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				3.5f,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FrozenSnap));

			return async boss => false;
		}

		#endregion

		private const int SpellId_ParasiticGrowth = 168885;
		private const int SpellId_Frostbolt = 166465;
		private const int SpellId_Fireball = 166464;
		private const int SpellId_ArcaneBurst = 166466;
		private const int AreaTriggerSpellId_Firebloom = 166560;
		private const int MissileSpellId_Firebloom = 166562;

		private const uint MobId_ArchmageSol = 82682;
		private const uint AreaTriggerId_Firebloom = 7368;

		private const uint AreaTriggerId_FrozenRain = 7388;

		private HashSet<uint> _sporeImageIds = new HashSet<uint>
		{
			84386,
			84387
		};
			
		[EncounterHandler((int)MobId_ArchmageSol, "Archmage Sol")]
		public Func<WoWUnit, Task<bool>> ArchmageSolEncounter()
		{
			AddAvoidObject(8, AreaTriggerId_FrozenRain);

			// Firebloom that tiggers the ring of fire. 
			AddAvoidLocation(
				ctx => true,
				2,
				o => ((WoWMissile)o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_Firebloom));

			AddAvoidObject(2, o => o is WoWAreaTrigger && ((WoWAreaTrigger)o).SpellId == AreaTriggerSpellId_Firebloom);

			return async boss =>
			{
				var shouldJump = ObjectManager.GetObjectsOfType<WoWAreaTrigger>().Any(
					a =>
					{
						if (a.Entry != AreaTriggerId_Firebloom)
							return false;

						var dist = a.Distance;
						var radius = 30 * (6001 - a.TimeLeft.TotalMilliseconds) / 6000;
						return dist > radius && dist - radius  < 3;
					});

				// jump to avoid getting hit by the fire bloom ring
				if (shouldJump)
				{
					try
					{
						WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
						await Coroutine.Sleep(120);
					}
					finally
					{
						WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
					}
				}
				

				return await ScriptHelpers.InterruptCast(
					boss,
					SpellId_ParasiticGrowth,
					SpellId_Frostbolt,
					SpellId_Fireball,
					SpellId_ArcaneBurst);
			};
		}

		#endregion

		// guide http://www.wowhead.com/guide=2665/everbloom-dungeon-strategy-guide#yalnu
		#region Yalnu

		private const int SpellId_ColossalBlow = 169179;

		private const uint MobId_Yalnu = 83846;
		private const uint MobId_ColossalBlow = 84964;
		private const uint MobId_Entanglement = 84499;

		private const uint MobId_ViciousMandragora = 84399;
		private const uint MobId_GnarledAncient = 84400;
		private const uint MobId_SwiftSproutling = 84401;

		private const uint MobId_FeralLasher = 86684;

		private readonly HashSet<uint> _yalnuAdds = new HashSet<uint>
													{
														MobId_ViciousMandragora,
														MobId_GnarledAncient,
														MobId_SwiftSproutling,
														MobId_FeralLasher
													};

		private readonly WoWPoint _portalToValnuLoc = new WoWPoint(623.4323, 1734.328, 144.1603);
		private readonly WoWPoint _yalnuFinalLoc = new WoWPoint(924.4457, -1220.136, 183.9173);

		[EncounterHandler((int) MobId_Yalnu, "Yalnu")]
		public Func<WoWUnit, Task<bool>> YalnuEncounter()
		{
			// AOE stun + damage
			AddAvoidObject(18, MobId_ColossalBlow);

			var hasAddAggro = new TimeCachedValue<bool>(
				TimeSpan.FromMilliseconds(500),
				() => !Me.IsMelee() && ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => _yalnuAdds.Contains(u.Entry) && u.Aggro));

			return async boss =>
			{
				if (boss.HealthPercent <= 97 && boss.HealthPercent > 20 && await ScriptHelpers.CastHeroism())
					return true;

				if (hasAddAggro && ScriptHelpers.Tank != null)
				{
					return await ScriptHelpers.StayAtLocationWhile(
						() => hasAddAggro && ScriptHelpers.Tank != null,
						ScriptHelpers.Tank.Location,
						"tank location");
				}

				// acquire a feral lasher trample target 
				if (Me.IsDps() && boss.HasAura("Genesis"))
				{
					var partyMemberLocs = Me.PartyMembers.Select(p => p.Location).ToList();
					var submergedFeralLasher = ObjectManager.GetObjectsOfType<WoWUnit>()
						.Where(
							u =>
							{
								if (u.Entry != MobId_FeralLasher || !u.HasAura("Genesis"))
									return false;

								if (Blacklist.Contains(u, BlacklistFlags.Interact))
									return false;

								if (partyMemberLocs.Any(l => ScriptHelpers.AtLocation(l, u.Location, 1)))
								{
									Blacklist.Add(u, BlacklistFlags.Interact, TimeSpan.FromSeconds(5));
									return false;
								}
								return true;
							})
						.OrderBy(u => u.DistanceSqr)
						.FirstOrDefault();

					// trample a feral lasher
					if (ScriptHelpers.IsViable(submergedFeralLasher))
					{
						return await ScriptHelpers.StayAtLocationWhile(
							() => ScriptHelpers.IsViable(submergedFeralLasher)
								&& submergedFeralLasher.HasAura("Submerged")
								&& !Blacklist.Contains(submergedFeralLasher, BlacklistFlags.Interact),
							submergedFeralLasher.Location,
							"Feral Lasher",
							1f);
					}
				}

				return false;
			};
		}

		private bool AtYalnuEncounter(WoWPoint loc)
		{
			return loc.DistanceSqr(_yalnuFinalLoc) <= 100*100;
		}

		#endregion
	}

	#endregion

	#region Heroic Difficulty

    public class TheEverbloomHeroic : TheEverbloom
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 866; }
		}


		#endregion
	}

	#endregion
}