using Perpetuum.Accounting.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Services.Channels
{
    public static class ChannelMessageHandler
    {
        private const string WELCOME_CHANNEL_NAME = "General chat";
        private const string SENDER_CHARACTER_NICKNAME = "Placeholder";

        public static void SendWelcomeMessageExitTutorial(IChannelManager channelManager, string newCharacterName)
        {
            string welcomeMessage = string.Format("Please welcome our new player: {0}", newCharacterName);
            Character sender = Character.GetByNick(newCharacterName);
            channelManager.Talk(WELCOME_CHANNEL_NAME, sender, welcomeMessage, null);
        }
    }
}
