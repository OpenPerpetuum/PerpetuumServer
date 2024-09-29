using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Collections
{
    public class LazyLookup<TKey, TValue> : ILookup<TKey, TValue>
    {
        private readonly Lazy<ILookup<TKey, TValue>> lookup;

        public LazyLookup(Func<ILookup<TKey, TValue>> lookupFactory)
        {
            lookup = new Lazy<ILookup<TKey, TValue>>(lookupFactory);
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            return lookup.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(TKey key)
        {
            return lookup.Value.Contains(key);
        }

        public int Count => lookup.Value.Count;

        public IEnumerable<TValue> this[TKey key] => lookup.Value[key];
    }
}