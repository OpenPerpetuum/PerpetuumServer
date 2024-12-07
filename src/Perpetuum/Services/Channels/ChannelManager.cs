using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels.ChatCommands;
using Perpetuum.Services.Sessions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Perpetuum.Services.Channels
{
    public class ChannelManager : IChannelManager
    {
        private readonly ISessionManager _sessionManager;
        private readonly IChannelRepository _channelRepository;
        private readonly IChannelMemberRepository _memberRepository;
        private readonly IChannelBanRepository _banRepository;
        private readonly ChannelLoggerFactory _channelLoggerFactory;
        private readonly ConcurrentDictionary<string, Channel> _channels = new ConcurrentDictionary<string, Channel>();
        private readonly AdminCommandRouter _adminCommand;

        public ChannelManager(ISessionManager sessionManager, IChannelRepository channelRepository, IChannelMemberRepository memberRepository, IChannelBanRepository banRepository, ChannelLoggerFactory channelLoggerFactory, AdminCommandRouter adminCommand)
        {
            _sessionManager = sessionManager;
            _sessionManager.SessionAdded += OnSessionAdded;

            _channelRepository = channelRepository;
            _memberRepository = memberRepository;
            _banRepository = banRepository;
            _channelLoggerFactory = channelLoggerFactory;
            _adminCommand = adminCommand;

            foreach (Channel channel in channelRepository.GetAll())
            {
                _channels[channel.Name] = channel;
            }
        }

        private void OnSessionAdded(ISession session)
        {
            session.CharacterSelected += SessionOnCharacterSelected;
            session.CharacterDeselected += SessionOnCharacterDeselected;
        }

        private void SessionOnCharacterSelected(ISession session, Character character)
        {
            foreach (KeyValuePair<string, ChannelMember> kvp in _memberRepository.GetAllByCharacter(character))
            {
                string channelName = kvp.Key;
                ChannelMember member = kvp.Value;

                Channel channel = UpdateChannel(channelName, c => c.SetMember(member));
                channel?.SendMemberOnlineStateToAll(_sessionManager, member, true);
            }

            if (character.IsDocked)
            {
                character.GetCurrentDockingBase()?.TryJoinChannel(character);
            }
        }

        private void SessionOnCharacterDeselected(ISession session, Character character)
        {
            foreach (string name in _channels.Keys)
            {
                ChannelMember m = null;
                Channel channel = UpdateChannel(name, c =>
                {
                    m = c.GetMember(character);
                    return m == null ? c : c.RemoveMember(character);
                });

                channel?.SendMemberOnlineStateToAll(_sessionManager, m, false);
            }
        }

        public Channel GetChannelByName(string name)
        {
            return _channels.GetOrDefault(name);
        }

        public IEnumerable<Channel> Channels => _channels.Values;

        public void CreateChannel(ChannelType type, string name)
        {
            IChannelLogger logger = _channelLoggerFactory(name);
            Channel channel = new Channel(type, name, logger);
            channel = _channelRepository.Insert(channel);
            _channels[name] = channel;
        }

        public void DeleteChannel(string channelName)
        {
            if (!_channels.TryGetValue(channelName, out Channel channel))
            {
                return;
            }

            _channelRepository.Delete(channel);
            _channels.Remove(channelName);

            foreach (ChannelMember member in channel.Members)
            {
                Dictionary<string, object> data = new Dictionary<string, object> { { k.member, member.ToDictionary() } };
                MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.RemoveMember, data);
                channel.SendToOne(_sessionManager, member.character, n);
            }
        }

        public void JoinChannel(string channelName, Character member, ChannelMemberRole role, string password)
        {
            ChannelMember newMember = null;
            Channel channel = UpdateChannel(channelName, c =>
            {
                if (c.IsOnline(member))
                {
                    return c;
                }

                if (_memberRepository.Get(c, member) != null)
                {
                    return c;
                }

                if (member.AccessLevel.IsAdminOrGm())
                {
                    role |= PresetChannelRoles.ROLE_GOD;
                }
                else
                {
                    _banRepository.IsBanned(c, member).ThrowIfTrue(ErrorCodes.CharacterIsBanned);
                    c.CheckPasswordAndThrowIfMismatch(password);
                }

                newMember = new ChannelMember(member, role);
                _memberRepository.Insert(c, newMember);

                return _sessionManager.IsOnline(member) ? c.SetMember(newMember) : c;
            });

            if (channel == null)
            {
                return;
            }

            channel.SendAddMemberToAll(_sessionManager, newMember);
            channel.SendJoinedToMember(_sessionManager, newMember);
            channel.Logger.MemberJoin(member);
        }

        public void LeaveAllChannels(Character character)
        {
            foreach (Channel channel in GetChannelsByMember(character))
            {
                LeaveChannel(channel.Name, character);
            }
        }

        public void LeaveChannel(string channelName, Character character)
        {
            UpdateChannel(channelName, c => LeaveChannel(c, character, false));
        }

        private Channel LeaveChannel(Channel channel, Character character, bool isKicked)
        {
            ChannelMember m = _memberRepository.Get(channel, character);
            if (m == null)
            {
                return channel;
            }

            _memberRepository.Delete(channel, m);

            channel = channel.RemoveMember(m.character);

            bool hasMembers = _memberRepository.HasMembers(channel);
            if (!hasMembers && !channel.IsConstant)
            {
                _channelRepository.Delete(channel);
                _banRepository.UnBanAll(channel);
                _channels.Remove(channel.Name);
            }

            if (!isKicked)
            {
                Dictionary<string, object> data = new Dictionary<string, object> { { k.member, m.ToDictionary() } };
                MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.RemoveMember, data);

                channel.SendToAll(_sessionManager, n);
                channel.SendToOne(_sessionManager, character, n);
            }

            channel.Logger.MemberLeft(m.character);
            return channel;
        }

        public void SetPassword(string channelName, Character issuer, string password)
        {
            Channel channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_CHANGE_PASSWORD);
                c = c.SetPassword(password);
                _channelRepository.Update(c);
                return c;
            });

            if (channel == null)
            {
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.password, password } };
            MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.ChangePassword, data);
            channel.SendToAll(_sessionManager, n);
        }

        public void SetTopic(string channelName, Character issuer, string topic)
        {
            Channel channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_CHANGE_TOPIC);
                c = c.SetTopic(topic);
                _channelRepository.Update(c);
                return c;
            });

            if (channel == null)
            {
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.topic, topic } };
            MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.ChangeTopic, data);
            channel.SendToAll(_sessionManager, n);
            channel.Logger.TopicChanged(issuer, topic);
        }

        public void SetMemberRole(string channelName, Character issuer, Character character, ChannelMemberRole role)
        {
            // adminokra / gm-ekre nem lehet
            if (character.AccessLevel.IsAdminOrGm() && role == ChannelMemberRole.Undefined)
            {
                return;
            }

            ChannelMember m = null;
            Channel channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_MODIFY_MEMBER_ROLE);

                m = c.GetMember(character);
                if (m == null)
                {
                    return c;
                }

                m = m.WithRole(role);
                _memberRepository.Update(c, m);
                return c.SetMember(m);
            });

            if (channel == null || m == null)
            {
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.member, m.ToDictionary() } };
            MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.ChangeMemberRole, data);
            channel.SendToAll(_sessionManager, n);
        }

        public void Talk(string channelName, Character sender, string message, IRequest request)
        {
            if (!_channels.TryGetValue(channelName, out Channel channel))
            {
                return;
            }

            ChannelMember m = channel.GetMember(sender);
            if (m == null)
            {
                return;
            }

            channel.Logger.LogMessage(sender, message);

            m.CanTalk.ThrowIfFalse(ErrorCodes.CharacterIsMuted);

            //check if it's an admin command and don't display it until later in _adminCommand
            if (_adminCommand.IsAdminCommand(message))
            {
                _adminCommand.TryParseAdminCommand(sender, message, request, channel, this);
            }
            else
            {
                channel.SendMessageToAll(_sessionManager, sender, message);
            }

        }

        public void Announcement(string channelName, Character sender, string message)
        {
            if (!_channels.TryGetValue(channelName, out Channel channel))
            {
                return;
            }

            channel.Logger.LogMessage(sender, message);

            channel.SendMessageToAll(_sessionManager, sender, message);
        }

        public void KickOrBan(string channelName, Character issuer, Character character, string message, bool ban)
        {
            if (issuer == character)
            {
                return;
            }

            // adminokat / gm-eket nem lehet kickelni
            character.AccessLevel.IsAdminOrGm().ThrowIfTrue(ErrorCodes.AccessDenied);

            ChannelMember m = null;

            Channel channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_KICK_MEMBER);

                if (ban)
                {
                    _banRepository.Ban(c, character);
                }

                m = c.GetMember(character);

                return LeaveChannel(c, character, true);
            });

            if (channel == null || m == null)
            {
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {k.issuerID, issuer.Id},
                {k.member, m.ToDictionary()},
                {k.ban,ban},
                {k.message,message}
            };

            MessageBuilder n = channel.CreateNotificationMessage(ChannelNotify.KickMember, data);
            channel.SendToAll(_sessionManager, n);
            channel.SendToOne(_sessionManager, character, n);
        }

        public void UnBan(string channelName, Character issuer, Character character)
        {
            if (!_channels.TryGetValue(channelName, out Channel channel))
            {
                return;
            }

            channel.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_REMOVE_BAN);
            _banRepository.UnBan(channel, character);
        }

        [CanBeNull]
        private Channel UpdateChannel(string name, Func<Channel, Channel> channelUpdater)
        {
            SpinWait spinWait = new SpinWait();
            while (true)
            {
                if (!_channels.TryGetValue(name, out Channel snapshot))
                {
                    return null;
                }

                Channel updated = channelUpdater(snapshot);
                if (updated == snapshot)
                {
                    return snapshot;
                }

                if (_channels.TryUpdate(name, updated, snapshot))
                {
                    return updated;
                }

                spinWait.SpinOnce();
            }
        }

        public IEnumerable<Character> GetBannedCharacters(string channelName, Character issuer)
        {
            if (!_channels.TryGetValue(channelName, out Channel channel))
            {
                return Enumerable.Empty<Character>();
            }

            channel.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_LIST_BANNED_MEMBERS);
            return _banRepository.GetBannedCharacters(channel);
        }

        public IEnumerable<Channel> GetChannelsByMember(Character member)
        {
            return _channels.Values.Where(channel => channel.IsOnline(member));
        }

        public IEnumerable<Channel> GetPublicChannels()
        {
            return _channels.Values.Where(c => c.Type == ChannelType.Public || c.Type == ChannelType.Highlighted);
        }

        public IEnumerable<Channel> GetAllChannels()
        {
            return _channels.Values;
        }
    }
}