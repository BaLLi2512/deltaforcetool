using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.Enums;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;

// ReSharper disable CheckNamespace

namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class UpperBlackrockSpire : Dungeon
	{
		#region Overrides of Dungeon

		private DynamicBlackspot _tharbeksDoorBlackspot;
		public override uint DungeonId { get { return 330; } }

		public override WoWPoint Entrance { get { return new WoWPoint(-7481.414, -1323.271, 301.3931); } }

		public override WoWPoint ExitLocation { get { return new WoWPoint(106.1491, -318.6653, 65.47925); } }

		public override void IncludeTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			var isleader = Me.IsLeader();
			var isDps = Me.IsDps();
			foreach (var unit in incomingObjects.OfType<WoWUnit>())
			{
				if (unit.Entry == MobId_RallyingBanner && isDps)
				{
					outgoingObjects.Add(unit);
				}
				else if (unit.Entry == MobId_SentryCannon && unit.IsHostile)
				{
					outgoingObjects.Add(unit);
				}
				else if (isleader && MobsIds_hostileNeutralMobs.Contains(unit.Entry) && !Me.Combat && unit.DistanceSqr < 35*35 &&
						unit.ZDiff < 10)
				{
					outgoingObjects.Add(unit);
				}
				else if (unit.Entry == MobId_WindfuryTotem)
				{
					outgoingObjects.Add(unit);
				}
			}
		}

		public override void WeighTargetsFilter(List<Targeting.TargetPriority> units)
		{
			var isDps = Me.IsDps();
			var isRangeDps = isDps && Me.IsRange();

			foreach (var p in units)
			{
				WoWUnit unit = p.Object.ToUnit();
				switch (unit.Entry)
				{
					case MobId_DrakonidMonstrosity:
					case MobId_DrakonidMonstrosityTrash:
					case MobId_IronbarbSkyreaver:
					case MobId_RagewingWhelp:
						if (isDps)
							p.Score += 3500;
						break;

					case MobId_VilemawHatchling:
						if (isRangeDps)
							p.Score += 3500;
						break;
					case MobId_RallyingBanner:
					case MobId_WindfuryTotem:
						if (isDps)
							p.Score += 4500;
						break;
				}
			}
		}

		public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			var isMelee = Me.IsMelee();

			units.RemoveAll(
				o =>
				{
					var unit = o as WoWUnit;
					if (unit == null) return false;

					// Melee should not try attack this boss she's while doing the cyclone ability 
					if (unit.Entry == MobId_WarlordZaela && isMelee && IsCastingBlackIronCylone(unit))
						return true;

					if (unit.Entry == MobId_SentryCannon && !unit.IsHostile)
						return true;

					if (unit.Entry == MobId_VilemawHatchling && Me.IsMelee() && unit.ZDiff > 8)
						return true;

					return false;
				});
		}

		public override void OnEnter()
		{
			_tharbeksDoorBlackspot = new DynamicBlackspot(
				ShouldAvoidTharbeksDoor,
				() => _tharbeksDoorLoc,
				LfgDungeon.MapId,
				2,
				3,
				"Commander Tharbek",
				true);
			DynamicBlackspotManager.AddBlackspot(_tharbeksDoorBlackspot);
		}

		public override void OnExit()
		{
			DynamicBlackspotManager.RemoveBlackspot(_tharbeksDoorBlackspot);
			_tharbeksDoorBlackspot = null;
		}

		public override async Task<bool> HandleMovement(WoWPoint location)
		{
			if (location.DistanceSqr(RagewingPhaseOneLoc) < 3*3)
				return (await CommonCoroutines.MoveTo(RagewingBridgeCenter)).IsSuccessful();

			return false;
		}

		#endregion

		#region Root

		private const uint MobId_RuneGlow = 76396;
		private const uint MobId_SentryCannon = 76314;
		private const uint GameObjectId_RuneConduit = 226704;
		private const uint MobId_RallyingBanner = 76222;
		private const uint MobId_DrakonidMonstrosityTrash = 76018;
		private const uint MobId_BlackIronAlchemist = 76100;
		private const uint MobId_BlackIronVeteran = 77034;
		private const uint MobId_BlackIronEngineer = 76101;
		private const uint GameObjectId_TharbeksDoor = 164726;

		// these mobs showup as neutral but are really hostile.
		private readonly uint[] MobsIds_hostileNeutralMobs =
		{
			MobId_DrakonidMonstrosityTrash,
			MobId_BlackIronAlchemist,
			MobId_BlackIronVeteran,
			MobId_BlackIronEngineer
		};

		private readonly WoWPoint _tharbeksDoorLoc = new WoWPoint(106.7544, -421.1986, 110.9228);
		private readonly WaitTimer _tharbeksDoorTimer = new WaitTimer(TimeSpan.FromSeconds(2));

		private readonly WaitTimer _updateBossKillStateTimer = WaitTimer.OneSecond;
		private bool _shouldAvoidTharbeksDoor;

		private LocalPlayer Me { get { return StyxWoW.Me; } }

		[EncounterHandler(0, "Root")]
		public Func<WoWUnit, Task<bool>> RootHandler()
		{
			return async npc => { return false; };
		}


		private bool ShouldAvoidTharbeksDoor()
		{
			if (_tharbeksDoorTimer.IsFinished)
			{
				var tharbeksDoor =
					ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(g => g.Entry == GameObjectId_TharbeksDoor);

				_shouldAvoidTharbeksDoor = tharbeksDoor != null && !((WoWDoor) tharbeksDoor.SubObj).IsOpen;
				_tharbeksDoorTimer.Reset();
			}

			return _shouldAvoidTharbeksDoor;
		}

		#endregion

		[ScenarioStage(1, "Extinguish Runes")]
		public async Task<bool> StageOne(ScenarioStage stage)
		{
			if (BotPoi.Current.Type != PoiType.None || !Targeting.Instance.IsEmpty())
				return false;

			TreeRoot.StatusText = "Extinguishing Runes";
			if (BotPoi.Current.Type == PoiType.None && Me.IsLeader())
			{
				var rune =
					ObjectManager.GetObjectsOfType<WoWUnit>()
						.Where(u => u.Entry == MobId_RuneGlow && u.HasAura("Rune Glow"))
						.OrderBy(u => u.DistanceSqr)
						.FirstOrDefault();

				// should always be not null.
				if (rune != null)
				{
					ScriptHelpers.SetLeaderMoveToPoi(rune.Location);
				}
			}

			// Idle if there is nothing to do as a tank.. (highly unlikely case)
			return Me.IsLeader();
		}

		#region Gor'ashan

		private const uint MobId_LightningField = 76464;
		private const int AuraTriggerId_LodestoneSpike = 6164;

		[EncounterHandler(76413, "Gor'ashan", Mode = CallBehaviorMode.Proximity)]
		public Func<WoWUnit, Task<bool>> GorashanEncounter()
		{
			WoWGameObject conduit = null;
			// avoid these spikes
			AddAvoidObject(ctx => true, 5, o => o.Entry == AuraTriggerId_LodestoneSpike, ignoreIfBlocking: true);
			AddAvoidObject(
				ctx => true,
				12,
				o => o.Entry == MobId_LightningField,
				o => o.Location.RayCast(o.Rotation, 10));

			var bossLoc = new WoWPoint(144.5426, -258.0315, 96.32333);
			var randomPointAtBoss = WoWMathHelper.GetRandomPointInCircle(bossLoc, 5);

			var rightDoorEdge = new WoWPoint(174.7191, -258.7988, 91.54621);
			var leftDoorEdge = new WoWPoint(174.8173, -259.9115, 91.54621);

			var pointInsideRoom = new WoWPoint(164.4416, -262.2839, 91.54202);
			var randomPointInsideRoom = WoWMathHelper.GetRandomPointInCircle(pointInsideRoom, 3);

			return async boss =>
			{
				if (ScriptHelpers.CurrentScenarioInfo.CurrentStageNumber != 2)
					return false;

				if (!boss.Combat && boss.HasAura("Power Conduit"))
				{
					if (!ScriptHelpers.IsViable(conduit) || !conduit.CanUse())
					{
						var conduits = ObjectManager.GetObjectsOfType<WoWGameObject>()
							.Where(u => u.Entry == GameObjectId_RuneConduit && u.CanUse())
							.ToList();

						if (conduits.Any())
						{
							if (Me.IsLeader())
							{
								conduit = conduits.OrderBy(u => u.Location.DistanceSqr(pointInsideRoom)).FirstOrDefault();
							}
							else if (conduits.Count > 1 && !ScriptHelpers.GetUnfriendlyNpsAtLocation(boss.Location, 30, unit => unit != boss).Any())
							{
								// Decide which follower helps activate the conduits based on max health.. 
								// this prevents multiple bots trying to activate same conduit 
								var highestMaxHpFollower = ScriptHelpers.GroupMembers.Where(g => !g.IsTank)
									.OrderByDescending(g => g.MaxHealth).FirstOrDefault();

								if (highestMaxHpFollower != null && highestMaxHpFollower.Guid == Me.Guid)
									conduit = conduits.OrderByDescending(u => u.Location.DistanceSqr(pointInsideRoom)).FirstOrDefault();
							}
						}
					}

					if (ScriptHelpers.IsViable(conduit))
						ScriptHelpers.SetInteractPoi(conduit);
					return false;
				}

				if (await ScriptHelpers.MoveInsideBossRoom(
					boss,
					leftDoorEdge,
					rightDoorEdge,
					randomPointInsideRoom,
					player => player.Z < 110 && player.Z > 85))
				{
					return true;
				}

				TreeRoot.StatusText = "Doing Gor'ashan boss encounter";
				var leader = ScriptHelpers.Leader;
				// having tank click the conduits seems to work best since they have larger hp pools thus can survive better
				// and dps can still continue dpsing.
				WoWPlayer conduitClicker = null;
				if (leader != null && leader.IsMe)
				{
					conduitClicker = Me;
				}
				else if (leader == null || !leader.IsAlive)
				{
					// if tank dies have a dps do the clicking.
					conduitClicker =
						ScriptHelpers.GroupMembers.Where(g => g.IsAlive && g.IsDps)
							.OrderByDescending(g => g.MaxHealth)
							.Select(g => g.Player)
							.FirstOrDefault();
				}

				if (conduitClicker == Me && (!ScriptHelpers.IsViable(conduit) || !conduit.CanUse()))
				{
					conduit = ObjectManager.GetObjectsOfType<WoWGameObject>()
						.Where(u => u.Entry == GameObjectId_RuneConduit && u.CanUse())
						.OrderBy(u => u.Location.DistanceSqr(bossLoc))
						.FirstOrDefault();
				}

				if (ScriptHelpers.IsViable(conduit) 
					&& await ScriptHelpers.InteractWithObject(conduit, 3000, true))
				{
					return true;
				}

				if (Me.IsRange())
				{
					return await ScriptHelpers.StayAtLocationWhile(
						() => ScriptHelpers.IsViable(boss) && boss.Combat,
						randomPointAtBoss,
						"location near Gor'ashan");
				}

				if (await ScriptHelpers.TankUnitAtLocation(bossLoc, 8))
					return true; 

				return false;
			};
		}

		#endregion

		#region Kyrak

		private const uint MobId_DrakonidMonstrosity = 82556;
		private const uint AreaTriggerId_VilebloodSerum = 6823;
		private const int SpellId_Eruption = 155037;
		private const int SpellId_DebilitatingFixation = 161199;

		[EncounterHandler(82556, "Drakonid Monstrosity")]
		[EncounterHandler(76018, "Drakonid Monstrosity")]
		public Func<WoWUnit, Task<bool>> DrakonidMonstrosityEncounter()
		{
			// These NPCs casts a spell that does damage to enemies in a line in front of it.
			AddAvoidObject(
				ctx => true,
				3,
				o =>
					(o.Entry == MobId_DrakonidMonstrosity || o.Entry == MobId_DrakonidMonstrosityTrash)
					&& o.ToUnit().CastingSpellId == SpellId_Eruption,
				o => WoWMathHelper.GetNearestPointOnLineSegment(Me.Location, o.Location, o.Location.RayCast(o.Rotation, 20)));

			// non-tanks should stay away from the front of these NPCs since they have a cleave.
			AddAvoidObject(
				ctx => !Me.IsLeader(),
				5,
				o =>
					(o.Entry == MobId_DrakonidMonstrosity || o.Entry == MobId_DrakonidMonstrosityTrash),
				o => o.Location.RayCast(o.Rotation, 4));

			return async npc => await ScriptHelpers.DispelEnemy(
				"Rejuvenating Serum",
				ScriptHelpers.EnemyDispelType.Magic,
				npc);
		}

		[EncounterHandler(76021, "Kyrak")]
		public Func<WoWUnit, Task<bool>> KyrakEncounter()
		{
			AddAvoidObject(ctx => true, 2.5f, AreaTriggerId_VilebloodSerum);

			return async boss =>
						{
							TreeRoot.StatusText = "Doing Kyrak boss encounter";

							if (await ScriptHelpers.DispelEnemy("Rejuvenating Serum", ScriptHelpers.EnemyDispelType.Magic, boss))
								return true;

							if (await ScriptHelpers.InterruptCast(boss, SpellId_DebilitatingFixation))
								return true;

							return false;
						};
		}

		#endregion

		#region Commander Tharbek

		private const int MissileSpellId_NoxiousSpit = 161824;
		private const int MissileSpellId_ImbuedIronAxe = 162090;

		private const uint AreaTriggerId_NoxiousSpit = 6880;
		private const uint MobId_BlackIronSiegebreaker = 77033;
		private const uint MobId_IronbarbSkyreaver = 80098;
		private const uint MobId_CommanderTharbek = 79912;
		private const uint MobId_VilemawHatchling = 77096;
		private const uint MobId_ImbuedIronAxe = 80307;

		// handles challenging trash pulls

		private PerFrameCachedValue<WoWUnit> _tharbek;

		private WoWUnit Tharbek
		{
			get
			{
				return _tharbek ??
						(_tharbek = new PerFrameCachedValue<WoWUnit>(
							() => ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == MobId_CommanderTharbek)));
			}
		}

		[LocationHandler(112.2449, -312.1981, 106.4356, radius: 40)]
		public Func<WoWPoint, Task<bool>> TrashToTharbek()
		{
			var siegeBreakerWestPathEnd = new WoWPoint(151.4737, -270.8368, 110.9437);
			var siegeBreakerEastPathEnd = new WoWPoint(151.8634, -335.1172, 110.9524);

			var packByEastDoorLoc = new WoWPoint(140.1003, -299.1566, 110.9622);
			var packTwoLoc = new WoWPoint(161.4485, -317.8856, 110.9393);

			var losByEastDoorLoc = new WoWPoint(107.9661, -305.4395, 106.4356);
			var pullFromLoc = new WoWPoint(126.643, -312.1581, 110.9481);

			return async loc =>
						{
							var stage = ScriptHelpers.CurrentScenarioInfo.CurrentStage;

							if (stage.StageNumber != 2 || !stage.GetStep(2).IsComplete)
								return false;

							if (Me.Z < 100)
								return false;
							var roamingSiegeBreaker = ObjectManager.GetObjectsOfType<WoWUnit>()
								.FirstOrDefault(
									u =>
										u.Entry == MobId_BlackIronSiegebreaker && u.IsAlive &&
										WoWMathHelper.GetNearestPointOnLineSegment(
											u.Location,
											siegeBreakerWestPathEnd,
											siegeBreakerEastPathEnd).DistanceSqr(u.Location) < 5*5);

							// pull the pack just to the left side of the east doorway.
							var packByEastDoor =
								ScriptHelpers.GetUnfriendlyNpsAtLocation(packByEastDoorLoc, 7).FirstOrDefault();
							if (await ScriptHelpers.PullNpcToLocation(
								() => packByEastDoor != null,
								() =>
									roamingSiegeBreaker == null ||
									roamingSiegeBreaker.Location.DistanceSqr(packByEastDoor.Location) > 30*30,
								packByEastDoor,
								losByEastDoorLoc,
								loc,
								5000,
								waitAtLocationRadius: 3))
							{
								return true;
							}

							// pack across the room of the east door entrance
							var packTwo = ScriptHelpers.GetUnfriendlyNpsAtLocation(packTwoLoc, 7).FirstOrDefault();
							if (await ScriptHelpers.PullNpcToLocation(
								() => packTwo != null,
								() =>
									roamingSiegeBreaker == null ||
									roamingSiegeBreaker.Location.DistanceSqr(packTwo.Location) > 30*30,
								packTwo,
								losByEastDoorLoc,
								Me.IsLeader() ? pullFromLoc : loc,
								5000,
								waitAtLocationRadius: 3))
							{
								return true;
							}

							// Finally, pull the roaming Siege Breaker
							if (await ScriptHelpers.PullNpcToLocation(
								() => roamingSiegeBreaker != null,
								() => roamingSiegeBreaker.Location.DistanceSqr(packTwoLoc) < 25*25,
								roamingSiegeBreaker,
								pullFromLoc,
								Me.IsLeader() ? pullFromLoc : loc,
								5000,
								waitAtLocationRadius: 3))
							{
								return true;
							}

							return false;
						};
		}


		[EncounterHandler(79912, "Commander Tharbek", Mode = CallBehaviorMode.CurrentBoss)]
		public Func<WoWUnit, Task<bool>> CommanderTharbekStartEncounter()
		{
			var tankSpot = new WoWPoint(169.1132, -419.7492, 110.4723);

			return async boss =>
			{
				if (!Me.IsLeader())
					return false;

				if (ScriptHelpers.CurrentScenarioInfo.CurrentStageNumber != 2
					|| ScriptHelpers.CurrentScenarioInfo.CurrentStage.GetStep(3).IsComplete)
				{
					return false;
				}

				if (Me.Location.DistanceSqr(tankSpot) > 30*30)
					return false;

				if (Tharbek != null && Tharbek.Combat)
					return false;

				return await ScriptHelpers.StayAtLocationWhile(() => Tharbek == null || !Tharbek.Combat, tankSpot, precision: 15);
			};
		}

		[EncounterHandler(79912, "Commander Tharbek")]
		public Func<WoWUnit, Task<bool>> CommanderTharbekEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				8,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_ImbuedIronAxe));

			AddAvoidObject(ctx => true, 8, MobId_ImbuedIronAxe);

			return async boss => { return false; };
		}

		[EncounterHandler(80098, "Ironbarb Skyreaver")]
		public Func<WoWUnit, Task<bool>> IronbarbSkyreaverEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				6,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_NoxiousSpit));

			AddAvoidObject(ctx => true, 6, AreaTriggerId_NoxiousSpit);

			// his breath is very directional and deadly
			AddAvoidObject(
				ctx => true,
				7,
				o => o.Entry == MobId_IronbarbSkyreaver && o.ToUnit().HasAura("Incinerating Breath"),
				o => WoWMathHelper.GetNearestPointOnLineSegment(Me.Location, o.Location, o.Location.RayCast(o.Rotation, 30)));

			return async boss => false;
		}

		#endregion

		#region Son of the Beast

		private const uint MobId_SonoftheBeast = 77927;
		private const uint AreaTriggerId_FieryTrail = 6472;

		[EncounterHandler((int) MobId_SonoftheBeast, "Son of the Beast")]
		public Func<WoWUnit, Task<bool>> SonoftheBeastEncounter()
		{
			AddAvoidObject(4, AreaTriggerId_FieryTrail);

			return async boss => { return false; };
		}

		#endregion

		// http://www.wowhead.com/guide=2670/upper-blackrock-spire-dungeon-strategy-guide#ragewing-the-untamed
		#region Ragewing the Untamed

		readonly WoWPoint RagewingPhaseOneLoc = new WoWPoint(20.56284,-404.3033,113.1969);
		readonly WoWPoint RagewingBridgeCenter = new WoWPoint(34.26516, -406.6279, 110.7208);

		private const int MissileSpellId_MagmaSpit = 155053;
		private const int MissileSpellId_FireStorm = 155073;

		private const uint MobId_RagewingtheUntamed = 76585;
		private const uint MobId_RagewingWhelp = 76801;

		private const uint AreaTriggerId_MagmaSpit = 6204;
		private const uint MobId_EngulfingFireInvisibleStalkerRtoL = 76813;
		private const uint MobId_EngulfingFireInvisibleStalkerLtoR = 76837;

		[EncounterHandler((int)MobId_RagewingtheUntamed, "Ragewing the Untamed", Mode=CallBehaviorMode.Proximity, BossRange = 100)]
		public Func<WoWUnit, Task<bool>> RagewingtheUntamedEncounter()
		{
			// avoid the impact of magma spit missiles and the puddles they leave behind
			AddAvoidLocation(
				ctx => true,
				3.5f,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_MagmaSpit));
			AddAvoidObject(3.5f, AreaTriggerId_MagmaSpit);
			
			// Fire storm is the ability used while flying over bridge.
			AddAvoidLocation(
				ctx => true,
				8,
				o => ((WoWMissile) o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FireStorm));

			var westBridgeSide = new WoWPoint(33.79537, -389.6173, 110.7289);
			var eastBridgeSide = new WoWPoint(34.6082, -422.5337, 110.8128);

			var leftDoorEdge = new WoWPoint(30.96826, -438.6647, 111.1953);
			var rightDoorEdge = new WoWPoint(37.24679, -438.27, 111.0201);

			// pick a random location inside door
			var randomPointOnBridge = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(34.0181, -431.451, 110.975), 2.5f);

			var avoidLeftToRightRagingFire = new PerFrameCachedValue<bool>( 
				() => ObjectManager.GetObjectsOfType<WoWUnit>()
						.Any(u => u.Entry == MobId_EngulfingFireInvisibleStalkerLtoR && u.HasAura("Engulfing Fire")));

			var avoidRightToLeftRagingFire = new PerFrameCachedValue<bool>(
				() => ObjectManager.GetObjectsOfType<WoWUnit>()
						.Any(u => u.Entry == MobId_EngulfingFireInvisibleStalkerRtoL && u.HasAura("Engulfing Fire")));

			return async boss =>
			{
				// Get onto the bridge before door closes.
				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointOnBridge))
					return true;
				
				// Hero if available. 
				if (boss.HealthPercent <= 97 && boss.HealthPercent > 25 && await ScriptHelpers.CastHeroism())
					return true;

				if (await ScriptHelpers.DispelEnemy("Burning Rage", ScriptHelpers.EnemyDispelType.Enrage, boss))
					return true;

				if ( avoidRightToLeftRagingFire)
				{
					return await ScriptHelpers.StayAtLocationWhile(
						() =>  avoidRightToLeftRagingFire,
						westBridgeSide,
						"West side of bridge that's safe from Raging Fire", 6);
				}

				if (avoidLeftToRightRagingFire)
				{
					return await ScriptHelpers.StayAtLocationWhile(
						() => avoidLeftToRightRagingFire,
						eastBridgeSide,
						"East side of bridge that's safe from Raging Fire", 6);
				}

				return  await ScriptHelpers.StayAtLocationWhile(
					() => !avoidLeftToRightRagingFire && !avoidRightToLeftRagingFire && ScriptHelpers.IsViable(boss) && boss.Combat,
					RagewingBridgeCenter,
					"Center of bridge", 15);
			};
		}

		#endregion

		// http://www.wowhead.com/guide=2670/upper-blackrock-spire-dungeon-strategy-guide#warlord-zaela
		#region Warlord Zaela

		#region Trash

		private const uint MobId_WindfuryTotem = 80703;
		private const uint AreaTriggerId_FlameSpit = 6990;
		private const int SpellId_BurningBreath = 166040;

		private const uint MobId_EmberscaleIronflight = 82428;
		[EncounterHandler((int) MobId_EmberscaleIronflight, "Emberscale Ironflight")]
		public Func<WoWUnit, Task<bool>> EmberscaleIronflightEncounter()
		{
			AddAvoidObject(3, AreaTriggerId_FlameSpit);

			AddAvoidLocation(
				ctx => true,
				10,
				o => (WoWPoint) o,
				GetDrakeAvoidPoints);

			return async npc => false;
		}

		private IEnumerable<object> GetDrakeAvoidPoints()
		{
			return ObjectManager.GetObjectsOfType<WoWUnit>()
				.Where(drake => drake.Entry == MobId_EmberscaleIronflight && IsCastingBurningBreath(drake))
				.SelectMany(drake => ScriptHelpers.GetPointsAlongLineSegment(drake.Location, drake.Location.RayCast(drake.Rotation, 60), 5))
				.Cast<object>();
		}

		private bool IsCastingBurningBreath(WoWUnit unit)
		{
			return unit.CastingSpellId == SpellId_BurningBreath || unit.HasAura("Burning Breath");
		}

		#endregion

		private const int SpellId_BlackIronCyclone = 155721;
		private const int MissileSpellId_ReboundingBlade = 155711;

		private const uint MobId_WarlordZaela = 77120;
		private const uint AreaTriggerId_BurningBridge = 7387;

		[EncounterHandler((int) MobId_WarlordZaela, "Warlord Zaela", Mode = CallBehaviorMode.Proximity)]
		public Func<WoWUnit, Task<bool>> WarlordZaelaEncounter()
		{
			WoWUnit boss = null;

			AddAvoidObject(
				ctx => true,
				o => o.ToUnit().CurrentTargetGuid == Me.Guid ? 10 : 8,
				u => u.Entry == MobId_WarlordZaela && IsCastingBlackIronCylone(u.ToUnit()));
			
			// Range should always stay away from boss.
			AddAvoidObject(
				ctx => Me.IsRange(),
				8,
				u => u.Entry == MobId_WarlordZaela && u.ToUnit().Combat && boss.ZDiff < 5);

			AddAvoidObject(5, AreaTriggerId_BurningBridge);

			// Don't run into players while fixated by cylone
			AddAvoidObject(
				ctx => ScriptHelpers.IsViable(boss) && IsCastingBlackIronCylone(boss) && boss.CurrentTargetGuid == Me.Guid,
				8,
				o => o is WoWPlayer && !o.IsMe && o.ToPlayer().IsAlive);

			// Run away from the target of Rebounding Blade to avoid chaining it
			AddAvoidLocation(
				ctx => true,
				8,
				o => (WoWPoint) o,
				() => WoWMissile.InFlightMissiles
					.Where(m => m.SpellId == MissileSpellId_ReboundingBlade)
					.Select(m => m.Target)
					.Where(t => t != null && !t.IsMe)
					.Select(t => t.Location)
					.Cast<object>());

			var leftDoorEdge = new WoWPoint(20.35804, -139.3033, 97.76679);
			var rightDoorEdge = new WoWPoint(27.55599, -139.106, 97.79568);

			// pick a random location inside door
			var randomPointOnBridge = WoWMathHelper.GetRandomPointInCircle(new WoWPoint(25.55703, -133.2437, 97.73102), 2.5f);
			return async npc =>
			{
				boss = npc;

				// Get onto the bridge before door closes.
				if (await ScriptHelpers.MoveInsideBossRoom(boss, leftDoorEdge, rightDoorEdge, randomPointOnBridge))
					return true;

				// Hero if available. 
				if (boss.HealthPercent <= 97 && boss.HealthPercent > 25 && await ScriptHelpers.CastHeroism())
					return true;

				return false;
			};
		}

		private bool IsCastingBlackIronCylone(WoWUnit unit)
		{
			return unit.CastingSpellId == SpellId_BlackIronCyclone || unit.HasAura("Black Iron Cyclone");
		}

		#endregion

	}


	public class UpperBlackrockSpireHeroic : UpperBlackrockSpire
	{
		#region Overrides of Dungeon

		public override uint DungeonId { get { return 860; } }

		public override void OnEnter()
		{
			// Followers will automatically leave when leader does so no need to show more than one popup.
			if (DungeonBuddySettings.Instance.PartyMode != PartyMode.Follower)
			{
				Alert.Show(
					"Dungeon Not Supported",
					string.Format(
						"The {0} dungeon is not supported. If you wish to stay in group and play manually then press 'Cancel'. " +
						"Otherwise Dungeonbuddy will automatically leave group.",
						Name),
					30,
					true,
					true,
					() => Lua.DoString("LeaveParty()"),
					null,
					"Leave",
					"Cancel");
			}
		}

		#endregion
	}
}