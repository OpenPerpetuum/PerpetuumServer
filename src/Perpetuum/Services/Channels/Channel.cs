using Perpetuum.Accounting.Characters;
using Perpetuum.Services.Sessions;
using System.Collections.Generic;

namespace Perpetuum.Services.Channels
{
    public class Channel
    {
        private Dictionary<Character, ChannelMember> _members = new Dictionary<Character, ChannelMember>();

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Topic { get; private set; }
        public string Password { get; private set; }

        public bool IsForcedJoin { get; private set; }

        public IChannelLogger Logger { get; private set; }
        private ChannelType _type;
        private ChannelType _prevType;
        public ChannelType Type
        {
            get => _type;
            private set
            {
                _prevType = _type;
                _type = value;
            }
        }

        private Channel()
        {

        }

        public Channel(int id, ChannelType type, string name, string topic, string password, bool isForcedJoin, IChannelLogger logger) : this(type, name, logger)
        {
            Id = id;
            Topic = topic;
            Password = password;
            IsForcedJoin = isForcedJoin;
        }

        public Channel(ChannelType type, string name, IChannelLogger logger)
        {
            Type = type;
            Name = name;

            Logger = logger;
        }

        public IEnumerable<ChannelMember> Members => _members.Values;

        public Channel SetId(int id)
        {
            return id == Id
                ? this
                : new Channel
                {
                    Id = id,
                    Type = Type,
                    Name = Name,
                    Topic = Topic,
                    Password = Password,
                    IsForcedJoin = IsForcedJoin,
                    Logger = Logger,
                    _members = new Dictionary<Character, ChannelMember>(_members)
                };
        }

        public Channel SetTopic(string topic)
        {
            if (!string.IsNullOrEmpty(topic) && topic.Length > 200)
            {
                topic = topic.Substring(0, 199);
            }

            return topic == Topic
                ? this
                : new Channel
                {
                    Id = Id,
                    Type = Type,
                    Name = Name,
                    Topic = topic,
                    Password = Password,
                    IsForcedJoin = IsForcedJoin,
                    Logger = Logger,
                    _members = new Dictionary<Character, ChannelMember>(_members)
                };
        }

        public Channel SetPassword(string password)
        {
            return password == Password
                ? this
                : new Channel
                {
                    Id = Id,
                    Type = Type,
                    Name = Name,
                    Topic = Topic,
                    Password = password,
                    IsForcedJoin = IsForcedJoin,
                    Logger = Logger,
                    _members = new Dictionary<Character, ChannelMember>(_members)
                };
        }

        public Channel SetMember(ChannelMember member)
        {
            if (member == null)
            {
                return this;
            }

            Dictionary<Character, ChannelMember> members = new Dictionary<Character, ChannelMember>(_members) { [member.character] = member };

            return new Channel
            {
                Id = Id,
                Type = Type,
                Name = Name,
                Topic = Topic,
                Password = Password,
                IsForcedJoin = IsForcedJoin,
                Logger = Logger,
                _members = members
            };
        }

        public void SetAdmin(bool isadminchannel)
        {
            Type = isadminchannel ? ChannelType.Admin : _prevType;
        }


        public Channel RemoveMember(Character member)
        {
            Dictionary<Character, ChannelMember> members = new Dictionary<Character, ChannelMember>(_members);
            return !members.Remove(member)
                ? this
                : new Channel
                {
                    Id = Id,
                    Type = Type,
                    Name = Name,
                    Topic = Topic,
                    Password = Password,
                    IsForcedJoin = IsForcedJoin,
                    Logger = Logger,
                    _members = members
                };
        }

        [CanBeNull]
        public ChannelMember GetMember(Character member)
        {
            return !_members.TryGetValue(member, out ChannelMember channelMember) ? null : channelMember;
        }

        public void CheckPasswordAndThrowIfMismatch(string password)
        {
            if (!HasPassword)
            {
                return;
            }

            Equals(Password, password ?? string.Empty).ThrowIfFalse(ErrorCodes.PasswordMismatch, gex => gex.SetData("channel", Name));
        }

        public void CheckRoleAndThrowIfFailed(Character member, ChannelMemberRole role)
        {
            if (member == Character.None || Character.IsSystemCharacter(member))
            {
                return;
            }

            GetMember(member).ThrowIfNull(ErrorCodes.NotMemberOfChannel).HasRole(role).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
        }

        public bool IsOnline(Character character)
        {
            return GetMember(character) != null;
        }

        public IDictionary<string, object> ToDictionary(Character issuer, bool withMembers)
        {
            bool isMember = true;
            if (issuer != Character.None)
            {
                isMember = IsOnline(issuer);
            }

            bool hasPassword = HasPassword;

            Dictionary<string, object> result = new Dictionary<string, object>
                             {
                                 {k.name,Name},
                                 {k.type, (int)Type},
                                 {k.password,hasPassword},
                             };

            if (isMember || !hasPassword)
            {
                result.Add(k.count, _members.Values.Count);
                result.Add(k.topic, Topic);
            }

            if (withMembers)
            {
                result.Add(
                    k.members,
                    _members.Values.ToDictionary(
                        "m",
                        m => m.ToDictionary()));
            }

            return result;
        }

        private bool HasPassword => !string.IsNullOrEmpty(Password);

        public bool IsConstant
        {
            get
            {
                switch (Type)
                {
                    case ChannelType.Public | ChannelType.Admin:
                        return false;
                }

                return true;
            }
        }

        public MessageBuilder CreateNotificationMessage(ChannelNotify notify, IDictionary<string, object> data)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                {k.channel,Name},
                {k.command, (int)notify},
                {k.data,data}
            };

            return Message.Builder.SetCommand(Commands.ChannelNotification).WithData(dictionary);
        }

        public void SendToAll(ISessionManager sessionManager, MessageBuilder messageBuilder)
        {
            SendToAll(sessionManager, messageBuilder, Character.None);
        }

        public void SendToAll(ISessionManager sessionManager, MessageBuilder messageBuilder, Character sender)
        {
            if (sessionManager == null || messageBuilder == null)
            {
                return;
            }

            IMessage message = messageBuilder.Build();

            foreach (Character member in _members.Keys)
            {
                if (sender != Character.None)
                {
                    if (member.IsBlocked(sender))
                    {
                        continue;
                    }
                }

                ISession session = sessionManager.GetByCharacter(member);
                session?.SendMessage(message);
            }
        }

        public void SendToOne(ISessionManager sessionManager, Character character, MessageBuilder messageBuilder)
        {
            ISession session = sessionManager?.GetByCharacter(character);
            session?.SendMessage(messageBuilder);
        }

        public void SendMessageToAll(ISessionManager sessionManager, Character sender, string message)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { k.sender, sender.Id },
                { k.message, message }
            };

            MessageBuilder n = CreateNotificationMessage(ChannelNotify.Message, data);
            SendToAll(sessionManager, n, sender);
        }

        public void SendMemberOnlineStateToAll(ISessionManager sessionManager, ChannelMember member, bool isOnline)
        {
            if (member == null)
            {
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object> { { k.member, member.ToDictionary() }, { k.state, isOnline } };
            MessageBuilder n = CreateNotificationMessage(ChannelNotify.OnlineState, data);
            SendToAll(sessionManager, n);
        }

        public void SendAddMemberToAll(ISessionManager sessionManager, ChannelMember member)
        {
            if (member == null || !sessionManager.IsOnline(member.character))
            {
                return;
            }

            MessageBuilder n = CreateNotificationMessage(ChannelNotify.AddMember, member.ToDictionary());
            SendToAll(sessionManager, n);
        }

        public void SendJoinedToMember(ISessionManager sessionManager, ChannelMember member)
        {
            if (member == null)
            {
                return;
            }

            Dictionary<string, object> d = new Dictionary<string, object>
                {
                    {k.member, member.ToDictionary()},
                    {k.channel,ToDictionary(member.character, true)}
                };

            MessageBuilder n = CreateNotificationMessage(ChannelNotify.Joined, d);
            SendToOne(sessionManager, member.character, n);
        }
    }
}