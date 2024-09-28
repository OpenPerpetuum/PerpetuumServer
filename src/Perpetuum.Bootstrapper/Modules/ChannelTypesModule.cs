using Autofac;
using Perpetuum.RequestHandlers.Channels;
using Perpetuum.Services.Channels;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class ChannelTypesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<ChannelRepository>().As<IChannelRepository>();
            _ = builder.RegisterType<ChannelMemberRepository>().As<IChannelMemberRepository>();
            _ = builder.RegisterType<ChannelBanRepository>().As<IChannelBanRepository>();
            _ = builder.RegisterType<ChannelManager>().As<IChannelManager>().SingleInstance();

            builder.RegisterRequestHandler<ChannelCreate>(Commands.ChannelCreate);
            builder.RegisterRequestHandler<ChannelList>(Commands.ChannelList);
            builder.RegisterRequestHandler<ChannelListAll>(Commands.ChannelListAll);
            builder.RegisterRequestHandler<ChannelMyList>(Commands.ChannelMyList);
            builder.RegisterRequestHandler<ChannelJoin>(Commands.ChannelJoin);
            builder.RegisterRequestHandler<ChannelLeave>(Commands.ChannelLeave);
            builder.RegisterRequestHandler<ChannelKick>(Commands.ChannelKick);
            builder.RegisterRequestHandler<ChannelTalk>(Commands.ChannelTalk);
            builder.RegisterRequestHandler<ChannelSetMemberRole>(Commands.ChannelSetMemberRole);
            builder.RegisterRequestHandler<ChannelSetPassword>(Commands.ChannelSetPassword);
            builder.RegisterRequestHandler<ChannelSetTopic>(Commands.ChannelSetTopic);
            builder.RegisterRequestHandler<ChannelBan>(Commands.ChannelBan);
            builder.RegisterRequestHandler<ChannelRemoveBan>(Commands.ChannelRemoveBan);
            builder.RegisterRequestHandler<ChannelGetBannedMembers>(Commands.ChannelGetBannedMembers);
            builder.RegisterRequestHandler<ChannelGlobalMute>(Commands.ChannelGlobalMute);
            builder.RegisterRequestHandler<ChannelGetMutedCharacters>(Commands.ChannelGetMutedCharacters);
            builder.RegisterRequestHandler<ChannelCreateForTerminals>(Commands.ChannelCreateForTerminals);
        }
    }
}
