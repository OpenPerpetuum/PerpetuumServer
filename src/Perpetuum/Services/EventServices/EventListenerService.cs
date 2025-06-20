using Discord;
using Discord.WebSocket;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perpetuum.Services.EventServices
{
    /// <summary>
    /// Listener Process that processes queue of EventMessages and notifies observers
    /// </summary>
    public class EventListenerService : Process
    {
        private readonly object _lock = new object();
        private readonly IDictionary<EventType, IList<IEventProcessor>> _observers;
        private readonly ConcurrentQueue<IEventMessage> _queue;
        private readonly DiscordSocketClient _client;

        //private readonly DiscordSocketClient _client;
        private readonly GlobalConfiguration _globalConfiguration;

        public EventListenerService(GlobalConfiguration globalConfiguration)
        {
            _observers = new Dictionary<EventType, IList<IEventProcessor>>();
            _queue = new ConcurrentQueue<IEventMessage>();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            _globalConfiguration = globalConfiguration;
        }

        /// <summary>
        /// Send a message where the type of EventMessage determines which listener is notified
        /// </summary>
        /// <param name="message">EventMessage of the type</param>
        public void PublishMessage(IEventMessage message)
        {
            if (message is DiscordIntegrationMessage discordMessage &&
                discordMessage.Type == EventType.PerpetuumToDiscord)
            {
                if (_client.GetChannel(discordMessage.ChannelDiscordId) is IMessageChannel discordChannel)
                {
                    string messageToSend = $"**<{discordMessage.Nick}>**: {discordMessage.Message}";
                    discordChannel.SendMessageAsync(
                        messageToSend,
                        allowedMentions: new AllowedMentions { AllowedTypes = AllowedMentionTypes.Users });
                }
            }
            else
            {
                _queue.Enqueue(message);
            }
        }

        public void NotifyListeners(IEventMessage message)
        {
            lock (_lock)
            {
                if (_observers.TryGetValue(message.Type, out IList<IEventProcessor> list))
                {
                    foreach (IEventProcessor obs in list)
                    {
                        obs.OnNext(message);
                    }
                }
            }
        }

        /// <summary>
        /// Listeners are subscribed and instantiated in the Bootstrapper
        /// </summary>
        /// <param name="observer">Listener</param>
        public void AttachListener(IEventProcessor observer)
        {
            lock (_lock)
            {
                if (!_observers.ContainsKey(observer.Type))
                {
                    _observers.Add(observer.Type, new List<IEventProcessor>() { observer });
                }
                else
                {
                    _observers[observer.Type].Add(observer);
                }
            }
        }

        public override void Update(TimeSpan time)
        {
            TimeKeeper timer = new TimeKeeper(TimeSpan.FromSeconds(1));
            while (!_queue.IsEmpty && !timer.Expired)
            {
                if (_queue.TryDequeue(out IEventMessage message))
                {
                    NotifyListeners(message);
                }
            }
        }

        public override void Stop()
        {
            Task.Run(async () =>
            {
                await _client.LogoutAsync();
                await _client.StopAsync();
            });

            _client.Log -= Log;
            _client.MessageReceived -= OnMessageReceived;

            base.Stop();
        }

        public override void Start()
        {
            base.Start();

            string discordBotToken = _globalConfiguration.DiscordBotToken;

            if (!string.IsNullOrEmpty(discordBotToken))
            {
                _client.Log += Log;
                _client.MessageReceived += OnMessageReceived;

                // Run the async logic in the background
                Task.Run(async () =>
                {
                    await _client.LoginAsync(TokenType.Bot, discordBotToken);
                    await _client.StartAsync();
                });
            }
        }

        private Task OnMessageReceived(SocketMessage message)
        {
            ulong.TryParse(_globalConfiguration.OpHelpChannelId, out ulong channelId);

            if (!message.Author.IsBot && /*message.Channel.Id == channelId &&*/ !string.IsNullOrEmpty(message.CleanContent))
            {
                string nick = message.Author.GlobalName;

                // No more imposting until we find a better approach
                /*
                if (message.Author is SocketGuildUser guildUser &&
                    !string.IsNullOrEmpty(guildUser.DisplayName))
                {
                    nick = guildUser.DisplayName;
                }
                */

                PublishMessage(new DiscordIntegrationMessage(EventType.DiscordToPerpetuum, message.Channel.Id, nick, message.CleanContent));
            }

            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            // There has to be logging logic but meh.

            return Task.CompletedTask;
        }
    }
}
