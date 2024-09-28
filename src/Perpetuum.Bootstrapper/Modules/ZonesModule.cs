using Autofac;
using Perpetuum.Common;
using Perpetuum.Common.Loggers;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.RequestHandlers.Zone.StatsMapDrawing;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers;
using Perpetuum.Services.HighScores;
using Perpetuum.Services.Relics;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.Strongholds;
using Perpetuum.Services.Weather;
using Perpetuum.Threading.Process;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Decors;
using Perpetuum.Zones.Effects.ZoneEffects;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.Training.Reward;
using Perpetuum.Zones.ZoneEntityRepositories;
using System;
using System.Net;
using System.Net.Sockets;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class ZonesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<ZoneSession>().AsSelf().As<IZoneSession>();

            _ = builder.RegisterType<SaveBitmapHelper>();
            _ = builder.RegisterType<ZoneDrawStatMap>();

            _ = builder.RegisterType<ZoneConfigurationReader>().As<IZoneConfigurationReader>();

            _ = builder.Register(c =>
            {
                return new WeatherService(new TimeRange(TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)));
            }).OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(5)));
            }).As<IWeatherService>();

            _ = builder.RegisterType<WeatherMonitor>();
            _ = builder.RegisterType<WeatherEventListener>();
            _ = builder.Register<Func<IZone, WeatherEventListener>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    return new WeatherEventListener(ctx.Resolve<EventListenerService>(), zone);
                };
            });

            _ = builder.Register<Func<IZone, EnvironmentalEffectHandler>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    EnvironmentalEffectHandler listener = new EnvironmentalEffectHandler(zone);
                    ctx.Resolve<EventListenerService>().AttachListener(listener);

                    return listener;
                };
            });

            _ = builder.RegisterType<DefaultZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();
            _ = builder.RegisterType<UserZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();

            _ = builder.Register<ZoneUnitServiceFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return zone =>
                {
                    return new ZoneUnitService
                    {
                        DefaultRepository = ctx.Resolve<DefaultZoneUnitRepository>(new TypedParameter(typeof(IZone), zone)),
                        UserRepository = ctx.Resolve<UserZoneUnitRepository>(new TypedParameter(typeof(IZone), zone))
                    };
                };
            });

            _ = builder.RegisterType<BeamService>().As<IBeamService>();
            _ = builder.RegisterType<MiningLogHandler>();
            _ = builder.RegisterType<HarvestLogHandler>();
            _ = builder.RegisterType<MineralConfigurationReader>().As<IMineralConfigurationReader>().SingleInstance();

            void RegisterZone<T>(ZoneType type) where T : Zone
            {
                _ = builder.RegisterType<T>().Keyed<Zone>(type).OnActivated(e =>
                {
                    e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync());
                });
            }

            RegisterZone<PveZone>(ZoneType.Pve);
            RegisterZone<PvpZone>(ZoneType.Pvp);
            RegisterZone<TrainingZone>(ZoneType.Training);
            RegisterZone<StrongHoldZone>(ZoneType.Stronghold);

            _ = builder.RegisterType<SettingsLoader>();
            _ = builder.RegisterType<PlantRuleLoader>();

            _ = builder.RegisterType<StrongholdPlayerStateManager>().As<IStrongholdPlayerStateManager>();


            _ = builder.Register<Func<IZone, IStrongholdPlayerStateManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return zone =>
                {
                    return new StrongholdPlayerStateManager(zone, ctx.Resolve<EventListenerService>());
                };
            });

            _ = builder.Register<Func<ZoneConfiguration, IZone>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return configuration =>
                {
                    Zone zone = ctx.ResolveKeyed<Zone>(configuration.Type);
                    zone.Configuration = configuration;
                    zone.Listener = new TcpListener(new IPEndPoint(IPAddress.Any, configuration.ListenerPort));
                    zone.ZoneEffectHandler = ctx.Resolve<Func<IZone, IZoneEffectHandler>>().Invoke(zone);
                    zone.UnitService = ctx.Resolve<ZoneUnitServiceFactory>().Invoke(zone);
                    zone.Weather = ctx.Resolve<IWeatherService>();
                    zone.Beams = ctx.Resolve<IBeamService>();
                    zone.HighScores = ctx.Resolve<IHighScoreService>();
                    zone.PlantHandler = ctx.Resolve<PlantHandler.Factory>().Invoke(zone);
                    zone.CorporationHandler = ctx.Resolve<CorporationHandler.Factory>().Invoke(zone);
                    zone.MiningLogHandler = ctx.Resolve<MiningLogHandler.Factory>().Invoke(zone);
                    zone.HarvestLogHandler = ctx.Resolve<HarvestLogHandler.Factory>().Invoke(zone);
                    zone.RiftManager = ctx.Resolve<Func<IZone, IRiftManager>>().Invoke(zone);
                    zone.ChatLogger = ctx.Resolve<ChatLoggerFactory>().Invoke("zone", zone.Configuration.Name);
                    zone.EnterQueueService = ctx.Resolve<ZoneEnterQueueService.Factory>().Invoke(zone);
                    zone.Terrain = ctx.Resolve<TerrainFactory>().Invoke(zone);
                    zone.PresenceManager = ctx.Resolve<Func<IZone, IPresenceManager>>().Invoke(zone);
                    zone.DecorHandler = ctx.Resolve<DecorHandler>(new TypedParameter(typeof(IZone), zone));
                    zone.Environment = ctx.Resolve<ZoneEnvironmentHandler>(new TypedParameter(typeof(IZone), zone));
                    zone.SafeSpawnPoints = ctx.Resolve<ISafeSpawnPointsRepository>(new TypedParameter(typeof(IZone), zone));
                    zone.ZoneSessionFactory = ctx.Resolve<ZoneSession.Factory>();
                    zone.RelicManager = ctx.Resolve<Func<IZone, IRelicManager>>().Invoke(zone);

                    if (configuration.Terraformable)
                    {
                        zone.HighwayHandler = ctx.Resolve<PBSHighwayHandler.Factory>().Invoke(zone);
                        zone.TerraformHandler = ctx.Resolve<TerraformHandler.Factory>().Invoke(zone);
                    }

                    if (configuration.Type == ZoneType.Stronghold)
                    {
                        zone.PlayerStateManager = ctx.Resolve<Func<IZone, IStrongholdPlayerStateManager>>().Invoke(zone);
                    }

                    ctx.Resolve<EventListenerService>().AttachListener(new NpcReinforcementSpawner(zone, ctx.Resolve<INpcReinforcementsRepository>()));
                    WeatherEventListener listener = ctx.Resolve<Func<IZone, WeatherEventListener>>().Invoke(zone);
                    listener.Subscribe(zone.Weather);

                    zone.LoadUnits();

                    return zone;
                };
            });

            _ = builder.Register(c => c.Resolve<ZoneManager>()).As<IZoneManager>();
            _ = builder.RegisterType<ZoneManager>().OnActivated(e =>
            {
                foreach (ZoneConfiguration c in e.Context.Resolve<IZoneConfigurationReader>().GetAll())
                {
                    Func<ZoneConfiguration, IZone> zoneFactory = e.Context.Resolve<Func<ZoneConfiguration, IZone>>();
                    IZone zone = zoneFactory(c);

                    _ = e.Context.Resolve<Func<IZone, EnvironmentalEffectHandler>>().Invoke(zone);

                    Logger.Info("------------------");
                    Logger.Info("--");
                    Logger.Info("--  zone " + zone.Configuration.Id + " loaded.");
                    Logger.Info("--");
                    Logger.Info("------------------");

                    e.Instance.Zones.Add(zone);
                }
            }).SingleInstance();

            _ = builder.RegisterType<TagHelper>();

            _ = builder.RegisterType<ZoneEnterQueueService>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).As<IZoneEnterQueueService>().InstancePerDependency();

            _ = builder.RegisterType<DecorHandler>().OnActivated(e => e.Instance.Initialize()).InstancePerDependency();
            _ = builder.RegisterType<ZoneEnvironmentHandler>();
            _ = builder.RegisterType<PlantHandler>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(5)));
            }).As<IPlantHandler>().InstancePerDependency();

            _ = builder.RegisterType<TeleportDescriptionBuilder>();
            _ = builder.RegisterType<TeleportWorldTargetHelper>();
            _ = builder.RegisterType<MobileTeleportZoneMapCache>().As<IMobileTeleportToZoneMap>().SingleInstance();
            _ = builder.RegisterType<StrongholdTeleportTargetHelper>();
            _ = builder.RegisterType<TeleportToAnotherZone>();
            _ = builder.RegisterType<TeleportWithinZone>();
            _ = builder.RegisterType<TrainingExitStrategy>();

            _ = builder.RegisterType<PBSHighwayHandler>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromMilliseconds(PBSHighwayHandler.DRAW_INTERVAL)).ToAsync());
            });

            _ = builder.RegisterType<MineralScanResultRepository>();
            _ = builder.RegisterType<RareMaterialHandler>().SingleInstance();
            _ = builder.RegisterType<PlantHarvester>().As<IPlantHarvester>();

            _ = builder.RegisterType<TeleportStrategyFactories>()
                .As<ITeleportStrategyFactories>()
                .PropertiesAutowired()
                .SingleInstance();

            _ = builder.RegisterType<TrainingRewardRepository>().SingleInstance().As<ITrainingRewardRepository>();
        }
    }
}
