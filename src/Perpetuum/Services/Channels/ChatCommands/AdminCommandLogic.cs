using System.Collections.Generic;
using Perpetuum.GenXY;

namespace Perpetuum.Services.Channels.ChatCommands
{
    public class AdminCommandLogic
    {
        public static string ZoneAddDecor(int? zoneId, int definition, int x, int y, int z, double qx, double qy, double qz, double qw, double scale, int cat)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", x*256 },
                    { "y", y*256 },
                    { "z", z*256 },
                    { "quaternionX", qx },
                    { "quaternionY", qy },
                    { "quaternionZ", qz },
                    { "quaternionW", qw },
                    { "scale", scale },
                    { "category", cat }
                };

            return string.Format("zoneDecorAdd:zone_{0}:{1}", zoneId, GenxyConverter.Serialize(dictionary));
        }
    }
}
