using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Units.ItemProperties
{
    public class SpeedMaxProperty : ItemProperty
    {
        private readonly Unit owner;

        public SpeedMaxProperty(Unit owner)
            : base(AggregateField.speed_max)
        {
            this.owner = owner;
        }

        protected override double CalculateValue()
        {
            if (owner.EffectHandler.ContainsEffect(EffectType.effect_dreadnought))
            {
                return 0.05;
            }

            ItemPropertyModifier speedMax = owner.GetPropertyModifier(AggregateField.speed_max);
            ItemPropertyModifier speedMaxMod = owner.GetPropertyModifier(AggregateField.speed_max_modifier);
            speedMaxMod.Modify(ref speedMax);

            owner.ApplyEffectPropertyModifiers(AggregateField.effect_speed_max_modifier, ref speedMax);
            owner.ApplyEffectPropertyModifiers(AggregateField.drone_amplification_speed_max_modifier, ref speedMax);
            owner.ApplyEffectPropertyModifiers(AggregateField.effect_massivness_speed_max_modifier, ref speedMax);

            if (owner.ActualMass > 0)
            {
                speedMax.Multiply(owner.Mass / owner.ActualMass);
            }

            owner.ApplyEffectPropertyModifiers(AggregateField.effect_speed_highway_modifier, ref speedMax);

            return speedMax.Value;
        }

        protected override bool IsRelated(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.speed_max:
                case AggregateField.speed_max_modifier:
                case AggregateField.drone_amplification_speed_max_modifier:
                case AggregateField.effect_speed_max_modifier:
                case AggregateField.effect_massivness_speed_max_modifier:
                case AggregateField.effect_dreadnought_speed_max_modifier:
                case AggregateField.effect_speed_highway_modifier:
                    return true;
            }

            return false;
        }
    }
}
