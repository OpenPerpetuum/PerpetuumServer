using System;
using System.Collections.Generic;

namespace Perpetuum.Collections
{
    public class PriorityQueue<T>
    {
        private readonly int capacity;
        private int count;
        private T[] items;
        private readonly IComparer<T> comparer;

        public PriorityQueue(int capacity, IComparer<T> comparer = null)
        {
            this.capacity = capacity;
            items = new T[capacity + 1];
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        public void Enqueue(T item)
        {
            if (count >= items.Length - 1)
            {
                Array.Resize(ref items, items.Length + (capacity / 2));
            }

            count++;
            items[count] = item;

            int m = count;

            while (m > 1)
            {
                int parentIndex = m / 2;

                T parentItem = items[parentIndex];
                T currentItem = items[m];

                if (comparer.Compare(currentItem, parentItem) >= 0)
                {
                    break;
                }

                items[parentIndex] = currentItem;
                items[m] = parentItem;
                m = parentIndex;
            }
        }

        public bool TryDequeue(out T item)
        {
            if (count < 1)
            {
                item = default;

                return false;
            }

            item = items[1];
            items[1] = items[count];

            count--;

            if (count == 0)
            {
                return true;
            }

            int v = 1;

            while (true)
            {
                int u = v;

                if (((2 * u) + 1) <= count)
                {
                    if (comparer.Compare(items[u], items[2 * u]) >= 0)
                    {
                        v = 2 * u;
                    }

                    if (comparer.Compare(items[v], items[(2 * u) + 1]) >= 0)
                    {
                        v = (2 * u) + 1;
                    }
                }
                else if (2 * u <= count)
                {
                    if (comparer.Compare(items[u], items[2 * u]) >= 0)
                    {
                        v = 2 * u;
                    }
                }

                if (u == v)
                {
                    break;
                }

                (items[v], items[u]) = (items[u], items[v]);
            }

            return true;
        }
    }
}