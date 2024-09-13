using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.RemoteControl;

namespace Perpetuum.Modules
{
    public class IndustrialRemoteControllerModule : RemoteControllerModule
    {
        private readonly ModuleProperty droneMiningAmount;
        private readonly ModuleProperty droneHarvestingAmount;

        public IndustrialRemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            droneMiningAmount = new ModuleProperty(this, AggregateField.drone_amplification_mining_amount_modifier);
            AddProperty(droneMiningAmount);

            droneHarvestingAmount = new ModuleProperty(this, AggregateField.drone_amplification_harvesting_amount_modifier);
            AddProperty(droneHarvestingAmount);
        }

        public override RemoteControlledCreature CreateAndConfigureRcu(RemoteControlledUnit ammo)
        {
            RemoteControlledCreature remoteControlledCreature = null;
            if (ammo.ED.Options.TurretType == TurretType.IndustrialDrone)
            {
                remoteControlledCreature = (IndustrialDrone)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                (remoteControlledCreature as IndustrialDrone).SetTurretType(ammo.ED.Options.TurretType);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledDrone);
                (remoteControlledCreature as IndustrialDrone).GuardRange = 5;
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
            double miningAmountModifier = GetPropertyModifier(AggregateField.drone_amplification_mining_amount_modifier).Value;
            double harvestingAmountModifier = GetPropertyModifier(AggregateField.drone_amplification_harvesting_amount_modifier).Value;
            double coreMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_core_max_modifier).Value;
            double coreRechargeTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier).Value;
            double speedMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_speed_max_modifier).Value;
            double reactorRadiationModifier = GetPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier).Value;

            _ = effectBuilder
                .SetType(EffectType.drone_amplification)
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_locking_time_modifier, AggregateFormula.Inverse, lockingTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_armor_max_modifier, AggregateFormula.Modifier, armorMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_mining_amount_modifier, AggregateFormula.Modifier, miningAmountModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_harvesting_amount_modifier, AggregateFormula.Modifier, harvestingAmountModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_max_modifier, AggregateFormula.Modifier, coreMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier, AggregateFormula.Inverse, coreRechargeTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_speed_max_modifier, AggregateFormula.Modifier, speedMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier, AggregateFormula.Inverse, reactorRadiationModifier));
        }
    }
}
