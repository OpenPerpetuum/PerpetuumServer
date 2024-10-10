using Perpetuum.EntityFramework;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using System;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Outpost Decay handler - updates outpost stability with a system-generated SAP event
    /// </summary>
    public class OutpostDecay
    {
        private readonly EventListenerService eventChannel;
        private static readonly TimeSpan noDecayBefore = TimeSpan.FromDays(5);
        private static readonly TimeSpan decayRate = TimeSpan.FromDays(1);
        private TimeSpan timeSinceLastDecay = TimeSpan.Zero;
        private TimeSpan lastSuccessfulIntrusion = TimeSpan.Zero;
        private static readonly int decayPts = -5;
        private readonly StabilityAffectingEvent.StabilityAffectBuilder builder;

        public OutpostDecay(EventListenerService eventChannel, Outpost outpost)
        {
            this.eventChannel = eventChannel;
            EntityDefault def = EntityDefault.GetByName("def_outpost_decay");
            builder = StabilityAffectingEvent.Builder()
               .WithOutpost(outpost)
               .WithSapDefinition(def.Definition)
               .WithPoints(decayPts);
        }

        public void OnUpdate(TimeSpan time)
        {
            lastSuccessfulIntrusion += time;
            if (lastSuccessfulIntrusion < noDecayBefore)
            {
                return;
            }

            timeSinceLastDecay += time;
            if (timeSinceLastDecay > decayRate)
            {
                timeSinceLastDecay = TimeSpan.Zero;
                DoDecay();
            }
        }

        /// <summary>
        /// Called when a SAP is completed
        /// </summary>
        public void ResetDecayTimer()
        {
            lastSuccessfulIntrusion = TimeSpan.Zero;
        }

        private void DoDecay()
        {
            eventChannel.PublishMessage(builder.Build());
        }
    }
}
