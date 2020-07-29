using Perpetuum.Players;
using Perpetuum.Timers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones
{
    /// <summary>
    /// A class used by a Zone to determine if a Player is orphaned by a timeout check on its ZoneSession
    /// </summary>
    public class SessionlessPlayerTimeout
    {
        private static readonly TimeSpan MAX_ORPHAN_TIME = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan UPDATE_RATE = TimeSpan.FromMinutes(1.5);
        private readonly IZone _zone;
        private IEnumerable<PlayerTimeout> _orphanPlayers;

        public SessionlessPlayerTimeout(IZone zone)
        {
            _zone = zone;
            _orphanPlayers = new HashSet<PlayerTimeout>();
        }

        private TimeSpan _elapsed = TimeSpan.Zero;
        public void Update(TimeSpan time)
        {
            _elapsed += time;
            if (_elapsed < UPDATE_RATE)
                return;

            DoUpdate();
            _elapsed = TimeSpan.Zero;
        }

        private void DoUpdate()
        {
            _orphanPlayers = AddSessionlessPlayers(_orphanPlayers, _zone);
            _orphanPlayers = RemoveExpiredOrphansFromZone(_orphanPlayers);
        }

        private static IEnumerable<PlayerTimeout> AddSessionlessPlayers(IEnumerable<PlayerTimeout> orphans, IZone zone)
        {
            var sessionLessPlayers = zone.Players.Where(p => p.Session == ZoneSession.None).Select(p => new PlayerTimeout(p)).ToHashSet();
            return orphans.Union(sessionLessPlayers).Where(o => o.Player.Session == ZoneSession.None).ToHashSet();
        }

        private static IEnumerable<PlayerTimeout> RemoveExpiredOrphansFromZone(IEnumerable<PlayerTimeout> orphans)
        {
            var toRemove = orphans.Where(o => o.Expired);
            toRemove.ForEach(o => o.Player.RemoveFromZone());
            return orphans.Except(toRemove).ToHashSet();
        }

        private class PlayerTimeout : IEquatable<PlayerTimeout>
        {
            public Player Player { get; private set; }
            public bool Expired { get { return _time.Expired; } }
            public long Eid { get { return Player is null ? 0 : Player.Eid; } }
            private readonly TimeKeeper _time;

            public PlayerTimeout(Player p)
            {
                _time = new TimeKeeper(MAX_ORPHAN_TIME);
                Player = p;
            }

            public bool Equals(PlayerTimeout other)
            {
                if (other is null)
                    return false;

                return ReferenceEquals(this, other) || Eid == other.Eid;
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }
                else if (obj is PlayerTimeout pt)
                {
                    return Equals(pt);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Eid.GetHashCode();
            }
        }
    }
}