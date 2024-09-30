using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.AdaptiveAlloy
{
    public sealed class AdaptiveAlloyModule : PassiveEffectModule
    {
        private double accumulatedKineticDamage = 1;
        private double accumulatedThermalDamage = 1;
        private double accumulatedExplosiveDamage = 1;
        private double accumulatedChemicalDamage = 1;

        public override void Unequip(Container container)
        {
            accumulatedKineticDamage = 1;
            accumulatedThermalDamage = 1;
            accumulatedExplosiveDamage = 1;
            accumulatedChemicalDamage = 1;
            base.Unequip(container);
        }

        public void RegisterDamage(DamageType damageType, double damage)
        {
            switch (damageType)
            {
                case DamageType.Kinetic:
                    accumulatedKineticDamage += damage;

                    break;
                case DamageType.Thermal:
                    accumulatedThermalDamage += damage;

                    break;
                case DamageType.Explosive:
                    accumulatedExplosiveDamage += damage;

                    break;
                case DamageType.Chemical:
                    accumulatedChemicalDamage += damage;

                    break;
            }
        }

        private double AccumulatedDamageSummary =>
            accumulatedKineticDamage +
            accumulatedThermalDamage +
            accumulatedExplosiveDamage +
            accumulatedChemicalDamage;

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            ItemPropertyModifier adaptiveResistPoints = GetPropertyModifier(AggregateField.adaptive_resist_points);

            double adaptedKineticValue = adaptiveResistPoints.Value / AccumulatedDamageSummary * accumulatedKineticDamage;
            double adaptedThermalValue = adaptiveResistPoints.Value / AccumulatedDamageSummary * accumulatedThermalDamage;
            double adaptedExplosiveValue = adaptiveResistPoints.Value / AccumulatedDamageSummary * accumulatedExplosiveDamage;
            double adaptedChemicalValue = adaptiveResistPoints.Value / AccumulatedDamageSummary * accumulatedChemicalDamage;

            ItemPropertyModifier kineticResistModifier =
                new ItemPropertyModifier(AggregateField.effect_resist_kinetic, AggregateFormula.Add, adaptedKineticValue);
            ItemPropertyModifier thermalResistModifier =
                new ItemPropertyModifier(AggregateField.effect_resist_thermal, AggregateFormula.Add, adaptedThermalValue);
            ItemPropertyModifier explosiveResistModifier =
                new ItemPropertyModifier(AggregateField.effect_resist_explosive, AggregateFormula.Add, adaptedExplosiveValue);
            ItemPropertyModifier chemicalResistModifier =
                new ItemPropertyModifier(AggregateField.effect_resist_chemical, AggregateFormula.Add, adaptedChemicalValue);

            effectBuilder
                .SetType(EffectType.effect_adaptive_alloy)
                .WithPropertyModifier(kineticResistModifier)
                .WithPropertyModifier(thermalResistModifier)
                .WithPropertyModifier(explosiveResistModifier)
                .WithPropertyModifier(chemicalResistModifier);
        }
    }
}
