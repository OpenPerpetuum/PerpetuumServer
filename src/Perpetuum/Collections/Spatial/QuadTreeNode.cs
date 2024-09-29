using System.Collections.Generic;

namespace Perpetuum.Collections.Spatial
{
    public class QuadTreeNode<T>
    {
        private const int CAPACITY = 4;
        private readonly List<QuadTreeItem<T>> items = new List<QuadTreeItem<T>>(CAPACITY);

        private QuadTreeNode<T>[] nodes;

        public QuadTreeItem<T>[] GetItems()
        {
            return items.ToArray();
        }

        public QuadTreeNode<T>[] GetNodes()
        {
            return nodes;
        }

        public Area Area { get; }

        public QuadTreeNode(Area area)
        {
            Area = area;
        }

        public bool TryAdd(int x, int y, T value, out QuadTreeItem<T> item)
        {
            if (!Area.Contains(x, y))
            {
                item = null;

                return false;
            }

            if (items.Count < CAPACITY)
            {
                item = new QuadTreeItem<T>(this, x, y, value);
                items.Add(item);

                return true;
            }

            if (nodes == null)
            {
                nodes = new QuadTreeNode<T>[CAPACITY];
                int w = Area.Width / 2;
                int h = Area.Height / 2;

                nodes[0] = new QuadTreeNode<T>(Area.FromRectangle(Area.X1, Area.Y1, w, h));
                nodes[1] = new QuadTreeNode<T>(Area.FromRectangle(Area.X1 + w, Area.Y1, w, h));
                nodes[2] = new QuadTreeNode<T>(Area.FromRectangle(Area.X1, Area.Y1 + h, w, h));
                nodes[3] = new QuadTreeNode<T>(Area.FromRectangle(Area.X1 + w, Area.Y1 + h, w, h));
            }

            for (int i = 0; i < 4; i++)
            {
                if (nodes[i].TryAdd(x, y, value, out item))
                {
                    return true;
                }
            }

            item = null;
            return false;
        }

        public void Remove(QuadTreeItem<T> item)
        {
            items.Remove(item);
        }
    }
}