using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.RemoteControl;

namespace Perpetuum.Modules
{
    public class SupportRemoteControllerModule : RemoteControllerModule
    {
        private readonly ModuleProperty droneRemoteRepairAmount;
        private readonly ModuleProperty droneRemoteRepairCycleTime;

        public SupportRemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            droneRemoteRepairAmount = new ModuleProperty(this, AggregateField.drone_amplification_remote_repair_amount_modifier);
            AddProperty(droneRemoteRepairAmount);

            droneRemoteRepairCycleTime = new ModuleProperty(this, AggregateField.drone_amplification_remote_repair_cycle_time_modifier);
            AddProperty(droneRemoteRepairCycleTime);
        }

        public override RemoteControlledCreature CreateAndConfigureRcu(RemoteControlledUnit ammo)
        {
            RemoteControlledCreature remoteControlledCreature = null;
            if (ammo.ED.Options.TurretType == TurretType.CombatDrone)
            {
                remoteControlledCreature = (SupportDrone)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledDrone);
                (remoteControlledCreature as SupportDrone).GuardRange = 5;
            }
            else
            {
                _ = PerpetuumException.Create(ErrorCodes.InvalidAmmoDefinition);
            }

            return remoteControlledCreature;
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            double lockingTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_locking_time_modifier).Value;
            double armorMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_armor_max_modifier).Value;
            double remoteRepairAmount = GetPropertyModifier(AggregateField.drone_amplification_remote_repair_amount_modifier).Value;
            double remoteRepairCycleTime = GetPropertyModifier(AggregateField.drone_amplification_remote_repair_cycle_time_modifier).Value;
            double coreMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_core_max_modifier).Value;
            double coreRechargeTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier).Value;
            double speedMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_speed_max_modifier).Value;
            double reactorRadiationModifier = GetPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier).Value;

            _ = effectBuilder
                .SetType(EffectType.drone_amplification)
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_locking_time_modifier, AggregateFormula.Inverse, lockingTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_armor_max_modifier, AggregateFormula.Inverse, armorMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_remote_repair_amount_modifier, AggregateFormula.Modifier, remoteRepairAmount))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_remote_repair_cycle_time_modifier, AggregateFormula.Inverse, remoteRepairCycleTime))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_max_modifier, AggregateFormula.Modifier, coreMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier, AggregateFormula.Inverse, coreRechargeTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_speed_max_modifier, AggregateFormula.Modifier, speedMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier, AggregateFormula.Inverse, reactorRadiationModifier));
        }
    }
}
