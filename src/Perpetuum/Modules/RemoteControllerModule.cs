using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Modules
{
    public class RemoteControllerModule : ActiveModule
    {
        private readonly EffectToken _token = EffectToken.NewToken();
        private const double SpawnRangeMin = 2;
        private const double SpawnRangeMax = 5;
        private readonly ModuleProperty bandwidthMax;
        private readonly ModuleProperty operationalRange;
        private readonly ModuleProperty lifetime;
        private readonly ModuleProperty droneLockingTime;
        private readonly ModuleProperty droneArmorMax;
        private readonly ModuleProperty droneCoreMax;
        private readonly ModuleProperty droneCoreRechargeTime;
        private readonly ModuleProperty droneSpeedMax;
        private readonly ModuleProperty droneReactorRadiation;

        private BandwidthHandler bandwidthHandler;

        public double BandwidthMax => bandwidthMax.Value;

        public RemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);

            bandwidthMax = new ModuleProperty(this, AggregateField.remote_control_bandwidth_max);
            AddProperty(bandwidthMax);

            operationalRange = new ModuleProperty(this, AggregateField.remote_control_operational_range);
            AddProperty(operationalRange);

            lifetime = new ModuleProperty(this, AggregateField.remote_control_lifetime);
            AddProperty(lifetime);

            droneLockingTime = new ModuleProperty(this, AggregateField.drone_amplification_locking_time_modifier);
            AddProperty(droneLockingTime);

            droneArmorMax = new ModuleProperty(this, AggregateField.drone_amplification_armor_max_modifier);
            AddProperty(droneArmorMax);

            droneCoreMax = new ModuleProperty(this, AggregateField.drone_amplification_core_max_modifier);
            AddProperty(droneCoreMax);

            droneCoreRechargeTime = new ModuleProperty(this, AggregateField.drone_amplification_core_recharge_time_modifier);
            AddProperty(droneCoreRechargeTime);

            droneSpeedMax = new ModuleProperty(this, AggregateField.drone_amplification_speed_max_modifier);
            AddProperty(droneSpeedMax);

            droneReactorRadiation = new ModuleProperty(this, AggregateField.drone_amplification_reactor_radiation_modifier);
            AddProperty(droneReactorRadiation);

            InitBandwidthHandler(this);
        }

        private void InitBandwidthHandler(RemoteControllerModule module)
        {
            bandwidthHandler = new BandwidthHandler(module);
        }

        public void SyncRemoteChannels()
        {
            bandwidthHandler.Update();
        }

        public bool HasFreeBandwidthFor(RemoteControlledUnit unit)
        {
            return bandwidthHandler.HasFreeBandwidthFor(unit);
        }

        public void UseRemoteChannel(RemoteControlledCreature turret)
        {
            bandwidthHandler.UseRemoteChannel(turret);
            turret.RemoteChannelDeactivated += bandwidthHandler.OnRemoteChannelDeactivated;
            bandwidthHandler.Update();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void CloseAllChannels()
        {
            bandwidthHandler.CloseAllChannels();
        }

        public virtual RemoteControlledCreature CreateAndConfigureRcu(RemoteControlledUnit ammo)
        {
            return null;
        }

        protected override void OnAction()
        {
            if (bandwidthHandler == null)
            {
                return;
            }

            SyncRemoteChannels();
            RemoteControlledUnit ammo = GetAmmo() as RemoteControlledUnit;
            HasFreeBandwidthFor(ammo).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);
            Position targetPosition = GetSpawnPosition(ParentRobot.CurrentPosition);
            Player player = ParentRobot is Player robotAsPlayer
                ? robotAsPlayer
                : null;
            if (player != null)
            {
                ammo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
            }

            RemoteControlledCreature remoteControlledCreature = CreateAndConfigureRcu(ammo);
            if (remoteControlledCreature == null)
            {
                return;
            }

            remoteControlledCreature.SetCommandRobot(player ?? ParentRobot);
            remoteControlledCreature.Owner = ParentRobot.Owner;
            remoteControlledCreature.SetBandwidthUsage(ammo.RemoteChannelBandwidthUsage);
            UseRemoteChannel(remoteControlledCreature);
            remoteControlledCreature.DespawnTime = TimeSpan.FromMilliseconds(lifetime.Value);
            remoteControlledCreature.SetGroup(bandwidthHandler);
            ClosestWalkablePositionFinder finder = new ClosestWalkablePositionFinder(Zone, targetPosition);
            Position position = finder.FindOrThrow();
            remoteControlledCreature.HomePosition = position;
            remoteControlledCreature.HomeRange = operationalRange.Value;
            remoteControlledCreature.Orientation = FastRandom.NextInt(0, 3) * 0.25;
            remoteControlledCreature.CallForHelp = true;
            BeamBuilder deployBeamBuilder = Beam.NewBuilder()
                .WithType(BeamType.dock_in)
                .WithSource(remoteControlledCreature.CommandRobot)
                .WithTarget(remoteControlledCreature)
                .WithState(BeamState.Hit)
                .WithDuration(TimeSpan.FromSeconds(5));
            remoteControlledCreature.AddToZone(Zone, position, ZoneEnterType.Default, deployBeamBuilder);
            EffectBuilder effectBuilder = remoteControlledCreature.NewEffectBuilder();
            SetupEffect(effectBuilder);
            _ = effectBuilder.WithToken(_token);
            remoteControlledCreature.ApplyEffect(effectBuilder);
            ConsumeAmmo();
        }

        private Position GetSpawnPosition(Position spawnOrigin)
        {
            double spawnRangeMin = SpawnRangeMin;
            double spawnRangeMax = SpawnRangeMax;
            Position spawnPosition = spawnOrigin.GetRandomPositionInRange2D(spawnRangeMin, spawnRangeMax).Clamp(Zone.Size);

            return spawnPosition;
        }

        protected virtual void SetupEffect(EffectBuilder effectBuilder)
        {
        }
    }
}
