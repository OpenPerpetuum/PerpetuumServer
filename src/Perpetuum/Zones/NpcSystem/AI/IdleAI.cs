using Perpetuum.Modules;
using Perpetuum.Zones.Movements;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IdleAI : BaseAI
    {
        private RandomMovement movement;

        public IdleAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            smartCreature.StopAllModules(new Type[] { typeof(NoxModule) });
            smartCreature.ResetLocks();

            movement = new RandomMovement(smartCreature.HomePosition, smartCreature.HomeRange);

            movement.Start(smartCreature);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (!smartCreature.IsInHomeRange)
            {
                ToHomeAI();

                return;
            }

            if (smartCreature.ThreatManager.IsThreatened)
            {
                ToAggressorAI();

                return;
            }

            movement?.Update(smartCreature, time);
        }
    }
}
