using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class AttackCombatDroneAI : CombatDroneAI
    {
        public AttackCombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Exit()
        {
            source?.Cancel();

            base.Exit();
        }

        public override void Update(TimeSpan time)
        {
            CombatDrone drone = smartCreature as CombatDrone;

            if (drone.IsReceivedRetreatCommand)
            {
                ToRetreatCombatDroneAI();

                return;
            }

            if (drone.GetPrimaryLock() == null)
            {
                ReturnToHomePosition();

                return;
            }

            UpdateHostile(time);

            base.Update(time);
        }

        protected override void ToAggressorAI() { }

        protected override void ReturnToHomePosition()
        {
            smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
            WriteLog("Returning to command robot.");
        }
    }
}
