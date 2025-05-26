using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.SapAttackers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers
{
    public class NpcReinforcementSpawner : NpcSpawnEventHandler<NpcReinforcementsMessage>
    {
        protected override TimeSpan SPAWN_DELAY => TimeSpan.FromSeconds(5);
        protected override TimeSpan SPAWN_LIFETIME => TimeSpan.FromMinutes(30);
        protected override int MAX_SPAWN_DIST => 15;

        public override EventType Type => EventType.NpcReinforce;

        private readonly IDictionary<NpcBossInfo, INpcPresences> _reinforcementsByNpc = new Dictionary<NpcBossInfo, INpcPresences>();
        public NpcReinforcementSpawner(
            IZone zone,
            INpcReinforcementsRepository reinforcementsRepo,
            ISapAttackersRepository sapAttackersRepository) : base(zone, reinforcementsRepo, sapAttackersRepository) { }

        protected override IEnumerable<INpcPresences> GetActiveReinforcments(Presence presence)
        {
            return _reinforcementsByNpc.Where(p => p.Value.HasActivePresence(presence)).Select(p => p.Value);
        }

        protected override bool CheckMessage(IEventMessage inMsg, out NpcReinforcementsMessage msg)
        {
            if (inMsg is NpcReinforcementsMessage message && _zone.Id == message.ZoneId)
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

        protected override void CheckReinforcements(NpcReinforcementsMessage msg)
        {
            NpcBossInfo info = msg.SmartCreature.BossInfo;
            if (!_reinforcementsByNpc.ContainsKey(info))
            {
                INpcPresences reinforcements = _npcReinforcementsRepo.CreateNpcBossAddSpawn(info, msg.ZoneId);
                _reinforcementsByNpc.Add(info, reinforcements);
            }
        }

        protected override bool CheckState(NpcReinforcementsMessage msg)
        {
            if (msg.SmartCreature.BossInfo.IsDead)
            {
                CleanupAllAttackers(msg);

                return true;
            }
            UpdateAggro(msg);

            return false;
        }

        private void UpdateAggro(NpcReinforcementsMessage msg)
        {
            NpcBossInfo info = msg.SmartCreature.BossInfo;
            if (_reinforcementsByNpc.ContainsKey(info))
            {
                IEnumerable<INpcPresence> activeWaves = _reinforcementsByNpc[info].GetAllActivePresences().Where(w => w.ActivePresence != null);
                foreach (INpcPresence wave in activeWaves)
                {
                    SpreadAggro(wave.ActivePresence, msg.SmartCreature);
                }
            }
        }

        protected override void CleanupAllAttackers(NpcReinforcementsMessage msg)
        {
            NpcBossInfo info = msg.SmartCreature.BossInfo;
            if (_reinforcementsByNpc.ContainsKey(info))
            {
                INpcPresence[] activeWaves = _reinforcementsByNpc[info].GetAllActivePresences();
                foreach (INpcPresence wave in activeWaves)
                {
                    ExpireWave(wave);
                }

                _reinforcementsByNpc.Remove(info);
            }
        }

        protected override Position FindSpawnPosition(NpcReinforcementsMessage msg, int maxRange)
        {
            RandomWalkableAroundPositionFinder finder = new RandomWalkableAroundPositionFinder(_zone, msg.SmartCreature.CurrentPosition, maxRange);

            return finder.Find(out Position result) ? result : msg.SmartCreature.CurrentPosition;
        }

        protected override INpcPresence GetNextWave(NpcReinforcementsMessage msg)
        {
            SmartCreature npc = msg.SmartCreature;
            double percent = 1.0 - npc.ArmorPercentage;

            return _reinforcementsByNpc[npc.BossInfo].GetNextPresence(percent);
        }

        protected override void OnSpawning(Presence pres, NpcReinforcementsMessage msg)
        {
            SpreadAggro(pres, msg.SmartCreature);
        }

        private void SpreadAggro(Presence presenceToAggro, SmartCreature smartCreatureWithAggro)
        {
            foreach (Npc npc in presenceToAggro.Flocks.GetMembers())
            {
                foreach (Zones.NpcSystem.ThreatManaging.Hostile threat in smartCreatureWithAggro.ThreatManager.Hostiles)
                {
                    npc.AddDirectThreat(threat.Unit, threat.Threat + FastRandom.NextDouble(5, 10));
                }
            }
        }
    }
}
