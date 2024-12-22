using Perpetuum.Builders;
using Perpetuum.Groups.Corporations;
using Perpetuum.IO;
using Perpetuum.Log;
using Perpetuum.Modules.Weapons;
using Perpetuum.Units;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones
{
    public interface ILayerFileIO
    {
        T[] LoadLayerData<T>(IZone zone, string name) where T : struct;
        void SaveLayerToDisk<T>(IZone zone, ILayer<T> layer) where T : struct;
    }

    public static class LayerFileIOExtensions
    {
        public static T[] Load<T>(this ILayerFileIO dataIO, IZone zone, LayerType layerType) where T : struct
        {
            return dataIO.LoadLayerData<T>(zone, layerType.ToString());
        }
    }

    public class LayerFileIO : ILayerFileIO
    {
        private readonly IFileSystem _fileSystem;

        public LayerFileIO(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T[] LoadLayerData<T>(IZone zone, string name) where T : struct
        {
            string path = zone.CreateTerrainDataFilename(name);
            T[] data = _fileSystem.ReadLayer<T>(path);
            Logger.Info("Layer data loaded. (" + name + ") zone:" + zone.Id);
            return data;
        }

        public void SaveLayerToDisk<T>(IZone zone, ILayer<T> layer) where T : struct
        {
            string baseFilename = zone.CreateTerrainDataFilename(layer.LayerType.ToString().ToLower(), "");

            using (MD5 md5 = MD5.Create())
            {
                string tmpFn = baseFilename + "tmp" + DateTime.Now.Ticks + ".bin";
                byte[] layerData = layer.RawData.ToByteArray();
                _fileSystem.WriteLayer(tmpFn, layerData);

                if (!md5.ComputeHash(layerData).SequenceEqual(md5.ComputeHash(_fileSystem.ReadLayerAsByteArray(tmpFn))))
                {
                    return;
                }

                _fileSystem.MoveLayerFile(tmpFn, baseFilename + "bin");
                Logger.Info("Layer saved. (" + baseFilename + ")");
            }
        }
    }

    public static partial class ZoneExtensions
    {
        public static List<Point> FindWalkableArea(this IZone zone, Area area, int size, double slope = 4.0)
        {
            area = area.Clamp(zone.Size);
            while (true)
            {
                Point startPosition;
                while (true)
                {
                    startPosition = area.GetRandomPosition();

                    if (!zone.Terrain.Blocks.GetValue(startPosition).Island && zone.IsWalkable(startPosition, slope))
                    {
                        break;
                    }
                }

                List<Point> p = FindWalkableArea(zone, startPosition, area, size, slope);
                if (p != null)
                {
                    return p;
                }

                Thread.Sleep(1);
            }
        }

        [CanBeNull]
        public static List<Point> FindWalkableArea(this IZone zone, Point startPosition, Area area, int size, double slope = 4.0)
        {
            Queue<Point> q = new Queue<Point>();
            q.Enqueue(startPosition);
            HashSet<Point> closed = new HashSet<Point> { startPosition };

            List<Point> result = new List<Point>();
            while (q.TryDequeue(out Point position))
            {
                result.Add(position);

                if (result.Count >= size)
                {
                    // nyert
                    return result;
                }

                foreach (Point np in position.GetNonDiagonalNeighbours())
                {
                    if (closed.Contains(np))
                    {
                        continue;
                    }

                    _ = closed.Add(np);

                    if (!area.Contains(np) || !zone.IsWalkable(np, slope))
                    {
                        continue;
                    }

                    q.Enqueue(np);
                }
            }

            return null;
        }

        /// <summary>
        /// A 2d raycast check for a line segment in cellular world
        /// An implementation of Bresenham's line algorithm
        /// </summary>
        /// <param name="zone">this</param>
        /// <param name="start">Start point of line segment</param>
        /// <param name="end">End point of line segment</param>
        /// <param name="slope">Slope capability check for slope-based blocking</param>
        /// <returns>True if tiles checked are walkable</returns>
        public static bool CheckLinearPath(this IZone zone, Point start, Point end, double slope = 4.0)
        {
            int x = start.X;
            int y = start.Y;
            int deltaX = Math.Abs(end.X - x);
            int deltaY = Math.Abs(end.Y - y);
            int travelDist = deltaX + deltaY;
            int xIncrement = (end.X > x) ? 1 : -1;
            int yIncrement = (end.Y > y) ? 1 : -1;
            int error = deltaX - deltaY;
            deltaX *= 2;
            deltaY *= 2;

            for (int i = 0; i <= travelDist; i++)
            {
                if (!zone.IsWalkable(x, y, slope))
                {
                    return false;
                }

                if (error > 0)
                {
                    x += xIncrement;
                    error -= deltaY;
                }
                else
                {
                    y += yIncrement;
                    error += deltaX;
                }
            }
            return true;
        }

        public static bool IsTerrainConditionsMatchInRange(this IZone zone, Position centerPosition, int range, double slope)
        {
            int totalTiles = range * range * 4;

            int illegalsFound = 0;

            for (int j = centerPosition.intY - range; j < centerPosition.intY + range; j++)
            {
                for (int i = centerPosition.intX - range; i < centerPosition.intX + range; i++)
                {
                    Position cPos = new Position(i, j);

                    if (centerPosition.IsInRangeOf2D(cPos, range))
                    {
                        BlockingInfo blockInfo = zone.Terrain.Blocks.GetValue(i, j);

                        if (blockInfo.Height > 0 || blockInfo.NonNaturally || blockInfo.Plant || !zone.Terrain.Slope.CheckSlope(cPos.intX, cPos.intY, slope))
                        {
                            illegalsFound++;
                        }
                    }
                }
            }

            double troubleFactor = illegalsFound / (double)totalTiles;

            Logger.Info("trouble factor: " + troubleFactor);

            if (troubleFactor > 0.5)
            {
                Logger.Warning("illegal tiles coverage: " + (troubleFactor * 100) + "%");
                return false;
            }

            return true;
        }

        public static void DoAoeDamageAsync(this IZone zone, IBuilder<DamageInfo> damageBuilder)
        {
            _ = Task.Run(() => DoAoeDamage(zone, damageBuilder));
        }

        public static void DoAoeDamage(this IZone zone, IBuilder<DamageInfo> damageBuilder)
        {
            DamageInfo damageInfo = damageBuilder.Build();
            IEnumerable<Unit> units = zone.Units
                .WithinRange(damageInfo.sourcePosition, damageInfo.Range)
                .Where(x => !x.HasTeleportSicknessEffect || x.HasPvpEffect);

            foreach (Unit unit in units)
            {
                if (unit is RemoteControlledCreature)
                {
                    continue;
                }

                LOSResult losResult = zone.IsInLineOfSight(damageInfo.attacker, unit, false);
                if (losResult.hit)
                {
                    continue;
                }

                unit.TakeDamage(damageInfo);
            }

            using (new TerrainUpdateMonitor(zone))
            {
                zone.DamageToPlantOnArea(damageInfo);
            }
        }

        public static string CreateTerrainDataFilename(this IZone zone, string name, string extension = "bin")
        {
            return CreateTerrainDataFilename(zone.Id, name, extension);
        }

        public static string CreateTerrainDataFilename(int zoneId, string name, string extension = "bin")
        {
            return $"{name.ToLower()}.{zoneId:0000}.{extension}".ToLower();
        }

        private const int MAX_SAMPLES = 1200;

        public static Position FindPassablePointInRadius(this IZone zone, Position origin, int radius)
        {
            int counter = 0;
            while (true)
            {
                counter++;
                if (counter > MAX_SAMPLES)
                {
                    return default;
                }

                Position randomPos = origin.GetRandomPositionInRange2D(0, radius).Clamp(zone.Size);
                if (zone.Terrain.IsPassable(randomPos))
                {
                    return randomPos;
                }
            }
        }

        public static bool IsValidPosition(this IZone zone, int x, int y)
        {
            return x >= 0 && x < zone.Size.Width && y >= 0 && y < zone.Size.Height;
        }

        public static void UpdateCorporation(this IZone zone, CorporationCommand command, Dictionary<string, object> data)
        {
            zone.CorporationHandler.HandleCorporationCommand(command, data);
        }

        public static Position ToWorldPosition(this IZone zone, Position position)
        {
            int zx = zone.Configuration.WorldPosition.X;
            int zy = zone.Configuration.WorldPosition.Y;
            return position.GetWorldPosition(zx, zy);
        }
    }
}