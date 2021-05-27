using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;
using System.Drawing;

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
                foreach (var tele in teleports)
                {
                    if(tele.CurrentPosition.IsInRangeOf2D(p, radius))
                    {
                        c.TerraformProtected = true;
                        return c;
                    }
                }
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
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}