using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Timers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones
{
    public class SessionlessPlayerTimeout
    {
        private static readonly TimeSpan MAX_ORPHAN_TIME = TimeSpan.FromMinutes(6);
        private static readonly TimeSpan UPDATE_RATE = TimeSpan.FromMinutes(2);
        private readonly IZone _zone;
        private readonly IDictionary<long, PlayerTimeout> _orphanPlayers;

        public SessionlessPlayerTimeout(IZone zone)
        {
            _zone = zone;
            _orphanPlayers = new Dictionary<long, PlayerTimeout>();
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
            Logger.DebugInfo($"Zone:{_zone.Id} Total players: {_zone.Players.Count()} Total orphan players: {_zone.Players.Count(p => p.Session == ZoneSession.None)}");

            // Players on the zone without a session - this is ok unless the session never connects!
            var playersWithoutSessions = _zone.Players.Where(p => p.Session == ZoneSession.None)
                .Where(p => !_orphanPlayers.Keys.Contains(p.Eid));

            foreach (var p in playersWithoutSessions)
            {
                _orphanPlayers.Add(p.Eid, new PlayerTimeout(p));
            }

            // Players that are no longer orphaned
            _orphanPlayers.RemoveRange(_orphanPlayers.Where(o => o.Value.Player.Session != ZoneSession.None).GetKeys().ToArray());

            // Players with Expired timers; Zone assumes the Player has failed to connect and removes them
            var toRemove = _orphanPlayers.Where(o => o.Value.Expired).ToArray();

            foreach (var o in toRemove)
            {
                o.Value.Player.RemoveFromZone();
                _orphanPlayers.Remove(o.Key);
                Logger.DebugInfo($"A player w/o session REMOVED: {o.Value.Player}");
            }
        }

        private class PlayerTimeout
        {
            public Player Player { get; private set; }
            public bool Expired { get { return _time.Expired; } }
            private readonly TimeKeeper _time;

            public PlayerTimeout(Player p)
            {
                _time = new TimeKeeper(MAX_ORPHAN_TIME);
                Player = p;
            }
        }

    }
}
