using Perpetuum.Data;
using Perpetuum.Players;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using Perpetuum.Zones;
using System;
using System.Linq;

namespace Perpetuum.Services.Strongholds
{
    public interface IStrongholdPlayerStateManager : IProcess { }

    public class StrongholdPlayerStateManager : Process, IStrongholdPlayerStateManager
    {
        private readonly int MAX_MINUTES = 60;
        private readonly IZone _zone;
        private readonly TimeTracker _updateTimer = new TimeTracker(TimeSpan.FromSeconds(31));

        public StrongholdPlayerStateManager(IZone zone)
        {
            _zone = zone;
            MAX_MINUTES = _zone.Configuration.TimeLimitMinutes ?? MAX_MINUTES;
        }

        public override void Update(TimeSpan time)
        {
            _updateTimer.Update(time);
            if (_updateTimer.Expired)
            {
                RunUpdate();
                _updateTimer.Reset();
            }
        }

        public void RunUpdate()
        {
            DockUpOfflinePlayers();
            SetDespawnOnlinePlayers();
        }

        private void DockUpOfflinePlayers()
        {
            using (var scope = Db.CreateTransaction())
            {
                // Query for all offline chars on zone
                var offlineCharIds = Db.Query().CommandText($"SELECT characterID FROM characters WHERE " +
                    $"inUse=0 AND @maxLogOffTimeMinutes < DATEDIFF(minute, lastLogOut, GETDATE()) AND zoneID=@zoneId;")
                    .SetParameter("@zoneId", _zone.Id)
                    .SetParameter("@maxLogOffTimeMinutes", MAX_MINUTES)
                    .Execute()
                    .Select(r => r.GetValue<int>("characterID"));

                foreach (var charId in offlineCharIds)
                {
                    Db.Query().CommandText("opp.characterForceDockHome")
                        .SetParameter("@characterId", charId)
                        .ExecuteNonQuery();
                }
                scope.Complete();
            }
        }

        private void SetDespawnOnlinePlayers()
        {
            foreach (var p in _zone.Players.Where(p => !p.HasDespawnEffect))
            {
                ApplyDespawn(p);
            }
        }

        private void ApplyDespawn(Player player)
        {
            player.SetDespawn(TimeSpan.FromMinutes(MAX_MINUTES), (u) =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    if (u is Player p)
                    {
                        var dockingBase = p.Character.GetHomeBaseOrCurrentBase();
                        p.DockToBase(dockingBase.Zone, dockingBase);
                    }
                    scope.Complete();
                }
            });
        }
    }
}
