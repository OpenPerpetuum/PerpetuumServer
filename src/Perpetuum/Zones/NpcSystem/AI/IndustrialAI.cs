using Perpetuum.Timers;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class IndustrialAI : BaseAI
    {
        private const int UpdateFrequency = 1650;
        private const int EjectFrequency = 300000;
        private readonly IntervalTimer processIndustrialTargetsTimer = new IntervalTimer(UpdateFrequency);
        private readonly IntervalTimer processEjectTimer = new IntervalTimer(EjectFrequency);
        private readonly IntervalTimer primarySelectTimer;
        private List<ModuleActivator> moduleActivators;
        private TimeSpan industrialTargetsUpdateFrequency = TimeSpan.FromMilliseconds(UpdateFrequency);
        private TimeSpan ejectCargoFrequency = TimeSpan.FromMilliseconds(EjectFrequency);
        private IndustrialPrimaryLockSelectionStrategySelector stratSelector;

        public IndustrialAI(SmartCreature smartCreature) : base(smartCreature)
        {
            primarySelectTimer = new IntervalTimer((this.smartCreature.ActiveModules.Max(x => x?.CycleTime.Milliseconds) ?? 0) + UpdateFrequency);
        }

        public override void Enter()
        {
            stratSelector = InitSelector();
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
            UpdateIndustrialTargets(time);
            UpdatePrimaryTarget(time);
            RunModules(time);
            EjectCargo(time);
        }

        protected virtual IndustrialPrimaryLockSelectionStrategySelector InitSelector()
        {
            return IndustrialPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RichestTile, 5)
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.PoorestTile, 5)
                .WithStrategy(IndustrialPrimaryLockSelectionStrategy.RandomTile, 20)
                .Build();
        }

        protected void UpdateIndustrialTargets(TimeSpan time)
        {
            _ = processIndustrialTargetsTimer.Update(time);

            if (processIndustrialTargetsTimer.Passed)
            {
                processIndustrialTargetsTimer.Reset();
                ProcessIndustrialTargets();
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

        protected void RunModules(TimeSpan time)
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
                (smartCreature as IndustrialTurret).EjectCargo(smartCreature.Zone);
            }
        }

        protected virtual TimeSpan SetPrimaryDwellTime()
        {
            return FastRandom.NextTimeSpan(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
        }

        protected virtual void SetPrimaryUpdateDelay(bool newPrimary)
        {
            primarySelectTimer.Interval = newPrimary
                ? SetPrimaryDwellTime()
                : TimeSpan.FromSeconds(1);
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

        protected virtual void ProcessIndustrialTargets()
        {
            if (!smartCreature.IndustrialValueManager.IndustrialTargets.Any())
            {
                if (smartCreature.GetLocks().Any())
                {
                    smartCreature.ResetLocks();
                }

                return;
            }

            List<IndustrialTarget> cleanUpList = new List<IndustrialTarget>();

            ImmutableSortedSet<IndustrialTarget>.Enumerator industrialTargetsEnumerator =
                smartCreature.IndustrialValueManager.IndustrialTargets.GetEnumerator();

            while (industrialTargetsEnumerator.MoveNext())
            {
                IndustrialTarget industrialTarget = industrialTargetsEnumerator.Current;

                if (industrialTarget.IndustrialValue > 0 &&
                    smartCreature.IsInLockingRange(industrialTarget.Position))
                {
                    SetLockForIndustrialTarget(industrialTarget);

                    break;
                }
                else
                {
                    cleanUpList.Add(industrialTarget);
                }
            }

            foreach (IndustrialTarget industrialTarget in cleanUpList)
            {
                smartCreature.ProcessIndustrialTarget(industrialTarget.Position, industrialTarget.IndustrialValue);
            }
        }

        protected bool TryMakeFreeLockSlotFor(IndustrialTarget industrialTarget)
        {
            if (smartCreature.HasFreeLockSlot)
            {
                return true;
            }

            smartCreature.GetSecondaryLocks().ForEach(x => x.Cancel());

            return true;
        }

        protected IndustrialTarget GetValuableIndustrialTarget()
        {
            IndustrialTarget primaryTarget = smartCreature.IndustrialValueManager.IndustrialTargets
                .Where(h => h.Position == (smartCreature.GetPrimaryLock() as TerrainLock)?.Location &&
                h.IndustrialValue > 0)
                .FirstOrDefault();

            return primaryTarget;
        }

        private void SetLockForIndustrialTarget(IndustrialTarget industrialTarget)
        {
            bool mostValuable = GetValuableIndustrialTarget() == industrialTarget;
            TerrainLock industrialLock = smartCreature.GetLockByPosition(industrialTarget.Position);

            if (industrialLock == null)
            {
                if (TryMakeFreeLockSlotFor(industrialTarget))
                {
                    smartCreature.AddLock(industrialTarget.Position, mostValuable);
                }
            }
            else
            {
                if (mostValuable && !industrialLock.Primary)
                {
                    smartCreature.SetPrimaryLock(industrialLock.Id);
                }
            }
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

        private bool SelectPrimaryTarget()
        {
            TerrainLock[] validLocks = GetValidLocks();

            return validLocks.Length >= 1 && (stratSelector?.TryUseStrategy(smartCreature, validLocks) ?? false);
        }
    }
}
