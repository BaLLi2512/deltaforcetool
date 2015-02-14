using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Helpers;
using Buddy.Coroutines;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Bars;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

// ReSharper disable CheckNamespace
namespace Bots.DungeonBuddy.DungeonScripts.WarlordsOfDraenor
// ReSharper restore CheckNamespace
{
	#region Normal Difficulty

	public class GrimrailDepot : Dungeon
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 822; }
		}

	    public override WoWPoint ExitLocation
	    {
            get { return new WoWPoint(1741.643, 1681.076, 7.678281); }
	    }

	    public override WoWPoint Entrance
	    {
            get { return new WoWPoint(7882.053, 565.3205, 123.8254); }
	    }

	    public override void RemoveTargetsFilter(List<WoWObject> units)
		{
			units.RemoveAll(
				ret =>
				{
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
	        var isDps = Me.IsDps();
		    var isRangedDps = isDps && Me.IsRange();
			foreach (var priority in units)
			{
				var unit = priority.Object as WoWUnit;
				if (unit != null)
				{
				    switch (unit.Entry)
				    {
                        case MobId_BorkatheBrute:
                            priority.Score += 3500;
                            break; 
                        case MobId_GrimrailTechnician:
                            if (isDps)
                                priority.Score += 3500;
                            break;
                        case MobId_GromkarGrenadier:
                        case MobId_GromkarCinderseer:
                            if (isDps)
                                priority.Score += 4000;
                            break;
                        case MobId_AssaultCannon:
				            priority.Score -= 4500;
                            break;
                        case MobId_GromkarGunner:
                        case MobId_GromkarBoomer:
                            if (isDps)
                                priority.Score += 4500;
                            break;
				    }

					if (unit.Entry == MobId_BorkatheBrute && isDps)
					{
						if (ScriptHelpers.IsViable(_borka) && ScriptHelpers.IsViable(_rocketspark) && _rocketspark.IsAlive)
						{
							priority.Score = _borka.HealthPercent - _rocketspark.HealthPercent;
						}
					}

					// try to take Borka and Rocketspark's health at the same time.
					if (unit.Entry == MobId_BorkatheBrute && isDps && ScriptHelpers.IsViable(_rocketspark) && _rocketspark.IsAlive)
					{
						priority.Score = _borka.HealthPercent - _rocketspark.HealthPercent;
					}

					if (unit.Entry == MobId_RailmasterRocketspark && isDps && ScriptHelpers.IsViable(_borka) && _borka.IsAlive)
					{
						if (!isRangedDps && unit.ZDiff > 4)
							priority.Score = -400;
						else
							priority.Score = _rocketspark.HealthPercent - _borka.HealthPercent;
					}
				}
			}
		}

	    public override void RemoveLootTargetsFilter(List<WoWObject> objects)
	    {
	        objects.RemoveAll(
	            obj =>
	            {
	                var unit = obj as WoWUnit;
	                if (unit != null)
	                {
                        // Don't try to loot the last boss until after zoning, 
                        // otherwise he'll get auto-blackisted because of not being able to loot for too long.
	                    if (unit.Entry == MobId_SkylordTovra && unit.Location.DistanceSqr(_skylordFinalCorpseLoc) > 5*5)
	                        return true;
	                }
	                return false;
	            });
	    }

	    public override async Task<bool> HandleMovement(WoWPoint location)
	    {
	        if (ScriptHelpers.CurrentScenarioInfo.CurrentStageNumber == 2)
	        {
	            var myLoc = Me.Location;
                // under no circumstance should the bot move back over the burning rail car after crossing it... 
	            if (myLoc.Y >= 1930 && location.Y <= 1930)
	                return true;
	        }
	        return false;
	    }

	    public override bool IsComplete
	    {
	        get
	        {
                // Wait for player to get ported to last area before considering
                // dungeon to be complete otherwise bot might leave without looting boss.
	            return base.IsComplete && Me.Location.DistanceSqr(_skylordFinalCorpseLoc) < 120 * 120;
	        }
	    }

	    #endregion
		
		private static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		#region Root
        [EncounterHandler(0, "Root Handler")]
        public Func<WoWUnit, Task<bool>> RootBehavior()
        {
            return async npc => await ScriptHelpers.CancelCinematicIfPlaying();
        }
		#endregion

        #region Rocketspark and Borka

        #region Trash

        private const uint MobId_GrimrailOverseer = 81212;
        private const uint MobId_GrimrailTechnician = 81236;
	    private const int SpellId_Dash = 164168;

	    [EncounterHandler((int) MobId_GrimrailOverseer, "Grimrail Overseer")]
	    public Func<WoWUnit, Task<bool>> GrimrailOverseerEncounter()
	    {
            AddAvoidObject(
                ctx => true,
                3,
                o => o.Entry == MobId_GrimrailOverseer && o.ToUnit().CastingSpellId == SpellId_Dash && o.ToUnit().CurrentTargetGuid != Me.Guid,
                o => Me.Location.GetNearestPointOnSegment(o.Location.RayCast(o.Rotation, 2), o.Location.RayCast(o.Rotation, 30)));

	        return async npc =>
	        {
                // run towards exit when NPC casts dash to prevent it from running into other packs and pulling more.
	            var runForExit = Me.IsTank() && npc.CastingSpellId == SpellId_Dash 
                    && ExitLocation.DistanceSqr(Me.Location) > 30 * 30
	                && ScriptHelpers.GetUnfriendlyNpsAtLocation(Me.Location, 40, u => !u.Combat && u.IsAlive).Any();
                
                if (runForExit)
	                return (await CommonCoroutines.MoveTo(ExitLocation)).IsSuccessful();
	            return false;
	        };
	    }
	    #endregion

	    private const int SpellId_MadDash = 161090;
		private const int SpellId_Slam = 161087;
        private const uint MobId_RailmasterRocketspark = 77803;
        private const uint MobId_BorkatheBrute = 77816;

	    private readonly int[] MissileSpellIds_VX18BTargetEliminator = {162494, 162509};
		private WoWUnit _rocketspark, _borka;
        // http://www.wowhead.com/guide=2666/grimrail-depot-dungeon-strategy-guide#rocketspark-and-borka
        [EncounterHandler((int)MobId_RailmasterRocketspark, "Railmaster Rocketspark")]
        public Func<WoWUnit, Task<bool>> RailmasterRocketsparkEncounter()
        {
	        AddAvoidLocation(
		        ctx => true,
		        3,
		        o => ((WoWMissile) o).ImpactPosition,
		        () => WoWMissile.InFlightMissiles.Where(m => MissileSpellIds_VX18BTargetEliminator.Contains(m.SpellId)));

	        return async boss =>
			{
				_rocketspark = boss;
				return false;
			};
        }

        [EncounterHandler((int)MobId_BorkatheBrute, "Borka the Brute")]
        public Func<WoWUnit, Task<bool>> BorkatheBruteEncounter()
        {
            var tankLoc = new WoWPoint(1718.657, 1542.948, 7.71374);
            AddAvoidObject(
                ctx => true,
                7,
                o => o.Entry == MobId_BorkatheBrute && o.ToUnit().CastingSpellId == SpellId_MadDash && o.ToUnit().CurrentTargetGuid != Me.Guid,
                o => Me.Location.GetNearestPointOnSegment(o.Location.RayCast(o.Rotation, 4), o.Location.RayCast(o.Rotation, 40)));

            // force range to stay away since they sometimes end up stacking on boss after avoiding missiles
            AddAvoidObject(ctx => Me.IsRange(), 10, o => o.Entry == MobId_BorkatheBrute && o.ToUnit().Combat);

	        var madDashAimLocation = new PerFrameCachedValue<WoWPoint>(
		        () =>
				{
					if (!ScriptHelpers.IsViable(_borka) || !ScriptHelpers.IsViable(_rocketspark))
						return WoWPoint.Zero;

					if (_borka.CastingSpellId != SpellId_MadDash || _borka.CurrentTargetGuid != Me.Guid)
						return WoWPoint.Zero;

					var start = WoWMathHelper.CalculatePointFrom(_rocketspark.Location, _borka.Location, 3);
					var end = WoWMathHelper.CalculatePointFrom(_rocketspark.Location, _borka.Location, _borka.MeleeRange());

					WoWPoint traceHit;
					var traceRet = Avoidance.Helpers.MeshTraceline(start, end, out traceHit);

					if (!traceRet.HasValue)
						return WoWPoint.Zero;

					if (traceRet.Value)
						end = traceHit;

					return Me.Location.GetNearestPointOnSegment(start, end);
				});

            return async boss =>
			{
				_borka = boss;

				if (boss.HealthPercent >= 97 && boss.HealthPercent > 50 && await ScriptHelpers.CastHeroism())
					return true;

				// Aim the mad dash at Rocket Spark to caue him to get stunned.
				if (madDashAimLocation != WoWPoint.Zero)
				{
					return await ScriptHelpers.StayAtLocationWhile(() => madDashAimLocation != WoWPoint.Zero, madDashAimLocation, null, 1);
				}

				if (boss.CastingSpellId == SpellId_Slam && Me.PowerType == WoWPowerType.Mana 
					&& boss.CurrentCastTimeLeft < TimeSpan.FromMilliseconds(500) && Me.IsCasting)
				{
					SpellManager.StopCasting();
					Logger.Write("Canceling cast to avoid getting interupted by Slam");
					await Coroutine.Sleep(boss.CurrentCastTimeLeft);
					return true;
				}

				return false;
			};
        }

        #endregion

	    #region Nitrogg Thundertower

	    #region Trash
		
		private const int MissileSpellId_BlackrockBomb=164187;
	    private const int SpellId_ShrapnelBlast = 166675;

	    private const uint MobId_GromkarGrenadier_Trash = 80936;
	    private const uint MobId_GromkarGunner = 80937;
	    private const uint MobId_GromkarBoomer_Trash = 80935;
	    private const uint MobId_GromkarCinderseer = 88163;
		private const uint MobId_GrimrailBombardier=81407;


	    [EncounterHandler((int) MobId_GromkarGrenadier_Trash, "Grom'kar Grenadier Trash")]
	    public Func<WoWUnit, Task<bool>> GromkarGrenadierEncounter()
	    {
	        return async npc => { return false; };
	    }

		[EncounterHandler((int)MobId_GrimrailBombardier, "Grimrail Bombardier")]
		public Func<WoWUnit, Task<bool>> GrimrailBombardierEncounter()
		{
			AddAvoidLocation(
				ctx => true,
				5,
				m => ((WoWMissile) m).ImpactPosition,
				() => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_BlackrockBomb && Math.Abs(Me.Z - m.ImpactPosition.Z) < 5));
			return async npc => { return false; };
		}

	    [EncounterHandler((int) MobId_GromkarGunner, "Grom'kar Gunner")]
	    public Func<WoWUnit, Task<bool>> GromkarGunnerEncounter()
	    {
            // side-step the Shrapnel Blast abiltiy
	        AddAvoidObject(
	            ctx => true,
	            4,
	            o => o.Entry == MobId_GromkarGunner && o.ToUnit().HasAura("Shrapnel Blast") ,
	            o => Me.Location.GetNearestPointOnSegment(o.Location, o.Location.RayCast(o.Rotation, 30)));

            // turn the NPC away from group when it's starting the Shrapnel Blast cast.
            return async npc => Me.CurrentTarget == npc 
                && npc.CastingSpellId == SpellId_ShrapnelBlast 
                && !npc.HasAura("Shrapnel Blast")
                && await ScriptHelpers.TankFaceUnitAwayFromGroup(npc, 30);
	    }

	    [EncounterHandler((int) MobId_GromkarBoomer_Trash, "Grom'kar Boomer Trash")]
	    public Func<WoWUnit, Task<bool>> GromkarBoomerEncounter()
	    {
	        return async npc => { return false; };
	    }

	    [EncounterHandler((int) MobId_GromkarCinderseer, "Grom'kar Cinderseer")]
	    public Func<WoWUnit, Task<bool>> GromkarCinderseerEncounter()
	    {
            // Need to run away from other playes to cause debuf to drop.
            AddAvoidObject(ctx => true, 10, o => o is WoWPlayer && !o.IsMe
                && (o.ToPlayer().HasAura("Lava Wreath") || Me.HasAura("Lava Wreath")));
	        return async boss => false;
	    }
        
        [ScenarioStage(2, "Board the Grimrail", 1)]
        public Func<ScenarioStage, Task<bool>> BoardLogicGrimrailLogic()
        {
            var stageLoc = new WoWPoint(1680.294f, 1561.311f, 54.72749f);
            return async boss => ScriptHelpers.SetLeaderMoveToPoi(stageLoc);
        }

	    #endregion

	    private const int SpellId_BlackrockGrenade = 161060;
	    private const int SpellId_Reloading = 160680;

	    private const int MissileSpellId_BlackrockGrenade = 163539;
	    private const int MissileSpellId_BlackrockMortar = 163541;

	    private const uint MobId_NitroggThundertower = 79545;
        private const uint MobId_GromkarBoomer = 79720;
        private const uint MobId_GromkarGrenadier = 79739;
        private const uint MobId_AssaultCannon = 79548;
        private const uint MobId_BlackrockTurret = 82721;

        private const uint GameObjectId_AssaultDoor = 232131;

	    [EncounterHandler((int) MobId_NitroggThundertower, "Nitrogg Thundertower")]
	    public Func<WoWUnit, Task<bool>> NitroggThundertowerEncounter()
	    {
            var suppressiveFireLosLoc = new WoWPoint(1636.23,1829.66, 107.716f);

            var eastSectionLoc = new WoWPoint (1647.201, 1796.381, 107.4893);
            var centerSectionLoc = new WoWPoint(1647.205, 1817.852, 107.4129);
            var westSectionLoc = new WoWPoint(1647.667, 1840.345, 107.6347);

	        var sectionLocations = new WoWPoint[] {eastSectionLoc, centerSectionLoc, westSectionLoc};

	        AddAvoidLocation(
	            ctx => true,
	            3,
	            o => ((WoWMissile) o).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_BlackrockGrenade));

	        AddAvoidLocation(
	            ctx => true,
	            4,
	            o => ((WoWMissile) o).ImpactPosition,
	            () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_BlackrockMortar));

            var targetedBySupressiveFire = new PerFrameCachedValue<bool>(
                () => Me.HasAura("Suppressive Fire") || ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Any( u => u.Entry == MobId_AssaultCannon && u.CastingSpellId == SpellId_Reloading 
                        && u.CurrentTargetGuid == Me.Guid));

	        return async boss =>
	        {
                var assaultDoor = ObjectManager.GetObjectsOfType<WoWGameObject>()
                .Where(g => g.Entry == GameObjectId_AssaultDoor)
                .Select(g => (WoWDoor)g.SubObj).FirstOrDefault();

                if (assaultDoor != null && assaultDoor.IsClosed && !Me.IsHealer() && assaultDoor.OwnerObject.DistanceSqr <= 16 * 16)
                {
                    TreeRoot.StatusText = "Waiting on assault door to open";
                    return true;
                }

	            // Phase 2 logic
                if (assaultDoor == null || assaultDoor.IsClosed)
	                return false;

	            if (await HandleTurret(boss))
	                return true;

                if (targetedBySupressiveFire)
	            {
	                return await ScriptHelpers.StayAtLocationWhile(
                        () => targetedBySupressiveFire,
	                    suppressiveFireLosLoc,
	                    "LOSing Suppressive Fire",
	                    0.9f);
	            }

	            if (await HandleGrenade())
	                return true;

	            // we need to loot the corpses of Grenadier and Boomers for their exposives.
	            if (!Me.IsHealer() && await LootExplosivesOnCorpse())
	                return true;

	            if (Me.IsTank())
	            {
	                if (Targeting.Instance.TargetList.All(g =>g.Entry == MobId_AssaultCannon || g.Aggro)
                        && await ScriptHelpers.TankUnitAtLocation(centerSectionLoc, 10))
	                {
	                    return true;
	                }
	            }
	            else if (Targeting.Instance.TargetList.Any(g => g.Aggro))
	            {
	                // move to tank if something aggroed.
	                var tank = ScriptHelpers.Tank;
	                if (tank != null && tank.DistanceSqr > 10*10)
	                {
	                    return
	                        await
	                            ScriptHelpers.MoveToContinue(
	                                () => tank.Location,
	                                () => ScriptHelpers.IsViable(tank));
	                }
	            }

	            if (Me.IsRange())
	            {
                    var standAtLoc = boss.HasAura("Mount Turret")
                        ? centerSectionLoc
                        : sectionLocations.OrderBy(l => l.DistanceSqr(boss.Location)).First();

	                return await ScriptHelpers.StayAtLocationWhile(
	                    () => LootExplosive == null,
                        standAtLoc,
	                    "Kill position",
	                    8);
	            }
	            return false;
	        };
	    }

	    private async Task<bool> HandleTurret(WoWUnit boss)
	    {
	        var shootBoss = boss.HasAura("Mount Turret");
            // Turret Behavior
            var transport = Me.Transport;
            if (transport != null && transport.Entry == MobId_BlackrockTurret)
            {
                if (!shootBoss)
                {
                    Lua.DoString("VehicleExit()");
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    return true;
                }

                TreeRoot.StatusText = "Blasting turret at boss";
                var button = ActionBar.Active.Buttons.FirstOrDefault();
                if (button != null && button.CanUse && !Me.IsCasting)
                {
                    button.Use();
                    await CommonCoroutines.SleepForLagDuration();
                    return true;
                }
            }

            if (!shootBoss)
                return false;

            // Get in Turret
            var turret = ObjectManager.GetObjectsOfType<WoWUnit>()
                .Where(u => u.Entry == MobId_BlackrockTurret && u.CanInteractNow)
                .OrderBy(u => u.DistanceSqr).FirstOrDefault();

            // Get in turret...
            if (turret != null )
            {
                if (!turret.WithinInteractRange)
                    return (await CommonCoroutines.MoveTo(turret.Location, "turret")).IsSuccessful();
                turret.Interact();
                return true;
            }
	        return false;
	    }

	    private async Task<bool> HandleGrenade()
	    {
            // blackrock grenade has a aoe damage plus knockback.
            if (ActionBar.Extra.IsActive && !Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.DistanceSqr <= 20 * 20)
            {
                var extraButton = ActionBar.Extra.Buttons.FirstOrDefault(b => b.Id == SpellId_BlackrockGrenade);

                if (extraButton != null && extraButton.CanUse)
                {
                    Logger.Write("Using {0}", extraButton);
                    extraButton.Use();
                    SpellManager.ClickRemoteLocation(Targeting.Instance.FirstUnit.Location);
                    return true;
                }
            }
	        return false;
	    }

        [LocationHandler(1647.707, 1805.648, 109.0918, 60, "Windy Rail Car")]
	    public Func<WoWPoint,Task<bool>> HandleWindyRailCar()
	    {
            var southSafeLoc = new WoWPoint (1637.696, 1780.395, 108.0132);
            var northSafeLoc = new WoWPoint (1654.123, 1792.469, 108.0255);

	        return async loc =>
	        {
	            var myLoc = Me.Location;
	            if (!IsOnWindyFlap(myLoc))
	                return false;
                // the pushback from the wind while standing on the flap causes stuck handler to trigger.
                Navigator.NavigationProvider.StuckHandler.Reset();
                
	            var tank =  ScriptHelpers.Tank;
	            var isTank = tank != null && tank.IsMe;

                var objectiveIsOnWindyFlap = tank != null && IsOnWindyFlap(tank.Location);

	            if (myLoc.Y < 1779)
	            {
	                WoWPoint moveTo;
	                if (!objectiveIsOnWindyFlap )
	                {
	                    moveTo = WoWMathHelper.GetRandomPointInCircle(myLoc.X > 1645 ? northSafeLoc : southSafeLoc, 2.5f);
	                }
	                else
	                {
                        moveTo = myLoc;
	                    moveTo.Y = StyxWoW.Random.Next(1785, 1796);
	                }

                    return await ScriptHelpers.MoveToContinue(
                        () => moveTo,
                        ignoreCombat: true,
                        name: "Moving forward to prevent falling off railcar edge");
	            }

                // get off the flap when tank is off the flap.
                if ((!Me.Combat || Me.IsRange()) && !isTank && tank != null && tank.DistanceSqr < 45 * 45 && !objectiveIsOnWindyFlap)
                    return (await CommonCoroutines.MoveTo(tank.Location)).IsSuccessful();
	            return false;
	        };
	    }

	    private bool IsOnWindyFlap(WoWPoint loc)
	    {
            if (loc.X < 1658 && loc.X > 1635 || loc.Y < 1769 || loc.Y > 1848)
                return false;
	        return true;
	    }

		private PerFrameCachedValue<WoWUnit> _lootExplosive;

		private WoWUnit LootExplosive
		{
			get
			{
				return _lootExplosive ?? (_lootExplosive = new PerFrameCachedValue<WoWUnit>(() =>
						{
							// only dps should loot the boomers.
							var isTank = Me.IsTank();
							return ObjectManager.GetObjectsOfType<WoWUnit>()
								.Where(
									u => (u.Entry == MobId_GromkarGrenadier || u.Entry == MobId_GromkarBoomer && !isTank)
										 && u.IsDead && u.HasAura("Blackrock Munitions"))
								.OrderBy(u => u.DistanceSqr)
								.FirstOrDefault();
						}));
			}
		}

	    private async Task<bool> LootExplosivesOnCorpse()
	    {
			if (Me.HasAura("Blackrock Mortar Shells"))
				return false;

            if (LootExplosive == null)
	            return false;

            TreeRoot.StatusText = "Looting " + LootExplosive.SafeName;

            if (!LootExplosive.WithinInteractRange)
                return (await CommonCoroutines.MoveTo(LootExplosive.Location)).IsSuccessful();

            LootExplosive.Interact();
	        await CommonCoroutines.SleepForRandomReactionTime();
	        return true;
	    }

	    #endregion

        #region Skylord Tovra

	    #region Trash

        private const uint MobId_GromkarFarSeer = 82579;
	    private const int AreaTriggerId_ThunderZone = 7352;

	
        [EncounterHandler((int)MobId_GromkarFarSeer, "Gromkar FarSeer")]
        public Func<WoWUnit, Task<bool>> GromkarFarSeerEncounter()
	    {
            AddAvoidObject(ctx => true, 6.5f, AreaTriggerId_ThunderZone);

	        return async npc => false;
	    }

	    #endregion

        #region Flaming Railcar

        [LocationHandler(1646.561, 1872.268, 107.7906, 40, "Burning Rail Car")]
        public Func<WoWPoint, Task<bool>> HandleBurningRailCar()
        {
            DateTime northStartTime = DateTime.MinValue;
            DateTime southStartTime = DateTime.MinValue;

            var southMidLoc = new WoWPoint(1638.535, 1895.036, 107.776);
            var northMidLoc = new WoWPoint(1655.452, 1897.461, 107.7816);
            var endLoc = new WoWPoint(1646.631, 1933.668, 106.9329);
            return async loc =>
            {
                var stage = ScriptHelpers.CurrentScenarioInfo.CurrentStage;
                if (stage.StageNumber != 2 || !stage.GetStep(2).IsComplete)
                    return false;

                var myLoc = Me.Location;

                if (!IsInFlamingRailCar(myLoc))
                    return false;

                UpdateBurningRailCarTimers(ref northStartTime, ref southStartTime);


                bool takeNorthSide = false, takeSouthSide = false;

                if (northStartTime != DateTime.MinValue)
                {
                    var now = DateTime.Now;
                    var timeTillNorthBurns = (northStartTime - now).TotalMilliseconds;
                    var timeTillSouthBurns = (southStartTime - now).TotalMilliseconds;

                    // Take north side if there's more than 5 seconds left before it starts burning or it's about to stop burning.
                    if (timeTillNorthBurns > 6000 || timeTillNorthBurns < -7000)
                        takeNorthSide = true;
                    else if (timeTillSouthBurns > 6000 || timeTillSouthBurns < -7000)
                        takeSouthSide = true;

                    if (takeNorthSide || takeSouthSide)
                    {
                        TreeRoot.StatusText = "Moving across burning railcar";
                        // we need to move to a mid point before moving on to end point to make sure navigator takes correct side.
                        // we use random points to prevent stacking of bots. 
                        var rndMidLoc = WoWMathHelper.GetRandomPointInCircle(takeNorthSide ? northMidLoc:southMidLoc, 2);
                        await ScriptHelpers.MoveToContinue(() => rndMidLoc);
                        var rndEndLoc = WoWMathHelper.GetRandomPointInCircle(endLoc, 2);
                        await ScriptHelpers.MoveToContinue(() => rndEndLoc);
                        return true;
                    }
                }

                if (!ScriptHelpers.AtLocation(Me.Location, loc, 3))
                {
                    var rndWaitLoc = WoWMathHelper.GetRandomPointInCircle(loc, 3);
                    await ScriptHelpers.MoveToContinue(() => rndWaitLoc);
                }

                TreeRoot.StatusText = "Waiting for an opportunity to cross burning railcar";
                return true;
            };
        }

        private readonly WoWPoint _northSideSlagStartLoc = new WoWPoint(1657.46, 1896.96, 107.755);
        private readonly WoWPoint _northSideLastSlagLoc = new WoWPoint(1655.1, 1871.33, 107.791);

        private readonly WoWPoint _southSideSlagStartLoc = new WoWPoint(1635.786, 1899.592, 107.7553);
        private readonly WoWPoint _southSideLastSlagLoc = new WoWPoint(1639.929, 1870.564, 107.7906);

        // Returns the burn start times for each side. 
        // Start times are always < DateTime.Now if side is burning and > DateTime.Now (future time) if not burning.
        private void UpdateBurningRailCarTimers(ref DateTime northSideBurnStart, ref DateTime southSideBurnStart)
        {
            foreach (var slag in ObjectManager.GetObjectsOfType<WoWAreaTrigger>()
                .Where(a => a.Entry == AreaTriggerId_SlagTanker))
            {
                // first area trigger dropped on north side. 
                if (slag.Location.Distance2DSqr(_northSideSlagStartLoc) < 1 * 1)
                {
                    UpdateBurningRailCarTimersFromFirstSlag(slag, out northSideBurnStart, out southSideBurnStart);
                    return;
                }

                // last area trigger dropped on north side. Last area trigger spawns roughly 4100 ms after first one.
                if (slag.Location.Distance2DSqr(_northSideLastSlagLoc) < 1 * 1)
                {
                    UpdateBurningRailCarTimersFromLastSlag(slag, out northSideBurnStart, out southSideBurnStart);
                    return;
                }

                // first area trigger dropped on south side. 
                if (slag.Location.Distance2DSqr(_southSideSlagStartLoc) < 1 * 1)
                {
                    UpdateBurningRailCarTimersFromFirstSlag(slag, out southSideBurnStart, out northSideBurnStart);
                    return;
                }

                // last area trigger dropped on south side. Last area trigger spawns roughly 4100 ms after first one.
                if (slag.Location.Distance2DSqr(_southSideLastSlagLoc) < 1 * 1)
                {
                    UpdateBurningRailCarTimersFromLastSlag(slag, out southSideBurnStart, out northSideBurnStart);
                    return;
                }
            }
        }

        private void UpdateBurningRailCarTimersFromFirstSlag(WoWAreaTrigger slag, out DateTime time1, out DateTime time2)
        {
            time1 = DateTime.Now - (slag.Duration - slag.TimeLeft) - TimeSpan.FromMilliseconds(StyxWoW.WoWClient.Latency);
            time2 = time1 + TimeSpan.FromMilliseconds(7500);
        }

        private void UpdateBurningRailCarTimersFromLastSlag(WoWAreaTrigger slag, out DateTime time1, out DateTime time2)
        {
            time1 = DateTime.Now - (slag.Duration - slag.TimeLeft) - TimeSpan.FromMilliseconds(StyxWoW.WoWClient.Latency + 4100);
            time2 = time1 + TimeSpan.FromMilliseconds(7500);
        }


        private bool IsInFlamingRailCar(WoWPoint loc)
        {
            if (loc.Y < 1865 || loc.Y > 1930 || loc.X > 1660 || loc.X < 1632)
                return false;
            return true;
        }

        #endregion

	    private const int SpellId_SpinningSpear = 162058;

        private const uint MobId_SkylordTovra = 80005;
        private const uint AreaTriggerId_SlagTanker = 7415;
        private const uint AreaTriggerId_DiffusedEnergy = 6864;
        private const uint AreaTriggerId_FreezingSnare = 6907;
	    private const int MissileSpellId_FreezingSnare = 162080;
	    private readonly WoWPoint _skylordFinalCorpseLoc = new WoWPoint(-2391.91, -1829.73, 9.60577);
        [EncounterHandler((int)MobId_SkylordTovra, "Skylord Tovra")]
        public Func<WoWUnit, Task<bool>> SkylordTovraEncounter()
        {
            WoWUnit boss = null;
            var eastGateAvoidLoc = new WoWPoint(1646.669, 1940.006, 106.9087);
            var westGateAvoidLoc = new WoWPoint(1647.193, 2009.193, 107.6591);
            // make range avoid this boss to avoid stacking on him.
            AddAvoidObject(ctx => Me.IsRange(), 8f, o => o.Entry == MobId_SkylordTovra && o.ToUnit().Combat);

            AddAvoidObject(ctx => true, 6.5f, AreaTriggerId_DiffusedEnergy);
            AddAvoidObject(ctx => true, 4, AreaTriggerId_FreezingSnare);

            AddAvoidLocation(ctx => ScriptHelpers.IsViable(boss) && boss.Combat, 10, o => eastGateAvoidLoc);
            AddAvoidLocation(ctx => ScriptHelpers.IsViable(boss) && boss.Combat, 10, o => westGateAvoidLoc);

            WoWPoint spinningSpearLoc = WoWPoint.Zero;
            var spinningSpearTimer = new WaitTimer(TimeSpan.FromSeconds(4));

            // Avoids the spinning spear.
            // Boss picks a target and then throws spear at the location of the target before cast started.
            AddAvoidObject(
                ctx => true,
                2,
                o =>
                    o.Entry == MobId_SkylordTovra && o.ToUnit().CastingSpellId == SpellId_SpinningSpear 
                    && o.ToUnit().CurrentTargetGuid != WoWGuid.Empty,
                o =>
                {
                    if (spinningSpearTimer.IsFinished)
                    {
                        spinningSpearLoc = o.ToUnit().CurrentTarget.Location;
                        spinningSpearTimer.Reset();
                    }
                    return spinningSpearLoc;
                });

            AddAvoidLocation(
                ctx => true,
                4,
                o => ((WoWMissile)o).ImpactPosition,
                () => WoWMissile.InFlightMissiles.Where(m => m.SpellId == MissileSpellId_FreezingSnare)); 

            return async npc =>
            {
                boss = npc;
                return false;
            };
        }

        #endregion

    }

	#endregion

	#region Heroic Difficulty

    public class GrimrailDepotHeroic : GrimrailDepot
	{
		#region Overrides of Dungeon

		public override uint DungeonId
		{
			get { return 858; }
		}

		#endregion
	}

	#endregion
}