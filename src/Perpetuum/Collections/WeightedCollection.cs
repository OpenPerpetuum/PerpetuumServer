using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Collections
{
    /// <summary>
    /// Collection that provides random access to stored items where weights bias the probability of selection
    /// </summary>
    /// <typeparam name="T">Any data to be kept in the list</typeparam>
    public class WeightedCollection<T>
    {
        private readonly List<WeightedEntry<T>> list = new List<WeightedEntry<T>>();
        private int sumWeights = 0;
        public void Add(T item, int weight = 1)
        {
            sumWeights += weight;
            list.Add(new WeightedEntry<T>(item, weight));
        }

        public void Clear()
        {
            sumWeights = 0;
            list.Clear();
        }

        public T GetRandom()
        {
            if (sumWeights == 0)
            {
                return default;
            }

            if (list.Count == 1)
            {
                return list.First().Item;
            }

            int weightTarget = FastRandom.NextInt(sumWeights - 1);
            int current = 0;
            List<WeightedEntry<T>>.Enumerator iterator = list.GetEnumerator();
            while (iterator.MoveNext())
            {
                current += iterator.Current.Weight;
                if (current > weightTarget)
                {
                    break;
                }
            }

            return iterator.Current != null ? iterator.Current.Item : default;
        }
    }
}
