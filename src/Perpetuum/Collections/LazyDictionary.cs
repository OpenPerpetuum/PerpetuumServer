using System;
using System.Collections;
using System.Collections.Generic;

namespace Perpetuum.Collections
{
    public class LazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Lazy<IDictionary<TKey, TValue>> dictionary;

        public LazyDictionary(Func<IDictionary<TKey, TValue>> dictionaryFactory)
        {
            dictionary = new Lazy<IDictionary<TKey, TValue>>(dictionaryFactory);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dictionary.Value.Add(item);
        }

        public void Clear()
        {
            dictionary.Value.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Value.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.Value.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Value.Remove(item);
        }

        public int Count => dictionary.Value.Count;

        public bool IsReadOnly => dictionary.Value.IsReadOnly;

        public bool ContainsKey(TKey key)
        {
            return dictionary.Value.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            dictionary.Value.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return dictionary.Value.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.Value.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => dictionary.Value[key];
            set => dictionary.Value[key] = value;
        }

        public ICollection<TKey> Keys => dictionary.Value.Keys;

        public ICollection<TValue> Values => dictionary.Value.Values;
    }
}