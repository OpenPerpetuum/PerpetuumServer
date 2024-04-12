using System;
using System.Threading.Tasks;
using System.Threading;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HarvestingIndustrialTurretAI : StationaryIndustrialAI
    {
        public HarvestingIndustrialTurretAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Update(TimeSpan time)
        {
            FindIndustrialTargets(time);

            base.Update(time);
        }

        private int lookingForHarvestingTargets;

        public void FindIndustrialTargets(TimeSpan time)
        {
            UpdateFrequency.Update(time);

            if (UpdateFrequency.Passed)
            {
                UpdateFrequency.Reset();
                if (Interlocked.CompareExchange(ref lookingForHarvestingTargets, 1, 0) == 1)
                {
                    return;
                }
                Task.Run(smartCreature.LookingForHarvestingTargets)
                    .ContinueWith((_) =>
                    {
                        Interlocked.Exchange(ref lookingForHarvestingTargets, 0);
                    });
            }
        }
    }
}
