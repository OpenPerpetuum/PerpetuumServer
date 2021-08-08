using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSmooth : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var zone = request.Zone;
            zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var area = zone.Size;
            var targetArea = new Area(1, 1, area.Width - 1, area.Height - 1);
            using (var terrainUpdateMonitor = new TerrainUpdateMonitor(zone))
            {
                foreach (var p in targetArea.GetPositions())
                {
                    var sum = 0.0;
                    var count = 0;
                    foreach (var n in p.EightNeighbours)
                    {
                        if (zone.IsValidPosition(n.intX, n.intY))
                        {
                            sum += zone.Terrain.Altitude.GetAltitudeAsDouble(n.intX, n.intY);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        var smoothed = sum / count;
                        var shortAlt = System.Convert.ToUInt16(smoothed * 32);
                        zone.Terrain.Altitude.SetValue(p.intX, p.intY, shortAlt);
                    }
                }
                zone.Terrain.Slope.UpdateSlopeByArea(targetArea);
            }
        }
    }
}
