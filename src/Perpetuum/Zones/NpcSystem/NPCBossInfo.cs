﻿using Perpetuum.Data;
using Perpetuum.Players;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Units;
using Perpetuum.Zones.Intrusion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem
{
    /// <summary>
    /// Specifies the behavior of a Boss-type NPC with various settings
    /// </summary>
    public class NpcBossInfo
    {
        public static NpcBossInfo CreateBossInfoFromDB(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var flockid = record.GetValue<int>("flockid");
            var respawnFactor = record.GetValue<double?>("respawnNoiseFactor");
            var lootSplit = record.GetValue<bool>("lootSplitFlag");
            var outpostEID = record.GetValue<long?>("outpostEID");
            var stabilityPts = record.GetValue<int?>("stabilityPts");
            var overrideRelations = record.GetValue<bool>("overrideRelations");
            var deathMessage = record.GetValue<string>("customDeathMessage");
            var aggressMessage = record.GetValue<string>("customAggressMessage");
            var info = new NpcBossInfo(id, flockid, respawnFactor, lootSplit, outpostEID, stabilityPts, overrideRelations, deathMessage, aggressMessage);

            return info;
        }

        public static NpcBossInfo GetBossInfoByFlockID(int flockid)
        {
            var bossInfos = Db.Query()
                .CommandText(@"SELECT TOP 1 id, flockid, respawnNoiseFactor, lootSplitFlag, outpostEID, stabilityPts, overrideRelations, customDeathMessage, customAggressMessage
                    FROM dbo.npcbossinfo WHERE flockid=@flockid;")
                .SetParameter("@flockid", flockid)
                .Execute()
                .Select(CreateBossInfoFromDB);

            return bossInfos.SingleOrDefault();
        }

        private readonly int _id;
        private readonly int _flockid;
        private readonly double? _respawnNoiseFactor;
        private readonly long? _outpostEID;
        private readonly int? _stabilityPts;
        private readonly string _deathMsg;
        private readonly string _aggroMsg;
        private bool _speak;

        private bool IsOutpostBoss { get { return _outpostEID != null; } }
        private int StabilityPoints { get { return _stabilityPts ?? 0; } }
        private bool OverrideRelations { get; }

        public bool IsLootSplit { get; }

        public NpcBossInfo(int id, int flockid, double? respawnNoiseFactor, bool lootSplit, long? outpostEID, int? stabilityPts, bool overrideRelations, string customDeathMsg, string customAggroMsg)
        {
            _id = id;
            _flockid = flockid;
            _respawnNoiseFactor = respawnNoiseFactor;
            IsLootSplit = lootSplit;
            _outpostEID = outpostEID;
            _stabilityPts = stabilityPts;
            OverrideRelations = overrideRelations;
            _deathMsg = customDeathMsg;
            _aggroMsg = customAggroMsg;
            _speak = true;
        }

        /// <summary>
        /// Handle any actions that this NPC Boss should do upon Aggression, including sending a message
        /// </summary>
        /// <param name="aggressor">Player aggressor</param>
        /// <param name="channel">the npc event channel</param>
        public void OnAggro(Player aggressor, EventListenerService channel)
        {
            CommunicateAggression(aggressor, channel);
            HandleBossOutpostAggro(aggressor);
        }

        /// <summary>
        /// Handle any death behavior for this Boss NPC
        /// Includes sending a message, and affecting outpost's stability if set
        /// </summary>
        /// <param name="npc">The npc Boss killed</param>
        /// <param name="killer">Player killer</param>
        /// <param name="channel">npc-event listener channel</param>
        public void OnDeath(Npc npc, Unit killer, EventListenerService channel)
        {
            CommunicateDeath(killer, channel);
            HandleBossOutpostDeath(npc, killer, channel);
        }

        /// <summary>
        /// Apply any respawn timer modifiers, and reset any internal state on-respawn
        /// </summary>
        /// <param name="respawnTime">normal respawn time of npc</param>
        /// <returns>modified respawn time of npc</returns>
        public TimeSpan OnRespawn(TimeSpan respawnTime)
        {
            _speak = true;
            var factor = _respawnNoiseFactor ?? 0.0;
            return respawnTime.Multiply(FastRandom.NextDouble(1.0 - factor, 1.0 + factor));
        }

        private void HandleBossOutpostAggro(Player aggressor)
        {
            if (IsOutpostBoss)
            {
                aggressor.ApplyPvPEffect();
            }
        }

        private void CommunicateAggression(Unit aggressor, EventListenerService channel)
        {
            if (_speak)
            {
                _speak = false;
                SendMessage(aggressor, channel, _aggroMsg);
            }
        }

        private void CommunicateDeath(Unit aggressor, EventListenerService channel)
        {
            SendMessage(aggressor, channel, _deathMsg);
        }

        private void HandleBossOutpostDeath(Npc npc, Unit killer, EventListenerService channel)
        {
            if (!IsOutpostBoss)
                return;

            var zone = npc.Zone;
            IEnumerable<Unit> outposts = zone.Units.OfType<Outpost>();
            var outpost = outposts.First(o => o.Eid == _outpostEID);
            if (outpost is Outpost)
            {
                var participants = npc.ThreatManager.Hostiles.Select(x => zone.ToPlayerOrGetOwnerPlayer(x.unit)).ToList();
                var builder = StabilityAffectingEvent.Builder()
                    .WithOutpost(outpost as Outpost)
                    .WithOverrideRelations(OverrideRelations)
                    .WithSapDefinition(npc.Definition)
                    .WithSapEntityID(npc.Eid)
                    .WithPoints(StabilityPoints)
                    .AddParticipants(participants)
                    .WithWinnerCorp(zone.ToPlayerOrGetOwnerPlayer(killer).CorporationEid);
                channel.PublishMessage(builder.Build());
            }
        }

        private static void SendMessage(Unit src, EventListenerService eventChannel, string msg)
        {
            if (!msg.IsNullOrEmpty())
            {
                EventMessage eventMessage = new NpcMessage(msg, src);
                Task.Run(() => eventChannel.PublishMessage(eventMessage));
            }
        }
    }
}
