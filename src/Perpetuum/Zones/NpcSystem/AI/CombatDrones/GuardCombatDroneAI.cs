using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class GuardCombatDroneAI : CombatDroneAI
    {
        private RandomMovement movement;

        public GuardCombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            smartCreature.StopAllModules();
            smartCreature.ResetLocks();

            movement = new RandomMovement(smartCreature.HomePosition, (smartCreature as CombatDrone).GuardRange);

            movement.Start(smartCreature);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            CombatDrone drone = smartCreature as CombatDrone;

            if (drone.IsReceivedRetreatCommand)
            {
                ToRetreatCombatDroneAI();

                return;
            }

            if (!drone.IsInGuardRange)
            {
                ToEscortCombatDroneAI();

                return;
            }

            if (drone.HasCommandBotPrimaryLock() != null)
            {
                ToAttackCombatDroneAI();

                return;
            }

            movement?.Update(smartCreature, time);
        }
    }
}
