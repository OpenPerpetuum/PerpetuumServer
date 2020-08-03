using Perpetuum.Data;
using Perpetuum.Zones;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.RiftSystem.StrongholdRifts
{
    public static class StrongholdRiftLocationRepository
    {
        public static IEnumerable<StrongholdRiftLocation> GetAllInZone(IZone zone)
        {
            var locations = Db.Query().CommandText("SELECT id, zoneid, x, y FROM strongholdexitconfig WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", zone.Id)
                .Execute()
                .Select((record) =>
                {
                    var x = record.GetValue<int>("x");
                    var y = record.GetValue<int>("y");
                    return new StrongholdRiftLocation(zone, new Position(x, y));
                });

            return locations;
        }
    }

    public class StrongholdRiftLocation
    {
        public IZone Zone { get; private set; }
        public Position Location { get; private set; }
        public bool Spawned { get; set; }

        public StrongholdRiftLocation(IZone zone, Position location)
        {
            Zone = zone;
            Location = location;
            Spawned = false;
        }
    }
}
