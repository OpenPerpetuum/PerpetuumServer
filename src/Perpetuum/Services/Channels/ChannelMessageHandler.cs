using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
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
        private const string SENDER_CHARACTER_NICKNAME = "New Player Announcer";

        public static void SendWelcomeMessageExitTutorial(IChannelManager channelManager, string newCharacterName)
        {
            string message;
            using (var scope = Db.CreateTransaction())
            {
                var records = Db.Query()
                    .CommandText("SELECT message FROM premadechatmessage WHERE name = @messageName")
                    .SetParameter("@messageName", "TutorialCompleteMessage")
                    .Execute();
                message = records.First().GetValue<string>("message");
                scope.Complete();
            }
            message = message.Replace("$NAME$", newCharacterName);

            Character sender = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            channelManager.Announcement(WELCOME_CHANNEL_NAME, sender, message);
        }
    }
}
