using Perpetuum.Data;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public class NpcBossInfoBuilder
    {
        private readonly ICustomRiftConfigReader _customRiftConfigReader;
        private readonly EventListenerService _eventChannel;

        public NpcBossInfoBuilder(ICustomRiftConfigReader customRiftConfigReader, EventListenerService eventChannel)
        {
            _customRiftConfigReader = customRiftConfigReader;
            _eventChannel = eventChannel;
        }

        public NpcBossInfo CreateBossInfoFromDB(IDataRecord record)
        {
            int id = record.GetValue<int>("id");
            int flockid = record.GetValue<int>("flockid");
            double? respawnFactor = record.GetValue<double?>("respawnNoiseFactor");
            bool lootSplit = record.GetValue<bool>("lootSplitFlag");
            long? outpostEID = record.GetValue<long?>("outpostEID");
            int? stabilityPts = record.GetValue<int?>("stabilityPts");
            bool overrideRelations = record.GetValue<bool>("overrideRelations");
            string deathMessage = record.GetValue<string>("customDeathMessage");
            string aggressMessage = record.GetValue<string>("customAggressMessage");
            int? riftConfigId = record.GetValue<int?>("riftConfigId");
            CustomRiftConfig riftConfig = _customRiftConfigReader.GetById(riftConfigId ?? -1);
            bool announce = record.GetValue<bool>("isAnnounced");
            bool isServerWideAnnouncement = record.GetValueOrDefault<bool>("isServerWideAnnouncement");
            bool isNoRadioDelay = record.GetValueOrDefault<bool>("isNoRadioDelay");
            NpcBossInfo info = new NpcBossInfo(
                _eventChannel,
                id,
                flockid,
                respawnFactor,
                lootSplit,
                outpostEID,
                stabilityPts,
                overrideRelations,
                deathMessage,
                aggressMessage,
                riftConfig,
                announce,
                isServerWideAnnouncement,
                isNoRadioDelay
             );

            return info;
        }

        public NpcBossInfo GetBossInfoByFlockID(int flockid, IFlockConfiguration config)
        {
            System.Collections.Generic.IEnumerable<NpcBossInfo> bossInfos = Db.Query()
                .CommandText(@"SELECT TOP 1 id, flockid, respawnNoiseFactor, lootSplitFlag, outpostEID,
                    stabilityPts, overrideRelations, customDeathMessage, customAggressMessage, riftConfigId,
                    isAnnounced, isServerWideAnnouncement, isNoRadioDelay
                    FROM dbo.npcbossinfo WHERE flockid=@flockid;")
                .SetParameter("@flockid", flockid)
                .Execute()
                .Select(CreateBossInfoFromDB);

            NpcBossInfo info = bossInfos.SingleOrDefault();

            if (info == null)
            {
                return null;
            }

            info.RespawnTime = config.RespawnTime;

            return info;
        }
    }
}
