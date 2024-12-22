using Perpetuum.PathFinders;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class RetreatCombatDroneAI : CombatDroneAI
    {
        private PathMovement movement;
        private readonly PathFinder pathFinder;

        public RetreatCombatDroneAI(SmartCreature smartCreature) : base(smartCreature)
        {
            pathFinder = new AStarFinder(Heuristic.Manhattan, smartCreature.IsWalkable);
        }

        public override void Enter()
        {
            smartCreature.StopAllModules();
            smartCreature.ResetLocks();

            Position randomHome = smartCreature.Zone.FindPassablePointInRadius(smartCreature.HomePosition, (int)(smartCreature as CombatDrone).GuardRange);

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
            CombatDrone drone = smartCreature as CombatDrone;

            if (!drone.IsReceivedRetreatCommand)
            {
                ToEscortCombatDroneAI();

                return;
            }

            if (movement != null)
            {
                movement.Update(smartCreature, time);

                if (movement.Arrived)
                {
                    if (!(smartCreature as CombatDrone).IsInGuardRange)
                    {
                        ToRetreatCombatDroneAI();

                        return;
                    }

                    drone.Scoop();

                    return;
                }
            }
        }
    }
}
