using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones.Intrusion;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// Processes StabilityAffectingEvent messages for the provided Outpost
    /// </summary>
    public class AffectOutpostStability : EventProcessor
    {
        private Outpost _outpost;
        public AffectOutpostStability(Outpost outpost)
        {
            _outpost = outpost;
        }

        public override EventType Type => EventType.OutpostStability;

        public override void HandleMessage(IEventMessage value)
        {
            if (value is StabilityAffectingEvent msg)
            {
                if (msg.Outpost.Equals(_outpost))
                {
                    _outpost.IntrusionEvent(msg);
                }
            }
        }
    }
}
