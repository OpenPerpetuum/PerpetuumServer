using Perpetuum.Players;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Intrusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem
{
    /// <summary>
    /// Specifies the behavior of a Boss-type NPC with various settings
    /// </summary>
    public class NpcBossInfo
    {
        private readonly EventListenerService eventChannel;
        private readonly int id;
        private readonly double? respawnNoiseFactor;
        private readonly long? outpostEID;
        private readonly int? stabilityPts;
        private readonly string deathMsg;
        private readonly string aggroMsg;
        private readonly CustomRiftConfig riftConfig;
        private bool speak;
        private readonly bool overrideRelations;
        private readonly TimeKeeper onDamageDebounce = new TimeKeeper(TimeSpan.FromSeconds(5));
        private readonly TimeKeeper aggroDebounce = new TimeKeeper(TimeSpan.FromSeconds(5));

        private bool IsOutpostBoss => outpostEID != null;

        private int StabilityPoints => stabilityPts ?? 0;

        private bool HasRiftToSpawn => riftConfig != null;

        public int FlockId { get; }

        public TimeSpan RespawnTime { get; set; }

        public bool IsLootSplit { get; }

        public bool IsDead { get; private set; }

        public bool IsAnnounced { get; private set; }

        public bool IsServerWideAnnouncement { get; private set; }

        public bool IsNoRadioDelay { get; private set; }

        public NpcBossInfo(
            EventListenerService eventChannel,
            int id,
            int flockid,
            double? respawnNoiseFactor,
            bool lootSplit,
            long? outpostEID,
            int? stabilityPts,
            bool overrideRelations,
            string customDeathMsg,
            string customAggroMsg,
            CustomRiftConfig riftConfig,
            bool announce,
            bool isServerWideAnnouncement,
            bool isNoRadioDelay)
        {
            this.eventChannel = eventChannel;
            this.id = id;
            FlockId = flockid;
            this.respawnNoiseFactor = respawnNoiseFactor;
            IsLootSplit = lootSplit;
            this.outpostEID = outpostEID;
            this.stabilityPts = stabilityPts;
            this.overrideRelations = overrideRelations;
            deathMsg = customDeathMsg;
            aggroMsg = customAggroMsg;
            this.riftConfig = riftConfig;
            IsAnnounced = announce;
            speak = true;
            IsDead = false;
            IsServerWideAnnouncement = isServerWideAnnouncement;
            IsNoRadioDelay = isNoRadioDelay;
        }

        /// <summary>
        /// Handle any actions when the boss loses aggro
        /// </summary>
        public void OnDeAggro()
        {
            speak = true;
        }

        /// <summary>
        /// Handle any actions that this NPC Boss should do upon Aggression, including sending a message
        /// </summary>
        /// <param name="aggressor">Player aggressor</param>
        public void OnAggro(Player aggressor)
        {
            CommunicateAggression(aggressor);
            HandleBossOutpostAggro(aggressor);
        }



        /// <summary>
        /// Handle events to dispatch when the npc boss takes damage
        /// </summary>
        /// <param name="smartCreature">The npc Boss killed</param>
        /// <param name="aggressor">Player damager</param>
        public void OnDamageTaken(SmartCreature smartCreature, Player aggressor)
        {
            if (onDamageDebounce.Expired)
            {
                PublishMessage(new NpcReinforcementsMessage(smartCreature, smartCreature.Zone.Id));
                onDamageDebounce.Reset();
            }
        }

        /// <summary>
        /// Handle any death behavior for this Boss NPC
        /// Includes sending a message, and affecting outpost's stability if set
        /// </summary>
        /// <param name="npc">The npc Boss killed</param>
        /// <param name="killer">Player killer</param>
        public void OnDeath(Npc npc, Unit killer)
        {
            CommunicateDeath(killer);
            HandleBossOutpostDeath(npc, killer);
            SpawnPortal(npc, killer);
            IsDead = true;
            PublishMessage(new NpcReinforcementsMessage(npc, npc.Zone.Id));
            AnnounceDisappearance(NpcState.Dead);
        }

        public void OnSafeDespawn()
        {
            AnnounceDisappearance(NpcState.SafeDespawned);
        }

        public void AnnounceServerWide()
        {
            if (IsServerWideAnnouncement)
            {
                Message.Builder
                .SetCommand(Commands.ServerMessage)
                .WithData(new Dictionary<string, object>
                {
                    { k.message, MessageConstants.NianiCultistsDetected },
                    { k.type, 0 },
                    { k.recipients, 0 },
                    { k.translate, 1 },
                })
                .ToOnlineCharacters()
                .Send();
            }
        }

        /// <summary>
        /// The boss lives again
        /// </summary>
        public void OnRespawn()
        {
            speak = true;
            IsDead = false;

            AnnounceServerWide();

            AnnouceRespawn(IsNoRadioDelay);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NpcBossInfo);
        }

        public bool Equals(NpcBossInfo other)
        {
            return (other != null && ReferenceEquals(this, other)) || (other.id == id && other.FlockId == FlockId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 23;

                hash = (hash * 31) + id.GetHashCode();
                hash = (hash * 31) + FlockId.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Apply any respawn timer modifiers
        /// </summary>
        /// <param name="respawnTime">normal respawn time of npc</param>
        /// <returns>modified respawn time of npc</returns>
        public TimeSpan GetNextSpawnTime(TimeSpan respawnTime)
        {
            double factor = respawnNoiseFactor ?? 0.0;

            return respawnTime.Multiply(FastRandom.NextDouble(1.0 - factor, 1.0 + factor));
        }

        private void AnnouceRespawn(bool noDelay = false)
        {
            if (!IsAnnounced)
            {
                return;
            }

            TimeSpan randomDelay = noDelay
                ? TimeSpan.Zero
                : FastRandom.NextTimeSpan(RespawnTime.Divide(5), RespawnTime.Divide(2));
            DateTime timeStamp = DateTime.UtcNow;

            _ = Task.Delay(randomDelay).ContinueWith((t) =>
            {
                PublishMessage(new NpcStateMessage(FlockId, NpcState.Alive, timeStamp));
            });
        }

        private void AnnounceDisappearance(NpcState npcState)
        {
            if (!IsAnnounced)
            {
                return;
            }

            PublishMessage(new NpcStateMessage(FlockId, npcState, DateTime.UtcNow));
        }

        private void HandleBossOutpostAggro(Player aggressor)
        {
            if (IsOutpostBoss)
            {
                aggressor.ApplyPvPEffect();
            }
        }

        private void CommunicateAggression(Unit aggressor)
        {
            if (aggroDebounce.Expired && speak)
            {
                speak = false;
                SendMessage(aggressor, aggroMsg);
                aggroDebounce.Reset();
            }
        }

        private void CommunicateDeath(Unit aggressor)
        {
            SendMessage(aggressor, deathMsg);
        }

        private void HandleBossOutpostDeath(Npc npc, Unit killer)
        {
            if (!IsOutpostBoss)
            {
                return;
            }

            IZone zone = npc.Zone;

            IEnumerable<Unit> outposts = zone.Units.OfType<Outpost>();

            Unit outpost = outposts.First(o => o.Eid == outpostEID);

            if (outpost is Outpost)
            {
                List<Player> participants = npc.ThreatManager.Hostiles
                    .Select(x => zone.ToPlayerOrGetOwnerPlayer(x.Unit))
                    .ToList();
                StabilityAffectingEvent.StabilityAffectBuilder builder = StabilityAffectingEvent.Builder()
                    .WithOutpost(outpost as Outpost)
                    .WithOverrideRelations(overrideRelations)
                    .WithSapDefinition(npc.Definition)
                    .WithSapEntityID(npc.Eid)
                    .WithPoints(StabilityPoints)
                    .AddParticipants(participants)
                    .WithWinnerCorp(zone.ToPlayerOrGetOwnerPlayer(killer).CorporationEid);

                PublishMessage(builder.Build());
            }
        }

        private void SpawnPortal(Npc npc, Unit killer)
        {
            if (!HasRiftToSpawn)
            {
                return;
            }

            PublishMessage(new SpawnPortalMessage(npc.Zone.Id, npc.CurrentPosition, riftConfig));
        }

        private void SendMessage(Unit src, string msg)
        {
            if (!msg.IsNullOrEmpty())
            {
                PublishMessage(new NpcMessage(msg, src));
            }
        }

        private void PublishMessage(IEventMessage eventMessage)
        {
            eventChannel.PublishMessage(eventMessage);
        }
    }
}
