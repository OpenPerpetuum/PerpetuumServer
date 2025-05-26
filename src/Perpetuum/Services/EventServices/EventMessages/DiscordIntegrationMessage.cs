namespace Perpetuum.Services.EventServices.EventMessages
{
    /// <summary>
    /// Message for sending a message from Discord to the Game
    /// </summary>
    public class DiscordIntegrationMessage : IEventMessage
    {
        public EventType Type => EventType.DiscordIntegration;
        public string Nick { get; private set; }
        public string Message { get; private set; }
        public DiscordIntegrationMessage(string nick, string message)
        {
            Nick = nick;
            Message = message;
        }
    }
}
