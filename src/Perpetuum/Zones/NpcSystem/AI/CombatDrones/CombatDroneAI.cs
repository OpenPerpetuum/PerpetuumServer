using Perpetuum.Collections;
using Perpetuum.Modules.Weapons;
using Perpetuum.PathFinders;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class CombatDroneAI : BaseAI
    {
        private const int UpdateFrequency = 1650;
        private const int Sqrt2 = 141;
        private const int Weight = 1000;
        // Timer for periodically checking the main hostile target.
        private readonly IntervalTimer updateHostileTimer = new IntervalTimer(UpdateFrequency, true);
        private readonly IntervalTimer processHostilesTimer = new IntervalTimer(UpdateFrequency);
        private readonly IntervalTimer primarySelectTimer = new IntervalTimer(UpdateFrequency);
        private List<ModuleActivator> moduleActivators;
        private TimeSpan hostilesUpdateFrequency = TimeSpan.FromMilliseconds(UpdateFrequency);
        private readonly CombatPrimaryLockSelectionStrategySelector stratSelector;
        private Position lastTargetPosition;
        private PathMovement movement;
        private PathMovement nextMovement;

        public CancellationTokenSource source;

        public bool IsNpcHasMissiles { get; set; } = false;

        public CombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            moduleActivators = smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
            IsNpcHasMissiles = smartCreature.ActiveModules
                .OfType<MissileWeaponModule>()
                .Any();
            _ = processHostilesTimer.Update(hostilesUpdateFrequency);
            _ = primarySelectTimer.Update(hostilesUpdateFrequency);

            base.Enter();
        }

        protected override List<ModuleActivator> FillModuleActivators()
        {
            return moduleActivators = smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
        }

        public override void Update(TimeSpan time)
        {
            UpdateHostiles(time);
            UpdatePrimaryTarget(time);
            base.Update(time);
        }

        protected void UpdateHostiles(TimeSpan time)
        {
            _ = processHostilesTimer.Update(time);

            if (processHostilesTimer.Passed)
            {
                processHostilesTimer.Reset();
                ProcessHostiles();
            }
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

        protected void SetPrimaryUpdateDelay(bool newPrimary)
        {
            primarySelectTimer.Interval = TimeSpan.FromSeconds(1.5);
        }

        protected bool IsAttackable(Hostile hostile)
        {
            if (!hostile.Unit.InZone)
            {
                return false;
            }

            if (hostile.Unit.States.Dead)
            {
                return false;
            }

            if (!hostile.Unit.IsLockable)
            {
                return false;
            }

            if (hostile.Unit.IsAttackable != ErrorCodes.NoError)
            {
                return false;
            }

            if (hostile.Unit.IsInvulnerable)
            {
                return false;
            }

            if (smartCreature.Behavior.Type == BehaviorType.Neutral && hostile.IsExpired)
            {
                return false;
            }

            bool isVisible = smartCreature.IsVisible(hostile.Unit);

            return isVisible;
        }

        protected void ProcessHostiles()
        {
            System.Collections.Immutable.ImmutableSortedSet<Hostile>.Enumerator hostileEnumerator = smartCreature.ThreatManager.Hostiles.GetEnumerator();

            while (hostileEnumerator.MoveNext())
            {
                Hostile hostile = hostileEnumerator.Current;

                if (!IsAttackable(hostile) || !smartCreature.IsInLockingRange(hostile.Unit))
                {
                    smartCreature.ThreatManager.Remove(hostile);
                }
                else
                {
                    smartCreature.AddDirectThreat(hostile.Unit, 100);
                }
            }
        }

        protected virtual void ToAttackCombatDroneAI()
        {
            smartCreature.AI.Push(new AttackCombatDroneAI(smartCreature));
        }

        protected virtual void ToEscortCombatDroneAI()
        {
            smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
        }

        protected Hostile GetPrimaryHostile()
        {
            return smartCreature.ThreatManager.Hostiles
                .Where(h => h.Unit == (smartCreature.GetPrimaryLock() as UnitLock)?.Target)
                .FirstOrDefault();
        }

        protected virtual void ReturnToHomePosition()
        {
            _ = smartCreature.AI.Pop();
            smartCreature.AI.Push(new HomingAI(smartCreature));
            WriteLog("Enter evade mode.");
        }

        protected Task<List<Point>> FindNewAttackPositionAsync(Unit hostile)
        {
            source?.Cancel();
            source = new CancellationTokenSource();

            return Task.Run(() => FindNewAttackPosition(hostile, source.Token), source.Token);
        }

        protected void UpdateHostile(TimeSpan time)
        {
            Hostile mostHated = GetPrimaryHostile();

            if (mostHated == null)
            {
                return;
            }

            bool forceCheckPrimary = false;
            _ = updateHostileTimer.Update(time);
            if (updateHostileTimer.Passed)
            {
                updateHostileTimer.Reset();

                // Forced check of the main hostile target.
                forceCheckPrimary = movement?.Arrived ?? true;
            }

            if (!mostHated.Unit.CurrentPosition.IsEqual2D(lastTargetPosition) || forceCheckPrimary)
            {
                lastTargetPosition = mostHated.Unit.CurrentPosition;

                bool findNewTargetPosition = false;

                if (!smartCreature.IsInRangeOf3D(mostHated.Unit, smartCreature.BestActionRange))
                {
                    findNewTargetPosition = true;
                }
                else
                {
                    IUnitVisibility visibility = smartCreature.GetVisibility(mostHated.Unit);

                    if (visibility != null)
                    {
                        LOSResult r = visibility.GetLineOfSight(IsNpcHasMissiles);

                        if (r.hit)
                        {
                            findNewTargetPosition = true;
                        }
                    }
                }

                if (findNewTargetPosition)
                {
                    _ = FindNewAttackPositionAsync(mostHated.Unit).ContinueWith(t =>
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

        protected UnitLock GetPrimaryUnitLock()
        {
            return (smartCreature as RemoteControlledCreature).CommandRobot
                .GetLocks()
                .Where(x => x is UnitLock && x.Primary)
                .FirstOrDefault() as UnitLock;
        }

        private bool SelectPrimaryTarget()
        {
            return SetLock(GetPrimaryUnitLock());
        }

        private bool SetLock(UnitLock unitLock)
        {
            bool isNewLock = false;
            if (unitLock == null)
            {
                smartCreature.ResetLocks();

                return false;
            }

            if (!smartCreature.HasFreeLockSlot)
            {
                smartCreature.GetLocks().First(x => !x.Primary).Cancel();
            }

            UnitLock primaryLock = smartCreature.GetPrimaryLock() as UnitLock;
            if ((primaryLock != null && primaryLock != unitLock) || smartCreature.GetLocks().Count == 0)
            {
                isNewLock = true;

                smartCreature.AddLock(unitLock.Target, true);
                smartCreature.AddDirectThreat(unitLock.Target, 100);
                unitLock.Target.UpdateVisibilityOf(smartCreature);
            }

            return isNewLock;
        }

        private List<Point> FindNewAttackPosition(Unit hostile, CancellationToken cancellationToken)
        {
            Point end = hostile.CurrentPosition.GetRandomPositionInRange2D(0, smartCreature.BestActionRange - 1).ToPoint();

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

                if (IsValidAttackPosition(hostile, current.position))
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

        private bool IsValidAttackPosition(Unit hostile, Point position)
        {
            Position position3 = smartCreature.Zone.FixZ(position.ToPosition()).AddToZ(smartCreature.Height);

            if (!hostile.CurrentPosition.IsInRangeOf3D(position3, smartCreature.BestActionRange))
            {
                return false;
            }

            LOSResult r = smartCreature.Zone.IsInLineOfSight(position3, hostile, false);

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
