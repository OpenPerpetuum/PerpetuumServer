using System;

namespace Perpetuum.Services.EventServices.EventMessages
{

    public class SapStateMessage : IEventMessage
    {
        public SapStateMessage(long eId, string sapEname, SapState state, DateTime time)
        {
            Eid = eId;
            SapEname = sapEname;
            State = state;
            TimeStamp = time;
        }

        public EventType Type => EventType.NpcState;

        public long Eid { get; }

        public string SapEname { get; }

        public SapState State { get; }

        public DateTime TimeStamp { get; }
    }
}
