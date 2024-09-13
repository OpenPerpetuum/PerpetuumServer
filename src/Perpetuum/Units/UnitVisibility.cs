using Perpetuum.Log;
using Perpetuum.Zones;
using System;
using System.Threading;

namespace Perpetuum.Units
{
    public class UnitVisibility : IUnitVisibility
    {
        private readonly Unit source;
        private ExpiringLosHolder linearLos;
        private ExpiringLosHolder ballisticLos;

        public UnitVisibility(Unit source, Unit unit)
        {
            this.source = source;
            Target = unit;
        }

        public void ResetLineOfSight()
        {
            linearLos = null;
            ballisticLos = null;
        }

        public Unit Target { get; }

        public LOSResult GetLineOfSight(bool ballistic)
        {
            return ballistic
                ? GetLineOfSight(ref ballisticLos, true)
                : GetLineOfSight(ref linearLos, false);
        }

        private LOSResult GetLineOfSight(ref ExpiringLosHolder losHolder, bool ballistic)
        {
            ExpiringLosHolder h = losHolder;
            if (h != null && h.Expired)
            {
                losHolder = null;
                Logger.DebugWarning("LOS expired");
            }

            ExpiringLosHolder holder =
                LazyInitializer.EnsureInitialized(
                    ref losHolder,
                    () =>
                    {
                        LOSResult losResult = source.Zone.IsInLineOfSight(source, Target, ballistic);

                        return new ExpiringLosHolder(losResult, TimeSpan.FromSeconds(4));
                    });

            return holder.losResult;
        }
    }
}
