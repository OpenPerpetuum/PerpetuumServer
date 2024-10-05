using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Units
{
    public class UnitProperty : ItemProperty
    {
        protected readonly Unit owner;
        private readonly AggregateField modifierField;
        private readonly IList<AggregateField> effectModifiers;

        public UnitProperty(
            Unit owner,
            AggregateField field,
            AggregateField modifierField = AggregateField.undefined,
            params AggregateField[] effectModifiers) : base(field)
        {
            this.owner = owner;
            this.modifierField = modifierField;
            if (effectModifiers == null)
            {
                return;
            }

            this.effectModifiers = new List<AggregateField>();
            foreach (AggregateField effectModifier in effectModifiers)
            {
                this.effectModifiers.Add(effectModifier);
            }
        }

        protected override double CalculateValue()
        {
            ItemPropertyModifier m = owner.GetPropertyModifier(Field);
            if (!effectModifiers.IsNullOrEmpty())
            {
                foreach (AggregateField effectModifier in effectModifiers)
                {
                    owner.ApplyEffectPropertyModifiers(effectModifier, ref m);
                }
            }

            if (modifierField == AggregateField.undefined)
            {
                return m.Value;
            }

            ItemPropertyModifier mod = owner.GetPropertyModifier(modifierField);
            mod.Modify(ref m);

            return m.Value;
        }

        protected override bool IsRelated(AggregateField field)
        {
            return modifierField == field || (effectModifiers != null
                ? effectModifiers.Any(m => m == field)
                : base.IsRelated(field));
        }

    }
}