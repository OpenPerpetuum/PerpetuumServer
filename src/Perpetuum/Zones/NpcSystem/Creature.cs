using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.LandMines;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public abstract class Creature : Robot
    {
        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is AreaBomb || target is LandMine)
            {
                UpdateVisibility(target);
            }
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            if (this is RemoteControlledCreature || ED.Options.Faction != Faction.Syndicate)
            {
                UpdateVisibility(player);
            }
        }

        protected override void OnUnitVisibilityUpdated(Unit target, Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                    {
                        Robot robot = target as Robot;
                        robot?.SubscribeLockEvents(OnUnitLockStateChanged);

                        target.TileChanged += OnUnitTileChanged;
                        target.EffectChanged += OnUnitEffectChanged;
                        OnUnitTileChanged(target);
                        break;
                    }
                case Visibility.Invisible:
                    {
                        Robot robot = target as Robot;
                        robot?.UnsubscribeLockEvents(OnUnitLockStateChanged);

                        target.TileChanged -= OnUnitTileChanged;
                        target.EffectChanged -= OnUnitEffectChanged;
                        break;
                    }
            }

            base.OnUnitVisibilityUpdated(target, visibility);
        }

        protected virtual void OnUnitEffectChanged(Unit unit, Effect effect, bool apply)
        {

        }

        protected virtual void OnUnitLockStateChanged(Lock @lock)
        {

        }

        protected virtual void OnUnitTileChanged(Unit unit)
        {

        }

        private const double PRIMARY_LOCK_CHANCE_FOR_SECONDARY_MODULE = 0.3;

        [CanBeNull]
        public UnitLock SelectOptimalLockTargetFor(ActiveModule module)
        {
            double range = module.OptimalRange + module.Falloff;
            UnitLock[] locks = GetLocks().OfType<UnitLock>().Where(l =>
            {
                if (l.State != LockState.Locked)
                {
                    return false;
                }

                bool isInOptimalRange = IsInRangeOf3D(l.Target, range);
                return isInOptimalRange;

            }).ToArray();

            UnitLock primaryLock = locks.FirstOrDefault(l => l.Primary);

            if (module.ED.AttributeFlags.PrimaryLockedTarget)
            {
                return primaryLock;
            }

            bool chance = FastRandom.NextDouble() <= PRIMARY_LOCK_CHANCE_FOR_SECONDARY_MODULE;
            return primaryLock != null && chance ? primaryLock : locks.RandomElement();
        }

        [CanBeNull]
        public TerrainLock SelectOptimalLockIndustrialTargetFor(ActiveModule module)
        {
            double range = module.OptimalRange;
            TerrainLock[] locks = GetLocks().OfType<TerrainLock>().Where(industrialLock =>
            {
                return industrialLock.State == LockState.Locked;
            }).ToArray();

            TerrainLock primaryLock = locks.FirstOrDefault(l => l.Primary);

            return primaryLock;
        }
    }
}