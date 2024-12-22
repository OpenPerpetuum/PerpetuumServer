using Perpetuum.ExportedTypes;
using System;
using System.Collections.Generic;

namespace Perpetuum.Items
{
    public struct ItemPropertyModifier
    {
        private readonly AggregateFormula formula;
        private readonly bool forceHasValue;

        public ItemPropertyModifier(AggregateField field, AggregateFormula formula, double value, bool forceHasValue = false) : this()
        {
            Field = field;
            this.formula = formula;
            Value = value;
            this.forceHasValue = forceHasValue;
        }

        public AggregateField Field { get; }

        public double Value { get; private set; }

        public bool HasValue => forceHasValue || Math.Abs(Field.GetDefaultValue() - Value) > double.Epsilon;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID,(int)Field},
                {k.value,Value}
            };
        }

        public void AddToDictionary(IDictionary<string, object> dictionary)
        {
            if (dictionary == null || !HasValue)
            {
                return;
            }

            dictionary["a" + (int)Field] = ToDictionary();
        }

        public static ItemPropertyModifier Create(string fieldName, string value)
        {
            AggregateField f = (AggregateField)Enum.Parse(typeof(AggregateField), fieldName);
            double v = double.Parse(value);

            return Create(f, v);
        }

        public static ItemPropertyModifier Create(AggregateField field)
        {
            return Create(field, field.GetDefaultValue());
        }

        public static ItemPropertyModifier Create(AggregateField field, double value, bool forceHasValue = false)
        {
            AggregateFormula formula = field.GetFormula();

            return new ItemPropertyModifier(field, formula, value, forceHasValue);
        }

        public void NormalizeExtensionBonus()
        {
            switch (formula)
            {
                case AggregateFormula.Modifier:
                    {
                        Value += 1.0;

                        break;
                    }
                case AggregateFormula.Inverse:
                    {
                        Value = 1 / (Value + 1);
                        if (Value < 0.1)
                        {
                            Value = 0.1;
                        }

                        break;
                    }
            }
        }

        public void AppendToPacket(BinaryStream binaryStream)
        {
            binaryStream.AppendInt((int)Field);
            binaryStream.AppendDouble(Value);
        }

        public void ResetToDefaultValue()
        {
            Value = Field.GetDefaultValue();
        }

        public void Add(double value)
        {
            Value += value;
        }

        public void Multiply(double mul)
        {
            if (mul > 0)
            {
                Value *= mul;
            }
        }

        public void Modify(ref ItemPropertyModifier targetModifier)
        {
            Modify(this, ref targetModifier);
        }

        public void Modify(ref double targetValue)
        {
            Modify(this, ref targetValue);
        }

        public static void Modify(ItemPropertyModifier source, ref ItemPropertyModifier targetModifier)
        {
            targetModifier = Modify(source, targetModifier);
        }

        public static ItemPropertyModifier Modify(ItemPropertyModifier source, ItemPropertyModifier target)
        {
            if (!source.HasValue)
            {
                return target;
            }

            double v = target.Value;
            Modify(source, ref v);

            return new ItemPropertyModifier(target.Field, target.formula, v);
        }

        public static void Modify(ItemPropertyModifier source, ref double targetValue)
        {
            if (!source.HasValue)
            {
                return;
            }

            switch (source.formula)
            {
                // mod & inverse
                case AggregateFormula.Modifier:
                case AggregateFormula.Inverse:
                    {
                        targetValue *= source.Value;

                        break;
                    }

                // add
                case AggregateFormula.Add:
                    {
                        targetValue += source.Value;

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"Field: {Field}, Formula: {formula}, Value: {Value}";
        }
    }
}