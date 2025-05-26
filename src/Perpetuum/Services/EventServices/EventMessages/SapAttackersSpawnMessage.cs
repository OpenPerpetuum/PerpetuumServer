using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class SapAttackersSpawnMessage : IEventMessage
    {
        public EventType Type => EventType.NpcSapAttackers;

        public SapState SapState { get; }

        public SAP Sap { get; }

        public int ZoneId { get; }

        public int Stability { get; set; }

        public SapAttackersSpawnMessage(SAP sap, SapState sapState, int zoneID, int stability)
        {
            ZoneId = zoneID;
            Sap = sap;
            SapState = sapState;
            Stability = stability;
        }
    }
}
