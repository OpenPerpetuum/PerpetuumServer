using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.IndustrialDrones
{
    public class GuardIndustrialDroneAI : BaseAI
    {
        private RandomMovement movement;

        public GuardIndustrialDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            smartCreature.StopAllModules();
            smartCreature.ResetLocks();

            movement = new RandomMovement(smartCreature.HomePosition, (smartCreature as IndustrialDrone).GuardRange);

            movement.Start(smartCreature);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (!(smartCreature as IndustrialDrone).IsInGuardRange)
            {
                ToEscortIndustrialDroneAI();

                return;
            }

            if (GetPrimaryTerrainLock() != null)
            {
                ToGatheringIndustrialDroneAI();

                return;
            }

            movement?.Update(smartCreature, time);
        }
    }
}
