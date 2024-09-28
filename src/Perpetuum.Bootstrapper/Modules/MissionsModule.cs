using Autofac;
using Perpetuum.RequestHandlers.Missions;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class MissionsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<DisplayMissionSpotsProcess>();
            _ = builder.RegisterType<MissionDataCache>().SingleInstance();
            _ = builder.RegisterType<MissionHandler>();
            _ = builder.RegisterType<MissionInProgress>();
            _ = builder.RegisterType<MissionAdministrator>();
            _ = builder.RegisterType<MissionProcessor>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).SingleInstance();

            builder.RegisterRequestHandler<MissionData>(Commands.MissionData);
            builder.RegisterRequestHandler<MissionStart>(Commands.MissionStart);
            builder.RegisterRequestHandler<MissionAbort>(Commands.MissionAbort);
            builder.RegisterRequestHandler<MissionAdminListAll>(Commands.MissionAdminListAll);
            builder.RegisterRequestHandler<MissionAdminTake>(Commands.MissionAdminTake);
            builder.RegisterRequestHandler<MissionLogList>(Commands.MissionLogList);
            builder.RegisterRequestHandler<MissionListRunning>(Commands.MissionListRunning);
            builder.RegisterRequestHandler<MissionReloadCache>(Commands.MissionReloadCache);
            builder.RegisterRequestHandler<MissionGetOptions>(Commands.MissionGetOptions);
            builder.RegisterRequestHandler<MissionResolveTest>(Commands.MissionResolveTest);
            builder.RegisterRequestHandler<MissionDeliver>(Commands.MissionDeliver);
            builder.RegisterRequestHandler<MissionFlush>(Commands.MissionFlush);
            builder.RegisterRequestHandler<MissionReset>(Commands.MissionReset);
            builder.RegisterRequestHandler<MissionListAgents>(Commands.MissionListAgents);

            _ = builder.RegisterType<DeliveryHelper>();
            _ = builder.RegisterType<MissionTargetInProgress>();
        }
    }
}
