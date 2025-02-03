using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public sealed class ExcavatorModule : EffectModule
    {
        private readonly ItemProperty miningAmountModifier;
        private readonly ItemProperty harvestingAmountModifier;
        private readonly ItemProperty stealthStrengthModifier;
        private readonly ItemProperty effectEnhancerAuraRadiusModifier;

        public ExcavatorModule()
        {
            miningAmountModifier = new ModuleProperty(this, AggregateField.effect_excavator_mining_amount_modifier);
            AddProperty(miningAmountModifier);

            harvestingAmountModifier = new ModuleProperty(this, AggregateField.effect_excavator_harvesting_amount_modifier);
            AddProperty(harvestingAmountModifier);

            stealthStrengthModifier = new ModuleProperty(this, AggregateField.effect_excavator_stealth_strength_modifier);
            AddProperty(stealthStrengthModifier);

            effectEnhancerAuraRadiusModifier = new ModuleProperty(this, AggregateField.effect_enhancer_aura_radius_modifier);
            AddProperty(effectEnhancerAuraRadiusModifier);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder
                .SetType(EffectType.effect_excavator)
                .SetOwnerToSource()
                .WithPropertyModifier(stealthStrengthModifier.ToPropertyModifier())
                .WithPropertyModifier(miningAmountModifier.ToPropertyModifier())
                .WithPropertyModifier(harvestingAmountModifier.ToPropertyModifier())
                .WithRadiusModifier(effectEnhancerAuraRadiusModifier.Value);
        }
    }
}
