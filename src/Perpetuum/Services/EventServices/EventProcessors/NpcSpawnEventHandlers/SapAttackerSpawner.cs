using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.SapAttackers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers
{
    public class SapAttackerSpawner : NpcSpawnEventHandler<SapAttackersSpawnMessage>
    {
        protected override TimeSpan SPAWN_DELAY => TimeSpan.FromSeconds(10);
        protected override TimeSpan SPAWN_LIFETIME => TimeSpan.FromHours(3);
        protected override int MAX_SPAWN_DIST => 100;

        private const int MIN_SPAWN_DIST_TOLERANCE = 30;

        public override EventType Type => EventType.NpcSapAttackers;

        private readonly IDictionary<int, INpcPresences> _attackersBySap = new Dictionary<int, INpcPresences>();

        public SapAttackerSpawner(
            IZone zone,
            INpcReinforcementsRepository reinforcementsRepo,
            ISapAttackersRepository sapAttackersRepository) : base(zone, reinforcementsRepo, sapAttackersRepository)
        {
        }

        protected override IEnumerable<INpcPresences> GetActiveReinforcments(Presence presence)
        {
            return _attackersBySap.Where(p => p.Value.HasActivePresence(presence)).Select(p => p.Value);
        }

        protected override bool CheckMessage(IEventMessage inMsg, out SapAttackersSpawnMessage msg)
        {
            if (inMsg is SapAttackersSpawnMessage message && _zone.Id == message.ZoneId)
            {
                msg = message;

                return true;
            }
            else
            {
                msg = null;

                return false;
            }
        }

        protected override void CheckReinforcements(SapAttackersSpawnMessage msg)
        {
            if (!_attackersBySap.ContainsKey(msg.Sap.Definition))
            {
                INpcPresences oreSpawn = _npcSapAttackersRepo.CreateSapAttackersSpawn(msg.Sap.Definition, msg.ZoneId);
                _attackersBySap.Add(msg.Sap.Definition, oreSpawn);
            }
        }

        protected override bool CheckState(SapAttackersSpawnMessage msg)
        {
            if (msg.SapState == SapState.Closed || msg.SapState == SapState.Completed)
            {
                CleanupAllAttackers(msg);

                return true;
            }

            return false;
        }

        protected override void CleanupAllAttackers(SapAttackersSpawnMessage msg)
        {
            if (_attackersBySap.ContainsKey(msg.Sap.Definition))
            {
                INpcPresence[] activeWaves = _attackersBySap[msg.Sap.Definition].GetAllActivePresences();
                foreach (INpcPresence wave in activeWaves)
                {
                    ExpireWave(wave);
                }

                _attackersBySap.Remove(msg.Sap.Definition);
            }
        }

        protected override Position FindSpawnPosition(SapAttackersSpawnMessage msg, int maxRange)
        {
            Position fieldCenter = msg.Sap.CurrentPosition;
            RandomWalkableOnCircle finder = new RandomWalkableOnCircle(_zone, fieldCenter, maxRange, MIN_SPAWN_DIST_TOLERANCE);

            return finder.Find(out Position result) ? result : Position.Empty;
        }

        protected override Position GetHomePos(SapAttackersSpawnMessage msg, Position spawnPos)
        {
            return msg.Sap.CurrentPosition;
        }

        protected override INpcPresence GetNextWave(SapAttackersSpawnMessage msg)
        {
            return _attackersBySap[msg.Sap.Definition].GetNextPresence(msg.Stability);
        }
    }
}
