using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public class DetectionModule : EffectModule
    {
        private readonly ItemProperty detectionStrengthModifier;
        private readonly ItemProperty stealthStrengthModifier;

        public DetectionModule()
        {
            detectionStrengthModifier = new ModuleProperty(this, AggregateField.effect_detection_strength_modifier);
            AddProperty(detectionStrengthModifier);
            stealthStrengthModifier = new ModuleProperty(this, AggregateField.effect_stealth_strength_modifier);
            AddProperty(stealthStrengthModifier);
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder
                .SetType(EffectType.effect_detection)
                .WithPropertyModifier(detectionStrengthModifier.ToPropertyModifier())
                .WithPropertyModifier(stealthStrengthModifier.ToPropertyModifier());
        }
    }
}
