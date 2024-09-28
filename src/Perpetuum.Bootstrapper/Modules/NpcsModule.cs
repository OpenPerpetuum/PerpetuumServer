using Autofac;
using Perpetuum.IDGenerators;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Threading.Process;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;
using Perpetuum.Zones.NpcSystem.Presences.GrowingPresences;
using Perpetuum.Zones.NpcSystem.Presences.InterzonePresences;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;
using System;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class NpcsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<CustomRiftConfigReader>().As<ICustomRiftConfigReader>();
            _ = builder.RegisterType<NpcBossInfoBuilder>().SingleInstance();
            _ = builder.RegisterType<NpcReinforcementsRepository>().SingleInstance().As<INpcReinforcementsRepository>();

            _ = builder.RegisterType<FlockConfiguration>().As<IFlockConfiguration>();
            _ = builder.RegisterType<FlockConfigurationBuilder>();
            _ = builder.RegisterType<IntIDGenerator>().Named<IIDGenerator<int>>("directFlockIDGenerator").SingleInstance().WithParameter("startID", 25000);


            _ = builder.RegisterType<FlockConfigurationRepository>().OnActivated(e =>
            {
                e.Instance.LoadAllConfig();
            }).As<IFlockConfigurationRepository>().SingleInstance();

            _ = builder.RegisterType<RandomFlockSelector>().As<IRandomFlockSelector>();

            _ = builder.RegisterType<RandomFlockReader>()
                .As<IRandomFlockReader>()
                .SingleInstance()
                .OnActivated(e => e.Instance.Init());

            _ = builder.RegisterType<EscalatingPresenceFlockSelector>().As<IEscalatingPresenceFlockSelector>().SingleInstance();

            _ = builder.RegisterType<EscalatingFlocksReader>()
                .As<IEscalatingFlocksReader>()
                .SingleInstance()
                .OnActivated(e => e.Instance.Init());

            _ = builder.RegisterType<NpcSafeSpawnPointsRepository>().As<ISafeSpawnPointsRepository>();
            _ = builder.RegisterType<PresenceConfigurationReader>().As<IPresenceConfigurationReader>();
            _ = builder.RegisterType<InterzonePresenceConfigReader>().As<IInterzonePresenceConfigurationReader>();
            _ = builder.RegisterType<InterzoneGroup>().As<IInterzoneGroup>();
            _ = builder.RegisterType<PresenceManager>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromSeconds(2)).ToAsync());

                e.Instance.LoadAll();

            }).As<IPresenceManager>();

            _ = builder.Register<Func<IZone, IPresenceManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    PresenceFactory presenceFactory = ctx.Resolve<PresenceFactory>();
                    IPresenceManager presenceService = ctx.Resolve<PresenceManager.Factory>().Invoke(zone, presenceFactory);

                    return presenceService;
                };
            });

            _ = builder.Register<FlockFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return (configuration, presence) =>
                {
                    return ctx.ResolveKeyed<Flock>(presence.Configuration.PresenceType, TypedParameter.From(configuration), TypedParameter.From(presence));
                };
            });

            builder.RegisterFlock<NormalFlock>(PresenceType.Normal);
            builder.RegisterFlock<Flock>(PresenceType.Direct);
            builder.RegisterFlock<NormalFlock>(PresenceType.DynamicPool);
            builder.RegisterFlock<NormalFlock>(PresenceType.Dynamic);
            builder.RegisterFlock<RemoteSpawningFlock>(PresenceType.DynamicExtended);
            builder.RegisterFlock<StaticExpiringFlock>(PresenceType.ExpiringRandom);
            builder.RegisterFlock<Flock>(PresenceType.Random);
            builder.RegisterFlock<RoamingFlock>(PresenceType.Roaming);
            builder.RegisterFlock<RoamingFlock>(PresenceType.FreeRoaming);
            builder.RegisterFlock<NormalFlock>(PresenceType.Interzone);
            builder.RegisterFlock<RoamingFlock>(PresenceType.InterzoneRoaming);
            builder.RegisterFlock<StaticExpiringFlock>(PresenceType.EscalatingRandomPresence);
            builder.RegisterFlock<StaticExpiringFlock>(PresenceType.GrowingNPCBasePresence);

            builder.RegisterPresence<Presence>(PresenceType.Normal);
            builder.RegisterPresence<DirectPresence>(PresenceType.Direct).OnActivated(e =>
            {
                e.Instance.FlockIDGenerator = e.Context.ResolveNamed<IIDGenerator<int>>("directFlockIDGenerator");
            });
            builder.RegisterPresence<DynamicPoolPresence>(PresenceType.DynamicPool);
            builder.RegisterPresence<DynamicPresence>(PresenceType.Dynamic);
            builder.RegisterPresence<DynamicPresenceExtended>(PresenceType.DynamicExtended);
            builder.RegisterPresence<RandomSpawningExpiringPresence>(PresenceType.ExpiringRandom);
            builder.RegisterPresence<RandomPresence>(PresenceType.Random);
            builder.RegisterPresence<RoamingPresence>(PresenceType.Roaming);
            builder.RegisterPresence<RoamingPresence>(PresenceType.FreeRoaming);
            builder.RegisterPresence<InterzonePresence>(PresenceType.Interzone);
            builder.RegisterPresence<InterzoneRoamingPresence>(PresenceType.InterzoneRoaming);
            builder.RegisterPresence<GrowingPresence>(PresenceType.EscalatingRandomPresence);
            builder.RegisterPresence<GrowingNPCBasePresence>(PresenceType.GrowingNPCBasePresence);

            _ = builder.Register<PresenceFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (zone, configuration) =>
                {
                    if (!ctx.IsRegisteredWithKey<Presence>(configuration.PresenceType))
                    {
                        return null;
                    }

                    Presence p = ctx.ResolveKeyed<Presence>(configuration.PresenceType, TypedParameter.From(zone), TypedParameter.From(configuration));

                    if (p is IRoamingPresence roamingPresence)
                    {
                        switch (p.Configuration.PresenceType)
                        {
                            case PresenceType.Roaming:
                                roamingPresence.PathFinder = new NormalRoamingPathFinder(zone);

                                break;
                            default:
                                roamingPresence.PathFinder = new FreeRoamingPathFinder(zone);

                                break;
                        }
                    }

                    return p;
                };
            });
        }
    }
}
