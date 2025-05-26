using Perpetuum.Accounting.Characters;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// Discord Integration EventProcessor
    /// </summary>
    public class DiscordIntegrationHandler : EventProcessor
    {
        private readonly IChannelManager _channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "Discord";
        private const string HelpChat = "regchannel_help";
        private readonly Character _announcer;

        public DiscordIntegrationHandler(IChannelManager channelManager)
        {
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            _channelManager = channelManager;
        }

        public override EventType Type => EventType.DiscordIntegration;
        public override void HandleMessage(IEventMessage message)
        {
            if (message is DiscordIntegrationMessage discordMessage)
            {
                string chatMessage = $"{discordMessage.Nick}: {discordMessage.Message}";

                _channelManager.Announcement(HelpChat, _announcer, chatMessage);
            }
        }
    }
}
