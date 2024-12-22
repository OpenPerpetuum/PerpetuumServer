using Perpetuum.Collections;
using Perpetuum.PathFinders;
using Perpetuum.Timers;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.NpcSystem.AI.IndustrialDrones;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IndustrialAI : BaseAI
    {
        private const int UpdateFrequency = 1650;
        private const int Sqrt2 = 141;
        private const int Weight = 1000;
        private const int EjectFrequency = 300000;
        private readonly IntervalTimer updateIndustrialTargetTimer = new IntervalTimer(UpdateFrequency, true);
        private readonly IntervalTimer processIndustrialTargetsTimer = new IntervalTimer(UpdateFrequency);
        private readonly IntervalTimer processEjectTimer = new IntervalTimer(EjectFrequency);
        private readonly IntervalTimer primarySelectTimer;
        private List<ModuleActivator> moduleActivators;
        private TimeSpan industrialTargetsUpdateFrequency = TimeSpan.FromMilliseconds(UpdateFrequency);
        private TimeSpan ejectCargoFrequency = TimeSpan.FromMilliseconds(EjectFrequency);
        private PathMovement movement;
        private PathMovement nextMovement;

        public IndustrialAI(SmartCreature smartCreature) : base(smartCreature)
        {
            primarySelectTimer = new IntervalTimer((this.smartCreature.ActiveModules.Max(x => x?.CycleTime.Milliseconds) ?? 0) + UpdateFrequency);
        }

        public CancellationTokenSource source;

        public override void Enter()
        {
            moduleActivators = smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
            _ = processIndustrialTargetsTimer.Update(industrialTargetsUpdateFrequency);
            _ = processEjectTimer.Update(ejectCargoFrequency);
            _ = primarySelectTimer.Update(industrialTargetsUpdateFrequency);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            UpdateIndustrialTarget(time);
            UpdatePrimaryTarget(time);
            RunModules(time);
            EjectCargo(time);
        }

        protected virtual void ToRetreatIndustrialDroneAI()
        {
            smartCreature.AI.Push(new RetreatIndustrialDroneAI(smartCreature));
        }

        protected void UpdatePrimaryTarget(TimeSpan time)
        {
            _ = primarySelectTimer.Update(time);

            if (primarySelectTimer.Passed)
            {
                bool success = SelectPrimaryTarget();
                SetPrimaryUpdateDelay(success);
            }
        }

        private bool SelectPrimaryTarget()
        {
            return SetLock(GetPrimaryTerrainLock());
        }

        protected virtual TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
        }

        protected virtual void SetPrimaryUpdateDelay(bool newPrimary)
        {
            primarySelectTimer.Interval = newPrimary
                ? SetPrimaryDwellTime()
                : GetValidLocks().Length > 0
                    ? TimeSpan.FromSeconds(1)
                    : smartCreature.GetLocks().Count > 0 ? TimeSpan.FromSeconds(1.5) : TimeSpan.FromSeconds(3.5);
        }

        protected void UpdateIndustrialTarget(TimeSpan time)
        {
            if (!(smartCreature.GetPrimaryLock() is TerrainLock mostValuable))
            {
                return;
            }

            bool forceCheckPrimary = false;
            _ = updateIndustrialTargetTimer.Update(time);
            if (updateIndustrialTargetTimer.Passed)
            {
                updateIndustrialTargetTimer.Reset();

                // Forced check of the main hostile target.
                forceCheckPrimary = movement?.Arrived ?? true;
            }

            if (forceCheckPrimary)
            {
                bool findNewTargetPosition = false;

                if (!smartCreature.IsInRangeOf3D(mostValuable.Location, smartCreature.BestActionRange))
                {
                    findNewTargetPosition = true;
                }
                else
                {
                    LOSResult losResult = smartCreature.Zone.IsInLineOfSight(smartCreature, mostValuable.Location, false);

                    if (losResult.hit)
                    {
                        findNewTargetPosition = true;
                    }
                }

                if (findNewTargetPosition)
                {
                    _ = FindNewAttackPositionAsync(mostValuable.Location).ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                            return;
                        }

                        List<Point> path = t.Result;

                        if (path == null)
                        {
                            return;
                        }

                        _ = Interlocked.Exchange(ref nextMovement, new PathMovement(path));
                    });
                }
            }

            if (nextMovement != null)
            {
                movement = Interlocked.Exchange(ref nextMovement, null);
                movement.Start(smartCreature);
            }

            movement?.Update(smartCreature, time);
        }

        protected new void RunModules(TimeSpan time)
        {
            foreach (ModuleActivator activator in moduleActivators)
            {
                activator.Update(time);
            }
        }

        protected void EjectCargo(TimeSpan time)
        {
            _ = processEjectTimer.Update(time);

            if (processEjectTimer.Passed)
            {
                processEjectTimer.Reset();
                (smartCreature as IndustrialDrone).EjectCargo(smartCreature.Zone);
            }
        }

        protected Task<List<Point>> FindNewAttackPositionAsync(Position position)
        {
            source?.Cancel();
            source = new CancellationTokenSource();

            return Task.Run(() => FindNewAttackPosition(position, source.Token), source.Token);
        }

        private bool IsLockValidTarget(TerrainLock industrialLock)
        {
            return industrialLock != null && industrialLock.State == LockState.Locked &&
                industrialLock.Location.IsInRangeOf2D(smartCreature.PositionWithHeight, smartCreature.MaxActionRange);
        }

        private TerrainLock[] GetValidLocks()
        {
            return smartCreature
                .GetLocks()
                .Select(l => (TerrainLock)l)
                .Where(u => IsLockValidTarget(u))
                .ToArray();
        }

        private bool SetLock(TerrainLock terrainLock)
        {
            bool isNewLock = false;
            if (terrainLock == null)
            {
                smartCreature.ResetLocks();

                return false;
            }

            if (!smartCreature.HasFreeLockSlot)
            {
                smartCreature.GetLocks().First(x => !x.Primary).Cancel();
            }

            TerrainLock primaryLock = smartCreature.GetPrimaryLock() as TerrainLock;
            if ((primaryLock != null && primaryLock != terrainLock) || smartCreature.GetLocks().Count == 0)
            {
                isNewLock = true;

                smartCreature.AddLock(terrainLock.Location, true);
            }

            return isNewLock;
        }

        private List<Point> FindNewAttackPosition(Position position, CancellationToken cancellationToken)
        {
            Point end = position.GetRandomPositionInRange2D(0, smartCreature.BestActionRange - 1).ToPoint();

            smartCreature.StopMoving();
            // Nulling movement so that the unit does not resume it at zero speed if the path is not found.
            movement = null;

            double maxNode = Math.Pow(smartCreature.HomeRange, 2) * Math.PI;
            PriorityQueue<Node> priorityQueue = new PriorityQueue<Node>((int)maxNode);
            Node startNode = new Node(smartCreature.CurrentPosition);

            priorityQueue.Enqueue(startNode);

            HashSet<Point> closed = new HashSet<Point>
            {
                startNode.position
            };


            while (priorityQueue.TryDequeue(out Node current))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (IsValidAttackPosition(position, current.position))
                {
                    return BuildPath(current);
                }

                foreach (Point n in current.position.GetNeighbours())
                {
                    if (closed.Contains(n))
                    {
                        continue;
                    }

                    _ = closed.Add(n);

                    if (!smartCreature.IsWalkable(n.X, n.Y))
                    {
                        continue;
                    }

                    if (!n.IsInRange(smartCreature.HomePosition, smartCreature.HomeRange))
                    {
                        continue;
                    }

                    int newG = current.g + (n.X - current.position.X == 0 || n.Y - current.position.Y == 0 ? 100 : Sqrt2);
                    int newH = Heuristic.Manhattan.Calculate(n.X, n.Y, end.X, end.Y) * Weight;
                    Node newNode = new Node(n)
                    {
                        g = newG,
                        f = newG + newH,
                        parent = current
                    };

                    priorityQueue.Enqueue(newNode);
                }
            }

            return null;
        }

        private bool IsValidAttackPosition(Position targetPosition, Point position)
        {
            Position position3 = smartCreature.Zone.FixZ(position.ToPosition()).AddToZ(smartCreature.Height);

            if (!targetPosition.IsInRangeOf3D(position3, smartCreature.BestActionRange))
            {
                return false;
            }

            LOSResult r = smartCreature.Zone.IsInLineOfSight(smartCreature, position3, false);

            return !r.hit;
        }

        private static List<Point> BuildPath(Node current)
        {
            Stack<Point> stack = new Stack<Point>();
            Node node = current;

            while (node != null)
            {
                stack.Push(node.position);
                node = node.parent;
            }

            return stack.ToList();
        }
    }
}
