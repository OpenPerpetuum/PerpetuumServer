using Perpetuum.Accounting.Characters;
using Perpetuum.Collections;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones.NpcSystem.Flocks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class NpcStateAnnouncer : EventProcessor
    {
        private readonly IChannelManager _channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private const string CHANNEL = "Syndicate Radio";
        private readonly Character _announcer;
        private readonly IFlockConfigurationRepository _flockConfigReader;
        private readonly IDictionary<string, object> _nameDictionary;

        public NpcStateAnnouncer(IChannelManager channelManager, IFlockConfigurationRepository flockConfigurationRepo, ICustomDictionary customDictionary)
        {
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            _channelManager = channelManager;
            _flockConfigReader = flockConfigurationRepo;
            _nameDictionary = customDictionary.GetDictionary(0);
            _channelManager.JoinChannel(CHANNEL, _announcer);
            _channelManager.SetMemberRole(CHANNEL, _announcer, ChannelMemberRole.Operator);
        }

        private string GetNpcName(NpcStateMessage msg)
        {
            var config = _flockConfigReader.Get(msg.FlockId);
            var nameToken = config.EntityDefault.Name + "_name";
            var name = _nameDictionary[nameToken]?.ToString();
            return name ?? string.Empty;
        }

        private IList<string> _aliveMessages = new List<string>()
        {
            "has spawned!",
            "has appeared on Syndicate scanners",
            "has been detected"
        };

        private IList<string> _deathMessages = new List<string>()
        {
            "has been defeated",
            "is no longer a threat to Syndicate activity",
            "'s signature is no longer detected at this time"
        };

        private string GetStateMessage(NpcStateMessage msg)
        {
            if(msg.State == NpcState.Alive)
            {
                return _aliveMessages[FastRandom.NextInt(_aliveMessages.Count-1)];
            }
            else if (msg.State == NpcState.Dead)
            {
                return _deathMessages[FastRandom.NextInt(_deathMessages.Count-1)];
            }
            return string.Empty;
        }

        private string BuildChatAnnouncement(NpcStateMessage msg)
        {
            var npcName = GetNpcName(msg);
            if (npcName == string.Empty)
                return string.Empty;
            var stateMessage = GetStateMessage(msg);
            if (stateMessage == string.Empty)
                return string.Empty;
            return $"{npcName} {stateMessage}";
        }

        private string BuildMoTD(NpcStateMessage msg)
        {
            var npcName = GetNpcName(msg);
            if (npcName == string.Empty)
                return string.Empty;
            var currentTopic = _channelManager.GetChannelByName(CHANNEL)?.Topic ?? "";
            var pipeSplit = currentTopic.Split('|').ToList();
            var npcIndex = pipeSplit.IndexOf(s => s.Contains(npcName));
            var segment = $"{npcName}:{msg.State.ToString()}";
            if (npcIndex == -1)
            {
                pipeSplit.Add(segment);
            }
            else
            {
                pipeSplit[npcIndex] = segment;
            }

            var motd = string.Join("|", pipeSplit);
            return motd;
        }

        public override EventType Type => EventType.NpcState;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is NpcStateMessage msg)
            {
                var announcement = BuildChatAnnouncement(msg);
                var motd = BuildMoTD(msg);
                if (!announcement.IsNullOrEmpty())
                {
                    _channelManager.Announcement(CHANNEL, _announcer, announcement);
                    _channelManager.SetTopic(CHANNEL, Character.None, motd);
                }
            }
        }
    }
}
