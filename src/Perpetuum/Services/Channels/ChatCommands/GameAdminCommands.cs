using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using System.Linq;
using System.Reflection;

namespace Perpetuum.Services.Channels.ChatCommands
{
    public class AdminCommandRouter
    {
        private readonly GlobalConfiguration _config;
        private readonly ISessionManager _sessionManager;
        private readonly MethodInfo[] _commands;
        public AdminCommandRouter(GlobalConfiguration configuration, ISessionManager sessionManager)
        {
            _config = configuration;
            _sessionManager = sessionManager;

            _commands = typeof(AdminCommandHandlers).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(ChatCommand), false).Length > 0)
                .ToArray();
        }

        public void TryParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (IsAdminCommand(sender, text))
            {
                ParseAdminCommand(sender, text, request, channel, channelManager);
            }
        }

        private bool IsAdminCommand(Character sender, string message)
        {
            return message.StartsWith("#") && sender.AccessLevel == AccessLevel.admin;
        }

        private void ParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (!IsAdminCommand(sender, text))
                return;

            string[] command = text.Split(new char[] { ',' });

            var data = AdminCommandData.Create(sender, command, request, channel, channelManager, _sessionManager, _config.EnableDev);

            // channel is not secured. must be secured first.
            if (channel.Type != ChannelType.Admin)
            {
                if (command[0] == "#secure")
                {
                    AdminCommandHandlers.Secure(data);
                    return;
                }
                channel.SendMessageToAll(_sessionManager, sender, "Channel must be secured before sending commands.");
                return;
            }

            ServerCommands(data);
        }

        private void ServerCommands(AdminCommandData data)
        {
            var commandMethod = _commands
                .Where(c => ((ChatCommand)c.GetCustomAttribute(typeof(ChatCommand))).Command == data.Command.Name)
                .FirstOrDefault();
            if(commandMethod != null)
            {
                commandMethod.Invoke(null, new object[] { data });
            }
            else
            {
                AdminCommandHandlers.Unknown(data);
            }
        }
    }
}
