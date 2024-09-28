using Autofac;
using Autofac.Builder;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host;
using Perpetuum.Services;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sessions;
using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class AutoActivatedTypesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = RegisterAutoActivate<HostOnlineStateWriter>(builder, TimeSpan.FromSeconds(7));
            _ = RegisterAutoActivate<ServerInfoService>(builder, TimeSpan.FromMinutes(5));
            _ = RegisterAutoActivate<MarketCleanUpService>(builder, TimeSpan.FromHours(1));
            _ = RegisterAutoActivate<SessionCountWriter>(builder, TimeSpan.FromMinutes(5));
            _ = RegisterAutoActivate<VolunteerCEOProcessor>(builder, TimeSpan.FromMinutes(10));
            _ = RegisterAutoActivate<GiveExtensionPointsService>(builder, TimeSpan.FromMinutes(10));
            _ = RegisterAutoActivate<ArtifactRefresher>(builder, TimeSpan.FromHours(7));
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterAutoActivate<T>(
            ContainerBuilder builder,
            TimeSpan interval)
            where T : IProcess
        {
            return builder.RegisterType<T>().SingleInstance().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(interval));
            });
        }
    }
}
