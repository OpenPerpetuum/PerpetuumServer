using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.SapAttackers;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers
{
    /// <summary>
    /// EventListener for each zone, receives messages for mineralnode mined and spawns npc presence based on the INpcReinforcementsRepository configurations
    /// </summary>
    public class OreNpcSpawner : NpcSpawnEventHandler<OreNpcSpawnMessage>
    {
        protected override TimeSpan SPAWN_DELAY => TimeSpan.FromSeconds(10);
        protected override TimeSpan SPAWN_LIFETIME => TimeSpan.FromHours(3);
        protected override int MAX_SPAWN_DIST => 100;

        private const int MIN_SPAWN_DIST_TOLERANCE = 30;

        public override EventType Type => EventType.NpcOre;

        private readonly IDictionary<MineralNode, INpcPresences> _reinforcementsByNode = new Dictionary<MineralNode, INpcPresences>();
        private readonly IEnumerable<IMineralConfiguration> _mineralConfigs;

        public OreNpcSpawner(
            IZone zone,
            INpcReinforcementsRepository reinforcementsRepo,
            ISapAttackersRepository sapAttackersRepository,
            IMineralConfigurationReader mineralConfigurationReader) : base(zone, reinforcementsRepo, sapAttackersRepository)
        {
            _mineralConfigs = mineralConfigurationReader.ReadAll().Where(c => c.ZoneId == zone.Id);
        }

        protected override IEnumerable<INpcPresences> GetActiveReinforcments(Presence presence)
        {
            return _reinforcementsByNode.Where(p => p.Value.HasActivePresence(presence)).Select(p => p.Value);
        }

        protected override bool CheckMessage(IEventMessage inMsg, out OreNpcSpawnMessage msg)
        {
            if (inMsg is OreNpcSpawnMessage message && _zone.Id == message.ZoneId)
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

        protected override void CheckReinforcements(OreNpcSpawnMessage msg)
        {
            MineralNode node = msg.Node;
            if (!_reinforcementsByNode.ContainsKey(node))
            {
                INpcPresences oreSpawn = _npcReinforcementsRepo.CreateOreNPCSpawn(node.Type, msg.ZoneId);
                _reinforcementsByNode.Add(node, oreSpawn);
            }
        }

        protected override bool CheckState(OreNpcSpawnMessage msg)
        {
            if (msg.NodeState == OreNodeState.Removed)
            {
                CleanupAllAttackers(msg);

                return true;
            }

            return false;
        }

        protected override void CleanupAllAttackers(OreNpcSpawnMessage msg)
        {
            MineralNode node = msg.Node;
            if (_reinforcementsByNode.ContainsKey(node))
            {
                INpcPresence[] activeWaves = _reinforcementsByNode[node].GetAllActivePresences();
                foreach (INpcPresence wave in activeWaves)
                {
                    ExpireWave(wave);
                }

                _reinforcementsByNode.Remove(node);
            }
        }

        protected override Position FindSpawnPosition(OreNpcSpawnMessage msg, int maxRange)
        {
            Position fieldCenter = msg.Node.Area.Center.ToPosition();
            RandomWalkableOnCircle finder = new RandomWalkableOnCircle(_zone, fieldCenter, maxRange, MIN_SPAWN_DIST_TOLERANCE);

            return finder.Find(out Position result) ? result : Position.Empty;
        }

        private double ComputeFieldPercentConsumed(MineralNode node)
        {
            int current = Convert.ToInt32(node.GetTotalAmount());
            int total = _mineralConfigs.Single(c => c.Type == node.Type).TotalAmountPerNode;
            double percent = 1.0 - (current / (double)total).Clamp();

            return percent;
        }

        protected override INpcPresence GetNextWave(OreNpcSpawnMessage msg)
        {
            MineralNode node = msg.Node;
            double percent = ComputeFieldPercentConsumed(node);

            return _reinforcementsByNode[node].GetNextPresence(percent);
        }

        protected override Position GetHomePos(OreNpcSpawnMessage msg, Position spawnPos)
        {
            return msg.Node.Area.Center.ToPosition();
        }
    }
}
