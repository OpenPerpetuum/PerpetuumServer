using Perpetuum.Data;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.SapAttackers
{
    public class SapAttackersRepository : ISapAttackersRepository
    {
        private const string queryStr = "SELECT MinStability, PresenceId from SapAttackers WHERE SapDefinition=@sapDefinition AND (zoneId IS NULL OR zoneId=@zone);";

        public INpcPresences CreateSapAttackersSpawn(int sapDefinition, int zoneId)
        {
            INpcPresence[] records = Db.Query()
                .CommandText(queryStr)
                .SetParameter("@sapDefinition", sapDefinition)
                .SetParameter("@zone", zoneId)
                .Execute()
                .Select(CreateFromRecord)
                .ToArray();

            return new SapAttackers(records);
        }

        private static INpcPresence CreateFromRecord(IDataRecord record)
        {
            int presence = record.GetValue<int>("PresenceId");
            int minStability = record.GetValue<int>("MinStability");
            SapAttackerPresence pair = new SapAttackerPresence(presence, minStability);

            return pair;
        }
    }
}
