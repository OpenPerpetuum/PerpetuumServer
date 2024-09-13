using System;

namespace Perpetuum.Zones.NpcSystem.AI.IndustrialDrones
{
    public class GatheringIndustrialDroneAI : IndustrialAI
    {
        public GatheringIndustrialDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Update(TimeSpan time)
        {
            if (GetPrimaryTerrainLock() == null)
            {
                ReturnToHomePosition();

                return;
            }

            UpdateIndustrialTarget(time);

            base.Update(time);
        }

        protected override void ToAggressorAI() { }

        protected void ReturnToHomePosition()
        {
            _ = smartCreature.AI.Pop();
            smartCreature.AI.Push(new EscortIndustrialDroneAI(smartCreature));
            WriteLog("Returning to command robot.");
        }
    }
}
