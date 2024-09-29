using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Collections.Spatial
{
    public class QuadTree<T>
    {
        public QuadTreeNode<T> Root { get; }

        public QuadTree(Area area)
        {
            Root = new QuadTreeNode<T>(area);
        }

        public QuadTreeItem<T> Add(Point position, T value)
        {
            return Add(position.X, position.Y, value);
        }

        public QuadTreeItem<T> Add(int x, int y, T value)
        {
            return !Root.TryAdd(x, y, value, out QuadTreeItem<T> item) ? null : item;
        }

        public IEnumerable<QuadTreeItem<T>> Query(Area area)
        {
            Queue<QuadTreeNode<T>> q = new Queue<QuadTreeNode<T>>();
            q.Enqueue(Root);

            while (q.TryDequeue(out QuadTreeNode<T> node))
            {
                if (!area.IntersectsWith(node.Area))
                {
                    continue;
                }

                foreach (QuadTreeItem<T> item in node.GetItems())
                {
                    if (area.Contains(item.X, item.Y))
                    {
                        yield return item;
                    }
                }

                QuadTreeNode<T>[] nodes = node.GetNodes();
                if (nodes == null)
                {
                    continue;
                }

                for (int i = 0; i < 4; i++)
                {
                    q.Enqueue(nodes[i]);
                }
            }
        }
    }
}