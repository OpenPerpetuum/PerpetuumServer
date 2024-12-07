using Perpetuum.PathFinders;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class EscortCombatDroneAI : CombatDroneAI
    {
        private PathMovement movement;
        private readonly double maxReturnGuardRadius;
        private readonly PathFinder pathFinder;

        public EscortCombatDroneAI(SmartCreature smartCreature) : base(smartCreature)
        {
            maxReturnGuardRadius = ((this.smartCreature as CombatDrone).GuardRange * 0.4).Clamp(3, 20);
            pathFinder = new AStarFinder(Heuristic.Manhattan, smartCreature.IsWalkable);
        }

        public override void Enter()
        {
            Position randomHome = smartCreature.Zone.FindPassablePointInRadius(smartCreature.HomePosition, (int)maxReturnGuardRadius);

            if (randomHome == default)
            {
                randomHome = smartCreature.HomePosition;
            }

            pathFinder
                .FindPathAsync(smartCreature.CurrentPosition, randomHome)
                .ContinueWith(t =>
                {
                    System.Drawing.Point[] path = t.Result;

                    if (path == null)
                    {
                        WriteLog("Path not found! (" + smartCreature.CurrentPosition + " => " + smartCreature.HomePosition + ")");

                        AStarFinder f = new AStarFinder(Heuristic.Manhattan, (x, y) => true);

                        path = f.FindPath(smartCreature.CurrentPosition, smartCreature.HomePosition);

                        if (path == null)
                        {
                            WriteLog("Safe path not found! (" + smartCreature.CurrentPosition + " => " + smartCreature.HomePosition + ")");
                        }
                    }

                    movement = new PathMovement(path);
                    movement.Start(smartCreature);
                });

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (smartCreature.ThreatManager.IsThreatened)
            {
                ToAttackCombatDroneAI();

                return;
            }

            if (movement != null)
            {
                movement.Update(smartCreature, time);

                if (movement.Arrived)
                {
                    smartCreature.AI.Pop();

                    return;
                }
            }

            base.Update(time);
        }
    }
}
