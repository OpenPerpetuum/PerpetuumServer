using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class DreadnoughtModule : EffectModule
    {
        private readonly ItemProperty effectSpeedMaxModifier;
        private readonly ItemProperty detectionStrengthModifier;
        private readonly ItemProperty stealthStrengthModifier;

        private readonly ItemProperty weaponCycleModifier;
        private readonly ItemProperty weaponRangeModifier;
        private readonly ItemProperty weaponDamageModifier;

        public DreadnoughtModule()
        {
            effectSpeedMaxModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_speed_max_modifier);
            AddProperty(effectSpeedMaxModifier);

            detectionStrengthModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_detection_strength_modifier);
            AddProperty(detectionStrengthModifier);

            stealthStrengthModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_stealth_strength_modifier);
            AddProperty(stealthStrengthModifier);

            weaponCycleModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_weapon_cycle_time_modifier);
            AddProperty(weaponCycleModifier);

            weaponRangeModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_optimal_range_modifier);
            AddProperty(weaponRangeModifier);

            weaponDamageModifier = new ModuleProperty(this, AggregateField.effect_dreadnought_weapon_damage_modifier);
            AddProperty(weaponDamageModifier);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            ItemPropertyModifier speedEffectProperty = effectSpeedMaxModifier.ToPropertyModifier();
            speedEffectProperty.Add(effectBuilder.Owner.Massiveness);

            if (speedEffectProperty.Value >= 1.0)
            {
                speedEffectProperty.ResetToDefaultValue();
            }

            effectBuilder
                .SetType(EffectType.effect_dreadnought)
                .WithPropertyModifier(speedEffectProperty)
                .WithPropertyModifier(detectionStrengthModifier.ToPropertyModifier())
                .WithPropertyModifier(stealthStrengthModifier.ToPropertyModifier())
                .WithPropertyModifier(weaponCycleModifier.ToPropertyModifier())
                .WithPropertyModifier(weaponRangeModifier.ToPropertyModifier())
                .WithPropertyModifier(weaponDamageModifier.ToPropertyModifier());
        }
    }
}
