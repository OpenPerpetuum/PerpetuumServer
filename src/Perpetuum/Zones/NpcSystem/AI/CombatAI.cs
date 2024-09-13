using Perpetuum.Collections;
using Perpetuum.Modules.Weapons;
using Perpetuum.PathFinders;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class CombatAI : BaseAI
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
        private CombatPrimaryLockSelectionStrategySelector stratSelector;
        private Position lastTargetPosition;
        private PathMovement movement;
        private PathMovement nextMovement;

        public CancellationTokenSource source;

        public bool IsNpcHasMissiles { get; set; } = false;

        public CombatAI(SmartCreature smartCreature) : base(smartCreature) { }

        public override void Enter()
        {
            stratSelector = InitSelector();
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

        protected virtual CombatPrimaryLockSelectionStrategySelector InitSelector()
        {
            return CombatPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(CombatPrimaryLockSelectionStrategy.Hostile, 9)
                .WithStrategy(CombatPrimaryLockSelectionStrategy.Random, 1)
                .Build();
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

        protected virtual void ProcessHostiles()
        {
            System.Collections.Immutable.ImmutableSortedSet<Hostile>.Enumerator hostileEnumerator = smartCreature.ThreatManager.Hostiles.GetEnumerator();

            while (hostileEnumerator.MoveNext())
            {
                Hostile hostile = hostileEnumerator.Current;

                if (!IsAttackable(hostile))
                {
                    smartCreature.ThreatManager.Remove(hostile);
                    smartCreature.AddPseudoThreat(hostile.Unit);

                    continue;
                }

                if (!smartCreature.IsInLockingRange(hostile.Unit))
                {
                    continue;
                }

                SetLockForHostile(hostile);
            }
        }

        protected bool TryMakeFreeLockSlotFor(Hostile hostile)
        {
            if (smartCreature.HasFreeLockSlot)
            {
                return true;
            }

            smartCreature.ThreatManager.Hostiles
                .Where(x => x.Threat == 0)
                .ForEach(x => smartCreature.GetLockByUnit(x.Unit).Cancel());

            UnitLock weakestLock = smartCreature.ThreatManager.Hostiles
                .SkipWhile(x => x != hostile)
                .Skip(1)
                .Select(x => smartCreature.GetLockByUnit(x.Unit))
                .LastOrDefault();

            if (weakestLock == null)
            {
                return false;
            }

            weakestLock.Cancel();

            return true;
        }

        protected Hostile GetPrimaryOrMostHatedHostile()
        {
            Hostile primaryHostile = GetPrimaryHostile();

            return primaryHostile ?? smartCreature.ThreatManager.GetMostHatedHostile();
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

        protected void UpdateHostile(TimeSpan time, bool moveThreatToPseudoThreat = true)
        {
            Hostile mostHated = GetPrimaryOrMostHatedHostile();

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
                            if (moveThreatToPseudoThreat)
                            {
                                smartCreature.ThreatManager.Remove(mostHated);
                                smartCreature.AddPseudoThreat(mostHated.Unit);
                            }

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

        private void SetLockForHostile(Hostile hostile)
        {
            bool mostHated = GetPrimaryOrMostHatedHostile() == hostile;
            UnitLock combatLock = smartCreature.GetLockByUnit(hostile.Unit);

            if (combatLock == null)
            {
                if (TryMakeFreeLockSlotFor(hostile))
                {
                    smartCreature.AddLock(hostile.Unit, mostHated);
                }
            }
            else
            {
                if (mostHated && !combatLock.Primary)
                {
                    smartCreature.SetPrimaryLock(combatLock.Id);
                }
            }
        }

        private bool IsLockValidTarget(UnitLock unitLock)
        {
            if (unitLock == null || unitLock.State != LockState.Locked)
            {
                return false;
            }

            IUnitVisibility visibility = smartCreature.GetVisibility(unitLock.Target);

            if (visibility == null)
            {
                return false;
            }

            LOSResult r = visibility.GetLineOfSight(IsNpcHasMissiles);

            return (r == null || !r.hit || (r.blockingFlags & BlockingFlags.Plant) != 0)
&& unitLock.Target.GetDistance(smartCreature) < smartCreature.MaxActionRange;
        }

        private UnitLock[] GetValidLocks()
        {
            return smartCreature
                .GetLocks()
                .Select(l => (UnitLock)l)
                .Where(u => IsLockValidTarget(u))
                .ToArray();
        }

        private bool SelectPrimaryTarget()
        {
            UnitLock[] validLocks = GetValidLocks();

            return validLocks.Length >= 1 && (stratSelector?.TryUseStrategy(smartCreature, validLocks) ?? false);
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
