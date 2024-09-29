using System;
using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Collections.Spatial
{
    public class Grid<TCell> where TCell : Cell
    {
        private readonly TCell[] cells;
        private readonly Dictionary<TCell, List<TCell>> neighbours = new Dictionary<TCell, List<TCell>>();

        private readonly int width;
        private readonly int height;
        private readonly int numCellsX;
        private readonly int numCellsY;
        private readonly int cellSizeX;
        private readonly int cellSizeY;

        public Grid(int width, int height, int cellsX, int cellsY, Func<Area, TCell> cellFactory)
        {
            this.width = width;
            this.height = height;
            numCellsX = cellsX;
            numCellsY = cellsY;

            cellSizeX = width / cellsX;
            cellSizeY = height / cellsY;

            cells = new TCell[numCellsX * numCellsY];

            for (int y = 0; y < numCellsY; y++)
            {
                for (int x = 0; x < numCellsX; x++)
                {
                    Area area = Area.FromRectangle(x * cellSizeX, y * cellSizeY, cellSizeX, cellSizeY);
                    TCell cell = cellFactory(area);
                    int index = GetCellCoordIndex(x, y);
                    cells[index] = cell;
                }
            }

            for (int y = 0; y < numCellsY; y++)
            {
                for (int x = 0; x < numCellsX; x++)
                {
                    int index = GetCellCoordIndex(x, y);
                    TCell cell = cells[index];

                    neighbours[cell] = new List<TCell>();

                    Point p = new Point(x, y);
                    foreach (Point np in p.GetNeighbours())
                    {
                        if (np.X < 0 || np.X >= numCellsX || np.Y < 0 || np.Y >= numCellsY)
                        {
                            continue;
                        }

                        neighbours[cell].Add(cells[GetCellCoordIndex(np.X, np.Y)]);
                    }
                }
            }
        }

        private int GetCellCoordIndex(int x, int y)
        {
            return x + (y * numCellsX);
        }

        [CanBeNull]
        public TCell GetCell(Point p)
        {
            return GetCell(p.X, p.Y);
        }

        [CanBeNull]
        public TCell GetCell(int x, int y)
        {
            int cx = x / cellSizeX;
            int cy = y / cellSizeY;

            if (cx < 0 || cx >= numCellsX || cy < 0 || cy >= numCellsY)
            {
                return null;
            }

            int index = GetCellCoordIndex(cx, cy);

            return cells[index];
        }

        public IEnumerable<TCell> GetCells()
        {
            return cells;
        }

        public IEnumerable<TCell> FloodFill(int x, int y, Func<TCell, bool> predicate)
        {
            Stack<TCell> s = new Stack<TCell>();
            TCell first = GetCell(x, y);
            s.Push(first);

            HashSet<TCell> closed = new HashSet<TCell> { first };

            while (s.Count > 0)
            {
                TCell current = s.Pop();

                yield return current;

                foreach (TCell neighbour in neighbours[current])
                {
                    if (closed.Contains(neighbour))
                    {
                        continue;
                    }

                    closed.Add(neighbour);

                    if (!predicate(neighbour))
                    {
                        continue;
                    }

                    s.Push(neighbour);
                }
            }
        }
    }
}