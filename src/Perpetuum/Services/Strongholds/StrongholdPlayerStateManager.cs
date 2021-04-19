using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Services.Sessions;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Services.Strongholds
{
    public interface IStrongholdPlayerStateManager : IProcess { }

    public class StrongholdPlayerStateManager : Process, IStrongholdPlayerStateManager
    {
        private readonly TimeSpan MAX_IDLE_TIME = TimeSpan.FromMinutes(1.5); //TODO: test value, TBD time period of no client activity
        private readonly TimeSpan MAX_DISCONNECT_TIME = TimeSpan.FromMinutes(2); //TODO: test value, TBD time period of logged off character
        private readonly IZone _zone;
        private readonly ISessionManager _sessionManager;
        private readonly TimeTracker _updateTimer = new TimeTracker(TimeSpan.FromSeconds(31));

        private IDictionary<int, TimeTracker> _loggedOffCharacters = new Dictionary<int, TimeTracker>();

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
                DoCheck(time);
                _updateTimer.Reset();
            }
        }

        public void DoCheck(TimeSpan time)
        {
            DockUpOfflinePlayers(time);
            DockOnlinePlayers();
        }

        private void DockUpOfflinePlayers(TimeSpan time)
        {
            IEnumerable<int> offlineCharIds;
            using (var scope = Db.CreateTransaction())
            {
                // Query for all offline chars on zone
                offlineCharIds = Db.Query().CommandText(
                    $"SELECT characterID FROM characters WHERE inUse=0 AND zoneID=@zoneId;")
                    .SetParameter("@zoneId", _zone.Id)
                    .Execute()
                    .Select(r => r.GetValue<int>("characterID"));

                foreach (var charId in offlineCharIds)
                {
                    // Track how long character is logged off for
                    if (_loggedOffCharacters.ContainsKey(charId))
                    {
                        _loggedOffCharacters[charId].Update(time);
                    }
                    else
                    {
                        _loggedOffCharacters[charId] = new TimeTracker(MAX_DISCONNECT_TIME);
                    }

                    // Set DB state of characters to be docked at homebase
                    if (_loggedOffCharacters[charId].Expired)
                    {
                        Db.Query().CommandText("opp.characterForceDockHome")
                            .SetParameter("@characterId", charId)
                            .ExecuteNonQuery();
                        // Remove character id from tracking
                        _loggedOffCharacters.Remove(charId);
                    }
                }
                scope.Complete();
            }
            // Remove any ids from tracking that are no longer in the current logged off set
            foreach (var key in _loggedOffCharacters.Keys.Where(k => !offlineCharIds.Contains(k)))
            {
                _loggedOffCharacters.Remove(key);
            }
        }

        private void DockOnlinePlayers()
        {
            var characters = _zone.Players.Where(p => p.Session.InactiveTime > MAX_IDLE_TIME).Select(p => p.Character);
            foreach (var character in characters)
            {
                DockPlayer(character);
            }
        }

        private void DockPlayer(Character character)
        {
            using (var scope = Db.CreateTransaction())
            {
                // Dock player to homebase
                var dockingBase = character.GetHomeBaseOrCurrentBase();
                dockingBase.DockIn(character, TimeSpan.FromSeconds(5), ZoneExitType.Docked);

                Transaction.Current.OnCommited(() =>
                {
                    // Send update to client if online
                    var session = _sessionManager.GetByCharacter(character);
                    Message.Builder.ToClient(session).WithError(ErrorCodes.YouAreHappyNow).Send();
                });

                scope.Complete();
            }
        }
    }
}
