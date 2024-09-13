using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Zones;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Modules
{
    public class Module : Item
    {
        private readonly ItemProperty _powerGridUsage;
        private readonly ItemProperty _cpuUsage;

        public Module()
        {
            _powerGridUsage = new ModuleProperty(this, AggregateField.powergrid_usage);
            AddProperty(_powerGridUsage);
            _cpuUsage = new ModuleProperty(this, AggregateField.cpu_usage);
            AddProperty(_cpuUsage);
        }

        public ILookup<AggregateField, AggregateField> PropertyModifiers { get; set; }

        public double PowerGridUsage => _powerGridUsage.Value;

        public double CpuUsage => _cpuUsage.Value;

        public IEnumerable<AggregateField> GetPropertyModifiers()
        {
            return PropertyModifiers.SelectMany(g => g);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        [CanBeNull]
        public RobotComponent ParentComponent => GetOrLoadParentEntity() as RobotComponent;

        [CanBeNull]
        public Robot ParentRobot => ParentComponent?.ParentRobot;

        [CanBeNull]
        protected IZone Zone => ParentRobot?.Zone;

        public bool ParentIsPlayer()
        {
            return ParentRobot is Player;
        }

        public int Slot
        {
            get => DynamicProperties.GetOrDefault<int>(k.slot);
            set => DynamicProperties.Update(k.slot, value);
        }

        public long ModuleFlag => ED.Options.ModuleFlag;

        public virtual void Unequip(Container container)
        {
            if (!IsRepackaged)
            {
                this.Pack();
            }

            container.AddItem(this, true);
            Slot = 0;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = base.ToDictionary();

            result.Add(k.slot, Slot);
            result.Add(k.state, (byte)ModuleStateType.Idle);

            return result;
        }

        public bool IsPassive => ED.AttributeFlags.PassiveModule;

        protected virtual void OnUpdateProperty(AggregateField field)
        {
        }

        public virtual void UpdateProperty(AggregateField field)
        {
            OnUpdateProperty(field);
        }

        public Packet BuildModuleInfoPacket()
        {
            Packet packet = new Packet(ZoneCommand.ModuleInfoResult);

            packet.AppendByte((byte)ParentComponent.Type);
            packet.AppendByte((byte)Slot);

            List<ItemProperty> properties = Properties.ToList();

            packet.AppendByte((byte)properties.Count);

            foreach (ItemProperty property in properties)
            {
                property.AppendToPacket(packet);
            }

            return packet;
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            ItemPropertyModifier modifier = base.GetPropertyModifier(field);
            ApplyRobotPropertyModifiers(ref modifier);
            return modifier;
        }

        public void ApplyRobotPropertyModifiers(ref ItemPropertyModifier modifier)
        {
            AggregateField[] modifiers = PropertyModifiers.GetOrEmpty(modifier.Field);

            foreach (AggregateField m in modifiers)
            {
                ParentRobot?.GetPropertyModifier(m).Modify(ref modifier);
            }
        }

        public void SimulateRobotPropertyModifiers(Robot parent, ref ItemPropertyModifier modifier)
        {
            AggregateField[] modifiers = PropertyModifiers.GetOrEmpty(modifier.Field);

            foreach (AggregateField m in modifiers)
            {
                parent.GetPropertyModifier(m).Modify(ref modifier);
            }
        }
    }
}