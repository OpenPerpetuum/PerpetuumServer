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
            if (!(smartCreature as CombatDrone).IsInGuardRange)
            {
                ToEscortCombatDroneAI();

                return;
            }

            if (smartCreature.ThreatManager.IsThreatened)
            {
                ToAttackCombatDroneAI();

                return;
            }

            movement?.Update(smartCreature, time);
        }
    }
}
