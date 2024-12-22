using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Items.Helpers;
using Perpetuum.Modules;
using Perpetuum.Modules.AdaptiveAlloy;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.RemoteControl;
using Perpetuum.Modules.Terraforming;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Services.ProductionEngine.ResearchKits;
using Perpetuum.Services.Relics;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.TechTree;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.LandMines;
using Perpetuum.Zones.NpcSystem;
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
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Training;
using System;
using System.Collections.Generic;
using System.Linq;
using Module = Autofac.Module;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class EntitiesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ItemHelper>();
            builder.RegisterType<ContainerHelper>();

            builder.RegisterType<EntityDefaultReader>().As<IEntityDefaultReader>().SingleInstance().OnActivated(e => e.Instance.Init());
            builder.RegisterType<EntityRepository>().As<IEntityRepository>();

            builder.RegisterType<ModulePropertyModifiersReader>().OnActivated(e => e.Instance.Init()).SingleInstance();

            builder.RegisterType<LootItemRepository>().As<ILootItemRepository>();
            builder.RegisterType<CoreRecharger>().As<ICoreRecharger>();
            builder.RegisterType<UnitHelper>();
            builder.RegisterType<DockingBaseHelper>();

            builder.RegisterType<EntityFactory>().AsSelf().As<IEntityFactory>();

            InitItems(builder);

            RegisterRobot<Npc>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<Player>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<SentryTurret>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<IndustrialTurret>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<CombatDrone>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<IndustrialDrone>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<SupportDrone>(builder).OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<PBSTurret>(builder);
            RegisterRobot<PunchBag>(builder);

            builder.RegisterType<EntityAggregateServices>().As<IEntityServices>().PropertiesAutowired().SingleInstance();


            RegisterEntity<Entity>(builder);
            RegisterCorporation<DefaultCorporation>(builder);
            RegisterCorporation<PrivateCorporation>(builder);
            RegisterEntity<PrivateAlliance>(builder);
            RegisterEntity<DefaultAlliance>(builder);

            RegisterEntity<RobotHead>(builder);
            RegisterEntity<RobotChassis>(builder);
            RegisterEntity<RobotLeg>(builder);
            RegisterUnit<DockingBase>(builder);
            RegisterUnit<PBSDockingBase>(builder);
            RegisterUnit<ExpiringPBSDockingBase>(builder);
            RegisterUnit<Outpost>(builder).OnActivated(e =>
            {
#if DEBUG
                TimeRange intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
#else
                TimeRange intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromHours(8), TimeSpan.FromHours(8));
#endif
                e.Instance.IntrusionWaitTime = intrusionWaitTime;
            });
            RegisterUnit<TrainingDockingBase>(builder);
            RegisterUnit<ItemShop>(builder);

            RegisterEntity<PublicCorporationHangarStorage>(builder);
            RegisterEntity<CalibrationProgram>(builder);
            RegisterEntity<DynamicCalibrationProgram>(builder);
            RegisterEntity<RandomCalibrationProgram>(builder);
            RegisterEntity<CalibrationProgramCapsule>(builder); // OPP: new CT Capsule item

            RegisterProductionFacility<Mill>(builder);
            RegisterProductionFacility<Prototyper>(builder);
            RegisterProductionFacility<OutpostMill>(builder);
            RegisterProductionFacility<OutpostPrototyper>(builder);
            RegisterProductionFacility<OutpostRefinery>(builder);
            RegisterProductionFacility<OutpostRepair>(builder);
            RegisterProductionFacility<OutpostReprocessor>(builder);
            RegisterProductionFacility<PBSMillFacility>(builder);
            RegisterProductionFacility<PBSPrototyperFacility>(builder);
            RegisterProductionFacility<ResearchLab>(builder);
            RegisterProductionFacility<OutpostResearchLab>(builder);
            RegisterProductionFacility<PBSResearchLabFacility>(builder);
            RegisterProductionFacility<Refinery>(builder);
            RegisterProductionFacility<Reprocessor>(builder);
            RegisterProductionFacility<Repair>(builder);
            RegisterProductionFacility<InsuraceFacility>(builder);
            RegisterProductionFacility<PBSResearchKitForgeFacility>(builder);
            RegisterProductionFacility<PBSCalibrationProgramForgeFacility>(builder);
            RegisterProductionFacility<PBSRefineryFacility>(builder);
            RegisterProductionFacility<PBSRepairFacility>(builder);
            RegisterProductionFacility<PBSReprocessorFacility>(builder);

            RegisterEntity<ResearchKit>(builder);
            RegisterEntity<RandomResearchKit>(builder);
            RegisterEntity<Market>(builder);
            RegisterEntity<LotteryItem>(builder);

            RegisterProximityDevices<ProximityProbe>(builder);
            RegisterProximityDevices<LandMine>(builder);

            RegisterUnit<TeleportColumn>(builder);
            RegisterUnit<LootContainer>(builder).OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<FieldContainer>(builder).OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromHours(1)));
            RegisterUnit<MissionContainer>(builder).OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<ActiveHackingSAP>(builder);
            RegisterUnit<PassiveHackingSAP>(builder);
            RegisterUnit<DestructionSAP>(builder);
            RegisterUnit<SpecimenProcessingSAP>(builder);
            RegisterUnit<MobileTeleport>(builder);
            RegisterUnit<NpcEgg>(builder);

            RegisterEntity<FieldContainerCapsule>(builder);
            RegisterEntity<Ice>(builder);
            RegisterEntity<RespecToken>(builder);
            RegisterEntity<SparkTeleportDevice>(builder);
            RegisterEntity<ServerWideEpBooster>(builder);
            RegisterEntity<Ammo>(builder);
            RegisterEntity<WeaponAmmo>(builder);
            RegisterEntity<RemoteControlledUnit>(builder);
            RegisterEntity<RemoteCommand>(builder);
            RegisterEntity<MiningAmmo>(builder);
            RegisterEntity<TileScannerAmmo>(builder);
            RegisterEntity<OneTileScannerAmmo>(builder);
            RegisterEntity<ArtifactScannerAmmo>(builder);
            RegisterEntity<IntrusionScannerAmmo>(builder);
            RegisterEntity<DirectionalScannerAmmo>(builder);
            RegisterEntity<DefaultSystemContainer>(builder);
            RegisterEntity<PublicContainer>(builder);
            RegisterEntity<RobotInventory>(builder);
            RegisterEntity<InfiniteBoxContainer>(builder);
            RegisterEntity<LimitedBoxContainer>(builder);
            RegisterEntity<CorporateHangar>(builder);
            RegisterEntity<CorporateHangarFolder>(builder);
            RegisterEntity<MobileTeleportDeployer>(builder);
            RegisterEntity<PlantSeedDeployer>(builder);
            RegisterEntity<PlantSeedDeployer>(builder);
            RegisterEntity<RiftActivator>(builder);
            RegisterEntity<MineralScanResultItem>(builder);

            RegisterModule<DrillerModule>(builder);
            RegisterModule<LargeDrillerModule>(builder);
            RegisterModule<RemoteControlledDrillerModule>(builder);
            RegisterModule<RemoteControlledHarvesterModule>(builder);
            RegisterModule<HarvesterModule>(builder);
            RegisterModule<Perpetuum.Modules.Module>(builder);
            RegisterModule<WeaponModule>(builder);
            RegisterModule<FirearmWeaponModule>(builder); // OPP: new subclass for firearms
            RegisterModule<MissileWeaponModule>(builder);
            RegisterModule<ArmorRepairModule>(builder);
            RegisterModule<RemoteArmorRepairModule>(builder);
            RegisterModule<CoreBoosterModule>(builder);
            RegisterModule<SensorJammerModule>(builder);
            RegisterModule<EnergyNeutralizerModule>(builder);
            RegisterModule<ScorcherModule>(builder);
            RegisterModule<EnergyTransfererModule>(builder);
            RegisterModule<EnergyVampireModule>(builder);
            RegisterModule<GeoScannerModule>(builder);
            RegisterModule<UnitScannerModule>(builder);
            RegisterModule<ContainerScannerModule>(builder);
            RegisterModule<SiegeHackModule>(builder);
            RegisterModule<NeuralyzerModule>(builder);
            RegisterModule<BlobEmissionModulatorModule>(builder);
            RegisterModule<TacticalRemoteControllerModule>(builder);
            RegisterModule<AssaultRemoteControllerModule>(builder);
            RegisterModule<IndustrialRemoteControllerModule>(builder);
            RegisterModule<SupportRemoteControllerModule>(builder);
            RegisterModule<TerraformMultiModule>(builder);
            RegisterModule<WallBuilderModule>(builder);
            RegisterModule<ConstructionModule>(builder);

            RegisterModule<AdaptiveAlloyModule>(builder);

            RegisterEffectModule<WebberModule>(builder);
            RegisterEffectModule<SensorDampenerModule>(builder);
            RegisterEffectModule<RemoteSensorBoosterModule>(builder);
            RegisterEffectModule<TargetPainterModule>(builder);
            RegisterEffectModule<TargetBlinderModule>(builder); //OPP: NPC-only module for detection debuff
            RegisterEffectModule<SensorBoosterModule>(builder);
            RegisterEffectModule<ArmorHardenerModule>(builder);
            RegisterEffectModule<StealthModule>(builder);
            RegisterEffectModule<DetectionModule>(builder);
            RegisterEffectModule<GangModule>(builder);
            RegisterEffectModule<NoxModule>(builder);
            RegisterEffectModule<ShieldGeneratorModule>(builder);
            RegisterEffectModule<MineDetectorModule>(builder);

            RegisterEffectModule<DreadnoughtModule>(builder);
            RegisterEffectModule<ExcavatorModule>(builder);

            RegisterEffectModule<RemoteCommandTranslatorModule>(builder);

            RegisterEntity<SystemContainer>(builder);
            RegisterEntity<PunchBagDeployer>(builder);

            RegisterUnit<BlobEmitterUnit>(builder);
            RegisterUnit<Kiosk>(builder);
            RegisterUnit<AlarmSwitch>(builder);
            RegisterUnit<SimpleSwitch>(builder);
            RegisterUnit<ItemSupply>(builder);
            RegisterUnit<MobileWorldTeleport>(builder);
            RegisterUnit<MobileStrongholdTeleport>(builder); // OPP: New mobile tele for entry to Strongholds
            RegisterUnit<AreaBomb>(builder);
            RegisterUnit<PBSEgg>(builder);
            RegisterPBSObject<PBSReactor>(builder);
            RegisterPBSObject<PBSCoreTransmitter>(builder);
            RegisterUnit<WallHealer>(builder);
            RegisterPBSProductionFacilityNode<PBSResearchLabEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSRepairEnablerNode>(builder);
            RegisterPBSObject<PBSFacilityUpgradeNode>(builder);
            RegisterPBSProductionFacilityNode<PBSReprocessEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSMillEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSRefineryEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSPrototyperEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSCalibrationProgramForgeEnablerNode>(builder);
            RegisterPBSProductionFacilityNode<PBSResearchKitForgeEnablerNode>(builder);
            RegisterPBSObject<PBSEffectSupplier>(builder);
            RegisterPBSObject<PBSEffectEmitter>(builder);
            RegisterPBSObject<PBSMiningTower>(builder);
            RegisterPBSObject<PBSArmorRepairerNode>(builder);
            RegisterPBSObject<PBSControlTower>(builder);
            RegisterPBSObject<PBSEnergyWell>(builder);
            RegisterPBSObject<PBSHighwayNode>(builder);
            RegisterUnit<FieldTerminal>(builder);
            RegisterUnit<Rift>(builder);
            RegisterUnit<TrainingKillSwitch>(builder);
            RegisterUnit<Gate>(builder);
            RegisterUnit<RandomRiftPortal>(builder);
            RegisterUnit<StrongholdEntryRift>(builder); // OPP: Special rift spawned eventfully to transport player to location
            RegisterUnit<StrongholdExitRift>(builder); // OPP: Special rift for exiting strongholds

            RegisterEntity<Item>(builder);
            RegisterEntity<AreaBombDeployer>(builder);

            RegisterEntity<ProximityProbeDeployer>(builder);
            RegisterEntity<LandMineDeployer>(builder);

            RegisterEntity<PBSDeployer>(builder);
            RegisterEntity<WallHealerDeployer>(builder);
            RegisterEntity<VolumeWrapperContainer>(builder);
            RegisterEntity<Kernel>(builder);
            RegisterEntity<RandomMissionItem>(builder);
            RegisterEntity<Trashcan>(builder);
            RegisterEntity<ZoneStorage>(builder);
            RegisterEntity<PunchBagDeployer>(builder);
            RegisterEntity<PlantSeedDeployer>(builder);
            RegisterEntity<GateDeployer>(builder);
            RegisterEntity<ExtensionPointActivator>(builder);
            RegisterEntity<CreditActivator>(builder);
            RegisterEntity<SparkActivator>(builder);
            RegisterEntity<Gift>(builder);
            RegisterEntity<Paint>(builder); // OPP: Robot paint item
            RegisterEntity<EPBoost>(builder);
            RegisterEntity<Relic>(builder);
            RegisterEntity<SAPRelic>(builder);

            builder.Register<Func<EntityDefault, Entity>>(x =>
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
                ByCategoryFlags<MiningAmmo>(CategoryFlags.cf_deep_mining_ammo);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_assault_drones_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_attack_drones_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_industrial_drones_units);
                ByCategoryFlags<RemoteControlledUnit>(CategoryFlags.cf_support_drones_units);
                ByCategoryFlags<RemoteCommand>(CategoryFlags.cf_remote_commands);
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


                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_robot_equipment);
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
                ByCategoryFlags<ScorcherModule>(CategoryFlags.cf_scorchers);
                ByCategoryFlags<EnergyTransfererModule>(CategoryFlags.cf_energy_transferers);
                ByCategoryFlags<EnergyVampireModule>(CategoryFlags.cf_energy_vampires);
                ByCategoryFlags<DrillerModule>(CategoryFlags.cf_small_drillers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_mining_ammo));
                ByCategoryFlags<DrillerModule>(CategoryFlags.cf_medium_drillers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_mining_ammo));
                ByCategoryFlags<LargeDrillerModule>(CategoryFlags.cf_large_drillers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_deep_mining_ammo));
                ByCategoryFlags<RemoteControlledDrillerModule>(CategoryFlags.cf_industrial_turret_drillers);
                ByCategoryFlags<RemoteControlledHarvesterModule>(CategoryFlags.cf_industrial_turret_harvesters);
                ByCategoryFlags<HarvesterModule>(CategoryFlags.cf_harvesters, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_harvesting_ammo));
                ByCategoryFlags<GeoScannerModule>(CategoryFlags.cf_mining_probes, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_mining_probe_ammo));
                ByCategoryFlags<UnitScannerModule>(CategoryFlags.cf_chassis_scanner);
                ByCategoryFlags<ContainerScannerModule>(CategoryFlags.cf_cargo_scanner);
                ByCategoryFlags<SiegeHackModule>(CategoryFlags.cf_siege_hack_modules);
                ByCategoryFlags<NeuralyzerModule>(CategoryFlags.cf_neuralyzer);
                ByCategoryFlags<BlobEmissionModulatorModule>(CategoryFlags.cf_blob_emission_modulator, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_blob_emission_modulator_ammo));
                ByCategoryFlags<TacticalRemoteControllerModule>(CategoryFlags.cf_tactical_remote_controllers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_attack_drones_units));
                ByCategoryFlags<AssaultRemoteControllerModule>(CategoryFlags.cf_assault_remote_controllers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_assault_drones_units));
                ByCategoryFlags<IndustrialRemoteControllerModule>(CategoryFlags.cf_industrial_remote_controllers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_industrial_drones_units));
                ByCategoryFlags<SupportRemoteControllerModule>(CategoryFlags.cf_support_remote_controllers, new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_support_drones_units));
                ByCategoryFlags<WebberModule>(CategoryFlags.cf_webber);
                ByCategoryFlags<SensorDampenerModule>(CategoryFlags.cf_sensor_dampeners);
                ByCategoryFlags<RemoteSensorBoosterModule>(CategoryFlags.cf_remote_sensor_boosters);
                ByCategoryFlags<TargetPainterModule>(CategoryFlags.cf_target_painter);
                ByCategoryFlags<SensorBoosterModule>(CategoryFlags.cf_sensor_boosters);
                ByCategoryFlags<ArmorHardenerModule>(CategoryFlags.cf_armor_hardeners);
                ByCategoryFlags<StealthModule>(CategoryFlags.cf_stealth_modules);
                ByCategoryFlags<DetectionModule>(CategoryFlags.cf_detection_modules);
                ByCategoryFlags<MineDetectorModule>(CategoryFlags.cf_landmine_detectors);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_armor_plates);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_core_batteries);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_core_rechargers);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_maneuvering_equipment);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_powergrid_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_cpu_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_mining_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_massmodifiers);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_weapon_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_tracking_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_armor_repair_upgrades);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_kers);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_shield_hardener);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_eccm);
                ByCategoryFlags<Perpetuum.Modules.Module>(CategoryFlags.cf_resistance_plating);

                ByCategoryFlags<AdaptiveAlloyModule>(CategoryFlags.cf_adaptive_alloys);

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

                ByCategoryFlags<NoxModule>(CategoryFlags.cf_nox_shield_negators, new NamedParameter("effectType", EffectType.nox_effect_shield_negation), new NamedParameter("effectModifier", AggregateField.nox_shield_absorbtion_modifier));
                ByCategoryFlags<NoxModule>(CategoryFlags.cf_nox_repair_negators, new NamedParameter("effectType", EffectType.nox_effect_repair_negation), new NamedParameter("effectModifier", AggregateField.nox_repair_amount_modifier));
                ByCategoryFlags<NoxModule>(CategoryFlags.cf_nox_teleport_negators, new NamedParameter("effectType", EffectType.nox_effect_teleport_negation), new NamedParameter("effectModifier", AggregateField.nox_teleport_negation));

                ByCategoryFlags<DreadnoughtModule>(CategoryFlags.cf_dreadnought_modules);
                ByCategoryFlags<ExcavatorModule>(CategoryFlags.cf_excavator_modules);

                ByCategoryFlags<RemoteCommandTranslatorModule>(
                    CategoryFlags.cf_remote_command_translators,
                    new NamedParameter("ammoCategoryFlags", CategoryFlags.cf_remote_commands));

                ByCategoryFlags<SystemContainer>(CategoryFlags.cf_logical_storage);
                ByCategoryFlags<Item>(CategoryFlags.cf_mission_items);
                ByCategoryFlags<Item>(CategoryFlags.cf_robotshards);
                ByCategoryFlags<PunchBagDeployer>(CategoryFlags.cf_others);
                ByCategoryFlags<BlobEmitterUnit>(CategoryFlags.cf_blob_emitter);
                ByCategoryFlags<SentryTurret>(CategoryFlags.cf_sentry_turrets);
                ByCategoryFlags<IndustrialTurret>(CategoryFlags.cf_mining_turrets);
                ByCategoryFlags<IndustrialTurret>(CategoryFlags.cf_harvesting_turrets);
                ByCategoryFlags<CombatDrone>(CategoryFlags.cf_combat_drones);
                ByCategoryFlags<CombatDrone>(CategoryFlags.cf_assault_drones);
                ByCategoryFlags<CombatDrone>(CategoryFlags.cf_attack_drones);
                ByCategoryFlags<SupportDrone>(CategoryFlags.cf_support_drones);
                ByCategoryFlags<IndustrialDrone>(CategoryFlags.cf_industrial_drones);
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
                ByCategoryFlags<ServerWideEpBooster>(CategoryFlags.cf_server_wide_ep_boosters);

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

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterEntity<T>(
            ContainerBuilder builder)
            where T : Entity
        {
            return builder.RegisterType<T>().OnActivated(e =>
            {
                e.Instance.EntityServices = e.Context.Resolve<IEntityServices>();
            });
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterUnit<T>(
            ContainerBuilder builder)
            where T : Unit
        {
            return RegisterEntity<T>(builder).PropertiesAutowired();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterPBSObject<T>(ContainerBuilder builder) where T : PBSObject
        {
            return RegisterUnit<T>(builder).OnActivated(e =>
            {
                e.Instance.SetReinforceHandler(e.Context.Resolve<PBSReinforceHandler<PBSObject>>(new TypedParameter(typeof(PBSObject), e.Instance)));
                e.Instance.SetPBSObjectHelper(e.Context.Resolve<PBSObjectHelper<PBSObject>>(new TypedParameter(typeof(PBSObject), e.Instance)));
            });
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterPBSProductionFacilityNode<T>(ContainerBuilder builder) where T : PBSProductionFacilityNode
        {
            return RegisterPBSObject<T>(builder).OnActivated(e =>
            {
                e.Instance.ProductionManager = e.Context.Resolve<ProductionManager>();
                e.Instance.SetProductionFacilityNodeHelper(e.Context.Resolve<PBSProductionFacilityNodeHelper>(new TypedParameter(typeof(PBSProductionFacilityNode), e.Instance)));
            });
        }

        protected void RegisterCorporation<T>(ContainerBuilder builder) where T : Corporation
        {
            _ = builder.RegisterType<CorporationTransactionLogger>();
            _ = RegisterEntity<T>(builder).PropertiesAutowired();
        }

        protected void RegisterProximityDevices<T>(ContainerBuilder builder) where T : ProximityDeviceBase
        {
            _ = RegisterUnit<T>(builder);
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterModule<T>(
            ContainerBuilder builder)
            where T : Perpetuum.Modules.Module
        {
            return RegisterEntity<T>(builder);
        }

        private void RegisterEffectModule<T>(ContainerBuilder builder) where T : EffectModule
        {
            _ = RegisterModule<T>(builder);
        }

        private void RegisterProductionFacility<T>(ContainerBuilder builder) where T : ProductionFacility
        {
            _ = RegisterEntity<T>(builder).PropertiesAutowired();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRobot<T>(
            ContainerBuilder builder)
            where T : Robot
        {
            return RegisterUnit<T>(builder);
        }

        private void InitItems(ContainerBuilder builder)
        {
            _ = builder.RegisterType<ItemDeployerHelper>();
            _ = builder.RegisterType<DefaultPropertyModifierReader>().AsSelf().OnActivated(e => e.Instance.Init()).SingleInstance();
        }

    }
}
