using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.TargettingStrategies
{
    public delegate bool IndustrialTargetSelectionStrategy(SmartCreature smartCreature, TerrainLock[] locks);

    public static class IndustrialStrategies
    {
        public static Dictionary<IndustrialPrimaryLockSelectionStrategy, IndustrialTargetSelectionStrategy> All =
            new Dictionary<IndustrialPrimaryLockSelectionStrategy, IndustrialTargetSelectionStrategy>()
        {
            { IndustrialPrimaryLockSelectionStrategy.RichestTile, TargetRichestTile },
            { IndustrialPrimaryLockSelectionStrategy.PoorestTile, TargetPoorestTile },
            { IndustrialPrimaryLockSelectionStrategy.RandomTile, TargetRandomTile },
        };

        public static IndustrialTargetSelectionStrategy GetStrategy(IndustrialPrimaryLockSelectionStrategy strategyType)
        {
            return All.GetOrDefault(strategyType);
        }

        public static bool TryInvokeStrategy(IndustrialPrimaryLockSelectionStrategy strategyType, SmartCreature smartCreature, TerrainLock[] locks)
        {
            IndustrialTargetSelectionStrategy industrialLockSelectionStrategy = GetStrategy(strategyType);

            return industrialLockSelectionStrategy != null && industrialLockSelectionStrategy(smartCreature, locks);
        }

        private static bool TargetRichestTile(SmartCreature smartCreature, TerrainLock[] locks)
        {
            ImmutableSortedSet<IndustrialTargetsManagement.IndustrialTarget> industrialTargets = smartCreature.IndustrialValueManager.IndustrialTargets;

            if (!industrialTargets.Any())
            {
                return false;
            }

            IEnumerable<TerrainLock> industrialTargetLocks = locks.Where(u => industrialTargets.Any(h => h.Position.ToString() == u.Location.ToString()));
            TerrainLock mostHostileLock = industrialTargetLocks
                .OrderByDescending(u => industrialTargets.FirstOrDefault(h => h.Position.ToString() == u.Location.ToString())?.IndustrialValue ?? 0)
                .FirstOrDefault();

            return TrySetPrimaryLock(smartCreature, mostHostileLock);
        }

        private static bool TargetPoorestTile(SmartCreature smartCreature, TerrainLock[] locks)
        {
            ImmutableSortedSet<IndustrialTargetsManagement.IndustrialTarget> industrialTargets = smartCreature.IndustrialValueManager.IndustrialTargets;

            if (!industrialTargets.Any())
            {
                return false;
            }

            IEnumerable<TerrainLock> industrialTargetLocks = locks.Where(u => industrialTargets.Any(h => h.Position.ToString() == u.Location.ToString()));
            TerrainLock mostHostileLock = industrialTargetLocks
                .OrderBy(u => industrialTargets.FirstOrDefault(h => h.Position.ToString() == u.Location.ToString())?.IndustrialValue ?? 0)
                .FirstOrDefault();

            return TrySetPrimaryLock(smartCreature, mostHostileLock);
        }

        private static bool TargetRandomTile(SmartCreature smartCreature, TerrainLock[] locks)
        {
            ImmutableSortedSet<IndustrialTargetsManagement.IndustrialTarget> industrialTargets = smartCreature.IndustrialValueManager.IndustrialTargets;

            if (!industrialTargets.Any())
            {
                return false;
            }

            IEnumerable<TerrainLock> industrialTargetLocks = locks.Where(u => industrialTargets.Any(h => h.Position.ToString() == u.Location.ToString()));

            return TrySetPrimaryLock(smartCreature, industrialTargetLocks.RandomElement());
        }

        private static bool TrySetPrimaryLock(SmartCreature smartCreature, Lock targetLock)
        {
            if (targetLock == null)
            {
                return false;
            }

            smartCreature.SetPrimaryLock(targetLock);

            return true;
        }
    }
}
