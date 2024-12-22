using Perpetuum.ExportedTypes;

namespace Perpetuum.Units.UnitProperties
{
    public class ArmorProperty : UnitProperty
    {
        public ArmorProperty(Unit owner)
            : base(
                  owner,
                  AggregateField.armor_current)
        { }

        protected override double CalculateValue()
        {
            double armor = owner.ArmorMax;

            if (owner.DynamicProperties.Contains(k.armor))
            {
                double armorPercentage = owner.DynamicProperties.GetOrAdd<double>(k.armor);
                armor = CalculateArmorByPercentage(armorPercentage);
            }

            return armor;
        }

        protected override void OnPropertyChanging(ref double newValue)
        {
            base.OnPropertyChanging(ref newValue);

            if (newValue < 0.0)
            {
                newValue = 0.0;
                return;
            }

            double armorMax = owner.ArmorMax;
            if (newValue >= armorMax)
            {
                newValue = armorMax;
            }
        }

        private double CalculateArmorByPercentage(double percent)
        {
            if (double.IsNaN(percent))
            {
                percent = 0.0;
            }

            // 0.0 - 1.0
            percent = percent.Clamp();

            double armorMax = owner.ArmorMax;

            if (double.IsNaN(armorMax))
            {
                armorMax = 0.0;
            }

            double val = armorMax * percent;
            return val;
        }
    }
}
