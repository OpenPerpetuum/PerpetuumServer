using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.Terrains
{
    public static class TerrainExtensions
    {
        public static bool IsBlocked(this ITerrain terrain, Position position)
        {
            return terrain.IsBlocked((int)position.X, (int)position.Y);
        }

        public static bool IsBlocked(this ITerrain terrain, int x, int y)
        {
            return terrain.Blocks[x, y].Flags > 0;
        }

        public static void ClearPlantBlocking(this ITerrain terrain, Position position)
        {
            terrain.ClearPlantBlocking(position.intX, position.intY);
        }

        private static void ClearPlantBlocking(this ITerrain terrain, int x, int y)
        {
            terrain.Blocks.UpdateValue(x, y, bi =>
            {
                bi.Height = 0;
                bi.Plant = false;

                return bi;
            });
        }

        public static void PutPlant(
            this ITerrain terrain,
            int x,
            int y,
            byte state,
            PlantType plantType,
            PlantRule plantRule)
        {
            terrain.Plants.UpdateValue(x, y, pi =>
            {
                pi.SetPlant(state, plantType);
                pi.health = plantRule.Health[state];

                return pi;
            });

            if (plantRule.IsBlocking(state))
            {
                terrain.Blocks.UpdateValue(x, y, bi =>
                {
                    bi.Plant = true;
                    bi.Height = plantRule.GetBlockingHeight(state);

                    return bi;
                });
            }

            if (plantRule.PlacesConcrete)
            {
                terrain.Controls.UpdateValue(x, y, ci =>
                {
                    if (FastRandom.NextDouble() > 0.5)
                    {
                        ci.ConcreteA = true;
                    }
                    else
                    {
                        ci.ConcreteB = true;
                    }

                    return ci;
                });
            }
        }

        public static int CountPlantsInArea(this IZone zone, PlantType plantType, Area area)
        {
            int counter = 0;
            zone.ForEachAreaInclusive(area, (x, y) =>
            {
                PlantInfo pi = zone.Terrain.Plants.GetValue(x, y);
                if (pi.type == plantType)
                {
                    counter++;
                }
            });

            return counter;
        }

        public static List<Position> GetPlantPositionsInArea(this IZone zone, PlantType plantType, Area area)
        {
            List<Position> result = new List<Position>();
            zone.ForEachAreaInclusive(area, (x, y) =>
            {
                PlantInfo pi = zone.Terrain.Plants.GetValue(x, y);
                if (pi.type == plantType)
                {
                    result.Add(new Position(x, y));
                }
            });

            return result;
        }

        public static void ForEachAll(this IZone zone, Action<int, int> action)
        {
            for (int y = 0; y < zone.Size.Height; y++)
            {
                for (int x = 0; x < zone.Size.Width; x++)
                {
                    action(x, y);
                }
            }
        }

        // p1 <= p2
        public static void ForEachAreaInclusive(this IZone zone, Area area, Action<int, int> areaAction)
        {
            area = area.Clamp(zone.Size);
            for (int y = area.Y1; y <= area.Y2; y++)
            {
                for (int x = area.X1; x <= area.X2; x++)
                {
                    areaAction(x, y);
                }
            }
        }

        public static void DamageToPlantOnArea(this IZone zone, DamageInfo damageInfo)
        {
            Area area = Area.FromRadius(damageInfo.sourcePosition, (int)damageInfo.Range);
            double damage = damageInfo.CalculatePlantDamages();
            int rangeFar = (int)damageInfo.Range;
            double originX = damageInfo.sourcePosition.intX;
            double originY = damageInfo.sourcePosition.intY;
            for (int y = area.Y1; y <= area.Y2; y++)
            {
                for (int x = area.X1; x <= area.X2; x++)
                {
                    double mult = MathHelper.DistanceFalloff(0, rangeFar, originX, originY, x, y);
                    double finalDamage = mult * damage;
                    if (finalDamage > 0)
                    {
                        zone.DamageToPlant(x, y, finalDamage);
                    }
                }
            }
        }

        public static void DamageToPlantOnArea(this IZone zone, Area area, double damage)
        {
            int w = area.Width / 2;
            int h = area.Height / 2;
            int maxd = (w * w) + (h * h);
            int cx = area.Center.X;
            int cy = area.Center.Y;
            for (int y = area.Y1; y <= area.Y2; y++)
            {
                for (int x = area.X1; x <= area.X2; x++)
                {
                    int dx = cx - x;
                    int dy = cy - y;

                    int d = (dx * dx) + (dy * dy);
                    double m = 1.0 - ((double)d / maxd);

                    double dmg = damage * m;
                    zone.DamageToPlant(x, y, dmg);
                }
            }
        }

        public static void DamageToPlant(this IZone zone, int x, int y, double damage)
        {
            PlantInfo currPlant = zone.Terrain.Plants[x, y];
            if (currPlant.type == PlantType.NotDefined || currPlant.health <= 0)
            {
                return;
            }

            PlantRule plantRule = zone.Configuration.PlantRules.GetPlantRule(currPlant.type);
            if (plantRule == null)
            {
                Logger.Error("plant rule was not found. plant type: " + currPlant.type + " zone:" + zone.Id);
                currPlant.Clear();
                zone.Terrain.Blocks[x, y] = new BlockingInfo();

                return;
            }

            if (FastRandom.NextDouble() < 0.3)
            {
                damage /= 3.0;
            }

            int damageInt = (int)(damage * plantRule.DamageScale).Clamp(int.MinValue, int.MaxValue);
            if (damageInt <= 0)
            {
                Logger.DebugInfo(plantRule.Type + " low damage");

                return;
            }

            Logger.DebugInfo(plantRule.Type + " damage   " + damageInt);
            int currentHealth = currPlant.health;
            currentHealth -= damageInt;
            if (currentHealth <= 0)
            {
                currPlant.Clear();
                currPlant.state = 1;
                zone.Terrain.Blocks[x, y] = new BlockingInfo();
                if (plantRule.PlacesConcrete)
                {
                    zone.Terrain.Controls.UpdateValue(x, y, ci =>
                    {
                        ci.ClearAllConcrete();

                        return ci;
                    });
                }
            }
            else
            {
                currPlant.health = (byte)currentHealth.Clamp(0, 255);
            }

            zone.Terrain.Plants[x, y] = currPlant;
        }

        public static Position GetRandomPassablePosition(this IZone zone)
        {
            Position position;
            bool isPassable;
            do
            {
                position = zone.GetRandomIslandPosition();
                isPassable = zone.Terrain.IsPassable(position);
            } while (!isPassable);

            return position;
        }

        private static Position GetRandomIslandPosition(this IZone zone)
        {
            int counter = 0;
            while (true)
            {
                int xo = FastRandom.NextInt(0, zone.Size.Width - 1);
                int yo = FastRandom.NextInt(0, zone.Size.Height - 1);
                Position p = new Position(xo, yo);
                BlockingInfo blockingInfo = zone.Terrain.Blocks.GetValue(xo, yo);
                if (!blockingInfo.Island)
                {
                    return p;
                }

                counter++;
                if (counter % 50 == 0)
                {
                    Thread.Sleep(1);
                }
            }
        }

        public static void UpdateAreaFromPacket(this ITerrain terrain, Packet packet)
        {
            LayerType layerType = (LayerType)packet.ReadByte();
            _ = packet.ReadByte(); // materialType
            _ = packet.ReadByte(); // sizeOfElement;
            int x1 = packet.ReadInt();
            int y1 = packet.ReadInt();
            int x2 = packet.ReadInt();
            int y2 = packet.ReadInt();
            Area area = new Area(x1, y1, x2, y2);
            IUpdateableLayer layer = terrain.GetLayerByType(layerType) as IUpdateableLayer;
            layer?.CopyFromStreamToArea(packet, area);
        }

        public static void UpdateNatureCube(this IZone zone, Area area, Action<NatureCube> updater)
        {
            NatureCube cube = new NatureCube(zone, area);
            cube.Check();
            NatureCube snapshot = cube.Clone();
            snapshot.Check();
            updater(snapshot);
            snapshot.Check();
            if (cube.Equals(snapshot))
            {
                return;
            }

            snapshot.Commit();
            BeamBuilder builder = Beam.NewBuilder().WithType(BeamType.nature_effect).WithDuration(8000);
            _ = Task.Delay(40).ContinueWith(t => zone.CreateBeam(builder.WithPosition(area.GetRandomPosition())));
            _ = Task.Delay(1500).ContinueWith(t => zone.CreateBeam(builder.WithPosition(area.GetRandomPosition())));
            _ = Task.Delay(2500).ContinueWith(t => zone.CreateBeams(2, () => builder.WithPosition(area.GetRandomPosition())));
        }

        public static MineralLayer GetMineralLayerOrThrow(
            this ITerrain terrain,
            MaterialType type,
            Action<PerpetuumException> exceptionAction = null)
        {
            return terrain
                .GetMaterialLayer(type)
                .ThrowIfNotType<MineralLayer>(ErrorCodes.NoSuchMineralOnZone, exceptionAction);
        }

        public static MaterialType GetMaterialTypeAtPosition(this ITerrain terrain, Position position)
        {
            MaterialType[] materials = Enum.GetValues(typeof(MaterialType)) as MaterialType[];
            IMaterialLayer layer = materials
                .Select(x => terrain.GetMaterialLayer(x))
                .Where(x => x is MineralLayer)
                .FirstOrDefault(x => (x as MineralLayer).Nodes.Any(y => y.Area.Contains(position)));

            return layer == null
                ? MaterialType.Undefined
                : (layer as MineralLayer).Type;
        }

        public static MaterialType[] GetAvailableMineralTypes(this ITerrain terrain)
        {
            MaterialType[] materials = Enum.GetValues(typeof(MaterialType)) as MaterialType[];

            return materials
                .Where(x => terrain.GetMaterialLayer(x) != null)
                .ToArray();
        }

        public static PlantType[] GetAvailablePlantTypes(this ITerrain terrain)
        {
            PlantType[] materials = Enum.GetValues(typeof(PlantType)) as PlantType[];

            return materials
                .Where(x => terrain.Plants.RawData.Any(y => y.type == x && y.material > 0))
                .ToArray();
        }

        private const int GZIP_THRESHOLD = 260;

        [CanBeNull]
        public static Packet BuildLayerUpdatePacket(this ITerrain terrain, LayerType layerType, Area area)
        {
            if (!(terrain.GetLayerByType(layerType) is IUpdateableLayer layer))
            {
                return null;
            }

            Packet packet = new Packet(ZoneCommand.LayerUpdate);
            packet.AppendByte((byte)layerType);
            packet.AppendByte(0); // material
            packet.AppendByte((byte)layer.SizeInBytes);
            packet.AppendInt(area.X1);
            packet.AppendInt(area.Y1);
            packet.AppendInt(area.X2);
            packet.AppendInt(area.Y2);
            bool compressed = false;
            byte[] data = layer.CopyAreaToByteArray(area);
            if (data.Length > GZIP_THRESHOLD)
            {
                byte[] compressedData = GZip.Compress(data);
                compressed = ((double)data.Length / compressedData.Length) > 1.0;
                if (compressed)
                {
                    data = compressedData;
                }
            }

            packet.AppendByte((byte)(compressed ? 1 : 0));
            packet.AppendInt(data.Length);
            packet.AppendByteArray(data);

            return packet;
        }

        public static bool IsPassable(this ITerrain terrain, Position position)
        {
            return terrain.IsPassable((int)position.X, (int)position.Y);
        }

        public static bool IsPassable(this ITerrain terrain, int x, int y)
        {
            if (terrain.Passable != null)
            {
                bool isPassable = terrain.Passable.GetValue(x, y);
                if (!isPassable)
                {
                    return false;
                }
            }

            BlockingInfo bi = terrain.Blocks.GetValue(x, y);

            return bi.Flags <= 0 && terrain.Slope.CheckSlope(x, y);
        }
    }
}
