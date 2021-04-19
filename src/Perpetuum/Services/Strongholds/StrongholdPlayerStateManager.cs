using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Services.Sessions;
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
        private readonly IZone _zone;
        private readonly ISessionManager _sessionManager;
        private readonly TimeTracker _updateTimer = new TimeTracker(TimeSpan.FromSeconds(31));
        public StrongholdPlayerStateManager(IZone zone, ISessionManager sessionManager)
        {
            _zone = zone;
            _sessionManager = sessionManager;
        }

        public override void Update(TimeSpan time)
        {
            _updateTimer.Update(time);
            if (_updateTimer.Expired)
            {
                DoCheck();
                _updateTimer.Reset();
            }
        }

        public void DoCheck()
        {
            DockUpOfflinePlayers();
            var characters = _zone.Players.Where(p => p.Session.InactiveTime > TimeSpan.FromMinutes(3)).Select(p => p.Character);
            foreach (var character in characters)
            {
                DockPlayer(character);
            }
        }

        private void DockUpOfflinePlayers()
        {
            using (var scope = Db.CreateTransaction())
            {
                // Query for all offline chars on zone
                var offlineCharIds = Db.Query().CommandText(
                    $"SELECT characterID FROM characters WHERE inUse=0 AND zoneID=@zoneId;")
                    .SetParameter("@zoneId", _zone.Id)
                    .Execute()
                    .Select(r => r.GetValue<int>("characterID"));

                // Set DB state of characters to be docked at homebase
                foreach (var charId in offlineCharIds)
                {
                    Db.Query().CommandText("opp.characterForceDockHome")
                                        .SetParameter("@characterId", charId)
                                        .ExecuteNonQuery();
                }
                scope.Complete();
            }
        }

        private void DockPlayer(Character character)
        {
            using (var scope = Db.CreateTransaction())
            {
                // Dock player to homebase
                var dockingBase = character.GetHomeBaseOrCurrentBase();
                dockingBase.DockIn(character, TimeSpan.FromSeconds(5), ZoneExitType.Docked);

                // Send update to client if online
                var session = _sessionManager.GetByCharacter(character);
                Message.Builder.ToClient(session).WithError(ErrorCodes.YouAreHappyNow).Send();

                scope.Complete();
            }
        }
    }
}
