using Perpetuum.ExportedTypes;

namespace Perpetuum.Items
{
    public class InfoProperty<T> : ItemProperty where T : Item
    {
        protected readonly T item;

        public InfoProperty(T item, AggregateField field)
            : base(field)
        {
            this.item = item;
        }

        protected override double CalculateValue()
        {
            ItemPropertyModifier mod = item.GetPropertyModifier(Field);

            return mod.Value;
        }
    }
}
