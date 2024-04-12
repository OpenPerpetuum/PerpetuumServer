using System;
using System.Threading.Tasks;
using System.Threading;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class MiningIndustrialTurretAI : StationaryIndustrialAI
    {
        public MiningIndustrialTurretAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Update(TimeSpan time)
        {
            FindIndustrialTargets(time);

            base.Update(time);
        }

        private int lookingForMiningTargets;

        public void FindIndustrialTargets(TimeSpan time)
        {
            UpdateFrequency.Update(time);

            if (UpdateFrequency.Passed)
            {
                UpdateFrequency.Reset();
                if (Interlocked.CompareExchange(ref lookingForMiningTargets, 1, 0) == 1)
                {
                    return;
                }
                Task.Run(smartCreature.LookingForMiningTargets)
                    .ContinueWith((_) =>
                    {
                        Interlocked.Exchange(ref lookingForMiningTargets, 0);
                    });
            }
        }
    }
}
