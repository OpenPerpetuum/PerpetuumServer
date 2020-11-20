using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class NpcReinforcementSpawner : EventProcessor<EventMessage>
    {
        private readonly TimeSpan SPAWN_DELAY = TimeSpan.FromSeconds(9);
        private readonly TimeSpan SPAWN_LIFETIME = TimeSpan.FromMinutes(15);

        private readonly IZone _zone;
        private readonly IDictionary<NpcBossInfo, INpcReinforcements> _reinforcementsByNpc = new Dictionary<NpcBossInfo, INpcReinforcements>();
        private readonly INpcReinforcementsRepository _npcReinforcementsRepo;
        public NpcReinforcementSpawner(IZone zone, INpcReinforcementsRepository reinforcementsRepo)
        {
            _zone = zone;
            _npcReinforcementsRepo = reinforcementsRepo;
        }

        private void CheckReinforcements(NpcReinforcementsMessage msg)
        {
            var info = msg.Npc.BossInfo;
            if (!_reinforcementsByNpc.ContainsKey(info))
            {
                var oreSpawn = _npcReinforcementsRepo.CreateNpcBossAddSpawn(info, msg.ZoneId);
                _reinforcementsByNpc.Add(info, oreSpawn);
            }
        }

        private bool IsNpcDead(NpcReinforcementsMessage msg)
        {
            if (msg.Npc.BossInfo.IsDead)
            {
                CleanupReinforcementsOnDead(msg.Npc.BossInfo);
                return true;
            }
            return false;
        }

        private void CleanupReinforcementsOnDead(NpcBossInfo info)
        {
            if (_reinforcementsByNpc.ContainsKey(info))
            {
                var activeWaves = _reinforcementsByNpc[info].GetAllActiveWaves();
                foreach (var wave in activeWaves)
                {
                    ExpireWave(wave);
                }
                _reinforcementsByNpc.Remove(info);
            }
        }

        private Position FindSpawnPosition(NpcReinforcementsMessage msg)
        {
            return msg.Npc.CurrentPosition;
        }

        private double ComputeArmorLevelOfNpc(Npc npc)
        {
            return 1.0 - npc.ArmorPercentage;
        }

        private INpcReinforcementWave GetNextWave(NpcReinforcementsMessage msg)
        {
            var info = msg.Npc.BossInfo;
            var percent = ComputeArmorLevelOfNpc(msg.Npc);
            return _reinforcementsByNpc[info].GetNextPresence(percent);
        }

        private void DoBeams(Position beamLocation)
        {
            _zone.CreateBeam(BeamType.npc_egg_beam, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
            _zone.CreateBeam(BeamType.teleport_storm, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
        }

        private void DoSpawning(INpcReinforcementWave wave, Position homePosition, Npc boss)
        {
            var pres = _zone.AddDynamicPresenceToPosition(wave.PresenceId, homePosition, homePosition, SPAWN_LIFETIME);
            foreach (var npc in pres.Flocks.GetMembers())
            {
                foreach(var threat in boss.ThreatManager.Hostiles)
                {
                    npc.AddDirectThreat(threat.unit, threat.Threat + FastRandom.NextDouble(5, 10));
                }
            }
            pres.PresenceExpired += OnPresenceExpired;
            wave.SetActivePresence(pres);
        }

        private void OnPresenceExpired(Presence presence)
        {
            var matchedEntries = _reinforcementsByNpc.Where(p => p.Value.HasActivePresence(presence)).ToList();
            foreach (var pair in matchedEntries)
            {
                var wave = pair.Value.GetActiveWaveOfPresence(presence);
                ExpireWave(wave);
            }
        }

        private void ExpireWave(INpcReinforcementWave wave)
        {
            wave.ActivePresence.PresenceExpired -= OnPresenceExpired;
            wave.DeactivatePresence();
        }

        private bool _spawning = false;

        public override void OnNext(EventMessage value)
        {
            if (value is NpcReinforcementsMessage msg && _zone.Id == msg.ZoneId)
            {
                if (_spawning)
                    return;

                CheckReinforcements(msg);

                if (IsNpcDead(msg))
                    return;

                var spawnPos = FindSpawnPosition(msg);
                if (spawnPos == Position.Empty)
                    return; // Failed to find valid spawn location, try again on next cycle

                var wave = GetNextWave(msg);
                if (wave == null)
                    return; // Presence already spawned once, or not found

                DoBeams(spawnPos);

                _spawning = true;
                Task.Delay(SPAWN_DELAY).ContinueWith(t =>
                {
                    try
                    {
                        DoSpawning(wave, spawnPos, msg.Npc);
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
