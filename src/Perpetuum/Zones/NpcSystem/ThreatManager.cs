using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Perpetuum.Log;
using Perpetuum.Threading;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.NpcSystem
{
    public enum ThreatType
    {
        Undefined,
        Bodypull,
        Damage,
        Support,
        Lock,
        Buff,
        Debuff,
        Direct
    }

    public struct Threat
    {

        public const double WEBBER = 25;
        public const double LOCK_PRIMARY = 2.0;
        public const double LOCK_SECONDARY = 1.0;
        public const double SENSOR_DAMPENER = 25.0;
        public const double BODY_PULL = 1.0;
        public const double SENSOR_BOOSTER = 15;
        public const double REMOTE_SENSOR_BOOSTER = 15;

        public readonly ThreatType type;
        public readonly double value;

        public Threat(ThreatType type, double value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{type} = {value}";
        }

        public static Threat Multiply(Threat threat, double multiplier)
        {
            return new Threat(threat.type,threat.value * multiplier);
        }
    }

    public class Hostile : IComparable<Hostile>
    {
        private static readonly TimeSpan _threatTimeOut = TimeSpan.FromSeconds(30);

        private double _threat;

        public readonly Unit unit;

        public TimeSpan LastThreatUpdate { get; private set; }

        public event Action<Hostile> Updated;

        public Hostile(Unit unit)
        {
            this.unit = unit;
            Threat = 0.0;
        }

        public void AddThreat(Threat threat)
        {
            if ( threat.value <= 0.0 )
                return;

            Threat += threat.value;
        }

        public bool IsExpired
        {
            get { return (GlobalTimer.Elapsed - LastThreatUpdate) >= _threatTimeOut; }
        }

        public double Threat
        {
            get { return _threat; }
            private set
            {
                if (Math.Abs(_threat - value) <= double.Epsilon)
                    return;

                _threat = value;

                OnThreatUpdated();
            }
        }

        private void OnThreatUpdated()
        {
            LastThreatUpdate = GlobalTimer.Elapsed;

            Updated?.Invoke(this);
        }

        public int CompareTo(Hostile other)
        {
            if (other._threat < _threat)
                return -1;

            if (other._threat > _threat)
                return 1;

            return 0;
        }
    }

    public interface IThreatManager
    {
        bool Contains(Unit hostile);
        void Remove(Hostile hostile);
        ImmutableSortedSet<Hostile> Hostiles { get; }
    }

    public static class ThreatExtensions
    {
        [CanBeNull]
        public static Hostile GetMostHatedHostile(this IThreatManager manager)
        {
            return manager.Hostiles.Min;
        }
    }

    public class ThreatManager : IThreatManager
    {
        private ImmutableDictionary<long,Hostile> _hostiles = ImmutableDictionary<long, Hostile>.Empty;

        public Hostile GetOrAddHostile(Unit unit)
        {
            return ImmutableInterlocked.GetOrAdd(ref _hostiles, unit.Eid, eid =>
            {
                var h = new Hostile(unit);
                return h;
            });
        }

        public ImmutableSortedSet<Hostile> Hostiles
        {
            get { return _hostiles.Values.ToImmutableSortedSet(); }
        }

        public bool Contains(Unit unit)
        {
            return _hostiles.ContainsKey(unit.Eid);
        }

        public void Clear()
        {
            _hostiles.Clear();
        }

        public void Remove(Hostile hostile)
        {
            ImmutableInterlocked.TryRemove(ref _hostiles, hostile.unit.Eid, out hostile);
        }

         public string ToDebugString()
        {
            if ( _hostiles.Count == 0 )
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("========== THREAT ==========");
            sb.AppendLine();

            foreach (var hostile in _hostiles.Values.OrderByDescending(h => h.Threat))
            {
                sb.AppendFormat("  {0} ({1}) => {2}", hostile.unit.ED.Name,hostile.unit.Eid, hostile.Threat);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("============================");

            return sb.ToString();
        }
    }


    public interface IPsuedoThreatManager
    {
        void Update(TimeSpan time);
        void AddOrRefreshExisting(Unit hostile);
        void Remove(Unit hostile);
        void AwardPsuedoThreats(IThreatManager threatManager, IZone zone, int ep);
    }


    public class PsuedoThreatManager : IPsuedoThreatManager
    {
        private readonly TimeSpan LOCK_TIMEOUT = TimeSpan.FromSeconds(1);
        private readonly List<PsuedoThreat> _psuedoThreats;
        private readonly ReaderWriterLockSlim _lock;

        public PsuedoThreatManager()
        {
            _psuedoThreats = new List<PsuedoThreat>();
            _lock = new ReaderWriterLockSlim();
        }

        public void AwardPsuedoThreats(IThreatManager threatManager, IZone zone, int ep)
        {
            var psuedoHostiles = _psuedoThreats.Where(p => !threatManager.Contains(p.Unit));
            foreach (var hostile in psuedoHostiles)
            {
                var hostilePlayer = zone.ToPlayerOrGetOwnerPlayer(hostile.Unit);
                hostilePlayer?.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Npc, ep / 2);
            }
        }

        public void AddOrRefreshExisting(Unit hostile)
        {
            using (_lock.Read(LOCK_TIMEOUT))
            {
                var existing = _psuedoThreats.Where(x => x.Unit == hostile).FirstOrDefault();
                if (existing != null)
                {
                    existing.RefreshThreat();
                    return;
                }
            }

            using (_lock.Write(LOCK_TIMEOUT))
                _psuedoThreats.Add(new PsuedoThreat(hostile));

        }

        public void Remove(Unit hostile)
        {
            using (_lock.Write(LOCK_TIMEOUT))
                _psuedoThreats.RemoveAll(x => x.Unit == hostile);
        }

        public void Update(TimeSpan time)
        {
            using (_lock.Read(LOCK_TIMEOUT))
            {
                foreach (var threat in _psuedoThreats)
                {
                    threat.Update(time);
                }
            }
            CleanExpiredThreats();
        }

        private void CleanExpiredThreats()
        {
            using (_lock.Write(LOCK_TIMEOUT))
                _psuedoThreats.RemoveAll(threat => threat.IsExpired);
        }
    }



    public class PsuedoThreat
    {
        private TimeSpan _lastUpdated = TimeSpan.Zero;
        private TimeSpan Expiration = TimeSpan.FromMinutes(1);

        public PsuedoThreat(Unit unit)
        {
            Unit = unit;
        }

        public Unit Unit { get; }

        public bool IsExpired
        {
            get { return _lastUpdated > Expiration; }
        }

        public void RefreshThreat()
        {
            _lastUpdated = TimeSpan.Zero;
        }

        public void Update(TimeSpan time)
        {
            _lastUpdated += time;
        }
    }
}
