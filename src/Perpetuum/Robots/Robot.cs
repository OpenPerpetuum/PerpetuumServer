using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Modules;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Insurance;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Robots
{
    public abstract partial class Robot : Unit
    {
        private Lazy<IEnumerable<Module>> _modules;
        private Lazy<IEnumerable<ActiveModule>> _activeModules;
        private Lazy<IEnumerable<Item>> _components;
        private Lazy<IEnumerable<RobotComponent>> _robotComponents;

        public RobotHelper RobotHelper { protected get; set; }
        public InsuranceHelper InsuranceHelper { protected get; set; }

        public RobotTemplate Template { get; set; }

        public bool IsSelected => RobotHelper.IsSelected(this);

        public override double Health => IsRepackaged
                    ? base.Health
                    : ArmorMax > 0.0 && Armor > 0.0
                    ? Armor.Ratio(ArmorMax) * 100
                    : base.Health;

        public override double Mass => RobotComponents.Sum(c => c.Mass);

        public bool HasModule => Modules.Any();

        public bool IsItemsInContainer
        {
            get
            {
                if (IsRepackaged)
                {
                    return false;
                }

                RobotInventory robotInventory = GetContainer();

                return robotInventory != null && robotInventory.HasChildren;
            }
        }

        public override bool IsStackable => base.IsStackable && IsRepackaged;

        public IEnumerable<Extension> ExtensionBonusEnablerExtensions => ExtensionBonuses.Select(cb => new Extension(cb.extensionId, 1));

        protected IEnumerable<ExtensionBonus> ExtensionBonuses => RobotComponents.SelectMany(component => component.ExtensionBonuses);

        public IEnumerable<Module> Modules => _modules.Value;

        public IEnumerable<ActiveModule> ActiveModules => _activeModules.Value;

        public IEnumerable<Item> Components => _components.Value;

        public IEnumerable<RobotComponent> RobotComponents => _robotComponents.Value;

        public override double Volume
        {
            get
            {
                if (IsRepackaged)
                {
                    return base.Volume;
                }

                double volume = RobotComponents.Sum(c => c.Volume);

                volume *= Quantity;

                return volume;
            }
        }

        public bool IsTrashed => Trashcan.IsItemTrashed(this);

        protected Robot()
        {
            InitLockHander();
            InitProperties();
        }

        public override void Initialize()
        {
            InitComponents();
            base.Initialize();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void VisitModules(IEntityVisitor visitor)
        {
            foreach (Module module in Modules)
            {
                module.AcceptVisitor(visitor);
            }
        }

        public void VisitRobotComponents(IEntityVisitor visitor)
        {
            foreach (RobotComponent component in RobotComponents)
            {
                component.AcceptVisitor(visitor);
            }
        }

        public void VisitRobotInventory(IEntityVisitor visitor)
        {
            RobotInventory container = GetContainer();

            container?.AcceptVisitor(visitor);
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            IDictionary<string, object> info = base.GetDebugInfo();

            info.Add("locksCount", _lockHandler.Count);

            return info;
        }

        public void FullArmorRepair()
        {
            DynamicProperties.Update(k.armor, 1.0);
            Armor = ArmorMax;
        }

        public void FullCoreRecharge()
        {
            double currentCore = DynamicProperties.GetOrAdd<double>(k.currentCore);
            double coreMaxValue = CoreMax;

            if (currentCore >= coreMaxValue)
            {
                return;
            }

            DynamicProperties.Update(k.currentCore, coreMaxValue);
            Core = CoreMax;
        }

        [CanBeNull]
        public RobotInventory GetContainer()
        {
            return Children.OfType<RobotInventory>().FirstOrDefault();
        }

        public void StopAllModules()
        {
            foreach (ActiveModule module in ActiveModules)
            {
                module.State.SwitchTo(ModuleStateType.Idle);
            }
        }

        public override void OnDeleteFromDb()
        {
            InsuranceHelper.DeleteAndInform(this);
            base.OnDeleteFromDb();
        }

        public void EmptyRobot(Character character, Container targetContainer, bool withContainer = true)
        {
            foreach (Module module in Modules)
            {
                module.Unequip(targetContainer);
            }

            if (!withContainer)
            {
                return;
            }

            RobotInventory container = GetContainer();

            container?.RelocateItems(character, character, container.GetItems(), targetContainer);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dictionary = base.ToDictionary();

            foreach (RobotComponent component in RobotComponents)
            {
                dictionary.Add(component.ComponentName, component.ToDictionary());
            }

            RobotInventory container = GetContainer();

            if (container != null)
            {
                dictionary.Add(k.container, container.ToDictionary());
            }

            dictionary.Add(k.decay, Decay);
            dictionary.Add(k.tint, Tint);

            return dictionary;
        }

        [CanBeNull]
        public Module FindModuleByCategoryFlag(CategoryFlags cf)
        {
            return Modules.FirstOrDefault(m => m.IsCategory(cf));
        }

        [CanBeNull]
        public Module GetModule(long moduleEid)
        {
            return Modules.FirstOrDefault(m => m.Eid == moduleEid);
        }

        [CanBeNull]
        public RobotComponent GetRobotComponent(string componentName)
        {
            return GetRobotComponent(componentName.ToEnum<RobotComponentType>());
        }

        [NotNull]
        public RobotComponent GetRobotComponentOrThrow(RobotComponentType componentType)
        {
            return GetRobotComponent(componentType).ThrowIfNull(ErrorCodes.RequiredComponentNotFound);
        }

        [CanBeNull]
        public RobotComponent GetRobotComponent(RobotComponentType componentType)
        {
            return RobotComponents.FirstOrDefault(c => c.Type == componentType);
        }

        public void CheckEnergySystemAndThrowIfFailed(Module module, bool isRemoving = false)
        {
            if (!CheckPowerGridForModule(module, isRemoving))
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfPowergrid);
            }

            if (!CheckCpuForModule(module, isRemoving))
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfCpu);
            }
        }

        public void CheckEnergySystemAndThrowIfFailed()
        {
            if (PowerGrid < 0)
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfPowergrid).SetData("powerGrid", Math.Abs(PowerGridMax - PowerGrid)).SetData("powerGridMax", PowerGridMax);
            }

            if (Cpu < 0)
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfCpu).SetData("cpu", Math.Abs(CpuMax - Cpu)).SetData("cpuMax", CpuMax);
            }
        }

        public void CreateComponents()
        {
            foreach (Item component in Template.BuildComponents())
            {
                AddChild(component);
            }
        }

        [CanBeNull]
        public T GetRobotComponent<T>() where T : RobotComponent
        {
            return RobotComponents.OfType<T>().FirstOrDefault();
        }

        protected virtual void OnLockError(Lock @lock, ErrorCodes error)
        {
        }

        protected virtual void OnLockStateChanged(Lock @lock)
        {
            States.LockSomething = _lockHandler.Count > 0;


            if (@lock is UnitLock unitLock)
            {
                UpdateTypes |= UnitUpdateTypes.Lock;
                UpdateVisibilityOf(unitLock.Target);
            }

            AnonymousBuilder<Packet> builder = new AnonymousBuilder<Packet>(() => LockPacketBuilder.BuildPacket(@lock));

            OnBroadcastPacket(builder.ToProxy());
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _lockHandler.Update(time);

            foreach (RobotComponent robotComponent in RobotComponents)
            {
                robotComponent.Update(time);
            }
        }

        protected internal override double ComputeHeight()
        {
            return RobotComponents.Sum(c => c.ComputeHeight());
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            double decayChance = this.decayChance.Value;
            double random = FastRandom.NextDouble();

            if (decayChance >= random)
            {
                int d = Decay;

                if (d > 0)
                {
                    d--;
                    Decay = d;
                }
            }
        }

        protected override bool IsDetected(Unit target)
        {
            return _lockHandler.IsLocked(target) || base.IsDetected(target);
        }

        protected override void OnBeforeRemovedFromZone(IZone zone)
        {
            Module remoteController = Modules?.FirstOrDefault(x => x is RemoteControllerModule);

            if (remoteController != null)
            {
                (remoteController as RemoteControllerModule).CloseAllChannels();
            }

            base.OnBeforeRemovedFromZone(zone);
        }

        private void InitComponents()
        {
            _components = new Lazy<IEnumerable<Item>>(() => Children.OfType<Item>().ToArray());
            _robotComponents = new Lazy<IEnumerable<RobotComponent>>(() => Components.OfType<RobotComponent>().ToArray());
            _modules = new Lazy<IEnumerable<Module>>(() => RobotComponents.SelectMany(c => c.Modules).ToArray());
            _activeModules = new Lazy<IEnumerable<ActiveModule>>(() => Modules.OfType<ActiveModule>().ToArray());
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            CamouflageUpdate();
        }
    }
}
