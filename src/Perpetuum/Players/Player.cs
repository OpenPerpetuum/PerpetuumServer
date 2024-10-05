using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Collections.Spatial;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.CombatLogs;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PlantTools;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.Players
{
    public sealed class Player : Robot, IBlobableUnit, IBlobEmitter
    {
        private readonly IExtensionReader extensionReader;
        private readonly ICorporationManager corporationManager;
        private readonly MissionHandler.Factory missionHandlerFactory;
        private readonly ITeleportStrategyFactories teleportStrategyFactories;
        private readonly DockingBaseHelper dockingBaseHelper;
        private readonly CombatLogger.Factory combatLoggerFactory;
        private readonly IBlobEmitter blobEmitter;
        private readonly BlobHandler<Player> blobHandler;
        private readonly PlayerMovement movement;
        private readonly IntervalTimer combatTimer = new IntervalTimer(TimeSpan.FromSeconds(10));
        private readonly GlobalConfiguration globalConfiguration;
        private CombatLogger combatLogger;
        private PlayerMoveCheckQueue check;
        private CancellableDespawnHelper despawnHelper;

        public static readonly TimeSpan NormalUndockDelay = TimeSpan.FromSeconds(7);
        public static readonly TimeSpan BlessedUndockDelay = TimeSpan.FromMinutes(3); // Supposed to be 15
        public const int ARKHE_REQUEST_TIMER_MINUTES_PVP = 3;
        public const int ARKHE_REQUEST_TIMER_MINUTES_NPC = 1;

        private readonly Dictionary<CategoryFlags, int> baseTokenPrices = new Dictionary<CategoryFlags, int>
        {
            { CategoryFlags.cf_runners, 50 },
            { CategoryFlags.cf_nuimqol_runners, 50 },
            { CategoryFlags.cf_pelistal_runners, 50 },
            { CategoryFlags.cf_thelodica_runners, 50 },
            { CategoryFlags.cf_industrial_runners, 50 },
            { CategoryFlags.cf_crawlers, 150 },
            { CategoryFlags.cf_nuimqol_crawlers, 150 },
            { CategoryFlags.cf_pelistal_crawlers, 150 },
            { CategoryFlags.cf_thelodica_crawlers, 150 },
            { CategoryFlags.cf_industrial_crawlers, 150 },
            { CategoryFlags.cf_mechs, 500 },
            { CategoryFlags.cf_nuimqol_mechs, 500 },
            { CategoryFlags.cf_pelistal_mechs, 500 },
            { CategoryFlags.cf_thelodica_mechs, 500 },
            { CategoryFlags.cf_industrial_mechs, 500 },
            { CategoryFlags.cf_heavy_mechs, 2000 },
            { CategoryFlags.cf_nuimqol_heavymechs, 2000 },
            { CategoryFlags.cf_pelistal_heavymechs, 2000 },
            { CategoryFlags.cf_thelodica_heavymechs, 2000 },
            { CategoryFlags.cf_industrial_heavymechs, 2000 },
            { CategoryFlags.cf_walkers, 5000 },
        };

        private bool HasAggressorEffect => EffectHandler.ContainsEffect(EffectType.effect_aggressor);

        public long CorporationEid { get; set; }

        public IZoneSession Session { get; private set; }

        public Character Character { get; set; } = Character.None;

        public bool HasGMStealth { get; set; }

        public MissionHandler MissionHandler { get; private set; }

        public bool HasSelfTeleportEnablerEffect => EffectHandler.ContainsEffect(EffectType.effect_teleport_self_enabler);

        public Gang Gang { get; set; }

        public bool IsInSafeArea
        {
            get
            {
                IZone zone = Zone;

                return zone != null
&& (zone.Configuration.Protected ||
                    EffectHandler.ContainsEffect(EffectType.effect_syndicate_area) ||
                    EffectHandler.ContainsEffect(EffectType.effect_safe_spot));
            }
        }

        public override bool IsLockable
        {
            get
            {
                bool isInvulnerable = IsInvulnerable;

                return !isInvulnerable && base.IsLockable;
            }
        }

        public IBlobHandler BlobHandler => blobHandler;

        public double BlobEmission => blobEmitter.BlobEmission;

        public double BlobEmissionRadius => blobEmitter.BlobEmissionRadius;

        public Player(
            IExtensionReader extensionReader,
            ICorporationManager corporationManager,
            MissionHandler.Factory missionHandlerFactory,
            ITeleportStrategyFactories teleportStrategyFactories,
            DockingBaseHelper dockingBaseHelper,
            CombatLogger.Factory combatLoggerFactory,
            GlobalConfiguration globalConfiguration)
        {
            this.extensionReader = extensionReader;
            this.corporationManager = corporationManager;
            this.missionHandlerFactory = missionHandlerFactory;
            this.teleportStrategyFactories = teleportStrategyFactories;
            this.dockingBaseHelper = dockingBaseHelper;
            this.combatLoggerFactory = combatLoggerFactory;
            this.globalConfiguration = globalConfiguration;
            Session = ZoneSession.None;
            movement = new PlayerMovement(this);

            blobEmitter = new BlobEmitter(this);
            blobHandler = new BlobHandler<Player>(this);
        }

        public void EnableSelfTeleport(TimeSpan duration, int affectedZoneId = -1)
        {
            if (affectedZoneId != -1 && Zone.Id != affectedZoneId)
            {
                return;
            }

            ApplySelfTeleportEnablerEffect(duration);
        }

        public bool TryMove(Position position)
        {
            if (!IsWalkable(position))
            {
                return false;
            }

            check.EnqueueMove(position);
            CurrentPosition = position;

            return true;
        }

        public void SetSession(IZoneSession session)
        {
            Session = session;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void OnUpdateToDb()
        {
            try
            {
                if (!IsRepackaged)
                {
                    DynamicProperties.Update(k.armor, Armor.Ratio(ArmorMax));
                    DynamicProperties.Update(k.currentCore, Core);
                }

                IZone zone = Zone;

                if (zone == null || States.Dead)
                {
                    return;
                }

                Character character = Character;

                character.ZoneId = zone.Id;
                character.ZonePosition = CurrentPosition;

                IDynamicProperty<int> p = DynamicProperties.GetProperty<int>(k.pvpRemaining);
                Effect pvpEffect = EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();

                if (pvpEffect == null)
                {
                    p.Clear();

                    return;
                }

                IntervalTimer effectTimer = pvpEffect.Timer;

                if (effectTimer != null)
                {
                    p.Value = (int)effectTimer.Remaining.TotalMilliseconds;
                }
            }
            finally
            {
                base.OnUpdateToDb();
            }
        }

        public void SetStrongholdDespawn(TimeSpan time, UnitDespawnStrategy strategy)
        {
            if (despawnHelper == null)
            {
                despawnHelper = CancellableDespawnHelper.Create(this, time);
                despawnHelper.DespawnStrategy = strategy;
            }
        }

        public void ClearStrongholdDespawn()
        {
            despawnHelper?.Cancel(this);
            despawnHelper = null;
        }

        public void SendModuleProcessError(Module module, ErrorCodes error)
        {
            Packet packet = new Packet(ZoneCommand.ModuleEvaluateError);

            packet.AppendByte((byte)module.ParentComponent.Type);
            packet.AppendByte((byte)module.Slot);
            packet.AppendInt((int)error);
            Session.SendPacket(packet);
        }

        public void ApplyInvulnerableEffect()
        {
            RemoveInvulnerableEffect(); // Remove existing effect, set new
            EffectBuilder builder = NewEffectBuilder().SetType(EffectType.effect_invulnerable);
            _ = builder.WithDurationModifier(0.75); //Reduce span of syndicate protection
            ApplyEffect(builder);
        }

        public void RemoveInvulnerableEffect()
        {
            EffectHandler.RemoveEffectsByType(EffectType.effect_invulnerable);
        }

        public void ApplyTeleportSicknessEffect()
        {
            IZone zone = Zone;

            if (zone == null || zone is TrainingZone)
            {
                return;
            }

            EffectBuilder effectBuilder = NewEffectBuilder().SetType(EffectType.effect_teleport_sickness);

            if (HasPvpEffect)
            {
                _ = effectBuilder.WithDurationModifier(3.0);
            }

            ApplyEffect(effectBuilder);
        }

        public void ApplySelfTeleportEnablerEffect(TimeSpan duration)
        {
            Effect effect = EffectHandler.GetEffectsByType(EffectType.effect_teleport_self_enabler).FirstOrDefault();

            if (effect != null)
            {
                EffectHandler.Remove(effect);
            }

            EffectBuilder builder = NewEffectBuilder().SetType(EffectType.effect_teleport_self_enabler).WithDuration(duration);

            ApplyEffect(builder);
        }

        public void RemoveSelfTeleportEnablerEffect()
        {
            EffectHandler.RemoveEffectsByType(EffectType.effect_teleport_self_enabler);
        }

        public void CheckDockingConditionsAndThrow(long baseEid, bool checkRange = true)
        {
            if (!Session.AccessLevel.IsAdminOrGm())
            {
                HasAggressorEffect.ThrowIfTrue(ErrorCodes.NotAllowedForAggressors);
                HasPvpEffect.ThrowIfTrue(ErrorCodes.CantDockThisState);
                HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.CantDockThisState);
                EffectHandler.ContainsEffect(EffectType.effect_dreadnought);
            }

            IZone zone = Zone;

            if (zone == null)
            {
                return;
            }

            DockingBase dockingBase = dockingBaseHelper.GetDockingBase(baseEid);

            if (dockingBase == null)
            {
                return;
            }

            if (dockingBase.Zone == zone && checkRange)
            {
                dockingBase.IsInDockingRange(this).ThrowIfFalse(ErrorCodes.DockingOutOfRange);
            }

            AccessLevel currentAccess = Session.AccessLevel;

            if (!currentAccess.IsAdminOrGm())
            {
                _ = dockingBase.IsDockingAllowed(Character).ThrowIfError();
            }

            DockToBase(zone, dockingBase);
        }

        /// <summary>
        /// Make the docking happen
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="dockingBase"></param>
        public void DockToBase(IZone zone, DockingBase dockingBase)
        {
            States.Dock = true;

            PublicContainer publicContainer = dockingBase.GetPublicContainer();

            FullArmorRepair();

            publicContainer.AddItem(this, false);
            publicContainer.Save();
            dockingBase.DockIn(Character, NormalUndockDelay, ZoneExitType.Docked);

            Transaction.Current.OnCommited(() =>
            {
                RemoveFromZone();
                MissionHelper.MissionAdvanceDockInTarget(Character.Id, zone.Id, CurrentPosition);
                _ = TransportAssignment.DeliverTransportAssignmentAsync(Character);
            });
        }

        public ErrorCodes CheckPvp()
        {
            IZone zone = Zone;

            Debug.Assert(zone != null);

            return !HasPvpEffect && (zone.Configuration.Protected || EffectHandler.ContainsEffect(EffectType.effect_syndicate_area))
                ? ErrorCodes.PvpIsNotAllowed
                : ErrorCodes.NoError;
        }

        public bool IsInDefaultCorporation()
        {
            return DefaultCorporationDataCache.IsCorporationDefault(CorporationEid);
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            ItemPropertyModifier modifier = base.GetPropertyModifier(field);

            if (Character == Character.None)
            {
                return modifier;
            }

            CharacterExtensionCollection characterExtensions = Character.GetExtensions();
            System.Collections.Immutable.ImmutableDictionary<int, ExtensionInfo> extensions = extensionReader.GetExtensions();
            double extensionBonus = characterExtensions
                .Select(e => extensions[e.id])
                .Where(e => e.aggregateField == field)
                .Sum(e => characterExtensions.GetLevel(e.id) * e.bonus);

            extensionBonus += ExtensionBonuses
                .Where(e => e.aggregateField == field)
                .Sum(e => characterExtensions.GetLevel(e.extensionId) * e.bonus);

            if (!extensionBonus.IsZero())
            {
                ItemPropertyModifier m = ItemPropertyModifier.Create(field, extensionBonus);

                m.NormalizeExtensionBonus();
                m.Modify(ref modifier);
            }

            return modifier;
        }

        public override void OnAggression(Unit victim)
        {
            base.OnAggression(victim);
            AddInCombatWith(victim);

            if (victim is ITaggable taggable)
            {
                taggable.Tag(this, TimeSpan.Zero);
            }

            if (IsUnitPVPAggro(victim))
            {
                ApplyPvPEffect();

                return;
            }

            if (!(victim is Player victimPlayer))
            {
                return;
            }

            victimPlayer.Session.CancelLogout();
            ApplyPvPEffect();

            if (IsInSameCorporation(victimPlayer))
            {
                return;
            }

            if (HasPvpEffect && victimPlayer.HasPvpEffect)
            {
                return;
            }
        }

        public void OnPvpSupport(Unit target)
        {
            if (target is Player player && player.HasPvpEffect)
            {
                ApplyPvPEffect();
            }
        }

        public override string InfoString => $"Player:{Character.Id}:{Definition}:{Eid}";

        public void SendInitSelf()
        {
            IZone zone = Zone;

            Debug.Assert(zone != null, "zone != null");
            Session.SendPacket(EnterPacketBuilder);
            Session.SendTerrainData();
            zone.SendBeamsToPlayer(this, GridDistricts.All);

            IEnumerable<Packet> lockPackets = GetLockPackets();

            Session.SendPackets(lockPackets);

            foreach (IUnitVisibility visibility in GetVisibleUnits())
            {
                Session.SendPacket(visibility.Target.EnterPacketBuilder);

                if (!(visibility.Target is Robot robot))
                {
                    continue;
                }

                IEnumerable<Packet> unitLockPackets = robot.GetLockPackets();

                Session.SendPackets(unitLockPackets);
            }

            Session.SendPacket(new GangUpdatePacketBuilder(Visibility.Visible, zone.GetGangMembers(Gang)));
            Session.SendPacket(zone.Weather.GetCurrentWeather().CreateUpdatePacket());

            foreach (Effect effect in EffectHandler.Effects)
            {
                Session.SendPacket(new EffectPacketBuilder(effect, true));
            }

            foreach (ActiveModule module in ActiveModules)
            {
                module.ForceUpdate();
            }
        }

        public void WriteFQLog(string message)
        {
            LogEvent e = new LogEvent
            {
                LogType = LogType.Info,
                Tag = "FQ",
                Message = $"{InfoString} - {message}",
            };

            Logger.Log(e);
        }

        public void SendForceUpdate()
        {
            Session.SendPacket(new UnitUpdatePacketBuilder(this, UpdatePacketControl.ForceReposition));
        }

        public override void OnCombatEvent(Unit source, CombatEventArgs e)
        {
            base.OnCombatEvent(source, e);

            Player player = Zone.ToPlayerOrGetOwnerPlayer(source);

            if (player == null)
            {
                return;
            }

            CombatLogger logger = LazyInitializer.EnsureInitialized(ref combatLogger, CreateCombatLogger);

            logger.Log(player, e);
        }

        public static Player LoadPlayerAndAddToZone(IZone zone, Character character)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                Player player = (Player)character.GetActiveRobot().ThrowIfNull(ErrorCodes.ARobotMustBeSelected);

                DockingBase dockingBase = null;
                ZoneEnterType zoneEnterType;
                Position spawnPosition;

                if (character.IsDocked)
                {
                    zoneEnterType = ZoneEnterType.Undock;
                    dockingBase = character.GetCurrentDockingBase();
                    spawnPosition = UndockSpawnPositionSelector.SelectSpawnPosition(dockingBase);
                    character.ZoneId = zone.Id;
                    character.ZonePosition = spawnPosition;
                    character.IsDocked = false;
                }
                else
                {
                    zoneEnterType = ZoneEnterType.Teleport;
                    _ = zone.Id.ThrowIfNotEqual(character.ZoneId ?? -1, ErrorCodes.InvalidZoneId);

                    Position? zonePosition = character.ZonePosition.ThrowIfNull(ErrorCodes.InvalidPosition);

                    spawnPosition = (Position)zonePosition;
                }

                spawnPosition = zone.FixZ(spawnPosition);

                ClosestWalkablePositionFinder finder = new ClosestWalkablePositionFinder(zone, spawnPosition, player);
                Position validPosition = finder.FindOrThrow();

                ZoneStorage zoneStorage = zone.Configuration.GetStorage();

                player.Parent = zoneStorage.Eid;
                player.FullCoreRecharge();
                player.Save();

                Transaction.Current.OnCommited(() =>
                {
                    dockingBase?.LeaveChannel(character);
                    player.CorporationEid = character.CorporationEid;
                    zone.SetGang(player);
                    player.AddToZone(zone, validPosition, zoneEnterType);
                    player.ApplyInvulnerableEffect();
                });

                scope.Complete();

                return player;
            }
        }

        [CanBeNull]
        public Task TeleportToPositionAsync(Position target, bool applyTeleportSickness, bool applyInvulnerable)
        {
            IZone zone = Zone;

            if (zone == null)
            {
                return null;
            }

            TeleportWithinZone teleport = teleportStrategyFactories.TeleportWithinZoneFactory();

            if (teleport == null)
            {
                return null;
            }

            teleport.TargetPosition = target;
            teleport.ApplyTeleportSickness = applyTeleportSickness;
            teleport.ApplyInvulnerable = applyInvulnerable;

            Task task = teleport.DoTeleportAsync(this);

            return task?.LogExceptions();
        }

        public void SendStartProgressBar(Unit unit, TimeSpan timeout, TimeSpan start)
        {
            Dictionary<string, object> data = unit.BaseInfoToDictionary();

            data.Add(k.timeOut, (int)timeout.TotalMilliseconds);
            data.Add(k.started, (long)start.TotalMilliseconds);
            data.Add(k.now, (long)start.TotalMilliseconds);
            Message.Builder
                .SetCommand(Commands.AlarmStart)
                .WithData(data)
                .ToCharacter(Character)
                .Send();
        }

        public void SendEndProgressBar(Unit unit, bool success = true)
        {
            Dictionary<string, object> info = unit.BaseInfoToDictionary();

            info.Add(k.success, success);
            Message.Builder.SetCommand(Commands.AlarmOver).WithData(info).ToCharacter(Character).Send();
        }

        public void SendArtifactRadarBeam(Position targetPosition)
        {
            BeamBuilder builder = Beam.NewBuilder()
                .WithType(BeamType.artifact_radar)
                .WithSourcePosition(targetPosition)
                .WithTargetPosition(targetPosition)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(TimeSpan.FromSeconds(30));

            Session.SendBeam(builder);
        }

        public bool IsStandingMatch(long targetCorporationEid, double? standingLimit)
        {
            return corporationManager.IsStandingMatch(targetCorporationEid, CorporationEid, standingLimit);
        }

        public void ReloadContainer()
        {
            if (Transaction.Current != null)
            {
                Reload();
            }
            else
            {
                using (TransactionScope scope = Db.CreateTransaction())
                {
                    Reload();
                    scope.Complete();
                }
            }
        }

        public void UpdateCorporationOnZone(long newCorporationEid)
        {
            CorporationEid = newCorporationEid;

            Character[] playersOnZone = Zone.GetCharacters().ToArray();

            if (playersOnZone.Length <= 0)
            {
                return;
            }

            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {k.corporationEID, newCorporationEid},
                {k.characterID, Character.Id},
            };

            Message.Builder.SetCommand(Commands.CharacterUpdate).WithData(result).ToCharacters(playersOnZone).Send();
        }

        public override void UpdateVisibilityOf(Unit target)
        {
            target.UpdatePlayerVisibility(this);
        }

        public void SetCombatState(bool state)
        {
            States.Combat = state;
            combatTimer.Reset();

            if (state)
            {
                Session.ResetLogoutTimer();
            }
        }

        public void AddInCombatWith(Unit enemy)
        {
            SetCombatState(true);

            Player enemyPlayer = enemy as Player;

            enemyPlayer?.SetCombatState(true);
        }

        public bool IsInSameCorporation(Player player)
        {
            return (CorporationEid == player.CorporationEid) && !IsInDefaultCorporation();
        }

        public bool IsUnitPVPAggro(Unit unit)
        {
            return unit is MobileTeleport ||
                unit is IPBSObject ||
                unit is WallHealer ||
                unit is ProximityDeviceBase ||
                (unit is BlobEmitterUnit b && b.IsPlayerSpawned) ||
                (unit is RemoteControlledCreature remoteControlledCreature &&
                    remoteControlledCreature.CommandRobot is Player player &&
                    remoteControlledCreature.CommandRobot != player);
        }

        protected override void OnDead(Unit killer)
        {
            _ = HandlePlayerDeadAsync(Zone, killer).ContinueWith(t => base.OnDead(killer));
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();

            IZone zone = Zone;

            if (zone == null)
            {
                return;
            }

            MissionHandler?.MissionUpdateOnTileChange();

            TerrainControlInfo controlInfo = zone.Terrain.Controls.GetValue(CurrentPosition);

            ApplyHighwayEffect(controlInfo.IsAnyHighway);

            if (zone.Configuration.Protected)
            {
                return;
            }

            ApplySyndicateAreaEffect(controlInfo.SyndicateArea);
        }

        protected override void OnCellChanged(CellCoord lastCellCoord, CellCoord currentCellCoord)
        {
            base.OnCellChanged(lastCellCoord, currentCellCoord);

            IZone zone = Zone;

            if (zone == null)
            {
                return;
            }

            _ = Task.Run(() =>
            {
                GridDistricts district = currentCellCoord.ComputeDistrict(lastCellCoord);
                zone.SendBeamsToPlayer(this, district);
                Session.SendTerrainData();
            }).LogExceptions();
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            UpdateVisibility(target);
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        protected override bool IsDetected(Unit target)
        {
            return Gang.IsMember(target) || base.IsDetected(target);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        protected override void OnBroadcastPacket(IBuilder<Packet> packetBuilder)
        {
            base.OnBroadcastPacket(packetBuilder);
            Session.SendPacket(packetBuilder.Build());
        }

        protected override void OnUnitVisibilityUpdated(Unit target, Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                    {
                        target.BroadcastPacket += OnUnitBroadcastPacket;
                        target.Updated += OnUnitUpdated;
                        Session.SendPacket(target.EnterPacketBuilder);

                        break;
                    }
                case Visibility.Invisible:
                    {
                        target.BroadcastPacket -= OnUnitBroadcastPacket;
                        target.Updated -= OnUnitUpdated;
                        Session.SendPacket(target.ExitPacketBuilder);

                        break;
                    }

            }

            if (Gang.IsMember(target))
            {
                Session.SendPacket(new GangUpdatePacketBuilder(visibility, (Player)target));
            }

            base.OnUnitVisibilityUpdated(target, visibility);
        }

        protected override void OnLockStateChanged(Lock @lock)
        {
            base.OnLockStateChanged(@lock);

            if (@lock is UnitLock u)
            {
                Player player = u.Target as Player;

                player?.Session.CancelLogout();

                if (@lock.State == LockState.Locked)
                {
                    if (u.Target is Npc npc)
                    {
                        MissionHandler.EnqueueMissionEventInfo(new LockUnitEventInfo(this, npc, npc.CurrentPosition));
                        MissionHandler.SignalParticipationByLocking(npc.GetMissionGuid());
                    }
                }
            }

            if (@lock.State == LockState.Inprogress && EffectHandler.ContainsEffect(EffectType.effect_invulnerable))
            {
                EffectHandler.RemoveEffectsByType(EffectType.effect_invulnerable);
            }
        }

        protected override void OnEffectChanged(Effect effect, bool apply)
        {
            base.OnEffectChanged(effect, apply);

            if (!apply)
            {
                return;
            }

            switch (effect.Type)
            {
                case EffectType.effect_demobilizer:
                    {
                        OnCombatEvent(effect.Source, new DemobilizerEventArgs());

                        break;
                    }

                case EffectType.effect_sensor_supress:
                    {
                        OnCombatEvent(effect.Source, new SensorDampenerEventArgs());

                        break;
                    }
            }
        }

        protected override void OnLockError(Lock @lock, ErrorCodes error)
        {
            SendError(error);
            base.OnLockError(@lock, error);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            check = PlayerMoveCheckQueue.Create(this, CurrentPosition);
            zone.SendPacketToGang(Gang, new GangUpdatePacketBuilder(Visibility.Visible, this));
            MissionHandler = missionHandlerFactory(zone, this);
            MissionHandler.InitMissions();
            Direction = FastRandom.NextDouble();

            IDynamicProperty<int> p = DynamicProperties.GetProperty<int>(k.pvpRemaining);

            if (!p.HasValue)
            {
                return;
            }

            ApplyPvPEffect(TimeSpan.FromMilliseconds(p.Value));
            p.Clear();
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            Session.SendPacket(ExitPacketBuilder);
            zone.SendPacketToGang(Gang, new GangUpdatePacketBuilder(Visibility.Invisible, this));
            check.StopAndDispose();

            if (!States.LocalTeleport)
            {
                Session.Stop();
            }

            base.OnRemovedFromZone(zone);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            UpdateCombat(time);
            movement.Update(time);
            blobHandler.Update(time);
            MissionHandler.Update(time);
            combatLogger?.Update(time);
            despawnHelper?.Update(time, this);
        }

        private void OnUnitUpdated(Unit unit, UnitUpdatedEventArgs e)
        {
            if (!Gang.IsMember(unit))
            {
                return;
            }

            bool send = (e.UpdateTypes & UnitUpdateTypes.Visibility) > 0 || (e.UpdatedProperties != null && e.UpdatedProperties.Any(p => p.Field.IsPublic()));

            if (!send)
            {
                return;
            }

            Visibility v = Visibility.Invisible;

            if (unit.InZone)
            {
                v = Visibility.Visible;
            }

            Session.SendPacket(new GangUpdatePacketBuilder(v, (Player)unit));
        }

        private void OnUnitBroadcastPacket(Unit sender, Packet packet)
        {
            Session.SendPacket(packet);
        }

        private void UpdateCombat(TimeSpan time)
        {
            if (!States.Combat)
            {
                return;
            }

            _ = combatTimer.Update(time);

            if (!combatTimer.Passed)
            {
                return;
            }

            SetCombatState(false);
        }

        private void Reload()
        {
            RobotInventory container = GetContainer();

            Debug.Assert(container != null, "container != null");
            container.EnlistTransaction();
            container.ReloadItems(Character);
            container.SendUpdateToOwner();
        }

        private CombatLogger CreateCombatLogger()
        {
            CombatLogger logger = combatLoggerFactory(this);

            logger.Expired = () =>
            {
                combatLogger = null;
            };

            return logger;
        }

        private void SaveCombatLog(IZone zone, Unit killer)
        {
            combatLogger?.Save(zone, killer);
        }

        private void ApplySyndicateAreaEffect(bool apply)
        {
            if (apply)
            {
                EffectBuilder effectBuilder = NewEffectBuilder().SetType(EffectType.effect_syndicate_area);
                ApplyEffect(effectBuilder);
            }
            else
            {
                EffectHandler.RemoveEffectsByType(EffectType.effect_syndicate_area);
            }
        }

        private void ApplyHighwayEffect(bool apply)
        {
            if (apply)
            {
                EffectBuilder effectBuilder = NewEffectBuilder()
                    .SetType(EffectType.effect_highway)
                    .WithPropertyModifier(ItemPropertyModifier.Create(AggregateField.effect_speed_highway_modifier, 1.0));

                ApplyEffect(effectBuilder);
            }
            else
            {
                EffectHandler.RemoveEffectsByType(EffectType.effect_highway);
            }
        }

        private void SendError(ErrorCodes error)
        {
            Session.SendPacket(new ErrorPacketBuilder(error));
        }

        private Task HandlePlayerDeadAsync(IZone zone, Unit killer)
        {
            return Task.Run(() => HandlePlayerDead(zone, killer));
        }

        private void HandlePlayerDead(IZone zone, Unit killer)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                EnlistTransaction();

                try
                {
                    killer = zone.ToPlayerOrGetOwnerPlayer(killer) ?? killer;

                    SaveCombatLog(zone, killer);

                    Character character = Character;
                    DockingBase dockingBase = character.GetHomeBaseOrCurrentBase();

                    dockingBase.DockIn(character, IsBlessed ? BlessedUndockDelay : NormalUndockDelay, ZoneExitType.Died);

                    PlayerDeathLogger.Log.Write(zone, this, killer);

                    bool wasInsured = InsuranceHelper.CheckInsuranceOnDeath(Eid, Definition);

                    if (!Session.AccessLevel.IsAdminOrGm() && !IsBlessed)
                    {
                        DieAndDropLoot(zone, killer, character, dockingBase, wasInsured);
                    }
                    else
                    {
                        ResurrectAndLoseCargo(zone, character, dockingBase);
                    }

                    Save();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        private void ResurrectAndLoseCargo(IZone zone, Character character, DockingBase dockingBase)
        {
            this.Repair();

            PublicContainer container = dockingBase.GetPublicContainer();

            container.AddItem(this, false);

            if (IsBlessed)
            {
                RobotInventory robotInventory = GetContainer();

                Debug.Assert(robotInventory != null);

                List<LootItem> lootItems = new List<LootItem>();

                foreach (Item item in robotInventory.GetItems(true).Where(i => i is VolumeWrapperContainer))
                {
                    if (!(item is VolumeWrapperContainer wrapper))
                    {
                        continue;
                    }

                    wrapper.SetAllowDelete();
                    Repository.Delete(wrapper);
                }

                foreach (Item item in robotInventory.GetItems())
                {
                    robotInventory.RemoveItemOrThrow(item);
                    Repository.Delete(item);
                }

                // Calculate and add tokens here
                ItemInfo tokens = CalculateTokensDrop();
                lootItems.Add(LootItemBuilder.Create(tokens).Build());

                if (lootItems.Count > 0)
                {
                    LootContainer lootContainer = LootContainer.Create()
                        .AddLoot(lootItems)
                        .BuildAndAddToZone(zone, CurrentPosition);

                    if (lootContainer != null)
                    {
                        TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                            .SetTransactionType(TransactionType.PutLoot)
                            .SetCharacter(character)
                            .SetContainer(lootContainer.Eid);

                        foreach (LootItem lootItem in lootItems)
                        {
                            _ = b.SetItem(lootItem.ItemInfo.Definition, lootItem.ItemInfo.Quantity);
                            Character.LogTransaction(b);
                        }
                    }
                }
            }
        }

        private void DieAndDropLoot(IZone zone, Unit killer, Character character, DockingBase dockingBase, bool wasInsured)
        {
            RobotInventory robotInventory = GetContainer();

            Debug.Assert(robotInventory != null);

            List<LootItem> lootItems = new List<LootItem>();

            foreach (Module module in Modules.Where(m => LootHelper.Roll()))
            {
                lootItems.Add(LootItemBuilder.Create(module).AsDamaged().Build());

                ActiveModule activeModule = module as ActiveModule;
                Items.Ammos.Ammo ammo = activeModule?.GetAmmo();

                if (ammo != null && LootHelper.Roll())
                {
                    lootItems.Add(LootItemBuilder.Create(ammo).Build());
                }

                module.Parent = robotInventory.Eid;

                Repository.Delete(module);
            }

            foreach (Item item in robotInventory.GetItems(true).Where(i => i is VolumeWrapperContainer))
            {
                if (!(item is VolumeWrapperContainer wrapper))
                {
                    continue;
                }

                lootItems.AddRange(wrapper.GetLootItems());
                wrapper.SetAllowDelete();
                Repository.Delete(wrapper);
            }

            foreach (Item item in robotInventory
                .GetItems()
                .Where(i => LootHelper.Roll() && !i.ED.AttributeFlags.NonStackable))
            {
                double qtyMod = FastRandom.NextDouble();

                item.Quantity = (int)(item.Quantity * qtyMod);

                if (item.Quantity > 0)
                {
                    lootItems.Add(
                        LootItemBuilder
                            .Create(item.Definition)
                            .SetQuantity(item.Quantity)
                            .SetRepackaged(item.ED.AttributeFlags.Repackable)
                            .Build());
                }
                else
                {
                    robotInventory.RemoveItemOrThrow(item);
                    Repository.Delete(item);
                }
            }

            if (ED.Config.Tint != Tint && LootHelper.Roll(0.5))
            {
                EntityDefault paint = EntityDefault.Reader.GetAll()
                    .Where(i => i.CategoryFlags == CategoryFlags.cf_paints)
                    .Where(i => i.Config.Tint == Tint).FirstOrDefault();

                if (paint != null)
                {
                    lootItems.Add(LootItemBuilder.Create(paint.Definition).SetQuantity(1).SetDamaged(false).Build());
                }
            }

            if (lootItems.Count > 0)
            {
                LootContainer lootContainer = LootContainer.Create()
                    .AddLoot(lootItems)
                    .BuildAndAddToZone(zone, CurrentPosition);

                if (lootContainer != null)
                {
                    TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                        .SetTransactionType(TransactionType.PutLoot)
                        .SetCharacter(character)
                        .SetContainer(lootContainer.Eid);

                    foreach (LootItem lootItem in lootItems)
                    {
                        _ = b.SetItem(lootItem.ItemInfo.Definition, lootItem.ItemInfo.Quantity);
                        Character.LogTransaction(b);
                    }
                }
            }

            bool killedByPlayer = killer != null && killer.IsPlayer();

            Trashcan.Get()
                .MoveToTrash(this, Session.DisconnectTime, wasInsured, killedByPlayer, Session.InactiveTime);

            character.NextAvailableRobotRequestTime =
                DateTime.Now.AddMinutes(killedByPlayer ? ARKHE_REQUEST_TIMER_MINUTES_PVP : ARKHE_REQUEST_TIMER_MINUTES_NPC);

            Robot activeRobot = null;

            if (!killedByPlayer)
            {
                activeRobot = dockingBase.CreateStarterRobotForCharacter(character);

                if (activeRobot != null)
                {
                    Transaction.Current.OnCommited(() =>
                    {
                        Dictionary<string, object> starterRobotInfo = new Dictionary<string, object>
                                    {
                                        {k.baseEID, Eid},
                                        {k.robotEID, activeRobot.Eid}
                                    };

                        Message.Builder.SetCommand(Commands.StarterRobotCreated).WithData(starterRobotInfo).ToCharacter(character).Send();
                    });
                }
            }

            character.SetActiveRobot(activeRobot);
        }

        private ItemInfo CalculateTokensDrop()
        {
            int baseCost = baseTokenPrices.ContainsKey(ED.CategoryFlags)
                ? baseTokenPrices[ED.CategoryFlags]
                : 50;

            return new ItemInfo(
                EntityDefault.GetByName(DefinitionNames.UNIVERSAL_MISSION_COIN).Definition,
                baseCost);
        }



        protected override double CamouflageBonus()
        {
            return Math.Round(base.CamouflageBonus() * globalConfiguration.CamouflageBonus, 2);
        }
    }
}
