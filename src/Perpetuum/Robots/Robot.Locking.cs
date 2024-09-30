using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Robots
{
    public partial class Robot
    {
        private LockHandler lockHandler;

        private void InitLockHander()
        {
            lockHandler = new LockHandler(this);
            lockHandler.LockStateChanged += OnLockStateChanged;
            lockHandler.LockError += OnLockError;
        }

        public bool IsLocked(Unit target)
        {
            return lockHandler.IsLocked(target);
        }

        [CanBeNull]
        public Lock GetLock(long lockId)
        {
            return lockHandler.GetLock(lockId);
        }

        [CanBeNull]
        public UnitLock GetLockByUnit(Unit unit)
        {
            return lockHandler.GetLockByUnit(unit);
        }

        [CanBeNull]
        public TerrainLock GetLockByPosition(Position position)
        {
            return lockHandler.GetLockByPosition(position);
        }

        public Lock GetPrimaryLock()
        {
            return lockHandler.GetPrimaryLock();
        }

        public IEnumerable<Lock> GetSecondaryLocks()
        {
            return lockHandler.GetSecondaryLocks();
        }

        public bool IsInLockingRange(Unit unit)
        {
            return lockHandler.IsInLockingRange(unit);
        }

        public bool IsInLockingRange(Position position)
        {
            return lockHandler.IsInLockingRange(position);
        }

        public void ResetLocks()
        {
            lockHandler.ResetLocks();
        }

        public List<Lock> GetLocks()
        {
            return lockHandler.Locks.ToList();
        }

        public bool HasFreeLockSlot => lockHandler.HasFreeLockSlot;

        public void AddLock(long targetEid, bool isPrimary)
        {
            lockHandler.AddLock(targetEid, isPrimary);
        }

        public void AddLock(Unit target, bool isPrimary)
        {
            lockHandler.AddLock(target, isPrimary);
        }

        public void AddLock(Position target, bool isPrimary)
        {
            lockHandler.AddLock(target, isPrimary);
        }

        public void AddLock(Lock newLock)
        {
            lockHandler.AddLock(newLock);
        }

        public void SetPrimaryLock(long lockId)
        {
            lockHandler.SetPrimaryLock(lockId);
        }

        public void SetPrimaryLock(Lock primaryLock)
        {
            lockHandler.SetPrimaryLock(primaryLock);
        }

        public void CancelLock(long lockId)
        {
            lockHandler.CancelLock(lockId);
        }

        public double MaxTargetingRange => lockHandler.MaxTargetingRange;

        public IEnumerable<Packet> GetLockPackets()
        {
            return GetLocks().Select(LockPacketBuilder.BuildPacket);
        }

        public void SubscribeLockEvents(LockEventHandler handler)
        {
            lockHandler.LockStateChanged += handler;
        }

        public void UnsubscribeLockEvents(LockEventHandler handler)
        {
            lockHandler.LockStateChanged -= handler;
        }

        public UnitLock GetFinishedPrimaryLock()
        {
            UnitLock unitLock = (UnitLock)lockHandler.GetPrimaryLock();
            return unitLock?.State == LockState.Locked ? unitLock : null;
        }
    }
}
