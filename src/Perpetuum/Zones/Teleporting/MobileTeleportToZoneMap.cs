using Perpetuum.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.Teleporting
{
    public interface IMobileTeleportToZoneMap
    {
        List<int> GetDestinationZones(int definition);
    }

    public class MobileTeleportZoneMapCached : IMobileTeleportToZoneMap
    {
        private readonly ConcurrentDictionary<int, List<int>> dictionary;
        private readonly MobileTeleportToZoneReader reader;
        public MobileTeleportZoneMapCached()
        {
            dictionary = new ConcurrentDictionary<int, List<int>>();
            reader = new MobileTeleportToZoneReader();
        }

        private List<int> GetZones(int definition)
        {
            return reader.GetDestinationZones(definition).ToList();
        }

        public List<int> GetDestinationZones(int definition)
        {
            return dictionary.GetOrAdd(definition, () => GetZones(definition));
        }
    }

    public class MobileTeleportToZoneReader
    {
        public MobileTeleportToZoneReader()
        {
        }

        protected int GetZoneId(IDataRecord record)
        {
            return record.GetValue<int>("zoneid");
        }

        public IEnumerable<int> GetDestinationZones(int definition)
        {
            var zoneIds = Db.Query().CommandText("SELECT zoneid FROM zoneteleportdevicemap WHERE sourcedefinition=@definition")
                .SetParameter("@definition", definition)
                .Execute()
                .Select(GetZoneId);
            return zoneIds;
        }
    }
}
