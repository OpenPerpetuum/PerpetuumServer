using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Zones.Effects;
using System;

namespace Perpetuum.Modules.EffectModules
{
    public class ShieldGeneratorModule : EffectModule
    {
        private readonly ItemProperty shieldRadius;
        private readonly ModuleProperty shieldAbsorbtion;

        public ShieldGeneratorModule()
        {
            shieldRadius = new ModuleProperty(this, AggregateField.shield_radius);
            AddProperty(shieldRadius);
            shieldAbsorbtion = new ModuleProperty(this, AggregateField.shield_absorbtion);
            shieldAbsorbtion.AddEffectModifier(AggregateField.effect_shield_absorbtion_modifier);
            shieldAbsorbtion.AddEffectModifier(AggregateField.nox_shield_absorbtion_modifier);

            AddProperty(shieldAbsorbtion);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.shield_absorbtion:
                case AggregateField.shield_absorbtion_modifier:
                case AggregateField.effect_shield_absorbtion_modifier:
                case AggregateField.nox_shield_absorbtion_modifier:
                    shieldAbsorbtion.Update();
                    break;
            }

            base.UpdateProperty(field);
        }

        public double AbsorbtionModifier
        {
            get
            {
                double ratio = ParentRobot.SignatureRadius / shieldRadius.Value;
                ratio = Math.Max(ratio, 1.0);
                double result = 1 / shieldAbsorbtion.Value * ratio;
                return result;
            }
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            _ = effectBuilder.SetType(EffectType.effect_shield);
        }
    }
}