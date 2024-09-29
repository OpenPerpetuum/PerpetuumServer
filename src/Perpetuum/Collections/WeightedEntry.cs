namespace Perpetuum.Collections
{
    internal class WeightedEntry<T>
    {
        public T Item { get; private set; }
        public int Weight { get; private set; }
        public WeightedEntry(T item, int weight)
        {
            Item = item;
            Weight = weight;
        }
    }
}
