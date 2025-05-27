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
        private readonly Character _discordIntegrationCharacter;

        public DiscordIntegrationHandler(IChannelManager channelManager)
        {
            _discordIntegrationCharacter = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            _channelManager = channelManager;
        }

        public override EventType Type => EventType.DiscordToPerpetuum;
        public override void HandleMessage(IEventMessage message)
        {
            if (message is DiscordIntegrationMessage discordMessage)
            {
                string channelName = _channelManager.GetChannelNameByDiscordId(discordMessage.ChannelDiscordId);

                if (!string.IsNullOrEmpty(channelName))
                {
                    string chatMessage = $"{discordMessage.Nick}: {discordMessage.Message}";

                    _channelManager.Announcement(channelName, _discordIntegrationCharacter, chatMessage);
                }
            }
        }
    }
}
