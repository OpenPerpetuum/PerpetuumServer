using Perpetuum.Accounting.Characters;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class SapStateAnnouncer : EventProcessor
    {
        private readonly IChannelManager channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private const string CHANNEL = "Syndicate Intel";
        private readonly Character announcer;
        private readonly IDictionary<string, object> nameDictionary;
        private readonly IDictionary<long, SapStateMessage> state;

        public SapStateAnnouncer(
            IChannelManager channelManager,
            ICustomDictionary customDictionary)
        {
            announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            this.channelManager = channelManager;
            nameDictionary = customDictionary.GetDictionary(0);
            state = new Dictionary<long, SapStateMessage>();
        }

        //TODO use ChannelMessageHandler w/ PreMadeChatMessages
        private readonly IList<string> openedMessages = new List<string>()
        {
            "SAP has started",
        };

        private readonly IList<string> completedMessages = new List<string>()
        {
            "SAP has been completed",
        };

        private readonly IList<string> closedMessages = new List<string>()
        {
            "SAP has ended",
        };

        private string GetOutpostName(string ename)
        {
            if (nameDictionary.TryGetValue(ename, out string name) != true)
            {
                WriteNPCStateAnnouncerLog($"Missing localized name for {ename}");
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

        private string GetStateMessage(SapStateMessage msg)
        {
            switch (msg.State)
            {
                case SapState.Opened:
                    return openedMessages[FastRandom.NextInt(openedMessages.Count - 1)];
                case SapState.Completed:
                    return completedMessages[FastRandom.NextInt(completedMessages.Count - 1)];
                case SapState.Closed:
                    return closedMessages[FastRandom.NextInt(closedMessages.Count - 1)];
                default:
                    return string.Empty;

            }
        }

        private string BuildChatAnnouncement(SapStateMessage msg)
        {
            string outpostName = GetOutpostName(msg.SapEname);
            if (outpostName == string.Empty)
            {
                return string.Empty;
            }

            string stateMessage = GetStateMessage(msg);

            return stateMessage == string.Empty ? string.Empty : $"{outpostName} {stateMessage}";
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
            foreach (KeyValuePair<long, SapStateMessage> pair in state)
            {
                string name = Abbreviate(GetOutpostName(pair.Value.SapEname), allowableNameLens);
                if (name == string.Empty)
                {
                    continue;
                }

                if (pair.Value.State == SapState.Opened)
                {
                    topic += $"{name}|";
                }
            }

            return topic;
        }

        private bool IsUpdatable(SapStateMessage current, SapStateMessage next)
        {
            return next.TimeStamp > current.TimeStamp;
        }

        private bool UpdateState(SapStateMessage msg)
        {
            if (!state.ContainsKey((int)msg.Eid) || IsUpdatable(state[msg.Eid], msg))
            {
                state[msg.Eid] = msg;

                return true;
            }

            return false;
        }

        public override EventType Type => EventType.NpcState;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is SapStateMessage msg)
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
