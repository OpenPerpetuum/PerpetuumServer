using Perpetuum.PathFinders;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.RemoteControl;
using System;

namespace Perpetuum.Zones.NpcSystem.AI.IndustrialDrones
{
    public class RetreatIndustrialDroneAI : IndustrialAI
    {
        private PathMovement movement;
        private readonly PathFinder pathFinder;

        public RetreatIndustrialDroneAI(SmartCreature smartCreature) : base(smartCreature)
        {
            pathFinder = new AStarFinder(Heuristic.Manhattan, smartCreature.IsWalkable);
        }

        public override void Enter()
        {
            smartCreature.StopAllModules();
            smartCreature.ResetLocks();

            Position randomHome = smartCreature.Zone.FindPassablePointInRadius(smartCreature.HomePosition, (int)(smartCreature as IndustrialDrone).GuardRange);

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
            IndustrialDrone drone = smartCreature as IndustrialDrone;

            if (!drone.IsReceivedRetreatCommand)
            {
                ToEscortIndustrialDroneAI();

                return;
            }

            if (movement != null)
            {
                movement.Update(smartCreature, time);

                if (movement.Arrived)
                {
                    if (movement.Arrived)
                    {
                        if (!(smartCreature as IndustrialDrone).IsInGuardRange)
                        {
                            ToRetreatIndustrialDroneAI();

                            return;
                        }

                        drone.Scoop();

                        return;
                    }
                }
            }
        }
    }
}
