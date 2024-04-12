using System;

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

        public void FindIndustrialTargets(TimeSpan time)
        {
            UpdateFrequency.Update(time);

            if (UpdateFrequency.Passed)
            {
                UpdateFrequency.Reset();
                // TODO: Use Task.Run for this.
                smartCreature.LookingForMiningTargets();
            }
        }
    }
}
