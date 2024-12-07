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
            if (!smartCreature.ThreatManager.IsThreatened)
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
            smartCreature.AI.Pop();
            smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
            WriteLog("Returning to command robot.");
        }
    }
}
