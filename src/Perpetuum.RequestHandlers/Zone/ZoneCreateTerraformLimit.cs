using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;
using System.Drawing;
using System.Drawing.Imaging;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateTerraformLimit : IRequestHandler<IZoneRequest>
    {
        private void Clear(IZoneRequest request)
        {
            request.Zone.Terrain.Controls.UpdateAll((x, y, c) =>
            {
                c.TerraformProtected = false;
                return c;
            });
        }

        private void SetRadiusOnTeleports(IZoneRequest request, int radius)
        {
            var teleports = request.Zone.GetTeleportColumns();
            request.Zone.Terrain.Controls.UpdateAll((x, y, c) =>
            {
                var p = new Point(x, y);
                var minDist = request.Zone.Size.Width;
                foreach(var tele in teleports)
                {
                    minDist = minDist.Min((int)tele.CurrentPosition.TotalDistance2D(p));
                }
                c.TerraformProtected = minDist < radius;
                return c;
            });
        }

        private void SetCoastline(IZoneRequest request, int radius)
        {
            var zone = request.Zone;
            var teleports = zone.GetTeleportColumns();
            var altitude = zone.Terrain.Altitude;
            var waterlevel = ZoneConfiguration.WaterLevel;
            Bitmap bmp = new Bitmap(zone.Size.Width, zone.Size.Height);
            for (var x = 0; x < altitude.Width; x++)
            {
                for (var y = 0; y < altitude.Width; y++)
                {
                    var altitudeVal = request.Zone.Terrain.Altitude.GetAltitude(x, y);
                    var isBelow = altitudeVal < waterlevel;
                    bmp.SetPixel(x, y, isBelow ? Color.Black: Color.White);
                }
            }
            bmp = bmp.DilateOrErode(radius, false);
            zone.Terrain.Controls.UpdateAll((x, y, c) =>
            {
                c.TerraformProtected = bmp.GetPixel(x, y) == Color.Black;
                return c;
            });
        }


        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var radius = request.Data.GetOrDefault<int>(k.distance);
            var mode = request.Data.GetOrDefault<string>(k.mode);
            radius = radius.Clamp(0, 500);

            if (mode == "clear")
            {
                Clear(request);
            }
            else if (mode == "teleports")
            {
                SetRadiusOnTeleports(request, radius);
            }
            else if(mode == "coast")
            {
                SetCoastline(request, radius);
            }
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}