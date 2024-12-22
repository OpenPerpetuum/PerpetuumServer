using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.RemoteControl;

namespace Perpetuum.Modules
{
    public class TacticalRemoteControllerModule : RemoteControllerModule
    {
        private readonly ModuleProperty droneDamage;
        private readonly ModuleProperty droneCycleTime;
        private readonly ModuleProperty droneLongRange;
        private readonly ModuleProperty droneAccuracy;

        public TacticalRemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            droneDamage = new ModuleProperty(this, AggregateField.drone_amplification_damage_modifier);
            AddProperty(droneDamage);

            droneCycleTime = new ModuleProperty(this, AggregateField.drone_amplification_cycle_time_modifier);
            AddProperty(droneCycleTime);

            droneLongRange = new ModuleProperty(this, AggregateField.drone_amplification_long_range_modifier);
            AddProperty(droneLongRange);
            droneAccuracy = new ModuleProperty(this, AggregateField.drone_amplification_accuracy_modifier);
            AddProperty(droneAccuracy);
        }

        public override RemoteControlledCreature CreateAndConfigureRcu(RemoteControlledUnit ammo)
        {
            RemoteControlledCreature remoteControlledCreature = null;
            if (ammo.ED.Options.TurretType == TurretType.CombatDrone)
            {
                remoteControlledCreature = (CombatDrone)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledDrone);
                (remoteControlledCreature as CombatDrone).GuardRange = 5;
            }
            else
            {
                _ = PerpetuumException.Create(ErrorCodes.InvalidAmmoDefinition);
            }

            return remoteControlledCreature;
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            double damageModifier = GetPropertyModifier(AggregateField.drone_amplification_damage_modifier).Value;
            double lockingTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_locking_time_modifier).Value;
            double cycleTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_cycle_time_modifier).Value;
            double armorMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_armor_max_modifier).Value;
            double coreMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_core_max_modifier).Value;
            double coreRechargeTimeModifier = GetPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier).Value;
            double longRangeModifier = GetPropertyModifier(AggregateField.drone_amplification_long_range_modifier).Value;
            double accuracyModifier = GetPropertyModifier(AggregateField.drone_amplification_accuracy_modifier).Value;
            double speedMaxModifier = GetPropertyModifier(AggregateField.drone_amplification_speed_max_modifier).Value;
            double reactorRadiationModifier = GetPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier).Value;

            _ = effectBuilder
                .SetType(EffectType.drone_amplification)
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_damage_modifier, AggregateFormula.Modifier, damageModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_locking_time_modifier, AggregateFormula.Inverse, lockingTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_cycle_time_modifier, AggregateFormula.Inverse, cycleTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_armor_max_modifier, AggregateFormula.Modifier, armorMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_max_modifier, AggregateFormula.Modifier, coreMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_core_recharge_time_modifier, AggregateFormula.Inverse, coreRechargeTimeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_long_range_modifier, AggregateFormula.Modifier, longRangeModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_accuracy_modifier, AggregateFormula.Inverse, accuracyModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_speed_max_modifier, AggregateFormula.Modifier, speedMaxModifier))
                .WithPropertyModifier(new ItemPropertyModifier(AggregateField.drone_amplification_reactor_radiation_modifier, AggregateFormula.Inverse, reactorRadiationModifier));
        }
    }
}
