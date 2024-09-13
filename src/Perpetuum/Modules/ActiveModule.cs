using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Perpetuum.Modules
{
    public abstract partial class ActiveModule : Module
    {
        private Lock _lock;
        protected readonly ModuleProperty coreUsage;
        protected readonly CycleTimeProperty cycleTime;
        protected readonly ItemProperty falloff = ItemProperty.None;
        protected readonly ModuleProperty optimalRange;

        private readonly CategoryFlags _ammoCategoryFlags;

        public Lock Lock
        {
            [CanBeNull]
            get => _lock;
            set
            {
                if (_lock != null)
                {
                    _lock.Changed -= LockChangedHandler;
                }

                _lock = value;

                if (_lock != null)
                {
                    _lock.Changed += LockChangedHandler;
                }
            }
        }

        public bool IsRanged { get; private set; }

        public override double Volume
        {
            get
            {
                double volume = base.Volume;
                Items.Ammos.Ammo ammo = GetAmmo();

                if (ammo != null)
                {
                    volume += ammo.Volume;
                }

                return volume;
            }
        }

        public TimeSpan CycleTime => TimeSpan.FromMilliseconds(cycleTime.Value);

        public double CoreUsage => coreUsage.Value;

        public double OptimalRange => optimalRange.Value;

        public double Falloff => falloff.Value;

        protected ActiveModule(CategoryFlags ammoCategoryFlags, bool ranged = false)
        {
            IsRanged = ranged;
            coreUsage = new ModuleProperty(this, AggregateField.core_usage);
            AddProperty(coreUsage);
            cycleTime = new CycleTimeProperty(this);
            AddProperty(cycleTime);

            if (ranged)
            {
                optimalRange = new OptimalRangeProperty(this);
                AddProperty(optimalRange);
                falloff = new FalloffProperty(this);
                AddProperty(falloff);
            }

            _ammoCategoryFlags = ammoCategoryFlags;
        }

        protected ActiveModule(bool ranged) : this(CategoryFlags.undefined, ranged)
        {
        }

        public override void Initialize()
        {
            InitState();
            InitAmmo();
            base.Initialize();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            Items.Ammos.Ammo ammo = GetAmmo();

            ammo?.UpdateRelatedProperties(field);
            base.UpdateRelatedProperties(field);
        }

        public bool IsInRange(Position position)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");

            return !IsRanged || ParentRobot.IsInRangeOf3D(position, OptimalRange + Falloff);
        }

        public void ForceUpdate()
        {
            SendModuleStateToPlayer();
            SendAmmoUpdatePacketToPlayer();
        }

        public void Update(TimeSpan time)
        {
            _states.Update(time);
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();

            Items.Ammos.Ammo ammo = GetAmmo();

            ammo?.UpdateAllProperties();
        }

        public override void Unequip(Container container)
        {
            _ = UnequipAmmoToContainer(container);
            base.Unequip(container);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = base.ToDictionary();

            result.Add(k.ammoCategoryFlags, (long)_ammoCategoryFlags);

            Items.Ammos.Ammo ammo = GetAmmo();

            if (ammo == null)
            {
                return result;
            }

            result.Add(k.ammo, ammo.ToDictionary());
            result.Add(k.ammoQuantity, ammo.Quantity);

            return result;
        }

        protected abstract void OnAction();

        protected virtual void HandleOffensivePVPCheck(Player parentPlayer, UnitLock unitLockTarget)
        {
            Player targetPlayer = unitLockTarget.Target as Player;
            if (targetPlayer is null &&
                ParentRobot is RemoteControlledCreature remoteControlledCreature &&
                remoteControlledCreature.CommandRobot is Player player)
            {
                parentPlayer = player;
            }

            if (parentPlayer != null && targetPlayer != null)
            {
                _ = ((unitLockTarget.Target as Player)?.CheckPvp().ThrowIfError());
                _ = targetPlayer.CheckPvp().ThrowIfError();
            }
        }

        protected Lock GetLock()
        {
            Lock currentLock = Lock.ThrowIfNull(ErrorCodes.LockTargetNotFound);

            _ = currentLock.State.ThrowIfNotEqual(LockState.Locked, ErrorCodes.LockIsInProgress);

            if (currentLock is UnitLock unitLockTarget)
            {
                IsInRange(unitLockTarget.Target.CurrentPosition).ThrowIfFalse(ErrorCodes.TargetOutOfRange);

                Player parentPlayer = ParentRobot as Player;

                if (parentPlayer is null &&
                    ParentRobot is RemoteControlledCreature remoteControlledCreature &&
                    remoteControlledCreature.CommandRobot is Player player)
                {
                    parentPlayer = player;
                }

                if (ED.AttributeFlags.OffensiveModule)
                {
                    HandleOffensivePVPCheck(parentPlayer, unitLockTarget);
                    Debug.Assert(ParentRobot != null, "ParentRobot != null");
                    ParentRobot.OnAggression(unitLockTarget.Target);
                }
                else if ((parentPlayer != null) && (unitLockTarget.Target is Player) && ED.AttributeFlags.PvpSupport)
                {
                    parentPlayer.OnPvpSupport(unitLockTarget.Target);
                }
            }
            else
            {
                ;
                if (currentLock is TerrainLock terrainLockTarget)
                {
                    IsInRange(terrainLockTarget.Location).ThrowIfFalse(ErrorCodes.TargetOutOfRange);
                }
            }

            return currentLock;
        }

        protected void CreateBeam(Unit target, BeamState beamState)
        {
            _ = CreateBeam(target, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(target, beamState, duration, bulletTime, 0);
        }

        protected void CreateBeam(Position location, BeamState beamState)
        {
            _ = CreateBeam(location, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(location, beamState, duration, bulletTime, 0);
        }

        protected override void OnUpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.core_usage:
                case AggregateField.effect_core_usage_gathering_modifier:
                    {
                        coreUsage.Update();

                        break;
                    }
                case AggregateField.cycle_time:
                case AggregateField.effect_weapon_cycle_time_modifier:
                case AggregateField.effect_gathering_cycle_time_modifier:
                case AggregateField.drone_amplification_cycle_time_modifier:
                case AggregateField.drone_amplification_remote_repair_cycle_time_modifier:
                    {
                        cycleTime.Update();

                        break;
                    }
                case AggregateField.optimal_range:
                case AggregateField.effect_optimal_range_modifier:
                case AggregateField.effect_ew_optimal_range_modifier:
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                case AggregateField.drone_amplification_long_range_modifier:
                    {
                        optimalRange.Update();

                        break;
                    }
                case AggregateField.falloff:
                    {
                        falloff.Update();

                        break;
                    }
            }

            base.OnUpdateProperty(field);
        }

        protected double ModifyValueByOptimalRange(Unit target, double value)
        {
            return ModifyValueByOptimalRange(ParentRobot, target, value);
        }

        protected double ModifyValueByOptimalRange(Unit issuer, Unit target, double value)
        {
            Debug.Assert(issuer != null, "issuer != null");

            double distance = issuer.GetDistance(target);

            if (distance <= OptimalRange)
            {
                return value;
            }

            if (distance > OptimalRange + Falloff)
            {
                return 0.0;
            }

            double x = (distance - OptimalRange) / Falloff;
            double m = (Math.Cos(x * Math.PI) / 2) + 0.5;

            return value * m;
        }

        protected LOSResult GetLineOfSight(Unit target)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");

            IUnitVisibility visibility = ParentRobot.GetVisibility(target);

            return visibility?.GetLineOfSight(IsCategory(CategoryFlags.cf_missiles)) ?? LOSResult.None;
        }

        protected LOSResult GetLineOfSight(Position location)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");

            LOSResult losResult = ParentRobot.Zone.IsInLineOfSight(ParentRobot, location, IsCategory(CategoryFlags.cf_missiles));

            return losResult;
        }

        protected bool LOSCheckAndCreateBeam(Unit target)
        {
            LOSResult result = GetLineOfSight(target);

            if (result.hit)
            {
                BeamState beamState = (result.blockingFlags != BlockingFlags.Undefined) ? BeamState.AlignToTerrain : BeamState.Hit;

                CreateBeam(result.position, beamState);

                return false;
            }

            CreateBeam(target, BeamState.Hit);

            return true;
        }

        protected void OnError(ErrorCodes error)
        {
            SendModuleErrorToPlayer(error);
        }

        private void LockChangedHandler(Lock @lock)
        {
            if (State.Type == ModuleStateType.Idle || State.Type == ModuleStateType.AmmoLoad)
            {
                return;
            }

            bool shutdown = @lock.State == LockState.Disabled || (ED.AttributeFlags.PrimaryLockedTarget && !@lock.Primary);

            if (!shutdown)
            {
                return;
            }

            State.SwitchTo(ModuleStateType.Shutdown);
            _lock = null;
        }

        private BeamType GetBeamType()
        {
            Items.Ammos.Ammo ammo = GetAmmo();

            return ammo != null
                ? BeamHelper.GetBeamByDefinition(ammo.Definition)
                : BeamHelper.GetBeamByDefinition(Definition);
        }

        private void SendModuleStateToPlayer()
        {
            IModuleState state = State;

            if (!(ParentRobot is Player player))
            {
                return;
            }

            Packet packet = new Packet(ZoneCommand.ModuleChangeState);

            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            packet.AppendByte((byte)ParentComponent.Type);
            packet.AppendByte((byte)Slot);
            packet.AppendByte((byte)state.Type);

            if (!(state is ITimedModuleState timed))
            {
                packet.AppendInt(0);
                packet.AppendInt(0);
            }
            else
            {
                packet.AppendInt((int)timed.Timer.Interval.TotalMilliseconds);
                packet.AppendInt((int)timed.Timer.Elapsed.TotalMilliseconds);
            }

            player.Session.SendPacket(packet);
        }

        private int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime, int visibility)
        {
            int delay = 0;
            BeamType beamType = GetBeamType();

            if (beamType <= 0)
            {
                return delay;
            }

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
            {
                duration = (int)CycleTime.TotalMilliseconds;
            }

            Debug.Assert(ParentComponent != null, "ParentComponent != null");

            int slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1
            BeamBuilder builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTarget(target)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);

            return delay;
        }

        private int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime, int visibility)
        {
            int delay = 0;
            BeamType beamType = GetBeamType();

            if (beamType <= 0)
            {
                return delay;
            }

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
            {
                duration = (int)CycleTime.TotalMilliseconds;
            }

            Debug.Assert(ParentComponent != null, "ParentComponent != null");

            int slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1
            BeamBuilder builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTargetPosition(location)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);

            return delay;
        }

        private void SendModuleErrorToPlayer(ErrorCodes error)
        {
            if (!(ParentRobot is Player player))
            {
                return;
            }

            CombatLogPacket packet = new CombatLogPacket(error, this, _lock);

            player.Session.SendPacket(packet);
        }
    }
}