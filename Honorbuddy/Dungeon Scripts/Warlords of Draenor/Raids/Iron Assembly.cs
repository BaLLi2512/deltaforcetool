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
using Vector2 = Tripper.Tools.Math.Vector2;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.Raids.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	// ToDo: Finish scripting this
    public class IronAssembly : BlackrockFoundry
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 848; }
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
			units.RemoveAll(
				ret =>
				{
					var unit = ret as WoWUnit;
					if (unit == null)
						return false;
				
					return false;
				});
		}

		public override void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
		{
			var isRange = Me.IsRange();

			foreach (var obj in incomingunits)
			{
				var unit = obj as WoWUnit;
				if (unit != null)
				{
					if (unit.Entry == MobId_HeavySpear)
						outgoingunits.Add(unit);

					if (isRange && unit.Entry == MobId_DominatorTurret)
						outgoingunits.Add(unit);
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
						case MobId_DominatorTurret:
							if (israngedDps)
								priority.Score += 5000;
							break;
						case MobId_Cruelfang:
						case MobId_Ironcrusher:
						case MobId_Dreadwing:
						case MobId_GromkarFiremender:
                            priority.Score += 5000;
							break;
                        case MobId_HeavySpear:
						case MobId_GromkarManatArms:
                            priority.Score += 4500;
							break;
						case MobId_IronCrackShot:
							priority.Score += 4000;
							break;
						case MobId_IronRaider:
							priority.Score += 3500;
							break;

                    }
				}
			}
		}

		#endregion

		#region Root

		private const uint AreaTriggerId_ElectricalStorm = 6976;
		[EncounterHandler(0, "Root Handler")]
		public Func<WoWUnit, Task<bool>> RootHandler()
		{
			AddAvoidObject(7.5f, o => o.Entry == AreaTriggerId_ElectricalStorm, ignoreIfBlocking: true);
			return async boss => false;
		}

		#endregion

		#region Beastlord Darmac

		private const int MissileSpellId_PinDown = 154951;

		private const uint MobId_HeavySpear = 76796;
		private const uint MobId_Cruelfang = 76884;
		private const uint MobId_Ironcrusher = 76945;
		private const uint MobId_Dreadwing = 76874;
		private const uint MobId_PackBeast = 77128;

		private const uint AreaTriggerId_InfernoBreath = 6417;
		private const uint AreaTriggerId_SuperheatedShrapnel = 6416;


		[EncounterHandler((int)MobId_Cruelfang, "Cruelfang")]
		public Func<WoWUnit, Task<bool>> CruelfangEncounter()
		{
			return async boss => await ScriptHelpers.DispelEnemy("Savage Howl", ScriptHelpers.EnemyDispelType.Enrage, boss);
		}

		[EncounterHandler((int)MobId_Dreadwing, "Dreadwing")]
		public Func<WoWUnit, Task<bool>> DreadwingEncounter()
		{
			return async boss => await ScriptHelpers.DispelGroup("Inferno Breath", ScriptHelpers.PartyDispelType.Magic);
		}
		private const uint MobId_BeastlordDarmac = 76865;
		[EncounterHandler((int)MobId_BeastlordDarmac, "Beastlord Darmac")]
		public Func<WoWUnit, Task<bool>> BeastlordDarmacEncounter()
		{
			AddAvoidLocation(ctx => true, 3, o => ((WoWMissile)o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_PinDown));

			AddAvoidObject(3, AreaTriggerId_SuperheatedShrapnel, AreaTriggerId_InfernoBreath);

			return async boss =>
			{
				return false;
			};
		}

		#endregion

		#region Operator Thogar
		private const uint MobId_Engine = 77181;
		private const uint MobId_PassengerTransport = 81752;
		private const uint AreaTriggerId_PrototypePulseGrenade = 7282;

		private const uint MobId_OperatorThogar = 76906;

		[EncounterHandler((int)MobId_OperatorThogar, "Operator Thogar")]
		public Func<WoWUnit, Task<bool>> OperatorThogarEncounter()
		{
			WoWUnit boss;

			AddAvoidObject(15, o =>
				(o.Entry == MobId_Engine || o.Entry == MobId_PassengerTransport) && o.ToUnit().HasAura("Moving Train") && o.DistanceSqr < 200 * 200,
				o =>
				{
					var transport = o.ToUnit().Transport;
					var loc = transport.Location;
					return Me.Location.GetNearestPointOnSegment(loc, loc.RayCast(transport.Rotation, 200));
				});

			AddAvoidObject(5, AreaTriggerId_PrototypePulseGrenade);

			return async npc =>
			{
				boss = npc;
                return false;
			};
		}

		#endregion

		#region Iron Maidens

		#region Enforcer Sorka
		private const int MissileSpellId_BombardmentPatternAlpha = 157856;

		private const uint MobId_GromkarFiremender = 87841;
	    private const uint MobId_GromkarManatArms = 78832;
	    private const uint MobId_IronCrackShot = 81315;
	    private const uint MobId_IronRaider = 81197;

	    private const uint MobId_EnforcerSorka = 77231;

	    [EncounterHandler((int) MobId_EnforcerSorka, "Enforcer Sorka")]
	    public Func<WoWUnit, Task<bool>> EnforcerSorkaEncounter()
	    {
			AddAvoidLocation(ctx => true, 5, o => ((WoWMissile)o).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_BombardmentPatternAlpha));

			return async boss =>
		    {
			    return false;
		    };
	    }

	    #endregion


	    #region Admiral Gar'an

	    private const uint MobId_AdmiralGaran = 77557;
	    private const int SpellId_PenetratingShot = 164271;
		private const uint MobId_DominatorTurret = 78583;

		[EncounterHandler((int) MobId_AdmiralGaran, "Admiral Gar'an")]
	    public Func<WoWUnit, Task<bool>> AdmiralGaranEncounter()
	    {
			AddAvoidObject(ctx => Me.HasAura("Rapid Fire"), o => Me.IsMoving ? 15 : 10, MobId_RapidFireStalker);
			AddAvoidObject(ctx => Me.HasAura("Blood Ritual"), 25, o => o.Entry == MobId_AdmiralGaran);
			return async boss =>
		    {
			    if (boss.CastingSpellId == SpellId_PenetratingShot )
			    {
				    var bossCt = boss.CurrentTarget;
					if (bossCt != null && !bossCt.IsMe)
					{
						var loc = Me.Location.GetNearestPointOnSegment(boss.Location, bossCt.Location);
						if (await ScriptHelpers.StayAtLocationWhile(
							() => ScriptHelpers.IsViable(boss) && boss.CastingSpellId == SpellId_PenetratingShot, loc, "Penetrating Shot", 1))
						{
							return true;
						}
					}
			    }
			    return false;
		    };
	    }

	    #endregion


	    #region Marak the Blooded

	    private const uint MobId_RapidFireStalker = 77636;

	    private const uint MobId_MaraktheBlooded = 77477;

	    [EncounterHandler((int) MobId_MaraktheBlooded, "Marak the Blooded")]
	    public Func<WoWUnit, Task<bool>> MaraktheBloodedEncounter()
	    {

		    return async boss =>
		    {
			    return false;
		    };
	    }

	    #endregion

	    #endregion
	}
}