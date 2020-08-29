﻿using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// EventListener for each zone, receives messages for mineralnode mined and spawns npc presence based on the INpcReinforcementsRepository configurations
    /// </summary>
    public class OreNpcSpawner : EventProcessor<EventMessage>
    {
        private const int SPAWN_DIST_FROM_FIELD = 100;
        private const int MIN_SPAWN_DIST_TOLERANCE = 30;
        private const int SPAWN_AREA_REQUIRED_SIZE = 3000;
        private const int POSITION_FIND_ATTEMPTS = 10;
        private readonly TimeSpan ORE_SPAWN_LIFETIME = TimeSpan.FromHours(3);
        private readonly TimeSpan SPAWN_DELAY = TimeSpan.FromSeconds(10);

        private readonly IZone _zone;
        private readonly IDictionary<MineralNode, INpcReinforcements> reinforcementsByMineralNode = new Dictionary<MineralNode, INpcReinforcements>();
        private readonly INpcReinforcementsRepository _npcReinforcementsRepo;
        private readonly IEnumerable<IMineralConfiguration> _mineralConfigs;

        public OreNpcSpawner(IZone zone, INpcReinforcementsRepository reinforcementsRepo, IMineralConfigurationReader mineralConfigurationReader)
        {
            _zone = zone;
            _npcReinforcementsRepo = reinforcementsRepo;
            _mineralConfigs = mineralConfigurationReader.ReadAll().Where(c => c.ZoneId == zone.Id);
        }

        private void OnPresenceExpired(Presence presence)
        {
            var matchedEntries = reinforcementsByMineralNode.Where(p => p.Value.HasActivePresence(presence)).ToList();
            foreach (var pair in matchedEntries)
            {
                var wave = pair.Value.GetActiveWaveOfPresence(presence);
                ExpireWave(wave);
            }
        }

        private void CleanupMineralNodeReinforcements(MineralNode node)
        {
            if (reinforcementsByMineralNode.ContainsKey(node))
            {
                var activeWaves = reinforcementsByMineralNode[node].GetAllActiveWaves();
                foreach (var wave in activeWaves)
                {
                    ExpireWave(wave);
                }
                reinforcementsByMineralNode.Remove(node);
            }
        }

        private void ExpireWave(INpcReinforcementWave wave)
        {
            wave.ActivePresence.PresenceExpired -= OnPresenceExpired;
            wave.DeactivatePresence();
        }

        private Position TryFindSpawnLocation(Position start, double range)
        {
            for (int i = 0; i < POSITION_FIND_ATTEMPTS; i++)
            {
                var random = FastRandom.NextDouble(0.0, 1.0);
                var pos = start.OffsetInDirection(random, range);
                var posFinder = new ClosestWalkablePositionFinder(_zone, pos);
                posFinder.Find(out Position p);
                var result = _zone.FindWalkableArea(p, _zone.Size.ToArea(), SPAWN_AREA_REQUIRED_SIZE);
                if (result != null && p.TotalDistance2D(start) > range - MIN_SPAWN_DIST_TOLERANCE)
                {
                    return p;
                }
            }
            return Position.Empty;
        }

        private double ComputeFieldPercentConsumed(MineralNode node)
        {
            var current = Convert.ToInt32(node.GetTotalAmount());
            var total = _mineralConfigs.Single(c => c.Type == node.Type).TotalAmountPerNode;
            var percent = 1.0 - (current / (double)total).Clamp();
            return percent;
        }

        private void CheckMineralNodeReinforcements(OreNpcSpawnMessage msg)
        {
            var node = msg.Node;
            if (!reinforcementsByMineralNode.ContainsKey(node))
            {
                var oreSpawn = _npcReinforcementsRepo.CreateOreNPCSpawn(node.Type, msg.ZoneId);
                reinforcementsByMineralNode.Add(node, oreSpawn);
            }
        }

        private bool IsNodeDead(OreNpcSpawnMessage msg)
        {
            if (msg.NodeState == OreNodeState.Removed)
            {
                CleanupMineralNodeReinforcements(msg.Node);
                return true;
            }
            return false;
        }

        private Position FindSpawnPosition(OreNpcSpawnMessage msg)
        {
            var fieldCenter = msg.Node.Area.Center.ToPosition();
            return TryFindSpawnLocation(fieldCenter, SPAWN_DIST_FROM_FIELD);
        }

        private INpcReinforcementWave GetNextWave(OreNpcSpawnMessage msg)
        {
            var node = msg.Node;
            var percent = ComputeFieldPercentConsumed(node);
            return reinforcementsByMineralNode[node].GetNextPresence(percent);
        }

        private Position GetOreFieldCenter(OreNpcSpawnMessage msg)
        {
            return msg.Node.Area.Center.ToPosition();
        }

        private void DoBeams(Position beamLocation)
        {
            _zone.CreateBeam(BeamType.npc_egg_beam, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
            _zone.CreateBeam(BeamType.teleport_storm, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
        }

        private void DoSpawning(INpcReinforcementWave wave, Position homePosition, Position spawnPosition, MineralNode node)
        {
            var pres = _zone.AddDynamicPresenceToPosition(wave.PresenceId, homePosition, spawnPosition, ORE_SPAWN_LIFETIME);
            pres.PresenceExpired += OnPresenceExpired;
            wave.SetActivePresence(pres);
        }

        private bool _spawning = false;

        public override void OnNext(EventMessage value)
        {
            if (value is OreNpcSpawnMessage msg && _zone.Id == msg.ZoneId)
            {
                if (_spawning)
                    return;

                CheckMineralNodeReinforcements(msg);

                if (IsNodeDead(msg))
                    return;

                var spawnPos = FindSpawnPosition(msg);
                if (spawnPos == Position.Empty)
                    return; // Failed to find valid spawn location, try again on next cycle

                var wave = GetNextWave(msg);
                if (wave == null)
                    return; // Presence already spawned once, or not found

                var fieldCenter = GetOreFieldCenter(msg);

                DoBeams(fieldCenter);

                _spawning = true;
                Task.Delay(SPAWN_DELAY).ContinueWith(t =>
                {
                    try
                    {
                        DoSpawning(wave, fieldCenter, spawnPos, msg.Node);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                    finally
                    {
                        _spawning = false;
                    }
                });
            }
        }
    }
}
