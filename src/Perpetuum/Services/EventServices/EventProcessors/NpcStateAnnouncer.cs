using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones.NpcSystem.Flocks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class NpcStateAnnouncer : EventProcessor
    {
        private readonly IChannelManager channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private const string CHANNEL = "Syndicate Radio";
        private readonly Character announcer;
        private readonly IFlockConfigurationRepository flockConfigReader;
        private readonly IDictionary<string, object> nameDictionary;
        private readonly IDictionary<int, NpcStateMessage> state;

        public NpcStateAnnouncer(
            IChannelManager channelManager,
            IFlockConfigurationRepository flockConfigurationRepo,
            ICustomDictionary customDictionary)
        {
            announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            this.channelManager = channelManager;
            flockConfigReader = flockConfigurationRepo;
            nameDictionary = customDictionary.GetDictionary(0);
            state = new Dictionary<int, NpcStateMessage>();
        }

        //TODO use ChannelMessageHandler w/ PreMadeChatMessages
        private readonly IList<string> aliveMessages = new List<string>()
        {
            "has spawned!",
            "has appeared on Syndicate scanners",
            "has been detected",
        };

        private readonly IList<string> deathMessages = new List<string>()
        {
            "has been defeated",
            "is no longer a threat to Syndicate activity",
            "'s signature is no longer detected at this time",
        };

        private readonly IList<string> safeDespawnMessages = new List<string>()
        {
            "has been recalled by Niani Gods",
            "is no longer on Syndicate scanners",
            "is disappeared",
        };

        private int GetNpcDef(NpcStateMessage msg)
        {
            IFlockConfiguration config = flockConfigReader.Get(msg.FlockId);
            return config?.EntityDefault?.Definition ?? -1;
        }

        private string GetNpcName(int def)
        {
            string nameToken = EntityDefault.Get(def).Name + "_name";
            if (nameDictionary.TryGetValue(nameToken, out string name) != true)
            {
                WriteNPCStateAnnouncerLog($"Missing localized name for {nameToken}");
            }
            return name ?? string.Empty;
        }

        private void WriteNPCStateAnnouncerLog(string message)
        {
            LogEvent e = new LogEvent
            {
                LogType = LogType.Error,
                Tag = "NpcStateAnnouncer",
                Message = message
            };

            Logger.Log(e);
        }

        private string GetStateMessage(NpcStateMessage msg)
        {
            switch (msg.State)
            {
                case NpcState.Alive:
                    return aliveMessages[FastRandom.NextInt(aliveMessages.Count - 1)];
                case NpcState.Dead:
                    return deathMessages[FastRandom.NextInt(deathMessages.Count - 1)];
                case NpcState.SafeDespawned:
                    return safeDespawnMessages[FastRandom.NextInt(safeDespawnMessages.Count - 1)];
                default:
                    return string.Empty;

            }
        }

        private string BuildChatAnnouncement(NpcStateMessage msg)
        {
            int def = GetNpcDef(msg);
            if (def < 0)
            {
                return string.Empty;
            }

            string npcName = GetNpcName(def);
            if (npcName == string.Empty)
            {
                return string.Empty;
            }

            string stateMessage = GetStateMessage(msg);
            return stateMessage == string.Empty ? string.Empty : $"{npcName} {stateMessage}";
        }

        private static string Strip(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "");
        }

        private static string Abbreviate(string name, int charLim)
        {
            string[] words = name.Split(' ');
            int perWordLen = (charLim / words.Length.Max(1)).Clamp(3, 24);
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = Strip(words[i]).Clamp(perWordLen) ?? "";
            }
            return string.Join(" ", words);
        }

        private string BuildTopicFromState()
        {
            string topic = "Current: ";
            int allowableNameLens = (190 / state.Count.Max(1)).Clamp(3, 64);
            foreach (KeyValuePair<int, NpcStateMessage> pair in state)
            {
                string name = Abbreviate(GetNpcName(pair.Key), allowableNameLens);
                if (name == string.Empty)
                {
                    continue;
                }

                if (pair.Value.State == NpcState.Alive)
                {
                    topic += $"{name}|";
                }
            }
            return topic;
        }

        private bool IsUpdatable(NpcStateMessage current, NpcStateMessage next)
        {
            return next.TimeStamp > current.TimeStamp;
        }

        private bool UpdateState(NpcStateMessage msg)
        {
            int defKey = GetNpcDef(msg);
            if (defKey < 0)
            {
                return false;
            }
            else if (!state.ContainsKey(defKey) || IsUpdatable(state[defKey], msg))
            {
                state[defKey] = msg;
                return true;
            }
            return false;
        }

        public override EventType Type => EventType.NpcState;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is NpcStateMessage msg)
            {
                if (UpdateState(msg))
                {
                    string announcement = BuildChatAnnouncement(msg);
                    string motd = BuildTopicFromState();
                    if (!announcement.IsNullOrEmpty())
                    {
                        channelManager.Announcement(CHANNEL, announcer, announcement);
                    }
                    if (!motd.IsNullOrEmpty())
                    {
                        channelManager.SetTopic(CHANNEL, announcer, motd);
                    }
                }

            }
        }
    }
}
