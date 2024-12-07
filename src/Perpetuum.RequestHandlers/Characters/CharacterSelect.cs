using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSelect : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public CharacterSelect(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            Services.Sessions.ISession session = request.Session;
            if (!session.IsAuthenticated)
            {
                throw new PerpetuumException(ErrorCodes.NotSignedIn);
            }

            Character character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            if (character.AccountId != request.Session.AccountId)
            {
                throw new PerpetuumException(ErrorCodes.AccessDenied);
            }

            if (character.IsOffensiveNick)
            {
                throw PerpetuumException.Create(ErrorCodes.OffensiveNick).SetData("characterID", character.Id);
            }

            bool isDocked = character.IsDocked;
            Zones.IZone zone = character.GetCurrentZone();

            if (!isDocked)
            {
                if (zone == null || character.ZonePosition == null || character.ActiveRobotEid == 0L)
                {
                    isDocked = true;
                }
            }

            using (TransactionScope scope = Db.CreateTransaction())
            {
                character.Nick = character.Nick.Replace("_renamed_", "");
                character.LastUsed = DateTime.Now;
                character.IsDocked = isDocked;
                character.Language = request.Data.GetOrDefault<int>(k.language);
                character.IsOnline = true;

                IEnumerable<Channel> forcedChannels = _channelManager.Channels.Where(x => x.IsForcedJoin);

                foreach (Channel channel in forcedChannels)
                {
                    _channelManager.JoinChannel(channel.Name, character);
                }

                if (isDocked)
                {
                    character.ZoneId = null;
                    character.ZonePosition = null;
                    character.GetCurrentDockingBase()?.TryJoinChannel(character);
                }

                Groups.Corporations.Corporation corporation = character.GetCorporation();
                Groups.Alliances.Alliance alliance = character.GetAlliance();

                Transaction.Current.OnCommited(() =>
                {
                    session.SelectCharacter(character);

                    if (isDocked)
                    {
                        Dictionary<string, object> result = new Dictionary<string, object>
                        {
                            {k.characterID, character.Id},
                            {k.rootEID,character.Eid},
                            {k.corporationEID, corporation.Eid},
                            {k.allianceEID,alliance?.Eid ?? 0L}
                        };

                        Message.Builder.FromRequest(request).WithData(result).Send();
                    }
                    else
                    {
                        zone?.Enter(character, Commands.CharacterSelect);
                    }
                });

                scope.Complete();
            }
        }

        public Dictionary<string, object> GetJoinChannelData(string channel)
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                { k.channel, channel },
            };

            return result;
        }
    }
}