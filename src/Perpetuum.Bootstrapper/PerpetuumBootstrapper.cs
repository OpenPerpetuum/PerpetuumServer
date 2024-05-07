using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Newtonsoft.Json;
using Open.Nat;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Loggers;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host;
using Perpetuum.Host.Requests;
using Perpetuum.IDGenerators;
using Perpetuum.IO;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Items.Helpers;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Log.Formatters;
using Perpetuum.Log.Loggers;
using Perpetuum.Modules;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Terraforming;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.RequestHandlers;
using Perpetuum.RequestHandlers.AdminTools;
using Perpetuum.RequestHandlers.Channels;
using Perpetuum.RequestHandlers.Characters;
using Perpetuum.RequestHandlers.Corporations;
using Perpetuum.RequestHandlers.Corporations.YellowPages;
using Perpetuum.RequestHandlers.Extensions;
using Perpetuum.RequestHandlers.FittingPreset;
using Perpetuum.RequestHandlers.Gangs;
using Perpetuum.RequestHandlers.Intrusion;
using Perpetuum.RequestHandlers.Mails;
using Perpetuum.RequestHandlers.Markets;
using Perpetuum.RequestHandlers.Missions;
using Perpetuum.RequestHandlers.Production;
using Perpetuum.RequestHandlers.RobotTemplates;
using Perpetuum.RequestHandlers.Socials;
using Perpetuum.RequestHandlers.Sparks;
using Perpetuum.RequestHandlers.Standings;
using Perpetuum.RequestHandlers.TechTree;
using Perpetuum.RequestHandlers.Trades;
using Perpetuum.RequestHandlers.TransportAssignments;
using Perpetuum.RequestHandlers.Zone;
using Perpetuum.RequestHandlers.Zone.Containers;
using Perpetuum.RequestHandlers.Zone.MissionRequests;
using Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints;
using Perpetuum.RequestHandlers.Zone.PBS;
using Perpetuum.RequestHandlers.Zone.StatsMapDrawing;
using Perpetuum.Robots;
using Perpetuum.Services;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Channels.ChatCommands;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.HighScores;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionBonusObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Services.ProductionEngine.ResearchKits;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Relics;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.RiftSystem.StrongholdRifts;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Social;
using Perpetuum.Services.Sparks;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Services.Standing;
using Perpetuum.Services.Steam;
using Perpetuum.Services.Strongholds;
using Perpetuum.Services.TechTree;
using Perpetuum.Services.Trading;
using Perpetuum.Services.Weather;
using Perpetuum.Threading.Process;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.CombatLogs;
using Perpetuum.Zones.Decors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Effects.ZoneEffects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.LandMines;
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
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.ArmorRepairers;
using Perpetuum.Zones.PBS.ControlTower;
using Perpetuum.Zones.PBS.CoreTransmitters;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.EffectNodes;
using Perpetuum.Zones.PBS.EnergyWell;
using Perpetuum.Zones.PBS.HighwayNode;
using Perpetuum.Zones.PBS.ProductionNodes;
using Perpetuum.Zones.PBS.Reactors;
using Perpetuum.Zones.PBS.Turrets;
using Perpetuum.Zones.PlantTools;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.PunchBags;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Scanning.Ammos;
using Perpetuum.Zones.Scanning.Modules;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Scanning.Scanners;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.Training;
using Perpetuum.Zones.Training.Reward;
using Perpetuum.Zones.ZoneEntityRepositories;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using ChangeAmmo = Perpetuum.RequestHandlers.ChangeAmmo;
using CorporationDocumentConfig = Perpetuum.RequestHandlers.Corporations.CorporationDocumentConfig;
using EquipAmmo = Perpetuum.RequestHandlers.EquipAmmo;
using EquipModule = Perpetuum.RequestHandlers.EquipModule;
using ListContainer = Perpetuum.RequestHandlers.ListContainer;
using LogEvent = Perpetuum.Log.LogEvent;
using Module = Perpetuum.Modules.Module;
using PackItems = Perpetuum.RequestHandlers.PackItems;
using RelocateItems = Perpetuum.RequestHandlers.RelocateItems;
using RemoveModule = Perpetuum.RequestHandlers.RemoveModule;
using SetItemName = Perpetuum.RequestHandlers.SetItemName;
using TrashItems = Perpetuum.RequestHandlers.TrashItems;
using UnpackItems = Perpetuum.RequestHandlers.UnpackItems;
using UnstackAmount = Perpetuum.RequestHandlers.UnstackAmount;

namespace Perpetuum.Bootstrapper
{
    internal class EntityAggregateServices : IEntityServices
    {
        public IEntityFactory Factory { get; set; }
        public IEntityDefaultReader Defaults { get; set; }
        public IEntityRepository Repository { get; set; }
    }

    internal class RobotTemplateServicesImpl : IRobotTemplateServices
    {
        public IRobotTemplateReader Reader { get; set; }
        public IRobotTemplateRelations Relations { get; set; }
    }

    internal class TeleportStrategyFactoriesImpl : ITeleportStrategyFactories
    {
        public TeleportWithinZone.Factory TeleportWithinZoneFactory { get; set; }
        public TeleportToAnotherZone.Factory TeleportToAnotherZoneFactory { get; set; }
        public TrainingExitStrategy.Factory TrainingExitStrategyFactory { get; set; }
    }

    public delegate ITerrain TerrainFactory(IZone zone);

    public class PerpetuumBootstrapper
    {
        private ContainerBuilder _builder;
        private IContainer _container;

        public void Start()
        {
            IHostStateService s = _container.Resolve<IHostStateService>();
            s.State = HostState.Starting;
        }

        public void Stop()
        {
            IHostStateService s = _container.Resolve<IHostStateService>();
            s.State = HostState.Stopping;
        }

        public void Stop(TimeSpan delay)
        {
            HostShutDownManager m = _container.Resolve<HostShutDownManager>();
            m.Shutdown(delay);
        }

        public IContainer GetContainer()
        {
            return _container;
        }

        public void WaitForStop()
        {
            AutoResetEvent are = new AutoResetEvent(false);

            IHostStateService s = _container.Resolve<IHostStateService>();
            s.StateChanged += (sender, state) =>
            {
                if (state == HostState.Off)
                {
                    _ = are.Set();
                }
            };

            _ = are.WaitOne();
        }

        public void WriteCommandsToFile(string path)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Command command in GetCommands().OrderBy(c => c.Text))
            {
                _ = sb.AppendLine($"{command.Text},{command.AccessLevel}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        public IEnumerable<Command> GetCommands()
        {
            return typeof(Commands).GetFields(BindingFlags.Static | BindingFlags.Public).Select(info => (Command)info.GetValue(null));
        }

        public void Init(string gameRoot)
        {
            _builder = new ContainerBuilder();
            InitContainer(gameRoot);
            _container = _builder.Build();
            Logger.Current = _container.Resolve<ILogger<LogEvent>>();

            GlobalConfiguration config = _container.Resolve<GlobalConfiguration>();
            _container.Resolve<IHostStateService>().State = HostState.Init;


            Logger.Info($"Game root: {config.GameRoot}");
            Logger.Info($"GC isServerGC: {GCSettings.IsServerGC}");
            Logger.Info($"GC Latency mode: {GCSettings.LatencyMode}");
            Logger.Info($"Vector is hardware accelerated: {Vector.IsHardwareAccelerated}");

            Db.DbQueryFactory = _container.Resolve<Func<DbQuery>>();

            using (System.Data.IDbConnection connection = _container.Resolve<DbConnectionFactory>()()) { Logger.Info($"Database: {connection.Database}"); }

            InitGame(_container);

            EntityDefault.Reader = _container.Resolve<IEntityDefaultReader>();
            Entity.Services = _container.Resolve<IEntityServices>();

            GenxyConverter.RegisterConverter<Character>((writer, character) =>
            {
                GenxyConverter.ConvertInt(writer, character.Id);
            });

            CorporationData.CorporationManager = _container.Resolve<ICorporationManager>();

            Character.CharacterFactory = _container.Resolve<CharacterFactory>();
            Character.CharacterCache = _container.Resolve<Func<string, ObjectCache>>().Invoke("CharacterCache");

            MissionHelper.Init(_container.Resolve<MissionDataCache>(), _container.Resolve<IStandingHandler>());
            MissionHelper.MissionProcessor = _container.Resolve<MissionProcessor>();
            MissionHelper.EntityServices = _container.Resolve<IEntityServices>();

            Mission.Init(_container.Resolve<MissionDataCache>());
            MissionInProgress.Init(_container.Resolve<MissionDataCache>());
            MissionAgent.Init(_container.Resolve<MissionDataCache>());
            MissionStandingChangeCalculator.Init(_container.Resolve<MissionDataCache>());
            ZoneMissionInProgress.Init(_container.Resolve<MissionDataCache>());
            MissionSpot.Init(_container.Resolve<MissionDataCache>());
            MissionSpot.ZoneManager = _container.Resolve<IZoneManager>();
            MissionLocation.Init(_container.Resolve<MissionDataCache>());
            MissionLocation.ZoneManager = _container.Resolve<IZoneManager>();
            MissionTarget.missionDataCache = _container.Resolve<MissionDataCache>();
            MissionTarget.ProductionDataAccess = _container.Resolve<IProductionDataAccess>();
            MissionTarget.RobotTemplateRelations = _container.Resolve<IRobotTemplateRelations>();
            MissionTarget.MissionTargetInProgressFactory = _container.Resolve<MissionTargetInProgress.Factory>();

            MissionTargetRewardCalculator.Init(_container.Resolve<MissionDataCache>());
            MissionTargetSuccessInfoGenerator.Init(_container.Resolve<MissionDataCache>());
            MissionBonus.Init(_container.Resolve<MissionDataCache>());
            ZoneMissionTarget.MissionProcessor = _container.Resolve<MissionProcessor>();
            ZoneMissionTarget.PresenceFactory = _container.Resolve<PresenceFactory>();

            MissionResolveTester.Init(_container.Resolve<MissionDataCache>());
            TransportAssignment.EntityServices = _container.Resolve<IEntityServices>();
            ProductionLine.ProductionLineFactory = _container.Resolve<ProductionLine.Factory>();
            MissionInProgress.MissionInProgressFactory = _container.Resolve<MissionInProgress.Factory>();
            MissionInProgress.MissionProcessor = _container.Resolve<MissionProcessor>();
            PriceCalculator.PriceCalculatorFactory = _container.Resolve<PriceCalculator.Factory>();

            Message.MessageBuilderFactory = _container.Resolve<MessageBuilder.Factory>();

            PBSHelper.ProductionDataAccess = _container.Resolve<IProductionDataAccess>();
            PBSHelper.ProductionManager = _container.Resolve<ProductionManager>();
            PBSHelper.ItemDeployerHelper = _container.Resolve<ItemDeployerHelper>();

            ProductionComponentCollector.ProductionComponentCollectorFactory = _container.Resolve<ProductionComponentCollector.Factory>();

            CorporationData.InfoCache = _container.Resolve<Func<string, ObjectCache>>().Invoke("CorporationInfoCache");

            _container.Resolve<IHostStateService>().StateChanged += (sender, state) =>
            {
                switch (state)
                {
                    case HostState.Stopping:
                        {
                            _container.Resolve<IProcessManager>().Stop();
                            NatDiscoverer.ReleaseAll();
                            sender.State = HostState.Off;
                            break;
                        }
                    case HostState.Starting:
                        {
                            _container.Resolve<IProcessManager>().Start();
                            sender.State = HostState.Online;
                            break;
                        }
                }
            };

            DefaultCorporationDataCache.LoadAll();
            _container.Resolve<MissionDataCache>().CacheMissionData();
            // initialize our markets.
            // this is dependent on all zones being loaded.
            _container.Resolve<MarketHelper>().Init();
            _container.Resolve<MarketHandler>().Init();


        }

        public bool TryInitUpnp(out bool success)
        {
            success = false;
            GlobalConfiguration config = _container.Resolve<GlobalConfiguration>();
            if (!config.EnableUpnp)
            {
                return false;
            }

            try
            {
                NatDiscoverer discoverer = new NatDiscoverer();
                NatDiscoverer.ReleaseAll();

                NatDevice natDevice = discoverer.DiscoverDeviceAsync().Result;
                if (natDevice == null)
                {
                    Logger.Error("[UPNP] NAT device not found!");
                    return false;
                }

                void Map(int port)
                {
                    System.Threading.Tasks.Task task = natDevice.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port)).ContinueWith(t =>
                    {
                        Logger.Info($"[UPNP] Port mapped: {port}");
                    });
                    task.Wait();
                }

                Map(config.ListenerPort);

                foreach (IZone zone in _container.Resolve<IZoneManager>().Zones)
                {
                    Map(zone.Configuration.ListenerPort);
                }

                success = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }

            return true;
        }

        /// <summary>
        /// this method cleans up every runtime table
        /// </summary>
        private static void InitGame(IComponentContext container)
        {
            //the current host has to clean up things in the onlinehost table, and other runtime tables
            _ = Db.Query().CommandText("initServer").ExecuteNonQuery();

            GlobalConfiguration globalConfiguration = container.Resolve<GlobalConfiguration>();
            if (!string.IsNullOrEmpty(globalConfiguration.PersonalConfig))
            {
                _ = Db.Query().CommandText(globalConfiguration.PersonalConfig).ExecuteNonQuery();
                Logger.Info("Personal sp executed:" + globalConfiguration.PersonalConfig);
            }

            Logger.Info("DB init done.");
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterAutoActivate<T>(TimeSpan interval) where T : IProcess
        {
            return _builder.RegisterType<T>().SingleInstance().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(interval));
            });
        }

        private void RegisterAutoActivatedTypes()
        {
            _ = RegisterAutoActivate<HostOnlineStateWriter>(TimeSpan.FromSeconds(7));
            _ = RegisterAutoActivate<ServerInfoService>(TimeSpan.FromMinutes(5));
            _ = RegisterAutoActivate<MarketCleanUpService>(TimeSpan.FromHours(1));
            _ = RegisterAutoActivate<SessionCountWriter>(TimeSpan.FromMinutes(5));
            _ = RegisterAutoActivate<VolunteerCEOProcessor>(TimeSpan.FromMinutes(10));
            _ = RegisterAutoActivate<GiveExtensionPointsService>(TimeSpan.FromMinutes(10));
            _ = RegisterAutoActivate<ArtifactRefresher>(TimeSpan.FromHours(7));
        }

        private void RegisterCommands()
        {
            foreach (Command command in GetCommands())
            {
                _ = _builder.RegisterInstance(command).As<Command>().Keyed<Command>(command.Text.ToUpper());
            }

            _ = _builder.Register<Func<string, Command>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return commandText =>
                {
                    commandText = commandText.ToUpper();
                    return ctx.IsRegisteredWithKey<Command>(commandText) ? ctx.ResolveKeyed<Command>(commandText) : null;
                };
            });
        }

        private void InitContainer(string gameRoot)
        {
            RegisterCommands();
            RegisterRequestHandlers();
            RegisterAutoActivatedTypes();
            RegisterLoggers();
            RegisterEntities();
            RegisterRobotTemplates();
            RegisterMissions();
            RegisterTerrains();
            RegisterNpcs();
            RegisterChannelTypes();
            RegisterMtProducts();
            RegisterRifts();
            RegisterRelics();
            RegisterEffects();
            RegisterIntrusions();
            RegisterZones();
            RegisterPBS();

            _ = _builder.Register<Func<string, ObjectCache>>(x =>
            {
                return name => new MemoryCache(name);
            });

            _ = _builder.RegisterType<CharacterProfileRepository>().AsSelf().As<ICharacterProfileRepository>();
            _ = _builder.Register(c =>
            {
                MemoryCache cache = new MemoryCache("CharacterProfiles");
                return new CachedReadOnlyRepository<int, CharacterProfile>(cache, c.Resolve<CharacterProfileRepository>());
            }).AsSelf().As<IReadOnlyRepository<int, CharacterProfile>>().SingleInstance();

            _ = _builder.RegisterType<CachedCharacterProfileRepository>().As<ICharacterProfileRepository>();

            _ = _builder.RegisterType<StandingRepository>().As<IStandingRepository>();
            _ = _builder.RegisterType<StandingHandler>().OnActivated(e =>
            {
                e.Instance.Init();
            }).As<IStandingHandler>().SingleInstance();

            _ = _builder.RegisterType<CentralBank>().As<ICentralBank>().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromHours(1)));
            }).SingleInstance();

            _ = _builder.RegisterType<TechTreeInfoService>().As<ITechTreeInfoService>();
            _ = _builder.RegisterType<TechTreeService>().As<ITechTreeService>();
            _ = _builder.RegisterType<TeleportDescriptionRepository>().As<ITeleportDescriptionRepository>();
            _ = _builder.RegisterType<CustomDictionary>().As<ICustomDictionary>().SingleInstance().AutoActivate();

            _ = _builder.RegisterType<Session>().AsSelf().As<ISession>();

            _ = _builder.RegisterType<SessionManager>().As<ISessionManager>().SingleInstance();

            InitRelayManager();

            _ = _builder.Register(c => new FileSystem(gameRoot)).As<IFileSystem>();
            _ = _builder.Register(c =>
            {
                IFileSystem fileManager = c.Resolve<IFileSystem>();
                string settingsFile = fileManager.ReadAllText("perpetuum.ini");
                GlobalConfiguration configuration = JsonConvert.DeserializeObject<GlobalConfiguration>(settingsFile);
                configuration.GameRoot = gameRoot;
                return configuration;
            }).SingleInstance();

            _ = _builder.RegisterType<AdminCommandRouter>().SingleInstance();

            _ = _builder.RegisterType<Gang>();
            _ = _builder.RegisterType<GangRepository>().As<IGangRepository>();
            _ = _builder.RegisterType<GangManager>().As<IGangManager>().SingleInstance();

            _ = _builder.Register(c =>
            {
                GlobalConfiguration config = c.Resolve<GlobalConfiguration>();
                return config.Corporation;
            }).As<CorporationConfiguration>();

            _ = _builder.RegisterType<HostStateService>().As<IHostStateService>().SingleInstance();
            _ = _builder.Register(c => new ProcessManager(TimeSpan.FromMilliseconds(50))).As<IProcessManager>().SingleInstance();

            _ = _builder.Register<DbConnectionFactory>(x =>
            {
                string connectionString = x.Resolve<GlobalConfiguration>().ConnectionStringNetCore;
                return () => new SqlConnection(connectionString);
            });

            _ = _builder.RegisterType<DbQuery>();


            _ = _builder.RegisterType<SparkTeleport>();

            _ = _builder.RegisterType<ExtensionReader>().As<IExtensionReader>().SingleInstance();
            _ = _builder.RegisterType<ExtensionPoints>();


            _ = _builder.RegisterType<LootService>().As<ILootService>().SingleInstance().OnActivated(e => e.Instance.Init());
            _ = _builder.RegisterType<ItemPriceHelper>().SingleInstance();
            _ = _builder.RegisterType<PriceCalculator>(); // this doesn't appear to be something that should be a singleton.


            _ = _builder.RegisterType<CharacterExtensions>().As<ICharacterExtensions>().SingleInstance();
            _ = _builder.RegisterType<AccountRepository>().As<IAccountRepository>();

            _ = _builder.RegisterType<SocialService>().As<ISocialService>().SingleInstance();

            _ = _builder.RegisterType<CharacterTransactionLogger>().As<ICharacterTransactionLogger>();

            _ = _builder.RegisterType<CharacterCreditService>().As<ICharacterCreditService>();
            _ = _builder.RegisterType<CharacterWallet>().AsSelf().As<ICharacterWallet>();
            _ = _builder.RegisterType<CharacterWalletHelper>();
            _ = _builder.Register<CharacterWalletFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (character, type) =>
                {
                    return ctx.Resolve<CharacterWallet>(new TypedParameter(typeof(Character), character),
                                                        new TypedParameter(typeof(TransactionType), type));
                };
            });

            _ = _builder.RegisterType<Character>().AsSelf();
            _ = _builder.Register(x => x.Resolve<Character>(TypedParameter.From(0))).Named<Character>("nullcharacter").SingleInstance();

            _ = _builder.Register<CharacterFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return id =>
                {
                    return id == 0 ? ctx.ResolveNamed<Character>("nullcharacter") : ctx.Resolve<Character>(TypedParameter.From(id));
                };
            });

            _ = _builder.RegisterType<MessageBuilder>();
            _ = _builder.RegisterType<MessageSender>().As<IMessageSender>();
            _ = _builder.RegisterType<CorporationMessageSender>().As<ICorporationMessageSender>().SingleInstance();

            _ = _builder.RegisterType<ServerInfo>();
            _ = _builder.RegisterType<ServerInfoManager>().As<IServerInfoManager>();


            _ = _builder.Register(x =>
            {
                GlobalConfiguration cfg = x.Resolve<GlobalConfiguration>();
                return new SteamManager(cfg.SteamAppID, cfg.SteamKey);
            }).As<ISteamManager>();
        }

        private void RegisterChannelTypes()
        {
            _ = _builder.RegisterType<ChannelRepository>().As<IChannelRepository>();
            _ = _builder.RegisterType<ChannelMemberRepository>().As<IChannelMemberRepository>();
            _ = _builder.RegisterType<ChannelBanRepository>().As<IChannelBanRepository>();
            _ = _builder.RegisterType<ChannelManager>().As<IChannelManager>().SingleInstance();

            _ = RegisterRequestHandler<ChannelCreate>(Commands.ChannelCreate);
            _ = RegisterRequestHandler<ChannelList>(Commands.ChannelList);
            _ = RegisterRequestHandler<ChannelListAll>(Commands.ChannelListAll);
            _ = RegisterRequestHandler<ChannelMyList>(Commands.ChannelMyList);
            _ = RegisterRequestHandler<ChannelJoin>(Commands.ChannelJoin);
            _ = RegisterRequestHandler<ChannelLeave>(Commands.ChannelLeave);
            _ = RegisterRequestHandler<ChannelKick>(Commands.ChannelKick);
            _ = RegisterRequestHandler<ChannelTalk>(Commands.ChannelTalk);
            _ = RegisterRequestHandler<ChannelSetMemberRole>(Commands.ChannelSetMemberRole);
            _ = RegisterRequestHandler<ChannelSetPassword>(Commands.ChannelSetPassword);
            _ = RegisterRequestHandler<ChannelSetTopic>(Commands.ChannelSetTopic);
            _ = RegisterRequestHandler<ChannelBan>(Commands.ChannelBan);
            _ = RegisterRequestHandler<ChannelRemoveBan>(Commands.ChannelRemoveBan);
            _ = RegisterRequestHandler<ChannelGetBannedMembers>(Commands.ChannelGetBannedMembers);
            _ = RegisterRequestHandler<ChannelGlobalMute>(Commands.ChannelGlobalMute);
            _ = RegisterRequestHandler<ChannelGetMutedCharacters>(Commands.ChannelGetMutedCharacters);
            _ = RegisterRequestHandler<ChannelCreateForTerminals>(Commands.ChannelCreateForTerminals);
        }

        private void RegisterEffects()
        {
            _ = _builder.RegisterType<EffectBuilder>();

            _ = _builder.RegisterType<ZoneEffectHandler>().As<IZoneEffectHandler>();

            _ = _builder.Register<Func<IZone, IZoneEffectHandler>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone => new ZoneEffectHandler(zone);
            });

            _ = _builder.RegisterType<InvulnerableEffect>().Keyed<Effect>(EffectType.effect_invulnerable);
            _ = _builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_eccm);
            _ = _builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_stealth);

            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_recharge_time);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_critical_hit_chance);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_time);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_signature_radius);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_fast_extraction);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_usage_gathering);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_siege);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_speed);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_repaired_amount);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_range);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_ewar_optimal);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_armor_max);
            _ = _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_shield_absorbtion_ratio);

            // intrusion effects

            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl1);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl2);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl3);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_signals_lvl4_combined);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_industrial_lvl4_combined);
            _ = _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_engineering_lvl4_combined);

            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl3);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl1);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl2);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl3);

            // New Bonuses - OPP
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta_bonus);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta2_bonus);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha_bonus);
            _ = _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha2_bonus);

            _ = _builder.Register<EffectFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return effectType =>
                {
                    return !ctx.IsRegisteredWithKey<Effect>(effectType) ? new Effect() : ctx.ResolveKeyed<Effect>(effectType);
                };
            });
        }

        public void InitItems()
        {
            _ = _builder.RegisterType<ItemDeployerHelper>();
            _ = _builder.RegisterType<DefaultPropertyModifierReader>().AsSelf().OnActivated(e => e.Instance.Init()).SingleInstance();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterEntity<T>() where T : Entity
        {
            return _builder.RegisterType<T>().OnActivated(e =>
            {
                e.Instance.EntityServices = e.Context.Resolve<IEntityServices>();
            });
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterUnit<T>() where T : Unit
        {
            return RegisterEntity<T>().PropertiesAutowired();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterPBSObject<T>() where T : PBSObject
        {
            return RegisterUnit<T>().OnActivated(e =>
            {
                e.Instance.SetReinforceHandler(e.Context.Resolve<PBSReinforceHandler<PBSObject>>(new TypedParameter(typeof(PBSObject), e.Instance)));
                e.Instance.SetPBSObjectHelper(e.Context.Resolve<PBSObjectHelper<PBSObject>>(new TypedParameter(typeof(PBSObject), e.Instance)));
            });
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterPBSProductionFacilityNode<T>() where T : PBSProductionFacilityNode
        {
            return RegisterPBSObject<T>().OnActivated(e =>
            {
                e.Instance.ProductionManager = e.Context.Resolve<ProductionManager>();
                e.Instance.SetProductionFacilityNodeHelper(e.Context.Resolve<PBSProductionFacilityNodeHelper>(new TypedParameter(typeof(PBSProductionFacilityNode), e.Instance)));
            });
        }

        protected void RegisterCorporation<T>() where T : Corporation
        {
            _ = _builder.RegisterType<CorporationTransactionLogger>();
            _ = RegisterEntity<T>().PropertiesAutowired();
        }

        protected void RegisterProximityDevices<T>() where T : ProximityDeviceBase
        {
            _ = RegisterUnit<T>();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterModule<T>() where T : Module
        {
            return RegisterEntity<T>();
        }

        private void RegisterEffectModule<T>() where T : EffectModule
        {
            _ = RegisterModule<T>();
        }

        private void RegisterProductionFacility<T>() where T : ProductionFacility
        {
            _ = RegisterEntity<T>().PropertiesAutowired();
        }

        public IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRobot<T>() where T : Robot
        {
            return RegisterUnit<T>();
        }

        private void RegisterEntities()
        {
            _builder.RegisterType<ItemHelper>();
            _builder.RegisterType<ContainerHelper>();

            _builder.RegisterType<EntityDefaultReader>().As<IEntityDefaultReader>().SingleInstance().OnActivated(e => e.Instance.Init());
            _builder.RegisterType<EntityRepository>().As<IEntityRepository>();

            _builder.RegisterType<ModulePropertyModifiersReader>().OnActivated(e => e.Instance.Init()).SingleInstance();

            _builder.RegisterType<LootItemRepository>().As<ILootItemRepository>();
            _builder.RegisterType<CoreRecharger>().As<ICoreRecharger>();
            _builder.RegisterType<UnitHelper>();
            _builder.RegisterType<DockingBaseHelper>();

            _builder.RegisterType<EntityFactory>().AsSelf().As<IEntityFactory>();

            InitItems();

            RegisterRobot<Npc>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<Player>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<SentryTurret>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<IndustrialTurret>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<CombatDrone>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<PBSTurret>();
            RegisterRobot<PunchBag>();

            _builder.RegisterType<EntityAggregateServices>().As<IEntityServices>().PropertiesAutowired().SingleInstance();


            RegisterEntity<Entity>();
            RegisterCorporation<DefaultCorporation>();
            RegisterCorporation<PrivateCorporation>();
            RegisterEntity<PrivateAlliance>();
            RegisterEntity<DefaultAlliance>();

            RegisterEntity<RobotHead>();
            RegisterEntity<RobotChassis>();
            RegisterEntity<RobotLeg>();
            RegisterUnit<DockingBase>();
            RegisterUnit<PBSDockingBase>();
            RegisterUnit<ExpiringPBSDockingBase>();
            RegisterUnit<Outpost>().OnActivated(e =>
            {
#if DEBUG
                TimeRange intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
#else
                TimeRange intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromHours(8), TimeSpan.FromHours(8));
#endif
                e.Instance.IntrusionWaitTime = intrusionWaitTime;
            });
            RegisterUnit<TrainingDockingBase>();
            RegisterUnit<ItemShop>();

            RegisterEntity<PublicCorporationHangarStorage>();
            RegisterEntity<CalibrationProgram>();
            RegisterEntity<DynamicCalibrationProgram>();
            RegisterEntity<RandomCalibrationProgram>();
            RegisterEntity<CalibrationProgramCapsule>(); // OPP: new CT Capsule item

            RegisterProductionFacility<Mill>();
            RegisterProductionFacility<Prototyper>();
            RegisterProductionFacility<OutpostMill>();
            RegisterProductionFacility<OutpostPrototyper>();
            RegisterProductionFacility<OutpostRefinery>();
            RegisterProductionFacility<OutpostRepair>();
            RegisterProductionFacility<OutpostReprocessor>();
            RegisterProductionFacility<PBSMillFacility>();
            RegisterProductionFacility<PBSPrototyperFacility>();
            RegisterProductionFacility<ResearchLab>();
            RegisterProductionFacility<OutpostResearchLab>();
            RegisterProductionFacility<PBSResearchLabFacility>();
            RegisterProductionFacility<Refinery>();
            RegisterProductionFacility<Reprocessor>();
            RegisterProductionFacility<Repair>();
            RegisterProductionFacility<InsuraceFacility>();
            RegisterProductionFacility<PBSResearchKitForgeFacility>();
            RegisterProductionFacility<PBSCalibrationProgramForgeFacility>();
            RegisterProductionFacility<PBSRefineryFacility>();
            RegisterProductionFacility<PBSRepairFacility>();
            RegisterProductionFacility<PBSReprocessorFacility>();

            RegisterEntity<ResearchKit>();
            RegisterEntity<RandomResearchKit>();
            RegisterEntity<Market>();
            RegisterEntity<LotteryItem>();

            RegisterProximityDevices<ProximityProbe>();
            RegisterProximityDevices<LandMine>();

            RegisterUnit<TeleportColumn>();
            RegisterUnit<LootContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<FieldContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromHours(1)));
            RegisterUnit<MissionContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<ActiveHackingSAP>();
            RegisterUnit<PassiveHackingSAP>();
            RegisterUnit<DestructionSAP>();
            RegisterUnit<SpecimenProcessingSAP>();
            RegisterUnit<MobileTeleport>();
            RegisterUnit<NpcEgg>();

            RegisterEntity<FieldContainerCapsule>();
            RegisterEntity<Ice>();
            RegisterEntity<RespecToken>();
            RegisterEntity<SparkTeleportDevice>();
            RegisterEntity<Ammo>();
            RegisterEntity<WeaponAmmo>();
            RegisterEntity<RemoteControlledUnit>();
            RegisterEntity<MiningAmmo>();
            RegisterEntity<TileScannerAmmo>();
            RegisterEntity<OneTileScannerAmmo>();
            RegisterEntity<ArtifactScannerAmmo>();
            RegisterEntity<IntrusionScannerAmmo>();
            RegisterEntity<DirectionalScannerAmmo>();
            RegisterEntity<DefaultSystemContainer>();
            RegisterEntity<PublicContainer>();
            RegisterEntity<RobotInventory>();
            RegisterEntity<InfiniteBoxContainer>();
            RegisterEntity<LimitedBoxContainer>();
            RegisterEntity<CorporateHangar>();
            RegisterEntity<CorporateHangarFolder>();
            RegisterEntity<MobileTeleportDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<RiftActivator>();
            RegisterEntity<MineralScanResultItem>();

            RegisterModule<DrillerModule>();
            RegisterModule<RemoteControlledDrillerModule>();
            RegisterModule<RemoteControlledHarvesterModule>();
            RegisterModule<HarvesterModule>();
            RegisterModule<Module>();
            RegisterModule<WeaponModule>();
            RegisterModule<FirearmWeaponModule>(); // OPP: new subclass for firearms
            RegisterModule<MissileWeaponModule>();
            RegisterModule<ArmorRepairModule>();
            RegisterModule<RemoteArmorRepairModule>();
            RegisterModule<CoreBoosterModule>();
            RegisterModule<SensorJammerModule>();
            RegisterModule<EnergyNeutralizerModule>();
            RegisterModule<EnergyTransfererModule>();
            RegisterModule<EnergyVampireModule>();
            RegisterModule<GeoScannerModule>();
            RegisterModule<UnitScannerModule>();
            RegisterModule<ContainerScannerModule>();
            RegisterModule<SiegeHackModule>();
            RegisterModule<NeuralyzerModule>();
            RegisterModule<BlobEmissionModulatorModule>();
            RegisterModule<RemoteControllerModule>();
            RegisterModule<TerraformMultiModule>();
            RegisterModule<WallBuilderModule>();
            RegisterModule<ConstructionModule>();
            RegisterEffectModule<WebberModule>();
            RegisterEffectModule<SensorDampenerModule>();
            RegisterEffectModule<RemoteSensorBoosterModule>();
            RegisterEffectModule<TargetPainterModule>();
            RegisterEffectModule<TargetBlinderModule>(); //OPP: NPC-only module for detection debuff
            RegisterEffectModule<SensorBoosterModule>();
            RegisterEffectModule<ArmorHardenerModule>();
            RegisterEffectModule<StealthModule>();
            RegisterEffectModule<DetectionModule>();
            RegisterEffectModule<GangModule>();
            RegisterEffectModule<ShieldGeneratorModule>();
            RegisterEffectModule<MineDetectorModule>();

            RegisterEntity<SystemContainer>();
            RegisterEntity<PunchBagDeployer>();

            RegisterUnit<BlobEmitterUnit>();
            RegisterUnit<Kiosk>();
            RegisterUnit<AlarmSwitch>();
            RegisterUnit<SimpleSwitch>();
            RegisterUnit<ItemSupply>();
            RegisterUnit<MobileWorldTeleport>();
            RegisterUnit<MobileStrongholdTeleport>(); // OPP: New mobile tele for entry to Strongholds
            RegisterUnit<AreaBomb>();
            RegisterUnit<PBSEgg>();
            RegisterPBSObject<PBSReactor>();
            RegisterPBSObject<PBSCoreTransmitter>();
            RegisterUnit<WallHealer>();
            RegisterPBSProductionFacilityNode<PBSResearchLabEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSRepairEnablerNode>();
            RegisterPBSObject<PBSFacilityUpgradeNode>();
            RegisterPBSProductionFacilityNode<PBSReprocessEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSMillEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSRefineryEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSPrototyperEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSCalibrationProgramForgeEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSResearchKitForgeEnablerNode>();
            RegisterPBSObject<PBSEffectSupplier>();
            RegisterPBSObject<PBSEffectEmitter>();
            RegisterPBSObject<PBSMiningTower>();
            RegisterPBSObject<PBSArmorRepairerNode>();
            RegisterPBSObject<PBSControlTower>();
            RegisterPBSObject<PBSEnergyWell>();
            RegisterPBSObject<PBSHighwayNode>();
            RegisterUnit<FieldTerminal>();
            RegisterUnit<Rift>();
            RegisterUnit<TrainingKillSwitch>();
            RegisterUnit<Gate>();
            RegisterUnit<RandomRiftPortal>();
            RegisterUnit<StrongholdEntryRift>(); // OPP: Special rift spawned eventfully to transport player to location
            RegisterUnit<StrongholdExitRift>(); // OPP: Special rift for exiting strongholds

            RegisterEntity<Item>();
            RegisterEntity<AreaBombDeployer>();

            RegisterEntity<ProximityProbeDeployer>();
            RegisterEntity<LandMineDeployer>();

            RegisterEntity<PBSDeployer>();
            RegisterEntity<WallHealerDeployer>();
            RegisterEntity<VolumeWrapperContainer>();
            RegisterEntity<Kernel>();
            RegisterEntity<RandomMissionItem>();
            RegisterEntity<Trashcan>();
            RegisterEntity<ZoneStorage>();
            RegisterEntity<PunchBagDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<GateDeployer>();
            RegisterEntity<ExtensionPointActivator>();
            RegisterEntity<CreditActivator>();
            RegisterEntity<SparkActivator>();
            RegisterEntity<Gift>();
            RegisterEntity<Paint>(); // OPP: Robot paint item
            RegisterEntity<EPBoost>();
            RegisterEntity<Relic>();
            RegisterEntity<SAPRelic>();

            _builder.Register<Func<EntityDefault, Entity>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                ContainerBuilder b = new ContainerBuilder();

                void ByDefinition<T>(int definition, params Parameter[] parameters) where T : Entity
                {
                    b.Register(_ => ctx.Resolve<T>(parameters)).Keyed<Entity>(definition);
                }

                void ByCategoryFlags<T>(CategoryFlags cf, params Parameter[] parameters) where T : Entity
                {
                    foreach (EntityDefault entityDefault in ctx.Resolve<IEntityDefaultReader>().GetAll().GetByCategoryFlags(cf))
                    {
                        ByDefinition<T>(entityDefault.Definition, parameters);
                    }
                }

                void ByName<T>(string name, params Parameter[] parameters) where T : Entity
                {
                    EntityDefault ed = ctx.Resolve<IEntityDefaultReader>().GetByName(name);
                    ByDefinition<T>(ed.Definition, parameters);
                }

                //TODO: bit of a hack for using the same category for many items grouped by definitionname prefixes
                //TODO: make separate category for new item groups!
                void ByNamePatternAndFlag<T>(string substr, CategoryFlags cf, params Parameter[] parameters) where T : Entity
                {
                    //TODO: this might be expensive -- string matching all defaults
                    IEnumerable<EntityDefault> matches = ctx.Resolve<IEntityDefaultReader>().GetAll()
                    .Where(i => i.CategoryFlags == cf)
                    .Where(i => i.Name.Contains(substr));
                    foreach (EntityDefault ed in matches)
                    {
                        ByDefinition<T>(ed.Definition, parameters);
                    }
                }

                ByName<LootContainer>(DefinitionNames.LOOT_CONTAINER_OBJECT);
                ByName<FieldContainer>(DefinitionNames.FIELD_CONTAINER);
                ByName<MissionContainer>(DefinitionNames.MISSION_CONTAINER);
                ByName<Ice>(DefinitionNames.ICE);

                ByCategoryFlags<FieldContainerCapsule>(CategoryFlags.cf_container_capsule);
                ByCategoryFlags<Npc>(CategoryFlags.cf_npc);
                ByCategoryFlags<DefaultCorporation>(CategoryFlags.cf_default_corporation);
                ByCategoryFlags<PrivateCorporation>(CategoryFlags.cf_private_corporation);
                ByCategoryFlags<PrivateAlliance>(CategoryFlags.cf_private_alliance);
                ByCategoryFlags<DefaultAlliance>(CategoryFlags.cf_default_alliance);
                ByCategoryFlags<Player>(CategoryFlags.cf_robots);
                ByCategoryFlags<Npc>(CategoryFlags.cf_npc);
                ByCategoryFlags<PunchBag>(CategoryFlags.cf_test_robot_punchbags);
                ByCategoryFlags<PunchBag>(CategoryFlags.cf_tutorial_punchbag);

                ByCategoryFlags<RobotHead>(CategoryFlags.cf_robot_head);
                ByCategoryFlags<RobotChassis>(CategoryFlags.cf_robot_chassis);
                ByCategoryFlags<RobotLeg>(CategoryFlags.cf_robot_leg);
                ByCategoryFlags<Ammo>(CategoryFlags.cf_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_railgun_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_laser_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_projectile_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_missile_ammo);
                ByCategoryFlags<MiningAmmo>(CategoryFlags.cf_mining_ammo);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_sentry_turret_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_mining_turret_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_harvesting_turret_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_pelistal_combat_drones_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_nuimqol_combat_drones_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_thelodica_combat_drones_units);
                ByCategoryFlags<TileScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_tile);
                ByCategoryFlags<OneTileScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_one_tile);
                ByCategoryFlags<ArtifactScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_artifact);
                ByCategoryFlags<IntrusionScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_intrusion);
                ByCategoryFlags<DirectionalScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_direction);

                ByCategoryFlags<DefaultSystemContainer>(CategoryFlags.cf_system_container);
                ByCategoryFlags<PublicContainer>(CategoryFlags.cf_public_container);
                ByCategoryFlags<RobotInventory>(CategoryFlags.cf_robot_inventory);
                ByCategoryFlags<InfiniteBoxContainer>(CategoryFlags.cf_infinite_capacity_box);
                ByCategoryFlags<LimitedBoxContainer>(CategoryFlags.cf_limited_capacity_box);
                ByCategoryFlags<CorporateHangar>(CategoryFlags.cf_corporate_hangar);
                ByCategoryFlags<CorporateHangarFolder>(CategoryFlags.cf_corporate_hangar_folder);
                ByCategoryFlags<PublicCorporationHangarStorage>(CategoryFlags.cf_public_corporation_hangar_storage);
                ByCategoryFlags<DockingBase>(CategoryFlags.cf_public_docking_base);
                ByCategoryFlags<PBSDockingBase>(CategoryFlags.cf_pbs_docking_base);
                ByName<ExpiringPBSDockingBase>(DefinitionNames.PBS_EXPIRING_DOCKING_BASE); //OPP: new expiring base
                ByCategoryFlags<Outpost>(CategoryFlags.cf_outpost);
                ByCategoryFlags<OutpostMill>(CategoryFlags.cf_outpost_mill);
                ByCategoryFlags<OutpostPrototyper>(CategoryFlags.cf_outpost_prototyper);
                ByCategoryFlags<OutpostRefinery>(CategoryFlags.cf_outpost_refinery);
                ByCategoryFlags<OutpostRepair>(CategoryFlags.cf_outpost_repair);
                ByCategoryFlags<OutpostReprocessor>(CategoryFlags.cf_outpost_reprocessor);
                ByCategoryFlags<OutpostResearchLab>(CategoryFlags.cf_outpost_research_lab);


                ByCategoryFlags<TrainingDockingBase>(CategoryFlags.cf_training_docking_base);
                ByCategoryFlags<Item>(CategoryFlags.cf_material);
                ByCategoryFlags<Item>(CategoryFlags.cf_dogtags);
                ByCategoryFlags<Market>(CategoryFlags.cf_public_market);
                ByCategoryFlags<Refinery>(CategoryFlags.cf_refinery_facility);
                ByCategoryFlags<Reprocessor>(CategoryFlags.cf_reprocessor_facility);
                ByCategoryFlags<Repair>(CategoryFlags.cf_repair_facility);
                ByCategoryFlags<InsuraceFacility>(CategoryFlags.cf_insurance_facility);
                ByCategoryFlags<ResearchKit>(CategoryFlags.cf_research_kits);
                ByCategoryFlags<ResearchLab>(CategoryFlags.cf_research_lab);
                ByCategoryFlags<Mill>(CategoryFlags.cf_mill);
                ByCategoryFlags<Prototyper>(CategoryFlags.cf_prototyper);

                ByCategoryFlags<ActiveHackingSAP>(CategoryFlags.cf_active_hacking_sap);
                ByCategoryFlags<PassiveHackingSAP>(CategoryFlags.cf_passive_hacking_sap);
                ByCategoryFlags<DestructionSAP>(CategoryFlags.cf_destrucion_sap);
                ByCategoryFlags<SpecimenProcessingSAP>(CategoryFlags.cf_specimen_processing_sap);
                ByCategoryFlags<MobileTeleportDeployer>(CategoryFlags.cf_mobile_teleport_capsule);
                ByCategoryFlags<PlantSeedDeployer>(CategoryFlags.cf_plant_seed);
                ByCategoryFlags<PlantSeedDeployer>(CategoryFlags.cf_deployable_structure);
                ByCategoryFlags<RiftActivator>(CategoryFlags.cf_npc_egg_deployer);
                ByCategoryFlags<TeleportColumn>(CategoryFlags.cf_public_teleport_column);
                ByCategoryFlags<TeleportColumn>(CategoryFlags.cf_training_exit_teleport);
                ByCategoryFlags<MobileTeleport>(CategoryFlags.cf_mobile_teleport);
                ByCategoryFlags<MineralScanResultItem>(CategoryFlags.cf_material_scan_result);
                ByCategoryFlags<NpcEgg>(CategoryFlags.cf_npc_eggs);
                ByCategoryFlags<CalibrationProgram>(CategoryFlags.cf_calibration_programs);
                ByCategoryFlags<DynamicCalibrationProgram>(CategoryFlags.cf_dynamic_cprg);
                ByCategoryFlags<RandomCalibrationProgram>(CategoryFlags.cf_random_calibration_programs);


                ByCategoryFlags<Module>(CategoryFlags.cf_robot_equipment);
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_small_lasers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_small_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_medium_lasers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_medium_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_large_lasers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_large_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_small_railguns, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_small_railgun_ammo));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_medium_railguns, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_medium_railgun_ammo));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_large_railguns, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_large_railgun_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_small_single_projectile, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_small_projectile_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_medium_single_projectile, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_medium_projectile_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_large_single_projectile, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_large_projectile_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_sentry_turret_guns, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_large_projectile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_small_missile_launchers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_small_missile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_medium_missile_launchers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_medium_missile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_large_missile_launchers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_large_missile_ammo));
                ByCategoryFlags<ShieldGeneratorModule>(CategoryFlags.cf_shield_generators);
                ByCategoryFlags<ArmorRepairModule>(CategoryFlags.cf_armor_repair_systems);
                ByCategoryFlags<RemoteArmorRepairModule>(CategoryFlags.cf_remote_armor_repairers);
                ByCategoryFlags<CoreBoosterModule>(CategoryFlags.cf_core_boosters, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_core_booster_ammo));
                ByCategoryFlags<SensorJammerModule>(CategoryFlags.cf_sensor_jammers);
                ByCategoryFlags<EnergyNeutralizerModule>(CategoryFlags.cf_energy_neutralizers);
                ByCategoryFlags<EnergyTransfererModule>(CategoryFlags.cf_energy_transferers);
                ByCategoryFlags<EnergyVampireModule>(CategoryFlags.cf_energy_vampires);
                ByCategoryFlags<DrillerModule>(CategoryFlags.cf_drillers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_mining_ammo));
                ByCategoryFlags<RemoteControlledDrillerModule>(CategoryFlags.cf_industrial_turret_drillers);
                ByCategoryFlags<RemoteControlledHarvesterModule>(CategoryFlags.cf_industrial_turret_harvesters);
                ByCategoryFlags<HarvesterModule>(CategoryFlags.cf_harvesters, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_harvesting_ammo));
                ByCategoryFlags<GeoScannerModule>(CategoryFlags.cf_mining_probes, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_mining_probe_ammo));
                ByCategoryFlags<UnitScannerModule>(CategoryFlags.cf_chassis_scanner);
                ByCategoryFlags<ContainerScannerModule>(CategoryFlags.cf_cargo_scanner);
                ByCategoryFlags<SiegeHackModule>(CategoryFlags.cf_siege_hack_modules);
                ByCategoryFlags<NeuralyzerModule>(CategoryFlags.cf_neuralyzer);
                ByCategoryFlags<BlobEmissionModulatorModule>(CategoryFlags.cf_blob_emission_modulator, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_blob_emission_modulator_ammo));
                ByCategoryFlags<RemoteControllerModule>(CategoryFlags.cf_remote_controllers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_remote_controlled_units));
                ByCategoryFlags<WebberModule>(CategoryFlags.cf_webber);
                ByCategoryFlags<SensorDampenerModule>(CategoryFlags.cf_sensor_dampeners);
                ByCategoryFlags<RemoteSensorBoosterModule>(CategoryFlags.cf_remote_sensor_boosters);
                ByCategoryFlags<TargetPainterModule>(CategoryFlags.cf_target_painter);
                ByCategoryFlags<SensorBoosterModule>(CategoryFlags.cf_sensor_boosters);
                ByCategoryFlags<ArmorHardenerModule>(CategoryFlags.cf_armor_hardeners);
                ByCategoryFlags<StealthModule>(CategoryFlags.cf_stealth_modules);
                ByCategoryFlags<DetectionModule>(CategoryFlags.cf_detection_modules);
                ByCategoryFlags<MineDetectorModule>(CategoryFlags.cf_landmine_detectors);
                ByCategoryFlags<Module>(CategoryFlags.cf_armor_plates);
                ByCategoryFlags<Module>(CategoryFlags.cf_core_batteries);
                ByCategoryFlags<Module>(CategoryFlags.cf_core_rechargers);
                ByCategoryFlags<Module>(CategoryFlags.cf_maneuvering_equipment);
                ByCategoryFlags<Module>(CategoryFlags.cf_powergrid_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_cpu_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_mining_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_massmodifiers);
                ByCategoryFlags<Module>(CategoryFlags.cf_weapon_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_tracking_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_armor_repair_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_kers);
                ByCategoryFlags<Module>(CategoryFlags.cf_shield_hardener);
                ByCategoryFlags<Module>(CategoryFlags.cf_eccm);
                ByCategoryFlags<Module>(CategoryFlags.cf_resistance_plating);

                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_speed, new NamedParameter("effectType", EffectType.effect_aura_gang_speed), new NamedParameter("effectModifier", AggregateField.effect_speed_max_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_defense, new NamedParameter("effectType", EffectType.effect_aura_gang_armor_max), new NamedParameter("effectModifier", AggregateField.effect_armor_max_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_information, new NamedParameter("effectType", EffectType.effect_aura_gang_locking_range), new NamedParameter("effectModifier", AggregateField.effect_locking_range_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_industry, new NamedParameter("effectType", EffectType.effect_aura_gang_core_usage_gathering), new NamedParameter("effectModifier", AggregateField.effect_core_usage_gathering_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_shared_dataprocessing, new NamedParameter("effectType", EffectType.effect_aura_gang_locking_time), new NamedParameter("effectModifier", AggregateField.effect_locking_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_coordinated_manuevering, new NamedParameter("effectType", EffectType.effect_aura_gang_signature_radius), new NamedParameter("effectModifier", AggregateField.effect_signature_radius_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_maintance, new NamedParameter("effectType", EffectType.effect_aura_gang_repaired_amount), new NamedParameter("effectModifier", AggregateField.effect_repair_amount_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_precision_firing, new NamedParameter("effectType", EffectType.effect_aura_gang_critical_hit_chance), new NamedParameter("effectModifier", AggregateField.effect_critical_hit_chance_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_core_management, new NamedParameter("effectType", EffectType.effect_aura_gang_core_recharge_time), new NamedParameter("effectModifier", AggregateField.effect_core_recharge_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_shield_calculations, new NamedParameter("effectType", EffectType.effect_aura_gang_shield_absorbtion_ratio), new NamedParameter("effectModifier", AggregateField.effect_shield_absorbtion_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_siege, new NamedParameter("effectType", EffectType.effect_aura_gang_siege), new NamedParameter("effectModifier", AggregateField.effect_weapon_cycle_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_ewar, new NamedParameter("effectType", EffectType.effect_aura_gang_ewar_optimal), new NamedParameter("effectModifier", AggregateField.effect_ew_optimal_range_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_fast_extracting, new NamedParameter("effectType", EffectType.effect_aura_gang_fast_extraction), new NamedParameter("effectModifier", AggregateField.effect_gathering_cycle_time_modifier));


                ByCategoryFlags<SystemContainer>(CategoryFlags.cf_logical_storage);
                ByCategoryFlags<Item>(CategoryFlags.cf_mission_items);
                ByCategoryFlags<Item>(CategoryFlags.cf_robotshards);
                ByCategoryFlags<PunchBagDeployer>(CategoryFlags.cf_others);
                ByCategoryFlags<BlobEmitterUnit>(CategoryFlags.cf_blob_emitter);
                ByCategoryFlags<SentryTurret>(CategoryFlags.cf_sentry_turrets);
                ByCategoryFlags<IndustrialTurret>(CategoryFlags.cf_mining_turrets);
                ByCategoryFlags<IndustrialTurret>(CategoryFlags.cf_harvesting_turrets);
                ByCategoryFlags<CombatDrone>(CategoryFlags.cf_combat_drones);
                ByCategoryFlags<Item>(CategoryFlags.cf_reactor_cores);
                ByCategoryFlags<Kiosk>(CategoryFlags.cf_kiosk);
                ByCategoryFlags<AlarmSwitch>(CategoryFlags.cf_alarm_switch);
                ByCategoryFlags<SimpleSwitch>(CategoryFlags.cf_simple_switch);
                ByCategoryFlags<ItemSupply>(CategoryFlags.cf_item_supply);
                ByCategoryFlags<MobileWorldTeleport>(CategoryFlags.cf_mobile_world_teleport);
                ByNamePatternAndFlag<MobileStrongholdTeleport>("def_mobile_teleport_stronghold", CategoryFlags.cf_mobile_world_teleport); // OPP: stronghold tele
                ByCategoryFlags<Item>(CategoryFlags.cf_mission_coin);
                ByCategoryFlags<AreaBomb>(CategoryFlags.cf_area_bomb);
                ByCategoryFlags<AreaBombDeployer>(CategoryFlags.cf_plasma_bomb);

                ByCategoryFlags<ProximityProbe>(CategoryFlags.cf_proximity_probe);
                ByCategoryFlags<LandMine>(CategoryFlags.cf_light_landmines);
                ByCategoryFlags<LandMine>(CategoryFlags.cf_medium_landmines);
                ByCategoryFlags<LandMine>(CategoryFlags.cf_heavy_landmines);

                ByCategoryFlags<RandomResearchKit>(CategoryFlags.cf_random_research_kits);
                ByCategoryFlags<LotteryItem>(CategoryFlags.cf_lottery_items);
                ByCategoryFlags<Paint>(CategoryFlags.cf_paints); // OPP Robot paint!
                ByCategoryFlags<CalibrationProgramCapsule>(CategoryFlags.cf_ct_capsules); // OPP CT capsules
                ByCategoryFlags<EPBoost>(CategoryFlags.cf_ep_boosters); // OPP EP Boosters
                ByCategoryFlags<Item>(CategoryFlags.cf_datashards); // OPP datashards
                ByCategoryFlags<RespecToken>(CategoryFlags.cf_respec_tokens); // OPP respec tokens
                ByCategoryFlags<SparkTeleportDevice>(CategoryFlags.cf_spark_teleport_devices);

                // OPP new Blinder module
                ByNamePatternAndFlag<TargetBlinderModule>(DefinitionNames.STANDARD_BLINDER_MODULE, CategoryFlags.cf_target_painter);

                ByCategoryFlags<ProximityProbeDeployer>(CategoryFlags.cf_proximity_probe_deployer);
                ByCategoryFlags<LandMineDeployer>(CategoryFlags.cf_landmine_deployer);

                ByCategoryFlags<Item>(CategoryFlags.cf_gift_packages);
                ByCategoryFlags<PBSDeployer>(CategoryFlags.cf_pbs_capsules);
                ByCategoryFlags<PBSEgg>(CategoryFlags.cf_pbs_egg);
                ByCategoryFlags<PBSReactor>(CategoryFlags.cf_pbs_reactor);
                ByCategoryFlags<PBSCoreTransmitter>(CategoryFlags.cf_pbs_core_transmitter);
                ByCategoryFlags<WallHealer>(CategoryFlags.cf_wall_healer);
                ByCategoryFlags<WallHealerDeployer>(CategoryFlags.cf_wall_healer_capsule);
                ByCategoryFlags<PBSResearchLabEnablerNode>(CategoryFlags.cf_pbs_reseach_lab_nodes);
                ByCategoryFlags<PBSRepairEnablerNode>(CategoryFlags.cf_pbs_repair_nodes);
                ByCategoryFlags<PBSFacilityUpgradeNode>(CategoryFlags.cf_pbs_production_upgrade_nodes);
                ByCategoryFlags<PBSReprocessEnablerNode>(CategoryFlags.cf_pbs_reprocessor_nodes);
                ByCategoryFlags<PBSMillEnablerNode>(CategoryFlags.cf_pbs_mill_nodes);
                ByCategoryFlags<PBSRefineryEnablerNode>(CategoryFlags.cf_pbs_refinery_nodes);
                ByCategoryFlags<PBSPrototyperEnablerNode>(CategoryFlags.cf_pbs_prototyper_nodes);
                ByCategoryFlags<PBSCalibrationProgramForgeEnablerNode>(CategoryFlags.cf_pbs_calibration_forge_nodes);
                ByCategoryFlags<PBSResearchKitForgeEnablerNode>(CategoryFlags.cf_pbs_research_kit_forge_nodes);
                ByCategoryFlags<PBSEffectSupplier>(CategoryFlags.cf_pbs_effect_supplier);
                ByCategoryFlags<PBSEffectEmitter>(CategoryFlags.cf_pbs_effect_emitter);
                ByCategoryFlags<PBSMiningTower>(CategoryFlags.cf_pbs_mining_towers);
                ByCategoryFlags<PBSTurret>(CategoryFlags.cf_pbs_turret);
                ByCategoryFlags<PBSArmorRepairerNode>(CategoryFlags.cf_pbs_armor_repairer);
                ByCategoryFlags<PBSResearchKitForgeFacility>(CategoryFlags.cf_research_kit_forge);
                ByCategoryFlags<PBSCalibrationProgramForgeFacility>(CategoryFlags.cf_calibration_program_forge);
                ByCategoryFlags<PBSControlTower>(CategoryFlags.cf_pbs_control_tower);
                ByCategoryFlags<Item>(CategoryFlags.cf_pbs_reactor_booster);
                ByCategoryFlags<VolumeWrapperContainer>(CategoryFlags.cf_volume_wrapper_container);
                ByCategoryFlags<Kernel>(CategoryFlags.cf_kernels);
                ByCategoryFlags<PBSEnergyWell>(CategoryFlags.cf_pbs_energy_well);
                ByCategoryFlags<PBSHighwayNode>(CategoryFlags.cf_pbs_highway_node);
                ByCategoryFlags<FieldTerminal>(CategoryFlags.cf_field_terminal);
                ByCategoryFlags<RandomMissionItem>(CategoryFlags.cf_generic_random_items);
                ByCategoryFlags<Rift>(CategoryFlags.cf_rifts);

                ByCategoryFlags<ExtensionPointActivator>(CategoryFlags.cf_package_activator_ep);
                ByCategoryFlags<CreditActivator>(CategoryFlags.cf_package_activator_credit);
                ByCategoryFlags<SparkActivator>(CategoryFlags.cf_package_activator_spark);
                ByCategoryFlags<ItemShop>(CategoryFlags.cf_zone_item_shop);


                ByName<TrainingKillSwitch>(DefinitionNames.TRAINING_KILL_SWITCH);
                ByName<Trashcan>(DefinitionNames.ADMIN_TRASHCAN);
                ByName<ZoneStorage>(DefinitionNames.ZONE_STORAGE);
                ByName<PunchBagDeployer>(DefinitionNames.DEPLOY_PUNCHBAG);
                ByName<TerraformMultiModule>(DefinitionNames.TERRAFORM_MULTI_MODULE, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_ammo_terraforming_multi));
                ByName<WallBuilderModule>(DefinitionNames.STANDARD_WALL_BUILDER, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_wall_builder_ammo));
                ByName<PBSMillFacility>(DefinitionNames.PBS_FACILITY_MILL);
                ByName<PBSPrototyperFacility>(DefinitionNames.PBS_FACILITY_PROTOTYPER);
                ByName<PBSRefineryFacility>(DefinitionNames.PBS_FACILITY_REFINERY);
                ByName<PBSRepairFacility>(DefinitionNames.PBS_FACILITY_REPAIR);
                ByName<PBSReprocessorFacility>(DefinitionNames.PBS_FACILITY_REPROCESSOR);
                ByName<PBSResearchLabFacility>(DefinitionNames.PBS_FACILITY_RESEARCH_LAB);
                ByName<ConstructionModule>(DefinitionNames.PBS_CONSTRUCTION_MODULE, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_construction_ammo));
                ByName<PlantSeedDeployer>(DefinitionNames.PLANT_SEED_DEVRINOL);
                ByName<Gate>(DefinitionNames.GATE);
                ByName<GateDeployer>(DefinitionNames.GATE_CAPSULE);
                ByName<RandomRiftPortal>(DefinitionNames.RANDOM_RIFT_PORTAL);
                ByName<ItemShop>(DefinitionNames.BASE_ITEM_SHOP);
                ByName<Gift>(DefinitionNames.ANNIVERSARY_PACKAGE);
                ByName<StrongholdExitRift>(DefinitionNames.STRONGHOLD_EXIT_RIFT); //OPP stronghold static exit rift
                ByName<StrongholdEntryRift>(DefinitionNames.TARGETTED_RIFT); //OPP targetted rift
                ByName<Relic>(DefinitionNames.RELIC); //OPP Relic
                ByName<SAPRelic>(DefinitionNames.RELIC_SAP); //OPP outpost Relic

                IContainer c = b.Build();

                return ed =>
                {
                    Entity entity = !c.IsRegisteredWithKey<Entity>(ed.Definition) ? ctx.Resolve<Entity>() : c.ResolveKeyed<Entity>(ed.Definition);
                    entity.ED = ed;
                    entity.Health = ed.Health;
                    entity.Quantity = ed.Quantity;
                    entity.IsRepackaged = ed.AttributeFlags.Repackable;
                    return entity;
                };
            }).SingleInstance();
        }

        private void RegisterLoggers()
        {
            _ = _builder.Register(x =>
            {
                return new LoggerCache(new MemoryCache("LoggerCache"))
                {
                    Expiration = TimeSpan.FromHours(1)
                };
            }).As<ILoggerCache>().SingleInstance();

            _ = _builder.Register<ChannelLoggerFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return name =>
                {
                    FileLogger<ChatLogEvent> fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke("channels", name);
                    return new ChannelLogger(fileLogger);
                };
            });

            _ = _builder.RegisterGeneric(typeof(FileLogger<>));

            _ = _builder.Register<Func<string, string, FileLogger<ChatLogEvent>>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (directory, filename) =>
                {
                    ChatLogFormatter formatter = new ChatLogFormatter();
                    return ctx.Resolve<FileLogger<ChatLogEvent>.Factory>().Invoke(formatter, () => Path.Combine("chatlogs", directory, filename, DateTime.Now.ToString("yyyy-MM-dd"), $"{filename.RemoveSpecialCharacters()}.txt"));
                };
            });

            _ = _builder.Register<ChatLoggerFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (directory, filename) =>
                {
                    FileLogger<ChatLogEvent> fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke(directory, filename);
                    return fileLogger;
                };
            });


            _ = _builder.Register(c =>
            {
                DefaultLogEventFormatter defaultFormater = new DefaultLogEventFormatter();

                DelegateLogEventFormatter<LogEvent, string> formater = new DelegateLogEventFormatter<LogEvent, string>(e =>
                {
                    string formatedEvent = defaultFormater.Format(e);

                    if (!(e.ThrownException is PerpetuumException gex))
                    {
                        return formatedEvent;
                    }

                    StringBuilder sb = new StringBuilder(formatedEvent);

                    _ = sb.AppendLine();
                    _ = sb.AppendLine();
                    _ = sb.AppendFormat("Error = {0}\n", gex.error);

                    if (gex.Data.Count > 0)
                    {
                        _ = sb.AppendFormat("Data: {0}", gex.Data.ToDictionary().ToDebugString());
                    }

                    return sb.ToString();
                });

                FileLogger<LogEvent> fileLogger = c.Resolve<FileLogger<LogEvent>.Factory>().Invoke(formater, () => Path.Combine("logs", DateTime.Now.ToString("yyyy-MM-dd"), "hostlog.txt"));
                fileLogger.BufferSize = 100;
                fileLogger.AutoFlushInterval = TimeSpan.FromSeconds(10);

                return new CompositeLogger<LogEvent>(fileLogger, new ColoredConsoleLogger(formater));
            }).As<ILogger<LogEvent>>();

            _ = _builder.RegisterType<CombatLogger>();
            _ = _builder.RegisterType<CombatLogHelper>();
            _ = _builder.RegisterType<CombatSummary>();
            _ = _builder.RegisterType<CombatLogSaver>().As<ICombatLogSaver>();

            _ = _builder.RegisterGeneric(typeof(DbLogger<>));

        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterPresence<T>(PresenceType presenceType) where T : Presence
        {
            return _builder.RegisterType<T>().Keyed<Presence>(presenceType).PropertiesAutowired();
        }

        private void RegisterFlock<T>(PresenceType presenceType) where T : Flock
        {
            _ = _builder.RegisterType<T>().Keyed<Flock>(presenceType).OnActivated(e =>
            {
                e.Instance.EntityService = e.Context.Resolve<IEntityServices>();
                e.Instance.LootService = e.Context.Resolve<ILootService>();
            });
        }

        public void RegisterNpcs()
        {
            _ = _builder.RegisterType<CustomRiftConfigReader>().As<ICustomRiftConfigReader>();
            _ = _builder.RegisterType<NpcBossInfoBuilder>().SingleInstance();
            _ = _builder.RegisterType<NpcReinforcementsRepository>().SingleInstance().As<INpcReinforcementsRepository>();

            _ = _builder.RegisterType<FlockConfiguration>().As<IFlockConfiguration>();
            _ = _builder.RegisterType<FlockConfigurationBuilder>();
            _ = _builder.RegisterType<IntIDGenerator>().Named<IIDGenerator<int>>("directFlockIDGenerator").SingleInstance().WithParameter("startID", 25000);


            _ = _builder.RegisterType<FlockConfigurationRepository>().OnActivated(e =>
            {
                e.Instance.LoadAllConfig();
            }).As<IFlockConfigurationRepository>().SingleInstance();

            _ = _builder.RegisterType<RandomFlockSelector>().As<IRandomFlockSelector>();

            _ = _builder.RegisterType<RandomFlockReader>()
                .As<IRandomFlockReader>()
                .SingleInstance()
                .OnActivated(e => e.Instance.Init());

            _ = _builder.RegisterType<EscalatingPresenceFlockSelector>().As<IEscalatingPresenceFlockSelector>().SingleInstance();

            _ = _builder.RegisterType<EscalatingFlocksReader>()
                .As<IEscalatingFlocksReader>()
                .SingleInstance()
                .OnActivated(e => e.Instance.Init());

            _ = _builder.RegisterType<NpcSafeSpawnPointsRepository>().As<ISafeSpawnPointsRepository>();
            _ = _builder.RegisterType<PresenceConfigurationReader>().As<IPresenceConfigurationReader>();
            _ = _builder.RegisterType<InterzonePresenceConfigReader>().As<IInterzonePresenceConfigurationReader>();
            _ = _builder.RegisterType<InterzoneGroup>().As<IInterzoneGroup>();
            _ = _builder.RegisterType<PresenceManager>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromSeconds(2)).ToAsync());

                e.Instance.LoadAll();

            }).As<IPresenceManager>();

            _ = _builder.Register<Func<IZone, IPresenceManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    PresenceFactory presenceFactory = ctx.Resolve<PresenceFactory>();
                    IPresenceManager presenceService = ctx.Resolve<PresenceManager.Factory>().Invoke(zone, presenceFactory);
                    return presenceService;
                };
            });

            _ = _builder.Register<FlockFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return (configuration, presence) =>
                {
                    return ctx.ResolveKeyed<Flock>(presence.Configuration.PresenceType, TypedParameter.From(configuration), TypedParameter.From(presence));
                };
            });

            RegisterFlock<NormalFlock>(PresenceType.Normal);
            RegisterFlock<Flock>(PresenceType.Direct);
            RegisterFlock<NormalFlock>(PresenceType.DynamicPool);
            RegisterFlock<NormalFlock>(PresenceType.Dynamic);
            RegisterFlock<RemoteSpawningFlock>(PresenceType.DynamicExtended);
            RegisterFlock<StaticExpiringFlock>(PresenceType.ExpiringRandom);
            RegisterFlock<Flock>(PresenceType.Random);
            RegisterFlock<RoamingFlock>(PresenceType.Roaming);
            RegisterFlock<RoamingFlock>(PresenceType.FreeRoaming);
            RegisterFlock<NormalFlock>(PresenceType.Interzone);
            RegisterFlock<RoamingFlock>(PresenceType.InterzoneRoaming);
            RegisterFlock<StaticExpiringFlock>(PresenceType.EscalatingRandomPresence);
            RegisterFlock<StaticExpiringFlock>(PresenceType.GrowingNPCBasePresence);

            _ = RegisterPresence<Presence>(PresenceType.Normal);
            _ = RegisterPresence<DirectPresence>(PresenceType.Direct).OnActivated(e =>
            {
                e.Instance.FlockIDGenerator = e.Context.ResolveNamed<IIDGenerator<int>>("directFlockIDGenerator");
            });
            _ = RegisterPresence<DynamicPoolPresence>(PresenceType.DynamicPool);
            _ = RegisterPresence<DynamicPresence>(PresenceType.Dynamic);
            _ = RegisterPresence<DynamicPresenceExtended>(PresenceType.DynamicExtended);
            _ = RegisterPresence<RandomSpawningExpiringPresence>(PresenceType.ExpiringRandom);
            _ = RegisterPresence<RandomPresence>(PresenceType.Random);
            _ = RegisterPresence<RoamingPresence>(PresenceType.Roaming);
            _ = RegisterPresence<RoamingPresence>(PresenceType.FreeRoaming);
            _ = RegisterPresence<InterzonePresence>(PresenceType.Interzone);
            _ = RegisterPresence<InterzoneRoamingPresence>(PresenceType.InterzoneRoaming);
            _ = RegisterPresence<GrowingPresence>(PresenceType.EscalatingRandomPresence);
            _ = RegisterPresence<GrowingNPCBasePresence>(PresenceType.GrowingNPCBasePresence);

            _ = _builder.Register<PresenceFactory>(x =>
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

        private void RegisterMtProducts()
        {
            _ = _builder.RegisterType<MtProductRepository>().As<IMtProductRepository>();
            _ = _builder.RegisterType<MtProductHelper>();
            _ = RegisterRequestHandler<MtProductPriceList>(Commands.MtProductPriceList);
        }

        private void RegisterMissions()
        {
            _ = _builder.RegisterType<DisplayMissionSpotsProcess>();
            _ = _builder.RegisterType<MissionDataCache>().SingleInstance();
            _ = _builder.RegisterType<MissionHandler>();
            _ = _builder.RegisterType<MissionInProgress>();
            _ = _builder.RegisterType<MissionAdministrator>();
            _ = _builder.RegisterType<MissionProcessor>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).SingleInstance();

            _ = RegisterRequestHandler<MissionData>(Commands.MissionData);
            _ = RegisterRequestHandler<MissionStart>(Commands.MissionStart);
            _ = RegisterRequestHandler<MissionAbort>(Commands.MissionAbort);
            _ = RegisterRequestHandler<MissionAdminListAll>(Commands.MissionAdminListAll);
            _ = RegisterRequestHandler<MissionAdminTake>(Commands.MissionAdminTake);
            _ = RegisterRequestHandler<MissionLogList>(Commands.MissionLogList);
            _ = RegisterRequestHandler<MissionListRunning>(Commands.MissionListRunning);
            _ = RegisterRequestHandler<MissionReloadCache>(Commands.MissionReloadCache);
            _ = RegisterRequestHandler<MissionGetOptions>(Commands.MissionGetOptions);
            _ = RegisterRequestHandler<MissionResolveTest>(Commands.MissionResolveTest);
            _ = RegisterRequestHandler<MissionDeliver>(Commands.MissionDeliver);
            _ = RegisterRequestHandler<MissionFlush>(Commands.MissionFlush);
            _ = RegisterRequestHandler<MissionReset>(Commands.MissionReset);
            _ = RegisterRequestHandler<MissionListAgents>(Commands.MissionListAgents);

            _ = _builder.RegisterType<DeliveryHelper>();
            _ = _builder.RegisterType<MissionTargetInProgress>();
        }

        private void InitRelayManager()
        {
            _ = _builder.RegisterType<MarketHelper>().SingleInstance();
            _ = _builder.RegisterType<MarketHandler>().SingleInstance();

            _ = _builder.RegisterType<MarketOrder>();
            _ = _builder.RegisterType<MarketOrderRepository>().As<IMarketOrderRepository>();
            _ = _builder.Register(c => new MarketInfoService(0.3, 10, false)).As<IMarketInfoService>();

            _ = _builder.RegisterType<MarketRobotPriceWriter>().As<IMarketRobotPriceWriter>().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromHours(4)));
            });

            _ = _builder.RegisterType<GangInviteService>().As<IGangInviteService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(2)));
            });

            _ = _builder.RegisterType<VolunteerCEOService>().As<IVolunteerCEOService>();

            _ = _builder.RegisterType<VolunteerCEORepository>().As<IVolunteerCEORepository>();

            _ = _builder.RegisterType<CorporationLogger>();
            _ = _builder.RegisterType<CorporationManager>().As<ICorporationManager>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            });
            _ = _builder.RegisterType<CorporateInvites>();
            _ = _builder.RegisterType<BulletinHandler>().As<IBulletinHandler>();
            _ = _builder.RegisterType<VoteHandler>().As<IVoteHandler>();

            _ = _builder.RegisterType<ReprocessSessionMember>();
            _ = _builder.RegisterType<ReprocessSession>();

            _ = _builder.RegisterType<ProductionCostReader>().As<IProductionCostReader>();
            _ = _builder.RegisterType<ProductionDataAccess>().OnActivated(e =>
            {
                e.Instance.Init();
            }).As<IProductionDataAccess>().SingleInstance();
            _ = _builder.RegisterType<ProductionDescription>();
            _ = _builder.RegisterType<ProductionComponentCollector>();
            _ = _builder.RegisterType<ProductionInProgressRepository>().As<IProductionInProgressRepository>();
            _ = _builder.RegisterType<ProductionLine>();
            _ = _builder.RegisterType<ProductionInProgress>();
            _ = _builder.RegisterType<ProductionProcessor>().SingleInstance().OnActivated(e =>
            {
                e.Instance.InitProcessor();
            });
            _ = _builder.RegisterType<ProductionManager>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            });

            _ = _builder.RegisterType<LoginQueueService>().As<ILoginQueueService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(5)));
            });


            _ = _builder.RegisterType<RelayStateService>().As<IRelayStateService>().SingleInstance();
            _ = _builder.RegisterType<RelayInfoBuilder>();

            _ = _builder.RegisterType<TradeService>().SingleInstance().As<ITradeService>();

            _ = _builder.RegisterType<HostShutDownManager>().SingleInstance();

            _ = _builder.RegisterType<HighScoreService>().As<IHighScoreService>();
            _ = _builder.RegisterType<CorporationHandler>();
            _ = _builder.RegisterType<TerraformHandler>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromMilliseconds(200)).ToAsync());
            });

            _ = _builder.RegisterType<InsuranceHelper>();
            _ = _builder.RegisterType<InsurancePayOut>();
            _ = _builder.RegisterType<InsuranceDescription>();
            _ = _builder.RegisterType<CharacterCleaner>();

            _ = _builder.RegisterType<SparkTeleportRepository>().As<ISparkTeleportRepository>();
            _ = _builder.RegisterType<SparkTeleportHelper>();

            _ = _builder.RegisterType<SparkExtensionsReader>().As<ISparkExtensionsReader>();
            _ = _builder.RegisterType<SparkRepository>().As<ISparkRepository>();
            _ = _builder.RegisterType<SparkHelper>();


            _ = _builder.RegisterType<Trade>();

            _ = _builder.RegisterType<GoodiePackHandler>();

            // OPP: EPBonusEventService singleton
            _ = _builder.RegisterType<EPBonusEventService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(1)));
            });

            // OPP: EventListenerService and consumers
            _ = _builder.RegisterType<ChatEcho>();
            _ = _builder.RegisterType<DirectMessenger>();
            _ = _builder.RegisterType<NpcChatEcho>();
            _ = _builder.RegisterType<AffectOutpostStability>();
            _ = _builder.RegisterType<PortalSpawner>();
            _ = _builder.RegisterType<NpcStateAnnouncer>();
            _ = _builder.RegisterType<OreNpcSpawner>().As<NpcSpawnEventHandler<OreNpcSpawnMessage>>();
            _ = _builder.RegisterType<NpcReinforcementSpawner>().As<NpcSpawnEventHandler<NpcReinforcementsMessage>>();
            _ = _builder.RegisterType<EventListenerService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(0.75)));
                e.Instance.AttachListener(e.Context.Resolve<ChatEcho>());
                e.Instance.AttachListener(e.Context.Resolve<DirectMessenger>());
                e.Instance.AttachListener(e.Context.Resolve<NpcChatEcho>());
                e.Instance.AttachListener(e.Context.Resolve<PortalSpawner>());
                e.Instance.AttachListener(e.Context.Resolve<NpcStateAnnouncer>());
                GameTimeObserver obs = new GameTimeObserver(e.Instance);
                obs.Subscribe(e.Context.Resolve<IGameTimeService>());
            });

            _ = _builder.RegisterType<GameTimeService>().As<IGameTimeService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(1.5)));
            });

            // OPP: InterzoneNPCManager
            _ = RegisterAutoActivate<InterzonePresenceManager>(TimeSpan.FromSeconds(10));

            _ = _builder.RegisterType<AccountManager>().As<IAccountManager>();

            _ = _builder.RegisterType<Account>();
            _ = _builder.RegisterType<AccountWallet>().AsSelf().As<IAccountWallet>();
            _ = _builder.Register<AccountWalletFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (account, type) =>
                {
                    return ctx.Resolve<AccountWallet>(new TypedParameter(typeof(Account), account),
                        new TypedParameter(typeof(AccountTransactionType), type));
                };
            });
            _ = _builder.RegisterType<AccountTransactionLogger>();
            _ = _builder.RegisterType<EpForActivityLogger>();
        }

        private IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterRequestHandler<TRequestHandler, TRequest>(Command command) where TRequestHandler : IRequestHandler<TRequest> where TRequest : IRequest
        {
            IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> res = _builder.RegisterType<TRequestHandler>();

            _ = _builder.Register(c =>
            {
                return c.Resolve<RequestHandlerProfiler<TRequest>>(new TypedParameter(typeof(IRequestHandler<TRequest>), c.Resolve<TRequestHandler>()));
            }).Keyed<IRequestHandler<TRequest>>(command);

            return res;
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<T>(Command command) where T : IRequestHandler<IRequest>
        {
            return RegisterRequestHandler<T, IRequest>(command);
        }

        private void RegisterRequestHandlerFactory<T>() where T : IRequest
        {
            _ = _builder.Register<RequestHandlerFactory<T>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return command =>
                {
                    return ctx.IsRegisteredWithKey<IRequestHandler<T>>(command) ? ctx.ResolveKeyed<IRequestHandler<T>>(command) : null;
                };
            });
        }

        private void RegisterRequestHandlers()
        {
            _ = _builder.RegisterGeneric(typeof(RequestHandlerProfiler<>));

            RegisterRequestHandlerFactory<IRequest>();
            RegisterRequestHandlerFactory<IZoneRequest>();

            _ = RegisterRequestHandler<GetEnums>(Commands.GetEnums);
            _ = RegisterRequestHandler<GetCommands>(Commands.GetCommands);
            _ = RegisterRequestHandler<GetEntityDefaults>(Commands.GetEntityDefaults).SingleInstance();
            _ = RegisterRequestHandler<GetAggregateFields>(Commands.GetAggregateFields).SingleInstance();
            _ = RegisterRequestHandler<GetDefinitionConfigUnits>(Commands.GetDefinitionConfigUnits).SingleInstance();
            _ = RegisterRequestHandler<GetEffects>(Commands.GetEffects).SingleInstance();
            _ = RegisterRequestHandler<GetDistances>(Commands.GetDistances);
            _ = RegisterRequestHandler<SignIn>(Commands.SignIn);
            _ = RegisterRequestHandler<SignInSteam>(Commands.SignInSteam);
            _ = RegisterRequestHandler<SignOut>(Commands.SignOut);
            _ = RegisterRequestHandler<SteamListAccounts>(Commands.SteamListAccounts);
            _ = RegisterRequestHandler<AccountConfirmEmail>(Commands.AccountConfirmEmail);
            _ = RegisterRequestHandler<CharacterList>(Commands.CharacterList);
            _ = RegisterRequestHandler<CharacterCreate>(Commands.CharacterCreate);
            _ = RegisterRequestHandler<CharacterSelect>(Commands.CharacterSelect);
            _ = RegisterRequestHandler<CharacterDeselect>(Commands.CharacterDeselect);
            _ = RegisterRequestHandler<CharacterForceDeselect>(Commands.CharacterForceDeselect);
            _ = RegisterRequestHandler<CharacterForceDisconnect>(Commands.CharacterForceDisconnect);
            _ = RegisterRequestHandler<CharacterDelete>(Commands.CharacterDelete);
            _ = RegisterRequestHandler<CharacterSetHomeBase>(Commands.CharacterSetHomeBase);
            _ = RegisterRequestHandler<CharacterGetProfiles>(Commands.CharacterGetProfiles);
            _ = RegisterRequestHandler<CharacterRename>(Commands.CharacterRename);
            _ = RegisterRequestHandler<CharacterCheckNick>(Commands.CharacterCheckNick);
            _ = RegisterRequestHandler<CharacterCorrectNick>(Commands.CharacterCorrectNick);
            _ = RegisterRequestHandler<CharacterIsOnline>(Commands.IsOnline);
            _ = RegisterRequestHandler<CharacterSettingsSet>(Commands.CharacterSettingsSet);
            _ = RegisterRequestHandler<CharacterSetMoodMessage>(Commands.CharacterSetMoodMessage);
            _ = RegisterRequestHandler<CharacterTransferCredit>(Commands.CharacterTransferCredit);
            _ = RegisterRequestHandler<CharacterSetAvatar>(Commands.CharacterSetAvatar);
            _ = RegisterRequestHandler<CharacterSetBlockTrades>(Commands.CharacterSetBlockTrades);
            _ = RegisterRequestHandler<CharacterSetCredit>(Commands.CharacterSetCredit);
            _ = RegisterRequestHandler<CharacterClearHomeBase>(Commands.CharacterClearHomeBase);
            _ = RegisterRequestHandler<CharacterSettingsGet>(Commands.CharacterSettingsGet);
            _ = RegisterRequestHandler<CharacterGetMyProfile>(Commands.CharacterGetMyProfile);
            _ = RegisterRequestHandler<CharacterSearch>(Commands.CharacterSearch);
            _ = RegisterRequestHandler<CharacterRemoveFromCache>(Commands.CharacterRemoveFromCache);
            _ = RegisterRequestHandler<CharacterListNpcDeath>(Commands.CharacterListNpcDeath);
            _ = RegisterRequestHandler<CharacterTransactionHistory>(Commands.CharacterTransactionHistory);
            _ = RegisterRequestHandler<CharacterGetZoneInfo>(Commands.CharacterGetZoneInfo);
            _ = RegisterRequestHandler<CharacterNickHistory>(Commands.CharacterNickHistory);
            _ = RegisterRequestHandler<CharacterGetNote>(Commands.CharacterGetNote);
            _ = RegisterRequestHandler<CharacterSetNote>(Commands.CharacterSetNote);
            _ = RegisterRequestHandler<CharacterCorporationHistory>(Commands.CharacterCorporationHistory);
            _ = RegisterRequestHandler<CharacterWizardData>(Commands.CharacterWizardData).SingleInstance();
            _ = RegisterRequestHandler<CharactersOnline>(Commands.GetCharactersOnline);
            _ = RegisterRequestHandler<ReimburseItemRequestHandler>(Commands.ReimburseItem);
            _ = RegisterRequestHandler<Chat>(Commands.Chat);
            _ = RegisterRequestHandler<GoodiePackList>(Commands.GoodiePackList);
            _ = RegisterRequestHandler<GoodiePackRedeem>(Commands.GoodiePackRedeem);
            _ = RegisterRequestHandler<Ping>(Commands.Ping);
            _ = RegisterRequestHandler<Quit>(Commands.Quit);
            _ = RegisterRequestHandler<SetMaxUserCount>(Commands.SetMaxUserCount);
            _ = RegisterRequestHandler<SparkTeleportSet>(Commands.SparkTeleportSet);
            _ = RegisterRequestHandler<SparkTeleportUse>(Commands.SparkTeleportUse);
            _ = RegisterRequestHandler<SparkTeleportDelete>(Commands.SparkTeleportDelete);
            _ = RegisterRequestHandler<SparkTeleportList>(Commands.SparkTeleportList);
            _ = RegisterRequestHandler<SparkChange>(Commands.SparkChange);
            _ = RegisterRequestHandler<SparkRemove>(Commands.SparkRemove);
            _ = RegisterRequestHandler<SparkList>(Commands.SparkList);
            _ = RegisterRequestHandler<SparkSetDefault>(Commands.SparkSetDefault);
            _ = RegisterRequestHandler<SparkUnlock>(Commands.SparkUnlock);
            _ = RegisterRequestHandler<Undock>(Commands.Undock)
                ;
            _ = RegisterRequestHandler<ProximityProbeRegisterSet>(Commands.ProximityProbeRegisterSet);
            _ = RegisterRequestHandler<ProximityProbeSetName>(Commands.ProximityProbeSetName);
            _ = RegisterRequestHandler<ProximityProbeList>(Commands.ProximityProbeList);
            _ = RegisterRequestHandler<ProximityProbeGetRegistrationInfo>(Commands.ProximityProbeGetRegistrationInfo);

            _ = RegisterRequestHandler<IntrusionEnabler>(Commands.IntrusionEnabler);
            _ = RegisterRequestHandler<AccountGetTransactionHistory>(Commands.AccountGetTransactionHistory);
            _ = RegisterRequestHandler<AccountList>(Commands.AccountList);

            _ = RegisterRequestHandler<AccountEpForActivityHistory>(Commands.AccountEpForActivityHistory);
            _ = RegisterRequestHandler<RedeemableItemList>(Commands.RedeemableItemList);
            _ = RegisterRequestHandler<RedeemableItemRedeem>(Commands.RedeemableItemRedeem);
            _ = RegisterRequestHandler<RedeemableItemActivate>(Commands.RedeemableItemActivate);
            _ = RegisterRequestHandler<CreateItemRequestHandler>(Commands.CreateItem);
            _ = RegisterRequestHandler<TeleportList>(Commands.TeleportList);
            _ = RegisterRequestHandler<TeleportConnectColumns>(Commands.TeleportConnectColumns);
            _ = RegisterRequestHandler<EnableSelfTeleport>(Commands.EnableSelfTeleport);
            _ = RegisterRequestHandler<ItemCount>(Commands.ItemCount);
            _ = RegisterRequestHandler<SystemInfo>(Commands.SystemInfo);
            _ = RegisterRequestHandler<TransferData>(Commands.TransferData);

            _ = RegisterRequestHandler<BaseReown>(Commands.BaseReown);
            _ = RegisterRequestHandler<BaseSetDockingRights>(Commands.BaseSetDockingRights);
            _ = RegisterRequestHandler<BaseSelect>(Commands.BaseSelect);
            _ = RegisterRequestHandler<BaseGetInfo>(Commands.BaseGetInfo);
            _ = RegisterRequestHandler<BaseGetMyItems>(Commands.BaseGetMyItems);
            _ = RegisterRequestHandler<BaseListFacilities>(Commands.BaseListFacilities).SingleInstance();

            _ = RegisterRequestHandler<GetZoneInfo>(Commands.GetZoneInfo);
            _ = RegisterRequestHandler<ItemCountOnZone>(Commands.ItemCountOnZone);


            _ = RegisterRequestHandler<CorporationCreate>(Commands.CorporationCreate);
            _ = RegisterRequestHandler<CorporationRemoveMember>(Commands.CorporationRemoveMember);
            _ = RegisterRequestHandler<CorporationGetMyInfo>(Commands.CorporationGetMyInfo);
            _ = RegisterRequestHandler<CorporationSetMemberRole>(Commands.CorporationSetMemberRole);
            _ = RegisterRequestHandler<CorporationCharacterInvite>(Commands.CorporationCharacterInvite);
            _ = RegisterRequestHandler<CorporationInviteReply>(Commands.CorporationInviteReply);
            _ = RegisterRequestHandler<CorporationInfo>(Commands.CorporationInfo);
            _ = RegisterRequestHandler<CorporationLeave>(Commands.CorporationLeave);
            _ = RegisterRequestHandler<CorporationSearch>(Commands.CorporationSearch);
            _ = RegisterRequestHandler<CorporationSetInfo>(Commands.CorporationSetInfo);
            _ = RegisterRequestHandler<CorporationDropRoles>(Commands.CorporationDropRoles);
            _ = RegisterRequestHandler<CorporationCancelLeave>(Commands.CorporationCancelLeave);
            _ = RegisterRequestHandler<CorporationPayOut>(Commands.CorporationPayOut);
            _ = RegisterRequestHandler<CorporationForceInfo>(Commands.CorporationForceInfo);
            _ = RegisterRequestHandler<CorporationGetDelegates>(Commands.CorporationGetDelegates);
            _ = RegisterRequestHandler<CorporationTransfer>(Commands.CorporationTransfer);
            _ = RegisterRequestHandler<CorporationHangarListAll>(Commands.CorporationHangarListAll);
            _ = RegisterRequestHandler<CorporationHangarListOnBase>(Commands.CorporationHangarListOnBase);
            _ = RegisterRequestHandler<CorporationRentHangar>(Commands.CorporationRentHangar);
            _ = RegisterRequestHandler<CorporationHangarPayRent>(Commands.CorporationHangarPayRent);
            _ = RegisterRequestHandler<CorporationHangarLogSet>(Commands.CorporationHangarLogSet);
            _ = RegisterRequestHandler<CorporationHangarLogClear>(Commands.CorporationHangarLogClear);
            _ = RegisterRequestHandler<CorporationHangarLogList>(Commands.CorporationHangarLogList);
            _ = RegisterRequestHandler<CorporationHangarSetAccess>(Commands.CorporationHangarSetAccess);
            _ = RegisterRequestHandler<CorporationHangarClose>(Commands.CorporationHangarClose);
            _ = RegisterRequestHandler<CorporationHangarSetName>(Commands.CorporationHangarSetName);
            _ = RegisterRequestHandler<CorporationHangarRentPrice>(Commands.CorporationHangarRentPrice);
            _ = RegisterRequestHandler<CorporationHangarFolderSectionCreate>(Commands.CorporationHangarFolderSectionCreate);
            _ = RegisterRequestHandler<CorporationHangarFolderSectionDelete>(Commands.CorporationHangarFolderSectionDelete);
            _ = RegisterRequestHandler<CorporationVoteStart>(Commands.CorporationVoteStart);
            _ = RegisterRequestHandler<CorporationVoteList>(Commands.CorporationVoteList);
            _ = RegisterRequestHandler<CorporationVoteDelete>(Commands.CorporationVoteDelete);
            _ = RegisterRequestHandler<CorporationVoteCast>(Commands.CorporationVoteCast);
            _ = RegisterRequestHandler<CorporationVoteSetTopic>(Commands.CorporationVoteSetTopic);
            _ = RegisterRequestHandler<CorporationBulletinStart>(Commands.CorporationBulletinStart);
            _ = RegisterRequestHandler<CorporationBulletinEntry>(Commands.CorporationBulletinEntry);
            _ = RegisterRequestHandler<CorporationBulletinDelete>(Commands.CorporationBulletinDelete);
            _ = RegisterRequestHandler<CorporationBulletinList>(Commands.CorporationBulletinList);
            _ = RegisterRequestHandler<CorporationBulletinDetails>(Commands.CorporationBulletinDetails);
            _ = RegisterRequestHandler<CorporationBulletinEntryDelete>(Commands.CorporationBulletinEntryDelete);
            _ = RegisterRequestHandler<CorporationBulletinNewEntries>(Commands.CorporationBulletinNewEntries);
            _ = RegisterRequestHandler<CorporationBulletinModerate>(Commands.CorporationBulletinModerate);
            _ = RegisterRequestHandler<CorporationCeoTakeOverStatus>(Commands.CorporationCeoTakeOverStatus);
            _ = RegisterRequestHandler<CorporationVolunteerForCeo>(Commands.CorporationVolunteerForCeo);
            _ = RegisterRequestHandler<CorporationRename>(Commands.CorporationRename);
            _ = RegisterRequestHandler<CorporationDonate>(Commands.CorporationDonate);
            _ = RegisterRequestHandler<CorporationTransactionHistory>(Commands.CorporationTransactionHistory);
            _ = RegisterRequestHandler<CorporationApply>(Commands.CorporationApply);
            _ = RegisterRequestHandler<CorporationDeleteMyApplication>(Commands.CorporationDeleteMyApplication);
            _ = RegisterRequestHandler<CorporationAcceptApplication>(Commands.CorporationAcceptApplication);
            _ = RegisterRequestHandler<CorporationDeleteApplication>(Commands.CorporationDeleteApplication);
            _ = RegisterRequestHandler<CorporationListMyApplications>(Commands.CorporationListMyApplications);
            _ = RegisterRequestHandler<CorporationListApplications>(Commands.CorporationListApplications);
            _ = RegisterRequestHandler<CorporationLogHistory>(Commands.CorporationLogHistory);
            _ = RegisterRequestHandler<CorporationNameHistory>(Commands.CorporationNameHistory);
            _ = RegisterRequestHandler<CorporationSetColor>(Commands.CorporationSetColor);
            _ = RegisterRequestHandler<CorporationDocumentConfig>(Commands.CorporationDocumentConfig).SingleInstance();
            _ = RegisterRequestHandler<CorporationDocumentTransfer>(Commands.CorporationDocumentTransfer);
            _ = RegisterRequestHandler<CorporationDocumentList>(Commands.CorporationDocumentList);
            _ = RegisterRequestHandler<CorporationDocumentCreate>(Commands.CorporationDocumentCreate);
            _ = RegisterRequestHandler<CorporationDocumentDelete>(Commands.CorporationDocumentDelete);
            _ = RegisterRequestHandler<CorporationDocumentOpen>(Commands.CorporationDocumentOpen);
            _ = RegisterRequestHandler<CorporationDocumentUpdateBody>(Commands.CorporationDocumentUpdateBody);
            _ = RegisterRequestHandler<CorporationDocumentMonitor>(Commands.CorporationDocumentMonitor);
            _ = RegisterRequestHandler<CorporationDocumentUnmonitor>(Commands.CorporationDocumentUnmonitor);
            _ = RegisterRequestHandler<CorporationDocumentRent>(Commands.CorporationDocumentRent);
            _ = RegisterRequestHandler<CorporationDocumentRegisterList>(Commands.CorporationDocumentRegisterList);
            _ = RegisterRequestHandler<CorporationDocumentRegisterSet>(Commands.CorporationDocumentRegisterSet);
            _ = RegisterRequestHandler<CorporationInfoFlushCache>(Commands.CorporationInfoFlushCache);
            _ = RegisterRequestHandler<CorporationGetReputation>(Commands.CorporationGetReputation);
            _ = RegisterRequestHandler<CorporationMyStandings>(Commands.CorporationMyStandings);
            _ = RegisterRequestHandler<CorporationSetMembersNeutral>(Commands.CorporationSetMembersNeutral);
            _ = RegisterRequestHandler<CorporationRoleHistory>(Commands.CorporationRoleHistory);
            _ = RegisterRequestHandler<CorporationMemberRoleHistory>(Commands.CorporationMemberRoleHistory);




            _ = RegisterRequestHandler<YellowPagesSearch>(Commands.YellowPagesSearch);
            _ = RegisterRequestHandler<YellowPagesSubmit>(Commands.YellowPagesSubmit);
            _ = RegisterRequestHandler<YellowPagesGet>(Commands.YellowPagesGet);
            _ = RegisterRequestHandler<YellowPagesDelete>(Commands.YellowPagesDelete);


            _ = RegisterRequestHandler<AllianceGetDefaults>(Commands.AllianceGetDefaults).SingleInstance();
            _ = RegisterRequestHandler<AllianceGetMyInfo>(Commands.AllianceGetMyInfo);
            _ = RegisterRequestHandler<AllianceRoleHistory>(Commands.AllianceRoleHistory);

            _ = RegisterRequestHandler<ExtensionTest>(Commands.ExtensionTest);
            _ = RegisterRequestHandler<ExtensionGetAll>(Commands.ExtensionGetAll).SingleInstance();
            _ = RegisterRequestHandler<ExtensionPrerequireList>(Commands.ExtensionPrerequireList).SingleInstance();
            _ = RegisterRequestHandler<ExtensionCategoryList>(Commands.ExtensionCategoryList).SingleInstance();
            _ = RegisterRequestHandler<ExtensionLearntList>(Commands.ExtensionLearntList);
            _ = RegisterRequestHandler<ExtensionGetAvailablePoints>(Commands.ExtensionGetAvailablePoints);
            _ = RegisterRequestHandler<ExtensionGetPointParameters>(Commands.ExtensionGetPointParameters);
            _ = RegisterRequestHandler<ExtensionHistory>(Commands.ExtensionHistory);
            _ = RegisterRequestHandler<ExtensionBuyForPoints>(Commands.ExtensionBuyForPoints);
            _ = RegisterRequestHandler<ExtensionRemoveLevel>(Commands.ExtensionRemoveLevel);
            _ = RegisterRequestHandler<ExtensionBuyEpBoost>(Commands.ExtensionBuyEpBoost);
            _ = RegisterRequestHandler<ExtensionResetCharacter>(Commands.ExtensionResetCharacter);
            _ = RegisterRequestHandler<ExtensionFreeLockedEp>(Commands.ExtensionFreeLockedEp);
            _ = RegisterRequestHandler<ExtensionFreeAllLockedEpByCommand>(Commands.ExtensionFreeAllLockedEpCommand); // For GameAdmin Channel Command
            _ = RegisterRequestHandler<ExtensionGive>(Commands.ExtensionGive);
            _ = RegisterRequestHandler<ExtensionReset>(Commands.ExtensionReset);
            _ = RegisterRequestHandler<ExtensionRevert>(Commands.ExtensionRevert);

            _ = RegisterRequestHandler<ItemShopBuy>(Commands.ItemShopBuy);
            _ = RegisterRequestHandler<ItemShopList>(Commands.ItemShopList);
            _ = RegisterRequestHandler<GiftOpen>(Commands.GiftOpen);
            _ = RegisterRequestHandler<GetHighScores>(Commands.GetHighScores);
            _ = RegisterRequestHandler<GetMyHighScores>(Commands.GetMyHighScores);
            _ = RegisterRequestHandler<ZoneSectorList>(Commands.ZoneSectorList).SingleInstance();

            _ = RegisterRequestHandler<ListContainer>(Commands.ListContainer);

            _ = RegisterRequestHandler<SocialGetMyList>(Commands.SocialGetMyList);
            _ = RegisterRequestHandler<SocialFriendRequest>(Commands.SocialFriendRequest);
            _ = RegisterRequestHandler<SocialConfirmPendingFriendRequest>(Commands.SocialConfirmPendingFriendRequest);
            _ = RegisterRequestHandler<SocialDeleteFriend>(Commands.SocialDeleteFriend);
            _ = RegisterRequestHandler<SocialBlockFriend>(Commands.SocialBlockFriend);

            _ = RegisterRequestHandler<PBSGetReimburseInfo>(Commands.PBSGetReimburseInfo);
            _ = RegisterRequestHandler<PBSSetReimburseInfo>(Commands.PBSSetReimburseInfo);
            _ = RegisterRequestHandler<PBSGetLog>(Commands.PBSGetLog);

            _ = RegisterRequestHandler<MineralScanResultList>(Commands.MineralScanResultList);
            _ = RegisterRequestHandler<MineralScanResultMove>(Commands.MineralScanResultMove);
            _ = RegisterRequestHandler<MineralScanResultDelete>(Commands.MineralScanResultDelete);
            _ = RegisterRequestHandler<MineralScanResultCreateItem>(Commands.MineralScanResultCreateItem);
            _ = RegisterRequestHandler<MineralScanResultUploadFromItem>(Commands.MineralScanResultUploadFromItem);

            _ = RegisterRequestHandler<FreshNewsCount>(Commands.FreshNewsCount);
            _ = RegisterRequestHandler<GetNews>(Commands.GetNews);
            _ = RegisterRequestHandler<AddNews>(Commands.AddNews);
            _ = RegisterRequestHandler<UpdateNews>(Commands.UpdateNews);
            _ = RegisterRequestHandler<NewsCategory>(Commands.NewsCategory).SingleInstance();

            _ = RegisterRequestHandler<EpForActivityDailyLog>(Commands.EpForActivityDailyLog);
            _ = RegisterRequestHandler<GetMyKillReports>(Commands.GetMyKillReports);
            _ = RegisterRequestHandler<UseLotteryItem>(Commands.UseLotteryItem);
            _ = RegisterRequestHandler<ContainerMover>(Commands.ContainerMover);


            _ = RegisterRequestHandler<MarketTaxChange>(Commands.MarketTaxChange);
            _ = RegisterRequestHandler<MarketTaxLogList>(Commands.MarketTaxLogList);
            _ = RegisterRequestHandler<MarketGetInfo>(Commands.MarketGetInfo);
            _ = RegisterRequestHandler<MarketAddCategory>(Commands.MarketAddCategory);
            _ = RegisterRequestHandler<MarketItemList>(Commands.MarketItemList);
            _ = RegisterRequestHandler<MarketGetMyItems>(Commands.MarketGetMyItems);
            _ = RegisterRequestHandler<MarketGetAveragePrices>(Commands.MarketGetAveragePrices);
            _ = RegisterRequestHandler<MarketCreateBuyOrder>(Commands.MarketCreateBuyOrder);
            _ = RegisterRequestHandler<MarketCreateSellOrder>(Commands.MarketCreateSellOrder);
            _ = RegisterRequestHandler<MarketBuyItem>(Commands.MarketBuyItem);
            _ = RegisterRequestHandler<MarketCancelItem>(Commands.MarketCancelItem);
            _ = RegisterRequestHandler<MarketGetState>(Commands.MarketGetState);
            _ = RegisterRequestHandler<MarketSetState>(Commands.MarketSetState);
            _ = RegisterRequestHandler<MarketFlush>(Commands.MarketFlush);
            _ = RegisterRequestHandler<MarketGetDefinitionAveragePrice>(Commands.MarketGetDefinitionAveragePrice);
            _ = RegisterRequestHandler<MarketAvailableItems>(Commands.MarketAvailableItems);
            _ = RegisterRequestHandler<MarketItemsInRange>(Commands.MarketItemsInRange);
            _ = RegisterRequestHandler<MarketInsertStats>(Commands.MarketInsertStats);
            _ = RegisterRequestHandler<MarketListFacilities>(Commands.MarketListFacilities);
            _ = RegisterRequestHandler<MarketInsertAverageForCF>(Commands.MarketInsertAverageForCF);
            _ = RegisterRequestHandler<MarketGlobalAveragePrices>(Commands.MarketGlobalAveragePrices);
            _ = RegisterRequestHandler<MarketModifyOrder>(Commands.MarketModifyOrder);
            _ = RegisterRequestHandler<MarketCreateGammaPlasmaOrders>(Commands.MarketCreateGammaPlasmaOrders);
            _ = RegisterRequestHandler<MarketRemoveItems>(Commands.MarketRemoveItems);
            _ = RegisterRequestHandler<MarketCleanUp>(Commands.MarketCleanUp);



            _ = RegisterRequestHandler<TradeBegin>(Commands.TradeBegin);
            _ = RegisterRequestHandler<TradeCancel>(Commands.TradeCancel);
            _ = RegisterRequestHandler<TradeSetOffer>(Commands.TradeSetOffer);
            _ = RegisterRequestHandler<TradeAccept>(Commands.TradeAccept);
            _ = RegisterRequestHandler<TradeRetractOffer>(Commands.TradeRetractOffer);


            _ = RegisterRequestHandler<GetRobotInfo>(Commands.GetRobotInfo).OnActivated(e => e.Instance.ForFitting = false);
            _ = RegisterRequestHandler<GetRobotInfo>(Commands.GetRobotFittingInfo);
            _ = RegisterRequestHandler<SelectActiveRobot>(Commands.SelectActiveRobot);
            _ = RegisterRequestHandler<RequestStarterRobot>(Commands.RequestStarterRobot);
            _ = RegisterRequestHandler<RobotEmpty>(Commands.RobotEmpty);
            _ = RegisterRequestHandler<SetRobotTint>(Commands.SetRobotTint);

            _ = RegisterRequestHandler<FittingPresetList>(Commands.FittingPresetList);
            _ = RegisterRequestHandler<FittingPresetSave>(Commands.FittingPresetSave);
            _ = RegisterRequestHandler<FittingPresetDelete>(Commands.FittingPresetDelete);
            _ = RegisterRequestHandler<FittingPresetApply>(Commands.FittingPresetApply);

            _ = RegisterRequestHandler<RobotTemplateAdd>(Commands.RobotTemplateAdd);
            _ = RegisterRequestHandler<RobotTemplateUpdate>(Commands.RobotTemplateUpdate);
            _ = RegisterRequestHandler<RobotTemplateDelete>(Commands.RobotTemplateDelete);
            _ = RegisterRequestHandler<RobotTemplateList>(Commands.RobotTemplateList);
            _ = RegisterRequestHandler<RobotTemplateBuild>(Commands.RobotTemplateBuild);

            _ = RegisterRequestHandler<EquipModule>(Commands.EquipModule);
            _ = RegisterRequestHandler<ChangeModule>(Commands.ChangeModule);
            _ = RegisterRequestHandler<RemoveModule>(Commands.RemoveModule);
            _ = RegisterRequestHandler<EquipAmmo>(Commands.EquipAmmo);
            _ = RegisterRequestHandler<ChangeAmmo>(Commands.ChangeAmmo);
            _ = RegisterRequestHandler<RemoveAmmo>(Commands.UnequipAmmo);
            _ = RegisterRequestHandler<PackItems>(Commands.PackItems);
            _ = RegisterRequestHandler<UnpackItems>(Commands.UnpackItems);
            _ = RegisterRequestHandler<TrashItems>(Commands.TrashItems);
            _ = RegisterRequestHandler<RelocateItems>(Commands.RelocateItems);
            _ = RegisterRequestHandler<StackSelection>(Commands.StackSelection);
            _ = RegisterRequestHandler<UnstackAmount>(Commands.UnstackAmount);
            _ = RegisterRequestHandler<SetItemName>(Commands.SetItemName);
            _ = RegisterRequestHandler<StackTo>(Commands.StackTo);
            _ = RegisterRequestHandler<ServerMessage>(Commands.ServerMessage);
            _ = RegisterRequestHandler<RequestInfiniteBox>(Commands.RequestInfiniteBox);
            _ = RegisterRequestHandler<DecorCategoryList>(Commands.DecorCategoryList);
            _ = RegisterRequestHandler<PollGet>(Commands.PollGet);
            _ = RegisterRequestHandler<PollAnswer>(Commands.PollAnswer);
            _ = RegisterRequestHandler<ForceDock>(Commands.ForceDock);
            _ = RegisterRequestHandler<ForceDockAdmin>(Commands.ForceDockAdmin);
            _ = RegisterRequestHandler<GetItemSummary>(Commands.GetItemSummary);

            _ = RegisterRequestHandler<ProductionHistory>(Commands.ProductionHistory);
            _ = RegisterRequestHandler<GetResearchLevels>(Commands.GetResearchLevels).SingleInstance();
            _ = RegisterRequestHandler<ProductionComponentsList>(Commands.ProductionComponentsList);
            _ = RegisterRequestHandler<ProductionRefine>(Commands.ProductionRefine);
            _ = RegisterRequestHandler<ProductionRefineQuery>(Commands.ProductionRefineQuery);
            _ = RegisterRequestHandler<ProductionCPRGInfo>(Commands.ProductionCPRGInfo);
            _ = RegisterRequestHandler<ProductionCPRGForge>(Commands.ProductionCPRGForge);
            _ = RegisterRequestHandler<ProductionCPRGForgeQuery>(Commands.ProductionCPRGForgeQuery);
            _ = RegisterRequestHandler<ProductionGetCPRGFromLine>(Commands.ProductionGetCprgFromLine);
            _ = RegisterRequestHandler<ProductionGetCPRGFromLineQuery>(Commands.ProductionGetCprgFromLineQuery);
            _ = RegisterRequestHandler<ProductionLineSetRounds>(Commands.ProductionLineSetRounds);
            _ = RegisterRequestHandler<ProductionPrototypeStart>(Commands.ProductionPrototypeStart);
            _ = RegisterRequestHandler<ProductionPrototypeQuery>(Commands.ProductionPrototypeQuery);
            _ = RegisterRequestHandler<ProductionInsuranceQuery>(Commands.ProductionInsuranceQuery);
            _ = RegisterRequestHandler<ProductionInsuranceList>(Commands.ProductionInsuranceList);
            _ = RegisterRequestHandler<ProductionInsuranceBuy>(Commands.ProductionInsuranceBuy);
            _ = RegisterRequestHandler<ProductionInsuranceDelete>(Commands.ProductionInsuranceDelete);
            _ = RegisterRequestHandler<ProductionMergeResearchKitsMulti>(Commands.ProductionMergeResearchKitsMulti);
            _ = RegisterRequestHandler<ProductionMergeResearchKitsMultiQuery>(Commands.ProductionMergeResearchKitsMultiQuery);
            _ = RegisterRequestHandler<ProductionQueryLineNextRound>(Commands.ProductionQueryLineNextRound);
            _ = RegisterRequestHandler<ProductionReprocess>(Commands.ProductionReprocess);
            _ = RegisterRequestHandler<ProductionReprocessQuery>(Commands.ProductionReprocessQuery);
            _ = RegisterRequestHandler<ProductionRepair>(Commands.ProductionRepair);
            _ = RegisterRequestHandler<ProductionRepairQuery>(Commands.ProductionRepairQuery);
            _ = RegisterRequestHandler<ProductionResearch>(Commands.ProductionResearch);
            _ = RegisterRequestHandler<ProductionResearchQuery>(Commands.ProductionResearchQuery);
            _ = RegisterRequestHandler<ProductionInProgressHandler>(Commands.ProductionInProgress);
            _ = RegisterRequestHandler<ProductionCancel>(Commands.ProductionCancel);
            _ = RegisterRequestHandler<ProductionFacilityInfo>(Commands.ProductionFacilityInfo);
            _ = RegisterRequestHandler<ProductionLineList>(Commands.ProductionLineList);
            _ = RegisterRequestHandler<ProductionLineCalibrate>(Commands.ProductionLineCalibrate);
            _ = RegisterRequestHandler<ProductionLineDelete>(Commands.ProductionLineDelete);
            _ = RegisterRequestHandler<ProductionLineStart>(Commands.ProductionLineStart);
            _ = RegisterRequestHandler<ProductionFacilityDescription>(Commands.ProductionFacilityDescription);
            _ = RegisterRequestHandler<ProductionInProgressCorporation>(Commands.ProductionInProgressCorporation);
            //admin 
            _ = RegisterRequestHandler<ProductionRemoveFacility>(Commands.ProductionRemoveFacility);
            _ = RegisterRequestHandler<ProductionSpawnComponents>(Commands.ProductionSpawnComponents);
            _ = RegisterRequestHandler<ProductionScaleComponentsAmount>(Commands.ProductionScaleComponentsAmount);
            _ = RegisterRequestHandler<ProductionUnrepairItem>(Commands.ProductionUnrepairItem);
            _ = RegisterRequestHandler<ProductionFacilityOnOff>(Commands.ProductionFacilityOnOff);
            _ = RegisterRequestHandler<ProductionForceEnd>(Commands.ProductionForceEnd);
            _ = RegisterRequestHandler<ProductionServerInfo>(Commands.ProductionServerInfo);
            _ = RegisterRequestHandler<ProductionSpawnCPRG>(Commands.ProductionSpawnCPRG);
            _ = RegisterRequestHandler<ProductionGetInsurance>(Commands.ProductionGetInsurance);
            _ = RegisterRequestHandler<ProductionSetInsurance>(Commands.ProductionSetInsurance);

            _ = RegisterRequestHandler<CreateCorporationHangarStorage>(Commands.CreateCorporationHangarStorage);
            _ = RegisterRequestHandler<DockAll>(Commands.DockAll);
            _ = RegisterRequestHandler<ReturnCorporationOwnderItems>(Commands.ReturnCorporateOwnedItems);

            _ = RegisterRequestHandler<RelayOpen>(Commands.RelayOpen);
            _ = RegisterRequestHandler<RelayClose>(Commands.RelayClose);
            _ = RegisterRequestHandler<ZoneSaveLayer>(Commands.ZoneSaveLayer);
            _ = RegisterRequestHandler<ZoneRemoveObject>(Commands.ZoneRemoveObject);
            _ = RegisterRequestHandler<ZoneDebugLOS>(Commands.ZoneDebugLOS);
            _ = RegisterRequestHandler<ZoneSetBaseDetails>(Commands.ZoneSetBaseDetails);
            _ = RegisterRequestHandler<ZoneSelfDestruct>(Commands.ZoneSelfDestruct);
            _ = RegisterRequestHandler<ZoneSOS>(Commands.ZoneSOS);
            _ = RegisterRequestHandler<ZoneCopyGroundType>(Commands.ZoneCopyGroundType); //OPP

            _ = RegisterRequestHandler<ZoneGetZoneObjectDebugInfo>(Commands.ZoneGetZoneObjectDebugInfo);
            _ = RegisterRequestHandler<ZoneDrawBlockingByEid>(Commands.ZoneDrawBlockingByEid);


            _ = RegisterRequestHandler<GangCreate>(Commands.GangCreate);
            _ = RegisterRequestHandler<GangDelete>(Commands.GangDelete);
            _ = RegisterRequestHandler<GangLeave>(Commands.GangLeave);
            _ = RegisterRequestHandler<GangKick>(Commands.GangKick);
            _ = RegisterRequestHandler<GangInfo>(Commands.GangInfo);
            _ = RegisterRequestHandler<GangSetLeader>(Commands.GangSetLeader);
            _ = RegisterRequestHandler<GangSetRole>(Commands.GangSetRole);
            _ = RegisterRequestHandler<GangInvite>(Commands.GangInvite);
            _ = RegisterRequestHandler<GangInviteReply>(Commands.GangInviteReply);

            _ = RegisterRequestHandler<TechTreeInfo>(Commands.TechTreeInfo);
            _ = RegisterRequestHandler<TechTreeUnlock>(Commands.TechTreeUnlock);
            _ = RegisterRequestHandler<TechTreeResearch>(Commands.TechTreeResearch);
            _ = RegisterRequestHandler<TechTreeDonate>(Commands.TechTreeDonate);
            _ = RegisterRequestHandler<TechTreeGetLogs>(Commands.TechTreeGetLogs);


            _ = RegisterRequestHandler<TransportAssignmentSubmit>(Commands.TransportAssignmentSubmit);
            _ = RegisterRequestHandler<TransportAssignmentList>(Commands.TransportAssignmentList);
            _ = RegisterRequestHandler<TransportAssignmentCancel>(Commands.TransportAssignmentCancel);
            _ = RegisterRequestHandler<TransportAssignmentTake>(Commands.TransportAssignmentTake);
            _ = RegisterRequestHandler<TransportAssignmentLog>(Commands.TransportAssignmentLog);
            _ = RegisterRequestHandler<TransportAssignmentContainerInfo>(Commands.TransportAssignmentContainerInfo);
            _ = RegisterRequestHandler<TransportAssignmentRunning>(Commands.TransportAssignmentRunning);
            _ = RegisterRequestHandler<TransportAssignmentRetrieve>(Commands.TransportAssignmentRetrieve);
            _ = RegisterRequestHandler<TransportAssignmentListContent>(Commands.TransportAssignmentListContent);
            _ = RegisterRequestHandler<TransportAssignmentGiveUp>(Commands.TransportAssignmentGiveUp);
            _ = RegisterRequestHandler<TransportAssignmentDeliver>(Commands.TransportAssignmentDeliver);


            _ = RegisterRequestHandler<SetStanding>(Commands.SetStanding);
            _ = RegisterRequestHandler<ForceStanding>(Commands.ForceStanding);
            _ = RegisterRequestHandler<ForceFactionStandings>(Commands.ForceFactionStandings);
            _ = RegisterRequestHandler<GetStandingForDefaultCorporations>(Commands.GetStandingForDefaultCorporations);
            _ = RegisterRequestHandler<GetStandingForDefaultAlliances>(Commands.GetStandingForDefaultAlliances);
            _ = RegisterRequestHandler<StandingList>(Commands.StandingList);
            _ = RegisterRequestHandler<StandingHistory>(Commands.StandingHistory);
            _ = RegisterRequestHandler<ReloadStandingForCharacter>(Commands.ReloadStandingForCharacter);

            _ = RegisterRequestHandler<MailList>(Commands.MailList);
            _ = RegisterRequestHandler<MailUsedFolders>(Commands.MailUsedFolders);
            _ = RegisterRequestHandler<MailSend>(Commands.MailSend);
            _ = RegisterRequestHandler<MailDelete>(Commands.MailDelete);
            _ = RegisterRequestHandler<MailOpen>(Commands.MailOpen);
            _ = RegisterRequestHandler<MailMoveToFolder>(Commands.MailMoveToFolder);
            _ = RegisterRequestHandler<MailDeleteFolder>(Commands.MailDeleteFolder);
            _ = RegisterRequestHandler<MailNewCount>(Commands.MailNewCount);
            _ = RegisterRequestHandler<MassMailOpen>(Commands.MassMailOpen);
            _ = RegisterRequestHandler<MassMailDelete>(Commands.MassMailDelete);
            _ = RegisterRequestHandler<MassMailSend>(Commands.MassMailSend);
            _ = RegisterRequestHandler<MassMailList>(Commands.MassMailList);
            _ = RegisterRequestHandler<MassMailNewCount>(Commands.MassMailNewCount);


            _ = RegisterRequestHandler<ServerShutDownState>(Commands.ServerShutDownState);
            _ = RegisterRequestHandler<ServerShutDown>(Commands.ServerShutDown);
            _ = RegisterRequestHandler<ServerShutDownCancel>(Commands.ServerShutDownCancel);

            RegisterZoneRequestHandlers();

            //Admin tool commands
            _ = RegisterRequestHandler<GetAccountsWithCharacters>(Commands.GetAccountsWithCharacters);
            _ = RegisterRequestHandler<AccountGet>(Commands.AccountGet);
            _ = RegisterRequestHandler<AccountUpdate>(Commands.AccountUpdate);
            _ = RegisterRequestHandler<AccountCreate>(Commands.AccountCreate);
            _ = RegisterRequestHandler<ChangeSessionPassword>(Commands.ChangeSessionPassword);
            _ = RegisterRequestHandler<AccountBan>(Commands.AccountBan);
            _ = RegisterRequestHandler<AccountUnban>(Commands.AccountUnban);
            _ = RegisterRequestHandler<AccountDelete>(Commands.AccountDelete);
            _ = RegisterRequestHandler<ServerInfoGet>(Commands.ServerInfoGet);
            _ = RegisterRequestHandler<ServerInfoSet>(Commands.ServerInfoSet);

            // Open account commands
            _ = RegisterRequestHandler<AccountOpenCreate>(Commands.AccountOpenCreate);

            // Event GM Commands
            _ = RegisterRequestHandler<EPBonusEvent>(Commands.EPBonusSet);
        }

        private void RegisterRobotTemplates()
        {
            _ = _builder.Register<RobotTemplateFactory>(x =>
            {
                IRobotTemplateRelations relations = x.Resolve<IRobotTemplateRelations>();
                return definition =>
                {
                    return relations.GetRelatedTemplateOrDefault(definition);
                };
            });

            _ = _builder.RegisterType<RobotTemplateReader>().AsSelf().As<IRobotTemplateReader>();
            _ = _builder.Register(x =>
            {
                return new CachedRobotTemplateReader(x.Resolve<RobotTemplateReader>());
            }).AsSelf().As<IRobotTemplateReader>().SingleInstance().OnActivated(e => e.Instance.Init());

            _ = _builder.RegisterType<RobotTemplateRepository>().As<IRobotTemplateRepository>();
            _ = _builder.RegisterType<RobotTemplateRelations>().As<IRobotTemplateRelations>().SingleInstance().OnActivated(e =>
            {
                e.Instance.Init();
            });

            _ = _builder.RegisterType<RobotTemplateServicesImpl>().As<IRobotTemplateServices>().PropertiesAutowired().SingleInstance();

            _ = _builder.RegisterType<HybridRobotBuilder>();

            _ = _builder.RegisterType<RobotHelper>();
        }

        private void RegisterTerrains()
        {
            _ = _builder.Register<Func<IZone, IEnumerable<IMaterialLayer>>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    IMineralConfigurationReader reader = ctx.Resolve<IMineralConfigurationReader>();
                    OreNpcSpawner listener = new OreNpcSpawner(zone, ctx.Resolve<INpcReinforcementsRepository>(), reader);
                    EventListenerService eventListenerService = ctx.Resolve<EventListenerService>();
                    eventListenerService.AttachListener(listener);
                    if (zone is TrainingZone)
                    {
                        GravelRepository repo = ctx.Resolve<GravelRepository>();
                        GravelConfiguration config = new GravelConfiguration(zone);
                        GravelLayer layer = new GravelLayer(zone.Size.Width, zone.Size.Height, config, repo);
                        layer.LoadMineralNodes();
                        return new[] { layer };
                    }

                    MineralNodeGeneratorFactory nodeGeneratorFactory = new MineralNodeGeneratorFactory(zone);

                    List<IMaterialLayer> materialLayers = new List<IMaterialLayer>();

                    foreach (IMineralConfiguration configuration in reader.ReadAll().Where(c => c.ZoneId == zone.Id))
                    {
                        MineralNodeRepository repo = new MineralNodeRepository(zone, configuration.Type);
                        switch (configuration.ExtractionType)
                        {
                            case MineralExtractionType.Solid:
                                {
                                    OreLayer layer = new OreLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                    layer.LoadMineralNodes();
                                    materialLayers.Add(layer);
                                    break;
                                }
                            case MineralExtractionType.Liquid:
                                {
                                    LiquidLayer layer = new LiquidLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                    layer.LoadMineralNodes();
                                    materialLayers.Add(layer);
                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return materialLayers;
                };
            });

            _ = _builder.RegisterType<Scanner>();
            _ = _builder.RegisterType<MaterialHelper>().SingleInstance();

            _ = _builder.RegisterType<GravelRepository>();
            _ = _builder.RegisterType<LayerFileIO>().As<ILayerFileIO>();
            _ = _builder.RegisterType<Terrain>();
            _ = _builder.RegisterGeneric(typeof(IntervalLayerSaver<>)).InstancePerDependency();

            _ = _builder.Register<TerrainFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    Terrain terrain = ctx.Resolve<Terrain>();

                    System.Drawing.Size size = zone.Configuration.Size;

                    ILayerFileIO loader = ctx.Resolve<ILayerFileIO>();

                    BlockingInfo[] blocks = loader.Load<BlockingInfo>(zone, LayerType.Blocks);
                    terrain.Blocks = new Layer<BlockingInfo>(LayerType.Blocks, blocks, size.Width, size.Height);

                    TerrainControlInfo[] controls = loader.Load<TerrainControlInfo>(zone, LayerType.Control);
                    terrain.Controls = new Layer<TerrainControlInfo>(LayerType.Control, controls, size.Width, size.Height);

                    PlantInfo[] plants = loader.Load<PlantInfo>(zone, LayerType.Plants);
                    terrain.Plants = new Layer<PlantInfo>(LayerType.Plants, plants, size.Width, size.Height);

                    ushort[] altitude = loader.Load<ushort>(zone, LayerType.Altitude);
                    AltitudeLayer altitudeLayer;

                    if (zone.Configuration.Terraformable)
                    {
                        Layer<ushort> originalAltitude = new Layer<ushort>(LayerType.OriginalAltitude, altitude, size.Width, size.Height);
                        ushort[] blend = loader.LoadLayerData<ushort>(zone, "altitude_blend");
                        Layer<ushort> blendLayer = new Layer<ushort>(LayerType.Blend, blend, size.Width, size.Height);
                        altitudeLayer = new TerraformableAltitude(originalAltitude, blendLayer, altitude);
                    }
                    else
                    {
                        altitudeLayer = new AltitudeLayer(altitude, size.Width, size.Height);
                    }

                    terrain.Altitude = altitudeLayer;
                    terrain.Slope = new SlopeLayer(altitudeLayer);

                    if (!zone.Configuration.Terraformable)
                    {
                        PassableMapBuilder b = new PassableMapBuilder(terrain.Blocks, terrain.Slope, zone.GetPassablePositionFromDb());
                        terrain.Passable = b.Build();
                    }

                    terrain.Materials = ctx.Resolve<Func<IZone, IEnumerable<IMaterialLayer>>>().Invoke(zone).ToDictionary(m => m.Type);

                    CompositeProcess layerSavers = new CompositeProcess();
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<BlockingInfo>.Factory>().Invoke(terrain.Blocks, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<TerrainControlInfo>.Factory>().Invoke(terrain.Controls, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<PlantInfo>.Factory>().Invoke(terrain.Plants, zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<ushort>.Factory>().Invoke(terrain.Altitude, zone));

                    ctx.Resolve<IProcessManager>().AddProcess(layerSavers.ToAsync().AsTimed(TimeSpan.FromHours(2)));
                    ctx.Resolve<IProcessManager>().AddProcess(terrain.Materials.Values.OfType<IProcess>().ToCompositeProcess().ToAsync().AsTimed(TimeSpan.FromMinutes(2)));
                    return terrain;
                };
            });
        }

        private void RegisterIntrusions()
        {
            _ = RegisterRequestHandler<BaseGetOwnershipInfo>(Commands.BaseGetOwnershipInfo);
            _ = RegisterRequestHandler<IntrusionGetPauseTime>(Commands.IntrusionGetPauseTime);
            _ = RegisterRequestHandler<IntrusionSetPauseTime>(Commands.IntrusionSetPauseTime);
            _ = RegisterRequestHandler<IntrusionUpgradeFacility>(Commands.IntrusionUpgradeFacility);
            _ = RegisterRequestHandler<SetIntrusionSiteMessage>(Commands.SetIntrusionSiteMessage);
            _ = RegisterRequestHandler<GetIntrusionLog>(Commands.GetIntrusionLog);
            _ = RegisterRequestHandler<GetIntrusionStabilityLog>(Commands.GetIntrusionStabilityLog);
            _ = RegisterRequestHandler<GetStabilityBonusThresholds>(Commands.GetStabilityBonusThresholds);
            _ = RegisterRequestHandler<GetIntrusionSiteInfo>(Commands.GetIntrusionSiteInfo);
            _ = RegisterRequestHandler<GetIntrusionPublicLog>(Commands.GetIntrusionPublicLog);
            _ = RegisterRequestHandler<GetIntrusionMySitesLog>(Commands.GetIntrusionMySitesLog);

            _ = RegisterZoneRequestHandler<IntrusionSAPGetItemInfo>(Commands.IntrusionSAPGetItemInfo);
            _ = RegisterZoneRequestHandler<IntrusionSAPSubmitItem>(Commands.IntrusionSAPSubmitItem);
            _ = RegisterZoneRequestHandler<IntrusionSiteSetEffectBonus>(Commands.IntrusionSiteSetEffectBonus);
            _ = RegisterZoneRequestHandler<IntrusionSetDefenseThreshold>(Commands.IntrusionSetDefenseThreshold);

            _ = RegisterZoneRequestHandler<GetRobotInfo>(Commands.GetRobotFittingInfo);
        }

        private void RegisterRifts()
        {
            _ = _builder.Register<Func<IZone, RiftSpawnPositionFinder>>(x =>
            {
                return zone =>
                {
                    return zone.Configuration.Terraformable ? new PvpRiftSpawnPositionFinder(zone) : (RiftSpawnPositionFinder)new PveRiftSpawnPositionFinder(zone);
                };
            });

            _ = _builder.RegisterType<RiftManager>();
            _ = _builder.RegisterType<StrongholdRiftManager>();

            _ = _builder.Register<Func<IZone, IRiftManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    if (zone is TrainingZone)
                    {
                        return null;
                    }

                    if (zone is StrongHoldZone)
                    {

                        int strongHoldExitConfigCount = Db.Query().CommandText("SELECT COUNT(*) FROM strongholdexitconfig WHERE zoneid = @zoneId;")
                        .SetParameter("@zoneId", zone.Id)
                        .ExecuteScalar<int>();
                        return strongHoldExitConfigCount < 1 ? null : (IRiftManager)ctx.Resolve<StrongholdRiftManager>(new TypedParameter(typeof(IZone), zone));
                    }


                    List<System.Data.IDataRecord> zoneConfigs = Db.Query().CommandText("SELECT maxrifts FROM zoneriftsconfig WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute();
                    if (zoneConfigs.Count < 1)
                    {
                        return null;
                    }

                    System.Data.IDataRecord record = zoneConfigs[0];
                    int maxrifts = record.GetValue<int>("maxrifts");

                    if (maxrifts < 1)
                    {
                        return null;
                    }

                    TimeRange spawnTime = TimeRange.FromLength(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                    RiftSpawnPositionFinder finder = ctx.Resolve<Func<IZone, RiftSpawnPositionFinder>>().Invoke(zone);
                    return ctx.Resolve<RiftManager>(new TypedParameter(typeof(IZone), zone), new NamedParameter("spawnTime", spawnTime), new NamedParameter("spawnPositionFinder", finder));
                };
            });
        }

        private void RegisterRelics()
        {
            _ = _builder.RegisterType<ZoneRelicManager>().As<IRelicManager>();

            _ = _builder.Register<Func<IZone, IRelicManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    int numRelicConfigs = Db.Query().CommandText("SELECT id FROM relicspawninfo WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute().Count;
                    if (numRelicConfigs < 1)
                    {
                        return null;
                    }

                    List<System.Data.IDataRecord> zoneConfigs = Db.Query().CommandText("SELECT maxspawn FROM reliczoneconfig WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute();
                    if (zoneConfigs.Count < 1)
                    {
                        return null;
                    }
                    System.Data.IDataRecord record = zoneConfigs[0];
                    int maxspawn = record.GetValue<int>("maxspawn");
                    if (maxspawn < 1)
                    {
                        return null;
                    }
                    //Do not register RelicManagers on zones without the necessary valid entries in reliczoneconfig and relicspawninfo
                    return ctx.Resolve<IRelicManager>(new TypedParameter(typeof(IZone), zone));
                };
            });
        }

        private void RegisterZones()
        {
            _ = _builder.RegisterType<ZoneSession>().AsSelf().As<IZoneSession>();

            _ = _builder.RegisterType<SaveBitmapHelper>();
            _ = _builder.RegisterType<ZoneDrawStatMap>();

            _ = _builder.RegisterType<ZoneConfigurationReader>().As<IZoneConfigurationReader>();

            _ = _builder.Register(c =>
            {
                return new WeatherService(new TimeRange(TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)));
            }).OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(5)));
            }).As<IWeatherService>();

            _ = _builder.RegisterType<WeatherMonitor>();
            _ = _builder.RegisterType<WeatherEventListener>();
            _ = _builder.Register<Func<IZone, WeatherEventListener>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    return new WeatherEventListener(ctx.Resolve<EventListenerService>(), zone);
                };
            });

            _ = _builder.Register<Func<IZone, EnvironmentalEffectHandler>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    EnvironmentalEffectHandler listener = new EnvironmentalEffectHandler(zone);
                    ctx.Resolve<EventListenerService>().AttachListener(listener);
                    return listener;
                };
            });

            _ = _builder.RegisterType<DefaultZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();
            _ = _builder.RegisterType<UserZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();

            _ = _builder.Register<ZoneUnitServiceFactory>(x =>
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

            _ = _builder.RegisterType<BeamService>().As<IBeamService>();
            _ = _builder.RegisterType<MiningLogHandler>();
            _ = _builder.RegisterType<HarvestLogHandler>();
            _ = _builder.RegisterType<MineralConfigurationReader>().As<IMineralConfigurationReader>().SingleInstance();

            void RegisterZone<T>(ZoneType type) where T : Zone
            {
                _ = _builder.RegisterType<T>().Keyed<Zone>(type).OnActivated(e =>
                {
                    e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync());
                });
            }

            RegisterZone<PveZone>(ZoneType.Pve);
            RegisterZone<PvpZone>(ZoneType.Pvp);
            RegisterZone<TrainingZone>(ZoneType.Training);
            RegisterZone<StrongHoldZone>(ZoneType.Stronghold);

            _ = _builder.RegisterType<SettingsLoader>();
            _ = _builder.RegisterType<PlantRuleLoader>();

            _ = _builder.RegisterType<StrongholdPlayerStateManager>().As<IStrongholdPlayerStateManager>();


            _ = _builder.Register<Func<IZone, IStrongholdPlayerStateManager>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    return new StrongholdPlayerStateManager(zone, ctx.Resolve<EventListenerService>());
                };
            });

            _ = _builder.Register<Func<ZoneConfiguration, IZone>>(x =>
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

            _ = _builder.Register(c => c.Resolve<ZoneManager>()).As<IZoneManager>();
            _ = _builder.RegisterType<ZoneManager>().OnActivated(e =>
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

            _ = _builder.RegisterType<TagHelper>();

            _ = _builder.RegisterType<ZoneEnterQueueService>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).As<IZoneEnterQueueService>().InstancePerDependency();

            _ = _builder.RegisterType<DecorHandler>().OnActivated(e => e.Instance.Initialize()).InstancePerDependency();
            _ = _builder.RegisterType<ZoneEnvironmentHandler>();
            _ = _builder.RegisterType<PlantHandler>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(5)));
            }).As<IPlantHandler>().InstancePerDependency();

            _ = _builder.RegisterType<TeleportDescriptionBuilder>();
            _ = _builder.RegisterType<TeleportWorldTargetHelper>();
            _ = _builder.RegisterType<MobileTeleportZoneMapCache>().As<IMobileTeleportToZoneMap>().SingleInstance();
            _ = _builder.RegisterType<StrongholdTeleportTargetHelper>();
            _ = _builder.RegisterType<TeleportToAnotherZone>();
            _ = _builder.RegisterType<TeleportWithinZone>();
            _ = _builder.RegisterType<TrainingExitStrategy>();

            _ = _builder.RegisterType<PBSHighwayHandler>().OnActivated(e =>
            {
                IProcessManager pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromMilliseconds(PBSHighwayHandler.DRAW_INTERVAL)).ToAsync());
            });

            _ = _builder.RegisterType<MineralScanResultRepository>();
            _ = _builder.RegisterType<RareMaterialHandler>().SingleInstance();
            _ = _builder.RegisterType<PlantHarvester>().As<IPlantHarvester>();

            _ = _builder.RegisterType<TeleportStrategyFactoriesImpl>()
                .As<ITeleportStrategyFactories>()
                .PropertiesAutowired()
                .SingleInstance();

            _ = _builder.RegisterType<TrainingRewardRepository>().SingleInstance().As<ITrainingRewardRepository>();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterZoneRequestHandler<T>(Command command) where T : IRequestHandler<IZoneRequest>
        {
            return RegisterRequestHandler<T, IZoneRequest>(command);
        }

        private void RegisterZoneRequestHandlers()
        {
            _ = RegisterZoneRequestHandler<TeleportGetChannelList>(Commands.TeleportGetChannelList);
            _ = RegisterZoneRequestHandler<TeleportToZoneObject>(Commands.TeleportToZoneObject);
            _ = RegisterZoneRequestHandler<TeleportUse>(Commands.TeleportUse);
            _ = RegisterZoneRequestHandler<TeleportQueryWorldChannels>(Commands.TeleportQueryWorldChannels);
            _ = RegisterZoneRequestHandler<JumpAnywhere>(Commands.JumpAnywhere);
            _ = RegisterZoneRequestHandler<MovePlayer>(Commands.MovePlayer);
            _ = RegisterZoneRequestHandler<ZoneDrawStatMap>(Commands.ZoneDrawStatMap);
            _ = RegisterZoneRequestHandler<MissionStartFromZone>(Commands.MissionStartFromZone);
            _ = RegisterZoneRequestHandler<ZoneItemShopBuy>(Commands.ItemShopBuy);
            _ = RegisterZoneRequestHandler<ZoneItemShopList>(Commands.ItemShopList);
            _ = RegisterZoneRequestHandler<ZoneMoveUnit>(Commands.ZoneMoveUnit);
            _ = RegisterZoneRequestHandler<ZoneGetQueueInfo>(Commands.ZoneGetQueueInfo);
            _ = RegisterZoneRequestHandler<ZoneSetQueueLength>(Commands.ZoneSetQueueLength);
            _ = RegisterZoneRequestHandler<ZoneCancelEnterQueue>(Commands.ZoneCancelEnterQueue);
            _ = RegisterZoneRequestHandler<ZoneGetBuildings>(Commands.ZoneGetBuildings);

            _ = RegisterZoneRequestHandler<Dock>(Commands.Dock);

            _ = RegisterZoneRequestHandler<ZoneDecorAdd>(Commands.ZoneDecorAdd);
            _ = RegisterZoneRequestHandler<ZoneDecorSet>(Commands.ZoneDecorSet);
            _ = RegisterZoneRequestHandler<ZoneDecorDelete>(Commands.ZoneDecorDelete);
            _ = RegisterZoneRequestHandler<ZoneDecorLock>(Commands.ZoneDecorLock);
            _ = RegisterZoneRequestHandler<ZoneDrawDecorEnvironment>(Commands.ZoneDrawDecorEnvironment);
            _ = RegisterZoneRequestHandler<ZoneSampleDecorEnvironment>(Commands.ZoneSampleDecorEnvironment);
            _ = RegisterZoneRequestHandler<ZoneDrawDecorEnvByDef>(Commands.ZoneDrawDecorEnvByDef);
            _ = RegisterZoneRequestHandler<ZoneDrawAllDecors>(Commands.ZoneDrawAllDecors);
            _ = RegisterZoneRequestHandler<ZoneEnvironmentDescriptionList>(Commands.ZoneEnvironmentDescriptionList);
            _ = RegisterZoneRequestHandler<ZoneSampleEnvironment>(Commands.ZoneSampleEnvironment);
            _ = RegisterZoneRequestHandler<ZoneCreateTeleportColumn>(Commands.ZoneCreateTeleportColumn);

            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.PackItems>(Commands.PackItems);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnpackItems>(Commands.UnpackItems);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.TrashItems>(Commands.TrashItems);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RelocateItems>(Commands.RelocateItems);
            _ = RegisterZoneRequestHandler<StackItems>(Commands.StackItems);
            _ = RegisterZoneRequestHandler<StackItems>(Commands.StackSelection);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnstackAmount>(Commands.UnstackAmount);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.SetItemName>(Commands.SetItemName);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ListContainer>(Commands.ListContainer);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipModule>(Commands.EquipModule);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RemoveModule>(Commands.RemoveModule);
            _ = RegisterZoneRequestHandler<ChangeModule>(Commands.ChangeModule);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipAmmo>(Commands.EquipAmmo);
            _ = RegisterZoneRequestHandler<UnequipAmmo>(Commands.UnequipAmmo);
            _ = RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ChangeAmmo>(Commands.ChangeAmmo);

            _ = RegisterZoneRequestHandler<MissionGetSupply>(Commands.MissionGetSupply);
            _ = RegisterZoneRequestHandler<MissionSpotPlace>(Commands.MissionSpotPlace);
            _ = RegisterZoneRequestHandler<MissionSpotUpdate>(Commands.MissionSpotUpdate);
            _ = RegisterZoneRequestHandler<ZoneUpdateStructure>(Commands.ZoneUpdateStructure);
            _ = RegisterZoneRequestHandler<RemoveMissionStructure>(Commands.RemoveMissionStructure);
            _ = RegisterZoneRequestHandler<KioskInfo>(Commands.KioskInfo);
            _ = RegisterZoneRequestHandler<KioskSubmitItem>(Commands.KioskSubmitItem);
            _ = RegisterZoneRequestHandler<AlarmStart>(Commands.AlarmStart);
            _ = RegisterZoneRequestHandler<TriggerMissionStructure>(Commands.TriggerMissionStructure);

            _ = RegisterZoneRequestHandler<ZoneUploadScanResult>(Commands.ZoneUploadScanResult);

            //admin
            _ = RegisterZoneRequestHandler<ZoneEntityChangeState>(Commands.ZoneEntityChangeState);
            _ = RegisterZoneRequestHandler<ZoneRemoveByDefinition>(Commands.ZoneRemoveByDefinition);
            _ = RegisterZoneRequestHandler<ZoneMakeGotoXY>(Commands.ZoneMakeGotoXY);
            _ = RegisterZoneRequestHandler<ZoneDrawBeam>(Commands.ZoneDrawBeam);
            _ = RegisterZoneRequestHandler<ZoneSetRuntimeZoneEntityName>(Commands.ZoneSetRuntimeZoneEntityName);
            _ = RegisterZoneRequestHandler<ZoneCheckRoaming>(Commands.ZoneCheckRoaming);
            _ = RegisterZoneRequestHandler<ZonePBSTest>(Commands.ZonePBSTest);
            _ = RegisterZoneRequestHandler<ZonePBSFixOrphaned>(Commands.ZonePBSFixOrphaned);
            _ = RegisterZoneRequestHandler<ZoneFixPBS>(Commands.ZoneFixPBS);
            _ = RegisterZoneRequestHandler<ZoneServerMessage>(Commands.ZoneServerMessage);
            _ = RegisterZoneRequestHandler<ZonePlaceWall>(Commands.ZonePlaceWall);
            _ = RegisterZoneRequestHandler<ZoneClearWalls>(Commands.ZoneClearWalls);
            _ = RegisterZoneRequestHandler<ZoneHealAllWalls>(Commands.ZoneHealAllWalls);
            _ = RegisterZoneRequestHandler<ZoneTerraformTest>(Commands.ZoneTerraformTest);
            _ = RegisterZoneRequestHandler<ZoneForceDeconstruct>(Commands.ZoneForceDeconstruct);
            _ = RegisterZoneRequestHandler<ZoneSetReinforceCounter>(Commands.ZoneSetReinforceCounter);
            _ = RegisterZoneRequestHandler<ZoneRestoreOriginalGamma>(Commands.ZoneRestoreOriginalGamma);
            _ = RegisterZoneRequestHandler<ZoneSwitchDegrade>(Commands.ZoneSwitchDegrade);
            _ = RegisterZoneRequestHandler<ZoneKillNPlants>(Commands.ZoneKillNPlants);
            _ = RegisterZoneRequestHandler<ZoneDisplayMissionRandomPoints>(Commands.ZoneDisplayMissionRandomPoints);
            _ = RegisterZoneRequestHandler<ZoneDisplayMissionSpots>(Commands.ZoneDisplayMissionSpots);
            _ = RegisterZoneRequestHandler<NPCCheckCondition>(Commands.NpcCheckCondition);
            _ = RegisterZoneRequestHandler<ZoneClearLayer>(Commands.ZoneClearLayer);
            _ = RegisterZoneRequestHandler<ZonePutPlant>(Commands.ZonePutPlant);
            _ = RegisterZoneRequestHandler<ZoneSetPlantSpeed>(Commands.ZoneSetPlantsSpeed);
            _ = RegisterZoneRequestHandler<ZoneGetPlantsMode>(Commands.ZoneGetPlantsMode);
            _ = RegisterZoneRequestHandler<ZoneSetPlantsMode>(Commands.ZoneSetPlantsMode);
            _ = RegisterZoneRequestHandler<ZoneCreateGarder>(Commands.ZoneCreateGarden);
            _ = RegisterZoneRequestHandler<ZoneCreateIsland>(Commands.ZoneCreateIsland);
            _ = RegisterZoneRequestHandler<ZoneCreateTerraformLimit>(Commands.ZoneCreateTerraformLimit);
            _ = RegisterZoneRequestHandler<ZoneSetLayerWithBitMap>(Commands.ZoneSetLayerWithBitMap);
            _ = RegisterZoneRequestHandler<ZoneDrawBlockingByDefinition>(Commands.ZoneDrawBlockingByDefinition);
            _ = RegisterZoneRequestHandler<ZoneCleanBlockingByDefinition>(Commands.ZoneCleanBlockingByDefinition);
            _ = RegisterZoneRequestHandler<ZoneCleanObstacleBlocking>(Commands.ZoneCleanObstacleBlocking);
            _ = RegisterZoneRequestHandler<ZoneFillGroundTypeRandom>(Commands.ZoneFillGroundTypeRandom);



            _ = RegisterZoneRequestHandler<NpcListSafeSpawnPoint>(Commands.NpcListSafeSpawnPoint);
            _ = RegisterZoneRequestHandler<NpcPlaceSafeSpawnPoint>(Commands.NpcPlaceSafeSpawnPoint);
            _ = RegisterZoneRequestHandler<NpcAddSafeSpawnPoint>(Commands.NpcAddSafeSpawnPoint);
            _ = RegisterZoneRequestHandler<NpcSetSafeSpawnPoint>(Commands.NpcSetSafeSpawnPoint);
            _ = RegisterZoneRequestHandler<NpcDeleteSafeSpawnPoint>(Commands.NpcDeleteSafeSpawnPoint);
            _ = RegisterZoneRequestHandler<ZoneListPresences>(Commands.ZoneListPresences);
            _ = RegisterZoneRequestHandler<ZoneNpcFlockNew>(Commands.ZoneNpcFlockNew);
            _ = RegisterZoneRequestHandler<ZoneNpcFlockSet>(Commands.ZoneNpcFlockSet);
            _ = RegisterZoneRequestHandler<ZoneNpcFlockDelete>(Commands.ZoneNpcFlockDelete);
            _ = RegisterZoneRequestHandler<ZoneNpcFlockKill>(Commands.ZoneNpcFlockKill);
            _ = RegisterZoneRequestHandler<ZoneNpcFlockSetParameter>(Commands.ZoneNpcFlockSetParameter);

            _ = RegisterZoneRequestHandler<GetRifts>(Commands.GetRifts);
            _ = RegisterZoneRequestHandler<UseItem>(Commands.UseItem);
            _ = RegisterZoneRequestHandler<GateSetName>(Commands.GateSetName);

            _ = RegisterZoneRequestHandler<ProximityProbeRemove>(Commands.ProximityProbeRemove);

            _ = RegisterZoneRequestHandler<FieldTerminalInfo>(Commands.FieldTerminalInfo);

            _ = RegisterZoneRequestHandler<PBSFeedableInfo>(Commands.PBSFeedableInfo);
            _ = RegisterZoneRequestHandler<PBSFeedItemsHander>(Commands.PBSFeedItems);
            _ = RegisterZoneRequestHandler<PBSMakeConnection>(Commands.PBSMakeConnection);
            _ = RegisterZoneRequestHandler<PBSBreakConnection>(Commands.PBSBreakConnection);
            _ = RegisterZoneRequestHandler<PBSRenameNode>(Commands.PBSRenameNode);
            _ = RegisterZoneRequestHandler<PBSSetConnectionWeight>(Commands.PBSSetConnectionWeight);
            _ = RegisterZoneRequestHandler<PBSSetOnline>(Commands.PBSSetOnline);
            _ = RegisterZoneRequestHandler<PBSGetNetwork>(Commands.PBSGetNetwork);
            _ = RegisterZoneRequestHandler<PBSCheckDeployment>(Commands.PBSCheckDeployment);
            _ = RegisterZoneRequestHandler<PBSSetStandingLimit>(Commands.PBSSetStandingLimit);
            _ = RegisterZoneRequestHandler<PBSNodeInfo>(Commands.PBSNodeInfo);
            _ = RegisterZoneRequestHandler<PBSGetTerritories>(Commands.PBSGetTerritories);
            _ = RegisterZoneRequestHandler<PBSSetTerritoryVisibility>(Commands.PBSSetTerritoryVisibility);
            _ = RegisterZoneRequestHandler<PBSSetBaseDeconstruct>(Commands.PBSSetBaseDeconstruct);
            _ = RegisterZoneRequestHandler<PBSSetReinforceOffset>(Commands.PBSSetReinforceOffset);
            _ = RegisterZoneRequestHandler<PBSSetEffect>(Commands.PBSSetEffect);
            _ = RegisterZoneRequestHandler<ZoneDrawRamp>(Commands.ZoneDrawRamp);
            _ = RegisterZoneRequestHandler<ZoneSmooth>(Commands.ZoneSmooth);

        }

        private void RegisterPBS()
        {
            _ = _builder.RegisterGeneric(typeof(PBSObjectHelper<>));
            _ = _builder.RegisterGeneric(typeof(PBSReinforceHandler<>));
            _ = _builder.RegisterType<PBSProductionFacilityNodeHelper>();
        }
    }
}