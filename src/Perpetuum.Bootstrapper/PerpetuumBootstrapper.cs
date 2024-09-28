using Autofac;
using Autofac.Builder;
using Newtonsoft.Json;
using Open.Nat;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Bootstrapper.Modules;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.GenXY;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Loggers;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host;
using Perpetuum.Host.Requests;
using Perpetuum.IO;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.Channels.ChatCommands;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.HighScores;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionBonusObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Social;
using Perpetuum.Services.Sparks;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Services.Standing;
using Perpetuum.Services.Steam;
using Perpetuum.Services.TechTree;
using Perpetuum.Services.Trading;
using Perpetuum.Threading.Process;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Presences.InterzonePresences;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Terraforming;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using LogEvent = Perpetuum.Log.LogEvent;

namespace Perpetuum.Bootstrapper
{
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

            foreach (Command command in CommandsModule.GetCommands().OrderBy(c => c.Text))
            {
                _ = sb.AppendLine($"{command.Text},{command.AccessLevel}");
            }

            File.WriteAllText(path, sb.ToString());
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

        private void InitContainer(string gameRoot)
        {
            _builder.RegisterModule(new CommandsModule());
            _builder.RegisterModule(new RequestHandlersModule());
            _builder.RegisterModule(new ZoneRequestHandlersModule());
            _builder.RegisterModule(new AutoActivatedTypesModule());
            _builder.RegisterModule(new LoggersModule());
            _builder.RegisterModule(new EntitiesModule());
            _builder.RegisterModule(new RobotTemplatesModule());
            _builder.RegisterModule(new MissionsModule());
            _builder.RegisterModule(new TerrainsModule());
            _builder.RegisterModule(new NpcsModule());
            _builder.RegisterModule(new ChannelTypesModule());
            _builder.RegisterModule(new MtProductsModule());
            _builder.RegisterModule(new RiftsModule());
            _builder.RegisterModule(new RelicsModule());
            _builder.RegisterModule(new EffectsModule());
            _builder.RegisterModule(new IntrusionsModule());
            _builder.RegisterModule(new ZonesModule());
            _builder.RegisterModule(new PbsModule());

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
                string connectionString = x.Resolve<GlobalConfiguration>().ConnectionString;
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

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterZoneRequestHandler<T>(Command command) where T : IRequestHandler<IZoneRequest>
        {
            return RegisterRequestHandler<T, IZoneRequest>(command);
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterAutoActivate<T>(
            TimeSpan interval)
            where T : IProcess
        {
            return _builder.RegisterType<T>().SingleInstance().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(interval));
            });
        }
    }
}