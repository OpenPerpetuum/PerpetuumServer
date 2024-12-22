using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;

namespace Perpetuum.Modules
{
    public abstract class ArmorRepairerBaseModule : ActiveModule
    {
        protected readonly ModuleProperty armorRepairAmount;

        protected ArmorRepairerBaseModule(bool ranged) : base(ranged)
        {
            armorRepairAmount = new ModuleProperty(this, AggregateField.armor_repair_amount);
            armorRepairAmount.AddEffectModifier(AggregateField.effect_repair_amount_modifier);
            armorRepairAmount.AddEffectModifier(AggregateField.drone_amplification_remote_repair_amount_modifier);
            armorRepairAmount.AddEffectModifier(AggregateField.drone_remote_command_translation_remote_repair_amount_modifier);
            armorRepairAmount.AddEffectModifier(AggregateField.nox_repair_amount_modifier);
            AddProperty(armorRepairAmount);

            cycleTime
                .AddEffectModifier(AggregateField.drone_amplification_remote_repair_cycle_time_modifier);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.armor_repair_amount:
                case AggregateField.armor_repair_amount_modifier:
                case AggregateField.effect_repair_amount_modifier:
                case AggregateField.nox_repair_amount_modifier:
                case AggregateField.drone_amplification_remote_repair_amount_modifier:
                case AggregateField.drone_remote_command_translation_remote_repair_amount_modifier:
                    {
                        armorRepairAmount.Update();

                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected void OnRepair(Unit target, double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            double armor = target.Armor;

            target.Armor += amount;

            double total = Math.Abs(armor - target.Armor);
            CombatLogPacket packet = new CombatLogPacket(CombatLogType.ArmorRepair, target, ParentRobot, this);

            packet.AppendDouble(amount);
            packet.AppendDouble(total);
            packet.Send(target, ParentRobot);
        }
    }

    public sealed class ArmorRepairModule : ArmorRepairerBaseModule
    {
        public ArmorRepairModule() : base(false)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            double amount = armorRepairAmount.Value;

            OnRepair(ParentRobot, amount);

            double threatAmount = Math.Sqrt(amount).Clamp(1, 100);

            ParentRobot.SpreadAssistThreatToNpcs(ParentRobot, new Threat(ThreatType.Support, threatAmount));
        }
    }
}