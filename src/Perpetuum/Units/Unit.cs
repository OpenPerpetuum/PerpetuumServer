using Perpetuum.Builders;
using Perpetuum.Collections.Spatial;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Timers;
using Perpetuum.Units.ItemProperties;
using Perpetuum.Units.UnitProperties;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.Turrets;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Teleporting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Units
{
    public delegate void UnitEventHandler(Unit unit);
    public delegate void UnitEventHandler<in T>(Unit unit, T args);
    public delegate void UnitEventHandler<in T1, in T2>(Unit unit, T1 args1, T2 args2);

    public abstract partial class Unit : Item
    {
        private ICoreRecharger _coreRecharger = CoreRecharger.None;

        private readonly DamageProcessor _damageProcessor;
        private readonly object _killSync = new object();

        private Position _currentPosition;
        private double _currentSpeed;
        private double _direction;
        private double _orientation;

        private ItemProperty _armorMax;
        private ItemProperty _armor;
        private ItemProperty _coreMax;
        private ItemProperty _core;
        private ItemProperty _actualMass;
        private ItemProperty _coreRechargeTime;
        private ItemProperty _resistChemical;
        private ItemProperty _resistExplosive;
        private ItemProperty _resistKinetic;
        private ItemProperty _resistThermal;
        private ItemProperty _kersChemical;
        private ItemProperty _kersExplosive;
        private ItemProperty _kersKinetic;
        private ItemProperty _kersThermal;
        private ItemProperty speedMax;
        private ItemProperty _criticalHitChance;
        private ItemProperty _sensorStrength;
        private ItemProperty detectionStrength;
        private ItemProperty stealthStrength;
        private ItemProperty _massiveness;
        private ItemProperty _reactorRadiation;
        private ItemProperty _signatureRadius;
        private ItemProperty _slope;

        private readonly Lazy<double> _height;

        protected Unit()
        {
            _damageProcessor = new DamageProcessor(this) { DamageTaken = OnDamageTaken };

            EffectHandler effectHandler = new EffectHandler(this);
            effectHandler.EffectChanged += OnEffectChanged;
            EffectHandler = effectHandler;

            OptionalProperties.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.OptionalProperty;
            };

            InitUnitProperties();

            States = new UnitStates(this);
            _height = new Lazy<double>(() => ComputeHeight() + 1.0);
        }

        public EffectBuilder.Factory EffectBuilderFactory { get; set; }

        public void SetCoreRecharger(ICoreRecharger recharger)
        {
            _coreRecharger = recharger;
        }

        public Guid GetMissionGuid()
        {
            ReadOnlyOptionalProperty<Guid> p = (ReadOnlyOptionalProperty<Guid>)OptionalProperties.Get(UnitDataType.MissionGuid);
            return p?.Value ?? Guid.Empty;
        }

        public int GetMissionDisplayOrder()
        {
            ReadOnlyOptionalProperty<int> p = (ReadOnlyOptionalProperty<int>)OptionalProperties.Get(UnitDataType.MissionDisplayOrder);
            return p == null ? -1 : p.Value;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        private IZone _zone;

        [CanBeNull]
        public IZone Zone => _zone;

        public bool InZone => Zone != null;

        public OptionalPropertyCollection OptionalProperties { get; } = new OptionalPropertyCollection();

        public EffectHandler EffectHandler { get; private set; }

        protected UnitUpdateTypes UpdateTypes { get; set; }

        public double CurrentSpeed
        {
            get => _currentSpeed;
            set
            {
                if (Math.Abs(_currentSpeed - value) < double.Epsilon)
                {
                    return;
                }

                _currentSpeed = value;
                UpdateTypes |= UnitUpdateTypes.Speed;
            }
        }

        public double Direction
        {
            get => _direction;
            set
            {
                if (Math.Abs(_direction - value) < double.Epsilon)
                {
                    return;
                }

                _direction = value;
                UpdateTypes |= UnitUpdateTypes.Direction;
            }
        }

        public double Orientation
        {
            get => _orientation;
            set
            {
                if (Math.Abs(_orientation - value) < double.Epsilon)
                {
                    return;
                }

                _orientation = value;
                UpdateTypes |= UnitUpdateTypes.Orientation;
            }
        }

        public double Height => _height.Value;

        public virtual bool IsLockable => !ED.AttributeFlags.NonLockable && !States.Dead && !States.Unlockable;

        public virtual ErrorCodes IsAttackable => !ED.AttributeFlags.NonAttackable && !States.Dead ? ErrorCodes.NoError : ErrorCodes.TargetIsNonAttackable;

        public bool IsInvulnerable => ED.AttributeFlags.Invulnerable || EffectHandler.ContainsEffect(EffectType.effect_invulnerable);


        public bool HasShieldEffect => EffectHandler.ContainsEffect(EffectType.effect_shield);

        public bool HasPvpEffect => EffectHandler.ContainsEffect(EffectType.effect_pvp);

        public bool HasTeleportSicknessEffect => EffectHandler.ContainsEffect(EffectType.effect_teleport_sickness);

        public bool HasNoxTeleportNegationEffect => EffectHandler.ContainsEffect(EffectType.nox_effect_teleport_negation);

        public bool HasDespawnEffect => EffectHandler.ContainsEffect(EffectType.effect_despawn_timer);

        public bool HasNoTeleportWhilePVP => EffectHandler.Effects.Any(e => e.PropertyModifiers.Any(p => p.Field == AggregateField.pvp_no_teleport));

        public Position WorldPosition => Zone?.ToWorldPosition(CurrentPosition) ?? CurrentPosition;

        public Position PositionWithHeight => CurrentPosition.AddToZ(Height);

        public Position CurrentPosition
        {
            get => _currentPosition;
            set
            {
                Position lastPosition = _currentPosition;

                _currentPosition = Zone.FixZ(value);

                UpdateTypes |= UnitUpdateTypes.Position;

                if (!lastPosition.IsTileChange(_currentPosition))
                {
                    return;
                }

                UpdateTypes |= UnitUpdateTypes.TileChanged;

                OnTileChanged();

                CellCoord lastCellCoord = lastPosition.ToCellCoord();
                CellCoord currentCellCoord = _currentPosition.ToCellCoord();

                if (lastCellCoord == currentCellCoord)
                {
                    return;
                }

                OnCellChanged(lastCellCoord, currentCellCoord);
            }
        }

        public IBuilder<Packet> EnterPacketBuilder { get; private set; }

        public IBuilder<Packet> ExitPacketBuilder => new UnitExitPacketBuilder(this);

        /// <summary>
        /// Updates the specified time.
        /// ez hivodik meg minden 50ms alatt
        /// </summary>
        public void Update(TimeSpan time)
        {
            if (!InZone || States.Dead)
            {
                return;
            }

            OnUpdate(time);
        }

        private readonly IntervalTimer _broadcastTimer = new IntervalTimer(200);

        protected virtual void OnUpdate(TimeSpan time)
        {
            _coreRecharger.RechargeCore(this, time);

            EffectHandler.Update(time);

            UnitUpdatedEventArgs e = null;

            _ = _broadcastTimer.Update(time);

            if (_broadcastTimer.Passed)
            {
                _broadcastTimer.Reset();

                if (UpdateTypes > 0)
                {
                    e = new UnitUpdatedEventArgs { UpdateTypes = UpdateTypes };

                    if ((UpdateTypes & UnitUpdateTypes.Unit) > 0)
                    {
                        UnitUpdatePacketBuilder packetBuilder = new UnitUpdatePacketBuilder(this);
                        OnBroadcastPacket(packetBuilder.ToProxy());
                    }

                    UpdateTypes = UnitUpdateTypes.None;
                }

                ImmutableHashSet<ItemProperty> changedProperties = GetChangedProperties();
                if (changedProperties != ImmutableHashSet<ItemProperty>.Empty)
                {
                    if (e == null)
                    {
                        e = new UnitUpdatedEventArgs();
                    }

                    e.UpdatedProperties = changedProperties;

                    UnitPropertiesUpdatePacketBuilder builder = new UnitPropertiesUpdatePacketBuilder(this, changedProperties);
                    OnBroadcastPacket(builder.ToProxy());
                }
            }

            if (e == null)
            {
                return;
            }

            OnUpdated(e);
        }

        public event UnitEventHandler<Packet> BroadcastPacket;
        public event UnitEventHandler<Effect, bool /* apply */> EffectChanged;

        protected virtual void OnBroadcastPacket(IBuilder<Packet> packetBuilder)
        {
            BroadcastPacket?.Invoke(this, packetBuilder.Build());
        }

        protected virtual void OnEffectChanged(Effect effect, bool apply)
        {
            EffectChanged?.Invoke(this, effect, apply);

            bool canBroadcast = effect.Display;
            if (canBroadcast)
            {
                EffectPacketBuilder packetBuilder = new EffectPacketBuilder(effect, apply);
                OnBroadcastPacket(packetBuilder.ToProxy());
            }
        }

        public void SendRefreshUnitPacket()
        {
            OnBroadcastPacket(UnitEnterPacketBuilder.Create(this, ZoneEnterType.Update).ToProxy());
        }

        public void AddToZone(IZone zone, Position position, ZoneEnterType enterType = ZoneEnterType.Default, IBeamBuilder enterBeamBuilder = null)
        {
            _zone = zone;
            CurrentPosition = zone.FixZ(position);

            OnEnterZone(zone, enterType);

            zone.AddUnit(this);

            if (enterBeamBuilder != null)
            {
                zone.CreateBeam(enterBeamBuilder);
            }

            EnterPacketBuilder = UnitEnterPacketBuilder.Create(this, enterType);
            zone.UpdateUnitRelations(this);
            EnterPacketBuilder = UnitEnterPacketBuilder.Create(this, ZoneEnterType.Default);
        }

        protected virtual void OnEnterZone(IZone zone, ZoneEnterType enterType) { }

        public event UnitEventHandler RemovedFromZone;

        public void RemoveFromZone(IBeamBuilder exitBeamBuilder = null)
        {
            IZone zone;

            if ((zone = Interlocked.CompareExchange(ref _zone, null, _zone)) == null)
            {
                return;
            }

            Debug.Assert(zone != null, "zone != null");

            if (exitBeamBuilder != null)
            {
                zone.CreateBeam(exitBeamBuilder);
            }

            OnBeforeRemovedFromZone(zone);
            zone.RemoveUnit(this);
            OnRemovedFromZone(zone);
            zone.UpdateUnitRelations(this);
            RemovedFromZone?.Invoke(this);
        }

        protected virtual void OnRemovedFromZone(IZone zone) { }

        protected virtual void OnBeforeRemovedFromZone(IZone zone) { }

        public void TakeDamage(DamageInfo damageInfo)
        {
            _damageProcessor.TakeDamage(damageInfo);
        }

        public event UnitEventHandler<Unit, DamageTakenEventArgs> DamageTaken;

        protected virtual void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            DamageTaken?.Invoke(this, source, e);

            CombatLogPacket packet = new CombatLogPacket(CombatLogType.Damage, this, source);
            packet.AppendByte((byte)(e.IsCritical ? 1 : 0));
            packet.AppendDouble(e.TotalDamage);
            packet.AppendDouble(e.TotalKers);
            packet.Send(this, source);

            if (!(e.TotalDamage >= 0.0))
            {
                return;
            }

            Armor -= e.TotalDamage;

            OnCombatEvent(source, e);

            if (Armor <= 0.0)
            {
                Kill(source);
            }
        }

        public virtual void OnCombatEvent(Unit source, CombatEventArgs e)
        {

        }


        public double GetDistance(Unit unit)
        {
            return GetDistance(unit.CurrentPosition);
        }

        private double GetDistance(Position targetPosition)
        {
            return CurrentPosition.TotalDistance3D(targetPosition);
        }

        public event UnitEventHandler<UnitUpdatedEventArgs> Updated;

        private void OnUpdated(UnitUpdatedEventArgs e)
        {
            Updated?.Invoke(this, e);
        }

        protected virtual void DoExplosion()
        {
            IZone zone = Zone;
            if (zone == null)
            {
                return;
            }

            if (zone.Configuration.Protected)
            {
                return;
            }

            IDamageBuilder damageBuilder = GetExplosionDamageBuilder();
            _ = Task.Delay(FastRandom.NextInt(0, 3000)).ContinueWith(t => zone.DoAoeDamage(damageBuilder));
        }

        /// <summary>
        /// Fitted curve to achieve desired explosion distances for bots of certain sizes by class
        /// Ensures output will be bounded and that arbitrary input will not result in error or undesired output
        /// </summary>
        /// <param name="x">SignativeRadius of bot</param>
        /// <returns>Radius in number of tiles (10m)</returns>
        private double SigRadiusToExplosionRadius(double x)
        {
            x = x.Clamp(3.0, 40.0);
            return (-3.336453 + (1.617863 * x) - (0.09721877 * (x * x)) + (0.002064723 * (x * x * x))).Clamp(1.0, 30.0);
        }

        private IDamageBuilder GetExplosionDamageBuilder()
        {
            double radius = SigRadiusToExplosionRadius(SignatureRadius);
            IDamageBuilder damageBuilder = DamageInfo.Builder.WithAttacker(this)
                                          .WithOptimalRange(1)
                                          .WithFalloff(radius)
                                          .WithExplosionRadius(radius);

            double armorMaxValue = ArmorMax;

            if (armorMaxValue.IsZero())
            {
                armorMaxValue = 1.0;
            }

            double coreMax = CoreMax;

            if (coreMax.IsZero())
            {
                coreMax = 1.0;
            }

            double damage = (Math.Sin(Core.Ratio(coreMax) * Math.PI) + 1) * (armorMaxValue * 0.1);
            _ = damageBuilder.WithAllDamageTypes(damage);
            return damageBuilder;
        }

        private class KillDetectorHelper : IEntityVisitor<PBSTurret>, IEntityVisitor<PBSDockingBase>, IEntityVisitor<PBSObject>
        {
            public KillDetectorHelper()
            {
                CanBeKilledResult = true;
            }

            public bool CanBeKilledResult { get; private set; }

            private bool CanBeKilled(IPBSObject pbsObject)
            {
                return pbsObject.ReinforceHandler.CurrentState.CanBeKilled;
            }

            public void Visit(PBSTurret turret)
            {
                CanBeKilledResult = CanBeKilled(turret);
            }

            public void Visit(PBSDockingBase dockingBase)
            {
                CanBeKilledResult = CanBeKilled(dockingBase);
            }

            public void Visit(PBSObject pbsObject)
            {
                CanBeKilledResult = CanBeKilled(pbsObject);
            }
        }


        public void Kill(Unit killer = null)
        {
            if (!Monitor.TryEnter(_killSync))
            {
                return;
            }

            try
            {
                KillDetectorHelper detector = new KillDetectorHelper();

                AcceptVisitor(detector);

                if (!detector.CanBeKilledResult)
                {
                    return;
                }

                if (States.Dead || !InZone)
                {
                    return;
                }

                if (killer != null)
                {
                    CombatLogPacket killingBlowPacket = new CombatLogPacket(CombatLogType.KillingBlow, this, killer);
                    killingBlowPacket.Send(this, killer);

                    OnCombatEvent(killer, new KillingBlowEventArgs());
                }

                States.Dead = true;
                OnDead(killer);
            }
            finally
            {
                Monitor.Exit(_killSync);
            }
        }

        protected virtual bool CanBeKilled()
        {
            return true;
        }

        public event Action<Unit /* killer */, Unit /* victim */> Dead;

        protected virtual void OnDead(Unit killer)
        {
            Dead?.Invoke(killer, this);

            DoExplosion();

            Logger.Info($"Unit died. Killer = {(killer != null ? killer.InfoString : "")} Victim = {InfoString}");
            RemoveFromZone(new WreckBeamBuilder(this));
        }

        public bool IsInRangeOf3D(Unit target, double range)
        {
            return IsInRangeOf3D(target.CurrentPosition, range);
        }

        public bool IsInRangeOf3D(Position targetPosition, double range)
        {
            return CurrentPosition.IsInRangeOf3D(targetPosition, range);
        }

        public event UnitEventHandler TileChanged;

        protected virtual void OnTileChanged()
        {
            TileChanged?.Invoke(this);
        }

        protected virtual void OnCellChanged(CellCoord lastCellCoord, CellCoord currentCellCoord) { }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = base.ToDictionary();

            if (!InZone)
            {
                return result;
            }

            result.Add(k.px, CurrentPosition.X);
            result.Add(k.py, CurrentPosition.Y);
            result.Add(k.pz, CurrentPosition.Z);
            result.Add(k.orientation, (byte)(_orientation * 255));

            IStandingController standingControlled = this as IStandingController;
            standingControlled?.AddStandingInfoToDictonary(result);

            return result;
        }

        public virtual string InfoString => $"Unit:{ED.Name}:{Definition}:{Eid}";

        public virtual void OnAggression(Unit victim)
        {
        }

        /// <summary>
        /// Must be called when modules are changed on zone!
        /// Handles any internal state dependent on modules being equipped.
        /// </summary>
        public void OnEquipChange()
        {
            _damageProcessor.OnRequipUnit();
        }

        public double GetKersByDamageType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Chemical: { return _kersChemical.Value; }
                case DamageType.Thermal: { return _kersThermal.Value; }
                case DamageType.Kinetic: { return _kersKinetic.Value; }
                case DamageType.Explosive: { return _kersExplosive.Value; }
            }
            return 1.0;
        }

        public double GetResistByDamageType(DamageType damageType)
        {
            double resist = 0.0;

            switch (damageType)
            {
                case DamageType.Chemical: { resist = _resistChemical.Value; break; }
                case DamageType.Thermal: { resist = _resistThermal.Value; break; }
                case DamageType.Kinetic: { resist = _resistKinetic.Value; break; }
                case DamageType.Explosive: { resist = _resistExplosive.Value; break; }
            }

            resist /= resist + 100;
            return resist;
        }

        public EffectBuilder NewEffectBuilder()
        {
            EffectBuilder builder = EffectBuilderFactory();
            _ = builder.WithOwner(this);
            return builder;
        }

        public void ApplyEffect(EffectBuilder builder)
        {
            EffectHandler.Apply(builder);
        }

        public void ApplyPvPEffect()
        {
            ApplyPvPEffect(TimeSpan.Zero);
        }

        public void ApplyPvPEffect(TimeSpan duration)
        {
            Effect effect = EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();
            EffectToken token = effect?.Token ?? EffectToken.NewToken();
            EffectBuilder builder = NewEffectBuilder();
            _ = builder.SetType(EffectType.effect_pvp).WithDuration(duration).WithToken(token);
            ApplyEffect(builder);
        }

        public virtual bool IsWalkable(Vector2 position)
        {
            return Zone.IsWalkable((int)position.X, (int)position.Y, Slope);
        }

        public virtual bool IsWalkable(Position position)
        {
            return Zone.IsWalkable((int)position.X, (int)position.Y, Slope);
        }

        public virtual bool IsWalkable(int x, int y)
        {
            return Zone.IsWalkable(x, y, Slope);
        }

        public virtual IDictionary<string, object> GetDebugInfo()
        {
            Dictionary<string, object> info = new Dictionary<string, object>
            {
                {k.eid, Eid},
                {k.definitionName, ED.Name},
                {k.owner, Owner},
                {k.state, States.ToString()},
                {"p", ItemProperty.ToDebugString(Properties)}
            };


            int counter = 0;
            foreach (Effect effect in EffectHandler.Effects)
            {
                info.Add("e" + counter++, effect.Type.ToString());
            }

            IStandingController standingControlled = this as IStandingController;
            standingControlled?.AddStandingInfoToDictonary(info);

            return info;
        }

        public void ApplyEffectPropertyModifiers(AggregateField modifierField, ref ItemPropertyModifier modifier)
        {
            foreach (Effect effect in EffectHandler.Effects)
            {
                effect.ApplyTo(ref modifier, modifierField);
            }
        }

        private class UnitEnterPacketBuilder : IBuilder<Packet>
        {
            private readonly ZoneEnterType _enterType;
            private readonly Unit _unit;

            private UnitEnterPacketBuilder(Unit unit, ZoneEnterType enterType)
            {
                _unit = unit;
                _enterType = enterType;
            }

            public Packet Build()
            {
                Packet packet = new Packet(ZoneCommand.EnterUnit);

                packet.AppendLong(_unit.Eid);
                Accounting.Characters.Character character = _unit.GetCharacter();
                packet.AppendInt(character.Id);

                packet.AppendPosition(_unit.CurrentPosition);
                packet.AppendByte((byte)(_unit.CurrentSpeed * 255));
                packet.AppendByte((byte)(_unit.Orientation * byte.MaxValue));
                packet.AppendByte((byte)(_unit.Direction * byte.MaxValue));

                byte[] desc = GetDescription(_unit);
                packet.AppendByteArray(desc);
                packet.AppendByte((byte)_enterType);
                _unit.States.AppendToPacket(packet);
                packet.AppendDouble(_unit.ArmorMax);
                packet.AppendDouble(_unit.Armor);
                packet.AppendDouble(_unit.speedMax.Value);
                packet.AppendLong(_unit.Owner);

                if (!(_unit is Robot robot))
                {
                    packet.AppendByte(0);
                }
                else
                {
                    Zones.Locking.Locks.Lock primaryLock = robot.GetPrimaryLock();
                    if (primaryLock != null)
                    {
                        packet.AppendByte(1);
                        LockPacketBuilder.AppendTo(primaryLock, packet);
                    }
                    else
                    {
                        packet.AppendByte(0);
                    }
                }

                // effektek
                Effect[] effects = _unit.EffectHandler.Effects.ToArray();
                packet.AppendInt(effects.Length);

                foreach (Effect effect in effects)
                {
                    effect.AppendToStream(packet);
                }

                _unit.OptionalProperties.WriteToStream(packet);
                return packet;
            }

            private static byte[] GetDescription(Unit unit)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(stream))
                    {
                        bw.Write(unit.Definition);

                        if (unit is Robot robot)
                        {
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotHead>());
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotChassis>());
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotLeg>());
                        }
                        else
                        {
                            bw.Write(new byte[15]);
                        }

                        return stream.ToArray();
                    }
                }
            }

            private static void WriteRobotComponent(BinaryWriter bw, RobotComponent component)
            {
                if (component == null)
                {
                    bw.Write(0);
                    bw.Write((byte)0);
                    return;
                }

                bw.Write(component.Definition);
                bw.Write((byte)component.MaxSlots);

                for (int i = 0; i < component.MaxSlots; i++)
                {
                    Module module = component.GetModule(i + 1);
                    WriteModule(bw, module);
                }
            }

            private static void WriteModule(BinaryWriter bw, Module module)
            {
                int moduleDefinition = module?.Definition ?? 0;
                bw.Write(moduleDefinition);
            }

            public static IBuilder<Packet> Create(Unit unit, ZoneEnterType enterType)
            {
                return new UnitEnterPacketBuilder(unit, enterType);
            }
        }

        private class UnitExitPacketBuilder : IBuilder<Packet>
        {
            private readonly Unit _unit;

            public UnitExitPacketBuilder(Unit unit)
            {
                _unit = unit;
            }

            private ZoneExitType ExitType => _unit.States.Dead
                        ? ZoneExitType.Died
                        : _unit.States.Dock
                        ? ZoneExitType.Docked
                        : _unit.States.Teleport
                        ? ZoneExitType.Teleport
                        : _unit.States.LocalTeleport ? ZoneExitType.LocalTeleport : ZoneExitType.LeftGrid;

            public Packet Build()
            {
                Packet packet = new Packet(ZoneCommand.ExitUnit);
                packet.AppendLong(_unit.Eid);
                packet.AppendByte((byte)ExitType);
                return packet;
            }
        }

        protected virtual bool IsHostileFor(Unit unit) { return false; }

        public bool IsHostile(Unit unit)
        {
            return unit.IsHostileFor(this);
        }

        public virtual bool IsHostile(Player player) { return false; }

        internal virtual bool IsHostile(AreaBomb bomb) { return false; }

        internal virtual bool IsHostile(Gate gate) { return false; }

        internal virtual bool IsHostile(SentryTurret turret) { return false; }

        internal virtual bool IsHostile(IndustrialTurret turret) { return false; }

        internal virtual bool IsHostile(IndustrialDrone turret) { return false; }

        internal virtual bool IsHostile(SupportDrone turret) { return false; }

        internal virtual bool IsHostile(Npc npc) { return false; }

        internal virtual bool IsHostile(CombatDrone drone) { return false; }

        internal virtual bool IsHostile(Rift rift)
        {
            return false;
        }

        internal virtual bool IsHostile(Portal portal)
        {
            return false;
        }

        internal virtual bool IsHostile(MobileTeleport teleport)
        {
            return false;
        }

        public void StopMoving()
        {
            CurrentSpeed = 0;
        }

        public IEnumerable<T> GetUnitsWithinRange<T>(double distance) where T : Unit
        {
            IZone zone = Zone;

            return zone == null
                ? Enumerable.Empty<T>()
                : zone.Units.OfType<T>().WithinRange(CurrentPosition, distance);
        }

        protected override void OnPropertyChanged(ItemProperty property)
        {
            base.OnPropertyChanged(property);

            switch (property.Field)
            {
                case AggregateField.blob_effect:
                    _sensorStrength.Update();
                    detectionStrength.Update();

                    break;
                case AggregateField.drone_amplification_core_max_modifier:
                    _coreMax.Update();

                    break;
                case AggregateField.drone_amplification_core_recharge_time_modifier:
                    _coreRechargeTime.Update();

                    break;
                case AggregateField.drone_amplification_reactor_radiation_modifier:
                    _reactorRadiation.Update();

                    break;
            }
        }

        public double ArmorPercentage => Armor.Ratio(ArmorMax);

        public double ArmorMax => _armorMax.Value;

        public double Armor
        {
            get => _armor.Value;
            set => _armor.SetValue(value);
        }

        public double ActualMass => _actualMass.Value;

        public double CorePercentage => Core.Ratio(CoreMax);

        public double CoreMax => _coreMax.Value;

        public double Core
        {
            get => _core.Value;
            set => _core.SetValue(value);
        }

        public double CriticalHitChance => _criticalHitChance.Value;

        public double SignatureRadius => _signatureRadius.Value;

        public double SensorStrength => _sensorStrength.Value;

        public double DetectionStrength => detectionStrength.Value;

        public virtual double StealthStrength => stealthStrength.Value;

        public double Massiveness => _massiveness.Value;

        public double ReactorRadiation => _reactorRadiation.Value;

        public double Slope => _slope.Value;

        public double Speed
        {
            get
            {
                double speedMax = this.speedMax.Value;
                return speedMax * _currentSpeed;
            }
        }

        public double MaxSpeed => speedMax.Value;

        public TimeSpan CoreRechargeTime => TimeSpan.FromSeconds(_coreRechargeTime.Value);

        private void InitUnitProperties()
        {
            _armorMax = new UnitProperty(
                this,
                AggregateField.armor_max,
                AggregateField.armor_max_modifier,
                AggregateField.effect_armor_max_modifier,
                AggregateField.drone_amplification_armor_max_modifier);

            _armorMax.PropertyChanged += property =>
            {
                if (Armor > property.Value)
                {
                    Armor = property.Value;
                }
            };

            AddProperty(_armorMax);

            _armor = new ArmorProperty(this);

            AddProperty(_armor);

            _coreMax = new UnitProperty(
                this,
                AggregateField.core_max,
                AggregateField.core_max_modifier,
                AggregateField.drone_amplification_core_max_modifier);
            AddProperty(_coreMax);

            _core = new CoreProperty(this);
            _core.PropertyChanged += property =>
            {
                if (property.Value > 1.0)
                {
                    return;
                }

                EffectHandler.RemoveEffectsByCategory(EffectCategory.effcat_zero_core_drop);
            };
            AddProperty(_core);

            _coreRechargeTime = new UnitProperty(
                this,
                AggregateField.core_recharge_time,
                AggregateField.core_recharge_time_modifier,
                AggregateField.drone_amplification_core_recharge_time_modifier,
                AggregateField.effect_core_recharge_time_modifier);
            AddProperty(_coreRechargeTime);

            _actualMass = new ActualMassProperty(this);
            AddProperty(_actualMass);

            speedMax = new SpeedMaxProperty(this);
            AddProperty(speedMax);

            _resistChemical = new UnitProperty(this, AggregateField.resist_chemical, AggregateField.resist_chemical_modifier, AggregateField.effect_resist_chemical);
            AddProperty(_resistChemical);

            _resistThermal = new UnitProperty(this, AggregateField.resist_thermal, AggregateField.resist_thermal_modifier, AggregateField.effect_resist_thermal);
            AddProperty(_resistThermal);

            _resistKinetic = new UnitProperty(this, AggregateField.resist_kinetic, AggregateField.resist_kinetic_modifier, AggregateField.effect_resist_kinetic);
            AddProperty(_resistKinetic);

            _resistExplosive = new UnitProperty(this, AggregateField.resist_explosive, AggregateField.resist_explosive_modifier, AggregateField.effect_resist_explosive);
            AddProperty(_resistExplosive);

            _slope = new UnitProperty(this, AggregateField.slope, AggregateField.slope_modifier);
            AddProperty(_slope);

            _criticalHitChance = new UnitProperty(this, AggregateField.critical_hit_chance, AggregateField.critical_hit_chance_modifier, AggregateField.effect_critical_hit_chance_modifier);
            AddProperty(_criticalHitChance);

            _massiveness = new UnitProperty(this, AggregateField.massiveness, AggregateField.massiveness_modifier, AggregateField.effect_massiveness);
            AddProperty(_massiveness);

            _signatureRadius = new UnitProperty(this, AggregateField.signature_radius, AggregateField.signature_radius_modifier, AggregateField.effect_signature_radius_modifier);
            AddProperty(_signatureRadius);

            _sensorStrength = new SensorStrengthProperty(this);
            AddProperty(_sensorStrength);

            stealthStrength = new UnitProperty(
                this,
                AggregateField.stealth_strength,
                AggregateField.stealth_strength_modifier,
                AggregateField.effect_stealth_strength_modifier,
                AggregateField.effect_dreadnought_stealth_strength_modifier,
                AggregateField.effect_excavator_stealth_strength_modifier);
            stealthStrength.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.Stealth;
            };
            AddProperty(stealthStrength);

            detectionStrength = new DetectionStrengthProperty(this);
            detectionStrength.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.Detection;
            };
            AddProperty(detectionStrength);

            _kersChemical = new UnitProperty(this, AggregateField.chemical_damage_to_core_modifier);
            AddProperty(_kersChemical);

            _kersThermal = new UnitProperty(this, AggregateField.thermal_damage_to_core_modifier);
            AddProperty(_kersThermal);

            _kersKinetic = new UnitProperty(this, AggregateField.kinetic_damage_to_core_modifier);
            AddProperty(_kersKinetic);

            _kersExplosive = new UnitProperty(this, AggregateField.explosive_damage_to_core_modifier);
            AddProperty(_kersExplosive);

            _reactorRadiation =
                new UnitProperty(
                    this,
                    AggregateField.reactor_radiation,
                    AggregateField.reactor_radiation_modifier,
                    AggregateField.drone_amplification_reactor_radiation_modifier);
            AddProperty(_reactorRadiation);
        }

        private class ArmorProperty : UnitProperty
        {
            public ArmorProperty(Unit owner)
                : base(owner, AggregateField.armor_current, AggregateField.drone_amplification_armor_max_modifier) { }

            protected override double CalculateValue()
            {
                double armor = owner.ArmorMax;

                if (owner.DynamicProperties.Contains(k.armor))
                {
                    double armorPercentage = owner.DynamicProperties.GetOrAdd<double>(k.armor);
                    armor = CalculateArmorByPercentage(armorPercentage);
                }

                return armor;
            }

            protected override void OnPropertyChanging(ref double newValue)
            {
                base.OnPropertyChanging(ref newValue);

                if (newValue < 0.0)
                {
                    newValue = 0.0;
                    return;
                }

                double armorMax = owner.ArmorMax;
                if (newValue >= armorMax)
                {
                    newValue = armorMax;
                }
            }

            private double CalculateArmorByPercentage(double percent)
            {
                if (double.IsNaN(percent))
                {
                    percent = 0.0;
                }

                // 0.0 - 1.0
                percent = percent.Clamp();

                double armorMax = owner.ArmorMax;

                if (double.IsNaN(armorMax))
                {
                    armorMax = 0.0;
                }

                double val = armorMax * percent;
                return val;
            }

        }

        private class CoreProperty : UnitProperty
        {
            public CoreProperty(Unit owner) : base(owner, AggregateField.core_current) { }

            protected override double CalculateValue()
            {
                double currentCore = owner.CoreMax;

                if (owner.DynamicProperties.Contains(k.currentCore))
                {
                    currentCore = owner.DynamicProperties.GetOrAdd<double>(k.currentCore);
                }

                return currentCore;
            }

            protected override void OnPropertyChanging(ref double newValue)
            {
                base.OnPropertyChanging(ref newValue);

                newValue = newValue.Clamp(1, owner.CoreMax);
            }
        }

        private class ActualMassProperty : UnitProperty
        {
            public ActualMassProperty(Unit owner) : base(owner, AggregateField.mass) { }

            protected override double CalculateValue()
            {
                double mass = owner.Mass;
                ItemPropertyModifier massMod = owner.GetPropertyModifier(AggregateField.mass_modifier);
                massMod.Modify(ref mass);

                if (owner is Robot robot)
                {
                    mass += robot.Modules.Sum(m => m.Mass);
                }

                return mass;
            }
        }

        private class SensorStrengthProperty : UnitProperty
        {
            public SensorStrengthProperty(Unit owner)
                : base(owner, AggregateField.sensor_strength, AggregateField.sensor_strength_modifier, AggregateField.effect_sensor_strength_modifier)
            {
            }

            protected override double CalculateValue()
            {
                double v = base.CalculateValue();

                IBlobableUnit blobableUnit = owner as IBlobableUnit;
                blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, 0.5);

                return v;
            }
        }

        public static Unit CreateUnitWithRandomEID(string definitionName)
        {
            return (Unit)Factory.Create(EntityDefault.GetByName(definitionName), EntityIDGenerator.Random);
        }

        public int BlockingRadius => ED.Config.blockingradius ?? 1;
        public double HitSize => ED.Config.HitSize;
    }
}