namespace Perpetuum.Services.EventServices.EventMessages
{
    /// <summary>
    /// Message for sending a message from Discord to the Game
    /// </summary>
    public class DiscordIntegrationMessage : IEventMessage
    {
        public EventType Type { get; private set; }
        public ulong ChannelDiscordId { get; private set; }
        public string Nick { get; private set; }
        public string Message { get; private set; }
        public DiscordIntegrationMessage(EventType type, ulong channelDiscordId, string nick, string message)
        {
            Type = type;
            ChannelDiscordId = channelDiscordId;
            Nick = nick;
            Message = message;
        }
    }
}
