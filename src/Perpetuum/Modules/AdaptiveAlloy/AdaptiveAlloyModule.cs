using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Effects;
using System.Threading;

namespace Perpetuum.Modules.AdaptiveAlloy
{
    public sealed class AdaptiveAlloyModule : PassiveEffectModule
    {
        private double accumulatedKineticDamage = 1;
        private double accumulatedThermalDamage = 1;
        private double accumulatedExplosiveDamage = 1;
        private double accumulatedChemicalDamage = 1;
        private const double MaxAccumulatedValue = double.MaxValue / 2;

        public override void Unequip(Container container)
        {
            Interlocked.Exchange(ref accumulatedKineticDamage, 1.0);
            Interlocked.Exchange(ref accumulatedThermalDamage, 1.0);
            Interlocked.Exchange(ref accumulatedExplosiveDamage, 1.0);
            Interlocked.Exchange(ref accumulatedChemicalDamage, 1.0);
            base.Unequip(container);
        }

        public void RegisterDamage(DamageType damageType, double damage)
        {
            switch (damageType)
            {
                case DamageType.Kinetic:
                    InterlockedAddWithOverflowCheck(ref accumulatedKineticDamage, damage);

                    break;
                case DamageType.Thermal:
                    InterlockedAddWithOverflowCheck(ref accumulatedThermalDamage, damage);

                    break;
                case DamageType.Explosive:
                    InterlockedAddWithOverflowCheck(ref accumulatedExplosiveDamage, damage);

                    break;
                case DamageType.Chemical:
                    InterlockedAddWithOverflowCheck(ref accumulatedChemicalDamage, damage);

                    break;
            }

            SetRenewRequired();
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            double kineticDamage = Interlocked.CompareExchange(ref accumulatedKineticDamage, 0, 0);
            double thermalDamage = Interlocked.CompareExchange(ref accumulatedThermalDamage, 0, 0);
            double explosiveDamage = Interlocked.CompareExchange(ref accumulatedExplosiveDamage, 0, 0);
            double chemicalDamage = Interlocked.CompareExchange(ref accumulatedChemicalDamage, 0, 0);

            double accumulatedDamageSummary = kineticDamage + thermalDamage + explosiveDamage + chemicalDamage;

            ItemPropertyModifier adaptiveResistPoints = GetPropertyModifier(AggregateField.adaptive_resist_points);

            double adaptedKineticValue = adaptiveResistPoints.Value / accumulatedDamageSummary * kineticDamage;
            double adaptedThermalValue = adaptiveResistPoints.Value / accumulatedDamageSummary * thermalDamage;
            double adaptedExplosiveValue = adaptiveResistPoints.Value / accumulatedDamageSummary * explosiveDamage;
            double adaptedChemicalValue = adaptiveResistPoints.Value / accumulatedDamageSummary * chemicalDamage;

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

        private static void InterlockedAddWithOverflowCheck(ref double location, double value)
        {
            double initialValue, computedValue;
            do
            {
                initialValue = location;

                // Prevent overflow by capping at a max threshold
                if (initialValue >= MaxAccumulatedValue)
                {
                    computedValue = MaxAccumulatedValue;
                }
                else
                {
                    computedValue = initialValue + value;

                    // If adding the value results in overflow, cap it
                    if (computedValue > MaxAccumulatedValue)
                    {
                        computedValue = MaxAccumulatedValue;
                    }
                }
            }
            while (Interlocked.CompareExchange(ref location, computedValue, initialValue) != initialValue);
        }
    }
}
