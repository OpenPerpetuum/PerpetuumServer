using Perpetuum.Modules;
using Perpetuum.PathFinders;
using Perpetuum.Zones.Movements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HomingAI : BaseAI
    {
        private PathMovement movement;
        private readonly double maxReturnHomeRadius;
        private readonly PathFinder pathFinder;

        public HomingAI(SmartCreature smartCreature) : base(smartCreature)
        {
            maxReturnHomeRadius = (smartCreature.HomeRange * 0.4).Clamp(3, 20);
            pathFinder = new AStarFinder(Heuristic.Manhattan, smartCreature.IsWalkable);
        }

        public override void Enter()
        {
            Position randomHome =
                smartCreature.Zone.FindPassablePointInRadius(smartCreature.HomePosition, (int)maxReturnHomeRadius);

            if (randomHome == default)
            {
                randomHome = smartCreature.HomePosition;
            }

            _ = pathFinder
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

        protected override List<ModuleActivator> FillModuleActivators()
        {
            return smartCreature.ActiveModules
                .Where(x => x is NoxModule)
                .Select(m => new ModuleActivator(m))
                .ToList();
        }

        public override void Update(TimeSpan time)
        {
            if (movement != null)
            {
                movement.Update(smartCreature, time);

                if (movement.Arrived)
                {
                    _ = smartCreature.AI.Pop();

                    return;
                }
            }

            base.Update(time);
        }

        protected override void ToHomeAI() { }

        protected override void ToAggressorAI() { }
    }
}
