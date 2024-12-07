using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Zones;
using Perpetuum.Zones.LandMines;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Perpetuum.Units
{
    public partial class Unit
    {
        private ImmutableDictionary<long, UnitVisibility> _visibleUnits = ImmutableDictionary<long, UnitVisibility>.Empty;

        public IReadOnlyCollection<IUnitVisibility> GetVisibleUnits()
        {
            return _visibleUnits.Values.ToArray();
        }

        public bool IsVisible(Unit target)
        {
            return _visibleUnits.ContainsKey(target.Eid);
        }

        [CanBeNull]
        public IUnitVisibility GetVisibility(Unit target)
        {
            return _visibleUnits.GetOrDefault(target.Eid);
        }

        protected virtual void UpdateUnitVisibility(Unit target)
        {
            // unit => unit nem latjak egymast
        }

        protected internal virtual void UpdatePlayerVisibility(Player player)
        {
            // unit => player nem latjak egymast
        }

        protected internal virtual void UpdateUnitVisibility(SentryTurret turret)
        {

        }

        protected internal virtual void UpdateUnitVisibility(Npc npc)
        {

        }

        public virtual void UpdateVisibilityOf(Unit target)
        {
            target.UpdateUnitVisibility(this);
        }

        protected void UpdateVisibility(Unit target)
        {
            Visibility visibility = Visibility.Invisible;

            if (InZone && target.InZone)
            {
                if (target.IsPlayer() && (target as Player).HasGMStealth)
                {
                    visibility = Visibility.Invisible;
                }
                else if (IsDetected(target))
                {
                    visibility = Visibility.Visible;
                }
            }

            if (!_visibleUnits.TryGetValue(target.Eid, out UnitVisibility info))
            {
                if (visibility == Visibility.Visible)
                {
                    info = new UnitVisibility(this, target);
                    _ = ImmutableInterlocked.TryAdd(ref _visibleUnits, target.Eid, info);
                    OnUnitVisibilityUpdated(target, Visibility.Visible);
                }
            }
            else
            {
                if (visibility == Visibility.Invisible)
                {
                    if (ImmutableInterlocked.TryRemove(ref _visibleUnits, target.Eid, out _))
                    {
                        OnUnitVisibilityUpdated(target, Visibility.Invisible);
                    }
                }
            }

            if (info != null && visibility == Visibility.Visible)
            {
                info.ResetLineOfSight();
            }
        }

        protected virtual void OnUnitVisibilityUpdated(Unit target, Visibility visibility)
        {
            Logger.DebugInfo(InfoString + " => " + target.InfoString + " => " + visibility);
        }

        protected virtual bool IsDetected(Unit target)
        {
            if (target is Robot robot && robot.IsLocked(this))
            {
                return true;
            }

            double range = target is LandMine
                ? (this as Robot).MineDetectionRange
                : 100 / Math.Max(1, target.StealthStrength) * Math.Max(1, DetectionStrength);

            return IsInRangeOf3D(target, range);
        }

        public List<T> GetWitnessUnits<T>() where T : Unit
        {
            List<T> result = new List<T>();

            IZone zone = Zone;
            if (zone == null)
            {
                return result;
            }

            foreach (T unit in zone.Units.OfType<T>())
            {
                if (unit.IsVisible(this))
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        protected IEnumerable<Unit> GetUnitsWithinRange2D(double range)
        {
            return Zone.GetUnitsWithinRange2D(CurrentPosition, range);
        }
    }
}
