using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Vector2 = Tripper.Tools.Math.Vector2;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	public class ArcaneSanctum : HighMaulFirstAndSecondWings
	{
		#region Overrides of Dungeon
	
		public override uint DungeonId
		{
			get { return 850; }
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
	        List<GroupMember> tanks = null;
	        var isMelee = Me.IsMelee();
			units.RemoveAll(
				ret =>
				{
				    var unit = ret as WoWUnit;
				    if (unit == null)
				        return false;

					if (unit.Combat && MobIds_CombatStuckTrash.Contains(unit.Entry))
					{
						if (tanks == null)
							tanks = ScriptHelpers.GroupMembers.Where(g => g.IsTank).ToList();
						var minTankRange = unit.MeleeRange() + 20;
						var meleeRangeSqr = minTankRange;
						// ignore if no tank is within melee range. These mobs are usually just ignored.
						if (!tanks.Any(t => t.Location.DistanceSqr(unit.Location) <= meleeRangeSqr))
							return true;
					}

					if (unit.Entry == MobId_Koragh && unit.HasAura("Vulnerability") && isMelee)
						return true;

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
            foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
                    switch (unit.Entry)
                    {
						case MobId_Oro:
		                    priority.Score += (unit.CastingSpellId == SpellId_Reconstitution ? 6000: 5000);
							break;
						case MobId_Lokk:
						case MobId_Rokkaa:
							priority.Score += (unit.CastingSpellId == SpellId_Reconstitution ? 6000 : 4000);
							break;
						case MobId_Phemos:
							if (unit.HasAura("Whirlwind"))
			                    priority.Score -= 5000;
							break;
						case MobId_VolatileAnomaly:
							// Strat calls for these to be killed while in suppression fields to prevent them from exploding and doing raid damage
							// but on LFR mode they do so little damage we don't need to bother
		                    priority.Score += 5000;
		                    break;
                    }
				}
			}
		}

	    #region Portal Behavior

	    private readonly Vector2[] _arcaneSanctumUpperArea =
	    {
			new Vector2(3787.323f, 8890.202f),
			new Vector2(3599.291f, 8277.019f),
			new Vector2(3946.942f, 8170.061f),
			new Vector2(4193.107f, 8304.675f),
			new Vector2(4220.591f, 8807.695f),
			new Vector2(3896.472f, 8911.454f),
		};

		private readonly WoWPoint _bottomPortalLoc = new WoWPoint(3687, 8041.954, 72.72411);
		private readonly WoWPoint _topPortalLoc = new WoWPoint(3778.452, 8318.854, 316.5559);

	    public override async Task<bool> HandleMovement(WoWPoint location)
	    {
		    var myLoc = Me.Location;
			var meIsInUpperArcaneSanctum = WoWMathHelper.IsPointInPoly(myLoc, _arcaneSanctumUpperArea);
			var destIsInUpperArcaneSanctum = WoWMathHelper.IsPointInPoly(location, _arcaneSanctumUpperArea);

		    if (!meIsInUpperArcaneSanctum && destIsInUpperArcaneSanctum)
			    return (await CommonCoroutines.MoveTo(_bottomPortalLoc, "Bottom Portal")).IsSuccessful();

			if (meIsInUpperArcaneSanctum && !destIsInUpperArcaneSanctum)
				return (await CommonCoroutines.MoveTo(_topPortalLoc, "Top Portal")).IsSuccessful();

			// See if we need to take shortcut.
		    if (await HandleTheButcherShortcut(location))
			    return true;

		    return false;
	    }


	    #endregion

	    #endregion
		
		#region Root

		#endregion

		#region Tectus

		#region Trash

		private const int SpellId_Reconstitution = 172116;
	    private const int MissileSpellId_StoneboltVolley = 172058;
	    private const int MissileSpellId_MeteoricEarthspire = 172110;
	    private const int MissileSpellVisualId_EarthanThrust = 19385;
		private const uint MobId_Oro = 86072;
		private const uint MobId_Lokk = 86073;
		private const uint MobId_Rokkaa = 86071;

		private const uint MobId_NightTwistedSoothsayer = 85240;
		private const uint MobId_GorianGuardsman = 81270;
		private const uint MobId_NightTwistedPale = 82694;
		private const uint MobId_GorianRunemaster = 81272;
		private const uint MobId_GorianSorcerer = 85225;
		private const uint MobId_NightTwistedBrute = 85241;
		private const uint MobId_NightTwistedDevout = 82698;
		private const uint MobId_GorianEnforcer = 82900;

		private readonly HashSet<uint> MobIds_CombatStuckTrash = new HashSet<uint>
														{
															MobId_NightTwistedSoothsayer,
															MobId_GorianGuardsman,
															MobId_NightTwistedPale,
															MobId_GorianRunemaster,
															MobId_GorianSorcerer,
															MobId_NightTwistedBrute,
															MobId_NightTwistedDevout,
															MobId_GorianEnforcer
														};


		private const uint AreaTriggerId_RuneofDisintegration = 8035;

		[EncounterHandler((int)MobId_Oro, "Oro")]
		public Func<WoWUnit, Task<bool>> OroEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				10,
				m => ((WoWMissile) m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_StoneboltVolley));

			AddAvoidObject(10, o => o is WoWPlayer && !o.IsMe && (o.ToPlayer().HasAura("Radiating Poison") || Me.HasAura("Radiating Poison")));

			return async npc =>
			{
				return false;
			};
		}

		[EncounterHandler((int)MobId_Lokk, "Lokk")]
		[EncounterHandler((int)MobId_Rokkaa, "Rokkaa")]
		public Func<WoWUnit, Task<bool>> LokkAndRokkaaEncounter()
		{
			AddAvoidObject(6, AreaTriggerId_RuneofDisintegration);

			AddAvoidLocation(
				ctx => true,
				10,
				m => ((WoWMissile)m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellVisualId == MissileSpellVisualId_EarthanThrust));

			var meteoricEarthspire =
				new PerFrameCachedValue<WoWMissile>(
					() => WoWMissile.InFlightMissiles.FirstOrDefault(m => m.SpellId == MissileSpellId_MeteoricEarthspire));

			return async npc =>
			{
				if (meteoricEarthspire.Value != null)
				{
					return await ScriptHelpers.StayAtLocationWhile(
								() => meteoricEarthspire.Value != null,
								meteoricEarthspire.Value.ImpactPosition,
								"Meteoric Earthspire",
								9);
				}
				return false;
			};
		}

	    #endregion Trash

	    private const int SpellId_CrystallineBarrage = 162346;

	    private const int MissileSpellId_Fracture = 163208;
		private const uint GameObjectId_EarthenPillar = 229649;
		private const uint MobId_EarthenPillarStalker = 80476;
		private const uint AreaTriggerId_CrystallineBarrage = 6957;

	    private readonly WoWPoint TectusRoomCenter = new WoWPoint(3549.25, 7953.533, 65.01971);
	    private const float TectusRoomRadius = 55f;

		private const uint MobId_Tectus = 78948;

		// http://www.wowhead.com/guides/raiding/highmaul/tectus-strategy-guide
		[EncounterHandler((int)MobId_Tectus, "Tectus")]
		public Func<WoWUnit, Task<bool>> TectusEncounter()
		{
			AddAvoidObject(10, GameObjectId_EarthenPillar, MobId_EarthenPillarStalker);

			var targetedByCrystallineBarrage = new PerFrameCachedValue<bool>(() => Me.HasAura(SpellId_CrystallineBarrage));

			AddAvoidObject(o => true, o => targetedByCrystallineBarrage ? 4 : 2.4f, AreaTriggerId_CrystallineBarrage);

			// Crystalline Barrage leaves a trail of pools that do damage behind so players with Crystalline Barrage should 
			// drop the pools away from boss and melee.
			AddAvoidObject(o => targetedByCrystallineBarrage, () => TectusRoomCenter, TectusRoomRadius, 25, o => o.Entry == MobId_Tectus);

			AddAvoidLocation(
				ctx => true,
				5,
				m => ((WoWMissile)m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_Fracture));

			return async boss =>
			{
				
				return false;
			};
		}

		#endregion Tectus

		#region Twin Orgron

		private const uint MobId_Pol = 78238;
		private const uint MobId_Phemos = 78237;
	    private const int SpellId_EnfeeblingRoar = 158057;
		private const uint AreaTriggerId_Blaze_Medium = 6987;
		private const uint AreaTriggerId_Blaze_Small = 6197;
		private const uint AreaTriggerId_Blaze_Big = 6630;
	    private const int SpellId_ShieldCharge = 158134;
	    private const int SpellId_InterruptingShout = 158093;
	    private const int MissileSpellVisualId_PulverizeSmall = 37673;
	    private const int MissileSpellVisualId_PulverizeBig = 37702;

		[EncounterHandler((int)MobId_Pol, "Pol")]
		public Func<WoWUnit, Task<bool>> PolEncounter()
		{
			WoWUnit pol = null;

			const float shieldChargeLineWidth = 4;

			AddAvoidLocation(ctx => ScriptHelpers.IsViable(pol) && pol.CastingSpellId == SpellId_ShieldCharge && pol.GotTarget,
				shieldChargeLineWidth * 1.33f,
				o => (WoWPoint)o,
				() => ScriptHelpers.GetPointsAlongLineSegment(
					pol.Location,
					pol.CurrentTarget.Location,
					shieldChargeLineWidth / 2).OfType<object>());

			AddAvoidLocation(
				ctx => true,
				8,
				m => ((WoWMissile) m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(
						m => m.SpellVisualId == MissileSpellVisualId_PulverizeSmall || m.SpellVisualId == MissileSpellVisualId_PulverizeBig));


			return async boss =>
			{

				pol = boss;

				if (boss.CastingSpellId == SpellId_InterruptingShout && Me.IsCasting)
				{
					var tps = GameStats.TicksPerSecond != 0
						? GameStats.TicksPerSecond
						: CharacterSettings.Instance.TicksPerSecond;

					var timeWindow = TimeSpan.FromMilliseconds(StyxWoW.WoWClient.Latency + 1000 /tps + 100) ;
					if (boss.CurrentCastTimeLeft < timeWindow)
					{
						SpellManager.StopCasting();
						return true;
					}
				}
				return false;
			};
		}

		[EncounterHandler((int)MobId_Phemos, "Phemos")]
		public Func<WoWUnit, Task<bool>> PhemosEncounter()
		{
			WoWUnit phemos = null;
			AddAvoidObject(25, o => o.Entry == MobId_Phemos && o.ToUnit().HasAura("Whirlwind"));

			var roomCenter = new WoWPoint(4069.416f, 8461.772, 322.9503);

			AddAvoidObject(ctx => true, () => roomCenter, 40, 4, o => o.Entry == AreaTriggerId_Blaze_Small, o => o.Location.RayCast(o.Rotation, 3));
			AddAvoidObject(ctx => true, () => roomCenter, 40, 5, o => o.Entry == AreaTriggerId_Blaze_Medium, o => o.Location.RayCast(o.Rotation, 3));
			AddAvoidObject(ctx => true, () => roomCenter, 40, 6, o => o.Entry == AreaTriggerId_Blaze_Big, o => o.Location.RayCast(o.Rotation, 3));

			var stackOnPhemos =
				new PerFrameCachedValue<bool>(() => ScriptHelpers.IsViable(phemos) && phemos.CastingSpellId == SpellId_EnfeeblingRoar);

			return async boss =>
			{
				phemos = boss;

				if (stackOnPhemos)
				{
					return await ScriptHelpers.StayAtLocationWhile(() => stackOnPhemos, boss.Location, boss.SafeName, 18);
				}
				return false;
			};
		}

		#endregion


		#region Ko'ragh
	    
		#region Trash

	    private const uint MobId_BreakerRitualist = 86329;

		[EncounterHandler((int)MobId_BreakerRitualist, "Breaker Ritualist")]
	    public Func<WoWUnit, Task<bool>> KoraghTrashEncounter()
	    {
		    Func<WoWPlayer, bool> hasFrozenCore =
				player => player.HasAura("Frozen Core");

			var iHaveFrozenCore = new PerFrameCachedValue<bool>(() => hasFrozenCore(Me));

			AddAvoidObject(5, o => o is WoWPlayer && !o.IsMe && (hasFrozenCore(o.ToPlayer()) || iHaveFrozenCore));
			AddAvoidObject(4, AreaTriggerId_WildFlames);


		    return async boss => false;
	    }

	    #endregion Trash


		private const uint AreaTriggerId_WildFlames = 7913;

		private const uint AreaTriggerId_OverflowingEnergy = 6863;
		private const uint AreaTriggerId_SuppressionField = 6838;
		private const uint AreaTriggerId_ExpelMagicFrost=7853;
		private const uint AreaTriggerId_GroundMarker = 7863;
		private const uint AreaTriggerId_CausticEnergy = 6784;

		private const uint MobId_VolatileAnomaly = 79956;
		private const uint MobId_Koragh = 79015;

		// http://www.wowhead.com/guide=2791/koragh-highmaul-raid-strategy-guide
		[EncounterHandler((int)MobId_Koragh, "Ko'ragh")]
		public Func<WoWUnit, Task<bool>> KoraghEncounter()
		{
			Func<WoWPlayer, bool> hasExpiringExpelMagicFire =
				player => player.HasAura("Expel Magic: Fire", aura => aura.TimeLeft < TimeSpan.FromSeconds(2));

			var hasExpelMagicFire =
				new PerFrameCachedValue<bool>(() => hasExpiringExpelMagicFire(Me));

			AddAvoidObject(ctx => !Me.HasAura("Nullification Barrier"), 2, AreaTriggerId_OverflowingEnergy);
			AddAvoidObject(8, AreaTriggerId_SuppressionField);
			AddAvoidObject(10, AreaTriggerId_ExpelMagicFrost);
			AddAvoidObject(9, AreaTriggerId_CausticEnergy);
			AddAvoidObject(5, AreaTriggerId_GroundMarker);
			AddAvoidObject(5, o => o is WoWPlayer && !o.IsMe && (hasExpiringExpelMagicFire(o.ToPlayer()) || hasExpelMagicFire));
			return async boss =>
			{
				return false;
			};
		}

		#endregion

	}

}