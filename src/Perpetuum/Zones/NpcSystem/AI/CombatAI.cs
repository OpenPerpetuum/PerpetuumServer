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
            moduleActivators = this.smartCreature.ActiveModules
                .Select(m => new ModuleActivator(m))
                .ToList();
            IsNpcHasMissiles = this.smartCreature.ActiveModules
                .OfType<MissileWeaponModule>()
                .Any();
            processHostilesTimer.Update(hostilesUpdateFrequency);
            primarySelectTimer.Update(hostilesUpdateFrequency);

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            UpdateHostiles(time);
            UpdatePrimaryTarget(time);
            RunModules(time);
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
            processHostilesTimer.Update(time);

            if (processHostilesTimer.Passed)
            {
                processHostilesTimer.Reset();
                ProcessHostiles();
            }
        }

        protected void UpdatePrimaryTarget(TimeSpan time)
        {
            primarySelectTimer.Update(time);

            if (primarySelectTimer.Passed)
            {
                var success = SelectPrimaryTarget();
                SetPrimaryUpdateDelay(success);
            }
        }

        protected void RunModules(TimeSpan time)
        {
            foreach (var activator in moduleActivators)
            {
                activator.Update(time);
            }
        }

        protected virtual TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
        }

        protected virtual void SetPrimaryUpdateDelay(bool newPrimary)
        {
            if (newPrimary)
            {
                primarySelectTimer.Interval = SetPrimaryDwellTime();
            }
            else if (GetValidLocks().Length > 0)
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(1);
            }
            else if (this.smartCreature.GetLocks().Count > 0)
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(1.5);
            }
            else
            {
                primarySelectTimer.Interval = TimeSpan.FromSeconds(3.5);
            }
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

            if (this.smartCreature.Behavior.Type == BehaviorType.Neutral && hostile.IsExpired)
            {
                return false;
            }

            var isVisible = this.smartCreature.IsVisible(hostile.Unit);

            if (!isVisible)
            {
                return false;
            }

            return true;
        }

        protected virtual void ProcessHostiles()
        {
            var hostileEnumerator = this.smartCreature.ThreatManager.Hostiles.GetEnumerator();

            while (hostileEnumerator.MoveNext())
            {
                var hostile = hostileEnumerator.Current;

                if (!IsAttackable(hostile))
                {
                    this.smartCreature.ThreatManager.Remove(hostile);
                    this.smartCreature.AddPseudoThreat(hostile.Unit);

                    continue;
                }

                if (!this.smartCreature.IsInLockingRange(hostile.Unit))
                {
                    continue;
                }

                SetLockForHostile(hostile);
            }
        }

        protected bool TryMakeFreeLockSlotFor(Hostile hostile)
        {
            if (this.smartCreature.HasFreeLockSlot)
            {
                return true;
            }

            this.smartCreature.ThreatManager.Hostiles
                .Where(x => x.Threat == 0)
                .ForEach(x => this.smartCreature.GetLockByUnit(x.Unit).Cancel());

            var weakestLock = this.smartCreature.ThreatManager.Hostiles
                .SkipWhile(x => x != hostile)
                .Skip(1)
                .Select(x => this.smartCreature.GetLockByUnit(x.Unit))
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
            var primaryHostile = GetPrimaryHostile();

            if (primaryHostile != null)
            {
                return primaryHostile;
            }

            return this.smartCreature.ThreatManager.GetMostHatedHostile();
        }

        protected Hostile GetPrimaryHostile()
        {
            return this.smartCreature.ThreatManager.Hostiles
                .Where(h => h.Unit == (this.smartCreature.GetPrimaryLock() as UnitLock)?.Target)
                .FirstOrDefault();
        }

        protected virtual void ReturnToHomePosition()
        {
            smartCreature.AI.Pop();
            smartCreature.AI.Push(new HomingAI(smartCreature));
            this.WriteLog("Enter evade mode.");
        }

        protected Task<List<Point>> FindNewAttackPositionAsync(Unit hostile)
        {
            this.source?.Cancel();
            this.source = new CancellationTokenSource();

            return Task.Run(() => FindNewAttackPosition(hostile, this.source.Token), this.source.Token);
        }

        protected void UpdateHostile(TimeSpan time, bool moveThreatToPseudoThreat = true)
        {
            var mostHated = GetPrimaryOrMostHatedHostile();

            if (mostHated == null)
            {
                return;
            }

            var forceCheckPrimary = false;
            updateHostileTimer.Update(time);
            if (updateHostileTimer.Passed)
            {
                updateHostileTimer.Reset();

                // Forced check of the main hostile target.
                forceCheckPrimary = movement?.Arrived ?? true;
            }

            if (!mostHated.Unit.CurrentPosition.IsEqual2D(this.lastTargetPosition) || forceCheckPrimary)
            {
                this.lastTargetPosition = mostHated.Unit.CurrentPosition;

                var findNewTargetPosition = false;

                if (!smartCreature.IsInRangeOf3D(mostHated.Unit, smartCreature.BestActionRange))
                {
                    findNewTargetPosition = true;
                }
                else
                {
                    var visibility = smartCreature.GetVisibility(mostHated.Unit);

                    if (visibility != null)
                    {
                        var r = visibility.GetLineOfSight(this.IsNpcHasMissiles);

                        if (r.hit)
                        {
                            findNewTargetPosition = true;
                        }
                    }
                }

                if (findNewTargetPosition)
                {
                    FindNewAttackPositionAsync(mostHated.Unit).ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                            return;
                        }

                        var path = t.Result;

                        if (path == null)
                        {
                            if (moveThreatToPseudoThreat)
                            {
                                smartCreature.ThreatManager.Remove(mostHated);
                                smartCreature.AddPseudoThreat(mostHated.Unit);
                            }

                            return;
                        }

                        Interlocked.Exchange(ref this.nextMovement, new PathMovement(path));
                    });
                }
            }

            if (this.nextMovement != null)
            {
                movement = Interlocked.Exchange(ref this.nextMovement, null);
                movement.Start(this.smartCreature);
            }

            this.movement?.Update(this.smartCreature, time);
        }

        private void SetLockForHostile(Hostile hostile)
        {
            var mostHated = GetPrimaryOrMostHatedHostile() == hostile;
            var combatLock = this.smartCreature.GetLockByUnit(hostile.Unit);

            if (combatLock == null)
            {
                if (TryMakeFreeLockSlotFor(hostile))
                {
                    this.smartCreature.AddLock(hostile.Unit, mostHated);
                }
            }
            else
            {
                if (mostHated && !combatLock.Primary)
                {
                    this.smartCreature.SetPrimaryLock(combatLock.Id);
                }
            }
        }

        private bool IsLockValidTarget(UnitLock unitLock)
        {
            if (unitLock == null || unitLock.State != LockState.Locked)
            {
                return false;
            }

            var visibility = this.smartCreature.GetVisibility(unitLock.Target);

            if (visibility == null)
            {
                return false;
            }

            var r = visibility.GetLineOfSight(IsNpcHasMissiles);

            if (r != null && r.hit && (r.blockingFlags & BlockingFlags.Plant) == 0)
            {
                return false;
            }

            return unitLock.Target.GetDistance(this.smartCreature) < this.smartCreature.MaxActionRange;
        }

        private UnitLock[] GetValidLocks()
        {
            return this.smartCreature
                .GetLocks()
                .Select(l => (UnitLock)l)
                .Where(u => IsLockValidTarget(u))
                .ToArray();
        }

        private bool SelectPrimaryTarget()
        {
            var validLocks = GetValidLocks();

            if (validLocks.Length < 1)
            {
                return false;
            }

            return stratSelector?.TryUseStrategy(smartCreature, validLocks) ?? false;
        }

        private List<Point> FindNewAttackPosition(Unit hostile, CancellationToken cancellationToken)
        {
            var end = hostile.CurrentPosition.GetRandomPositionInRange2D(0, smartCreature.BestActionRange - 1).ToPoint();

            smartCreature.StopMoving();
            // Nulling movement so that the unit does not resume it at zero speed if the path is not found.
            movement = null;

            var maxNode = Math.Pow(smartCreature.HomeRange, 2) * Math.PI;
            var priorityQueue = new PriorityQueue<Node>((int)maxNode);
            var startNode = new Node(smartCreature.CurrentPosition);

            priorityQueue.Enqueue(startNode);

            var closed = new HashSet<Point>
            {
                startNode.position
            };

            Node current;

            while (priorityQueue.TryDequeue(out current))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (IsValidAttackPosition(hostile, current.position))
                {
                    return BuildPath(current);
                }

                foreach (var n in current.position.GetNeighbours())
                {
                    if (closed.Contains(n))
                    {
                        continue;
                    }

                    closed.Add(n);

                    if (!smartCreature.IsWalkable(n.X, n.Y))
                    {
                        continue;
                    }

                    if (!n.IsInRange(smartCreature.HomePosition, smartCreature.HomeRange))
                    {
                        continue;
                    }

                    var newG = current.g + (n.X - current.position.X == 0 || n.Y - current.position.Y == 0 ? 100 : Sqrt2);
                    var newH = Heuristic.Manhattan.Calculate(n.X, n.Y, end.X, end.Y) * Weight;
                    var newNode = new Node(n)
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
            var position3 = smartCreature.Zone.FixZ(position.ToPosition()).AddToZ(smartCreature.Height);

            if (!hostile.CurrentPosition.IsInRangeOf3D(position3, smartCreature.BestActionRange))
            {
                return false;
            }

            var r = smartCreature.Zone.IsInLineOfSight(position3, hostile, false);

            return !r.hit;
        }

        private static List<Point> BuildPath(Node current)
        {
            var stack = new Stack<Point>();
            var node = current;

            while (node != null)
            {
                stack.Push(node.position);
                node = node.parent;
            }

            return stack.ToList();
        }
    }
}
