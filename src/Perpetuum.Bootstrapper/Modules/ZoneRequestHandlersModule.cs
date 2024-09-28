using Autofac;
using Perpetuum.RequestHandlers;
using Perpetuum.RequestHandlers.Zone;
using Perpetuum.RequestHandlers.Zone.Containers;
using Perpetuum.RequestHandlers.Zone.MissionRequests;
using Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints;
using Perpetuum.RequestHandlers.Zone.PBS;
using Perpetuum.RequestHandlers.Zone.StatsMapDrawing;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class ZoneRequestHandlersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterZoneRequestHandler<TeleportGetChannelList>(Commands.TeleportGetChannelList);
            builder.RegisterZoneRequestHandler<TeleportToZoneObject>(Commands.TeleportToZoneObject);
            builder.RegisterZoneRequestHandler<TeleportUse>(Commands.TeleportUse);
            builder.RegisterZoneRequestHandler<TeleportQueryWorldChannels>(Commands.TeleportQueryWorldChannels);
            builder.RegisterZoneRequestHandler<JumpAnywhere>(Commands.JumpAnywhere);
            builder.RegisterZoneRequestHandler<MovePlayer>(Commands.MovePlayer);
            builder.RegisterZoneRequestHandler<ZoneDrawStatMap>(Commands.ZoneDrawStatMap);
            builder.RegisterZoneRequestHandler<MissionStartFromZone>(Commands.MissionStartFromZone);
            builder.RegisterZoneRequestHandler<ZoneItemShopBuy>(Commands.ItemShopBuy);
            builder.RegisterZoneRequestHandler<ZoneItemShopList>(Commands.ItemShopList);
            builder.RegisterZoneRequestHandler<ZoneMoveUnit>(Commands.ZoneMoveUnit);
            builder.RegisterZoneRequestHandler<ZoneGetQueueInfo>(Commands.ZoneGetQueueInfo);
            builder.RegisterZoneRequestHandler<ZoneSetQueueLength>(Commands.ZoneSetQueueLength);
            builder.RegisterZoneRequestHandler<ZoneCancelEnterQueue>(Commands.ZoneCancelEnterQueue);
            builder.RegisterZoneRequestHandler<ZoneGetBuildings>(Commands.ZoneGetBuildings);

            builder.RegisterZoneRequestHandler<Dock>(Commands.Dock);

            builder.RegisterZoneRequestHandler<ZoneDecorAdd>(Commands.ZoneDecorAdd);
            builder.RegisterZoneRequestHandler<ZoneDecorSet>(Commands.ZoneDecorSet);
            builder.RegisterZoneRequestHandler<ZoneDecorDelete>(Commands.ZoneDecorDelete);
            builder.RegisterZoneRequestHandler<ZoneDecorLock>(Commands.ZoneDecorLock);
            builder.RegisterZoneRequestHandler<ZoneDrawDecorEnvironment>(Commands.ZoneDrawDecorEnvironment);
            builder.RegisterZoneRequestHandler<ZoneSampleDecorEnvironment>(Commands.ZoneSampleDecorEnvironment);
            builder.RegisterZoneRequestHandler<ZoneDrawDecorEnvByDef>(Commands.ZoneDrawDecorEnvByDef);
            builder.RegisterZoneRequestHandler<ZoneDrawAllDecors>(Commands.ZoneDrawAllDecors);
            builder.RegisterZoneRequestHandler<ZoneEnvironmentDescriptionList>(Commands.ZoneEnvironmentDescriptionList);
            builder.RegisterZoneRequestHandler<ZoneSampleEnvironment>(Commands.ZoneSampleEnvironment);
            builder.RegisterZoneRequestHandler<ZoneCreateTeleportColumn>(Commands.ZoneCreateTeleportColumn);

            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.PackItems>(Commands.PackItems);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnpackItems>(Commands.UnpackItems);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.TrashItems>(Commands.TrashItems);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RelocateItems>(Commands.RelocateItems);
            builder.RegisterZoneRequestHandler<StackItems>(Commands.StackItems);
            builder.RegisterZoneRequestHandler<StackItems>(Commands.StackSelection);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnstackAmount>(Commands.UnstackAmount);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.SetItemName>(Commands.SetItemName);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ListContainer>(Commands.ListContainer);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipModule>(Commands.EquipModule);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RemoveModule>(Commands.RemoveModule);
            builder.RegisterZoneRequestHandler<ChangeModule>(Commands.ChangeModule);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipAmmo>(Commands.EquipAmmo);
            builder.RegisterZoneRequestHandler<UnequipAmmo>(Commands.UnequipAmmo);
            builder.RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ChangeAmmo>(Commands.ChangeAmmo);

            builder.RegisterZoneRequestHandler<MissionGetSupply>(Commands.MissionGetSupply);
            builder.RegisterZoneRequestHandler<MissionSpotPlace>(Commands.MissionSpotPlace);
            builder.RegisterZoneRequestHandler<MissionSpotUpdate>(Commands.MissionSpotUpdate);
            builder.RegisterZoneRequestHandler<ZoneUpdateStructure>(Commands.ZoneUpdateStructure);
            builder.RegisterZoneRequestHandler<RemoveMissionStructure>(Commands.RemoveMissionStructure);
            builder.RegisterZoneRequestHandler<KioskInfo>(Commands.KioskInfo);
            builder.RegisterZoneRequestHandler<KioskSubmitItem>(Commands.KioskSubmitItem);
            builder.RegisterZoneRequestHandler<AlarmStart>(Commands.AlarmStart);
            builder.RegisterZoneRequestHandler<TriggerMissionStructure>(Commands.TriggerMissionStructure);

            builder.RegisterZoneRequestHandler<ZoneUploadScanResult>(Commands.ZoneUploadScanResult);

            //admin
            builder.RegisterZoneRequestHandler<ZoneEntityChangeState>(Commands.ZoneEntityChangeState);
            builder.RegisterZoneRequestHandler<ZoneRemoveByDefinition>(Commands.ZoneRemoveByDefinition);
            builder.RegisterZoneRequestHandler<ZoneMakeGotoXY>(Commands.ZoneMakeGotoXY);
            builder.RegisterZoneRequestHandler<ZoneDrawBeam>(Commands.ZoneDrawBeam);
            builder.RegisterZoneRequestHandler<ZoneSetRuntimeZoneEntityName>(Commands.ZoneSetRuntimeZoneEntityName);
            builder.RegisterZoneRequestHandler<ZoneCheckRoaming>(Commands.ZoneCheckRoaming);
            builder.RegisterZoneRequestHandler<ZonePBSTest>(Commands.ZonePBSTest);
            builder.RegisterZoneRequestHandler<ZonePBSFixOrphaned>(Commands.ZonePBSFixOrphaned);
            builder.RegisterZoneRequestHandler<ZoneFixPBS>(Commands.ZoneFixPBS);
            builder.RegisterZoneRequestHandler<ZoneServerMessage>(Commands.ZoneServerMessage);
            builder.RegisterZoneRequestHandler<ZonePlaceWall>(Commands.ZonePlaceWall);
            builder.RegisterZoneRequestHandler<ZoneClearWalls>(Commands.ZoneClearWalls);
            builder.RegisterZoneRequestHandler<ZoneHealAllWalls>(Commands.ZoneHealAllWalls);
            builder.RegisterZoneRequestHandler<ZoneTerraformTest>(Commands.ZoneTerraformTest);
            builder.RegisterZoneRequestHandler<ZoneForceDeconstruct>(Commands.ZoneForceDeconstruct);
            builder.RegisterZoneRequestHandler<ZoneSetReinforceCounter>(Commands.ZoneSetReinforceCounter);
            builder.RegisterZoneRequestHandler<ZoneRestoreOriginalGamma>(Commands.ZoneRestoreOriginalGamma);
            builder.RegisterZoneRequestHandler<ZoneSwitchDegrade>(Commands.ZoneSwitchDegrade);
            builder.RegisterZoneRequestHandler<ZoneKillNPlants>(Commands.ZoneKillNPlants);
            builder.RegisterZoneRequestHandler<ZoneDisplayMissionRandomPoints>(Commands.ZoneDisplayMissionRandomPoints);
            builder.RegisterZoneRequestHandler<ZoneDisplayMissionSpots>(Commands.ZoneDisplayMissionSpots);
            builder.RegisterZoneRequestHandler<NPCCheckCondition>(Commands.NpcCheckCondition);
            builder.RegisterZoneRequestHandler<ZoneClearLayer>(Commands.ZoneClearLayer);
            builder.RegisterZoneRequestHandler<ZonePutPlant>(Commands.ZonePutPlant);
            builder.RegisterZoneRequestHandler<ZoneSetPlantSpeed>(Commands.ZoneSetPlantsSpeed);
            builder.RegisterZoneRequestHandler<ZoneGetPlantsMode>(Commands.ZoneGetPlantsMode);
            builder.RegisterZoneRequestHandler<ZoneSetPlantsMode>(Commands.ZoneSetPlantsMode);
            builder.RegisterZoneRequestHandler<ZoneCreateGarder>(Commands.ZoneCreateGarden);
            builder.RegisterZoneRequestHandler<ZoneCreateIsland>(Commands.ZoneCreateIsland);
            builder.RegisterZoneRequestHandler<ZoneCreateTerraformLimit>(Commands.ZoneCreateTerraformLimit);
            builder.RegisterZoneRequestHandler<ZoneSetLayerWithBitMap>(Commands.ZoneSetLayerWithBitMap);
            builder.RegisterZoneRequestHandler<ZoneDrawBlockingByDefinition>(Commands.ZoneDrawBlockingByDefinition);
            builder.RegisterZoneRequestHandler<ZoneCleanBlockingByDefinition>(Commands.ZoneCleanBlockingByDefinition);
            builder.RegisterZoneRequestHandler<ZoneCleanObstacleBlocking>(Commands.ZoneCleanObstacleBlocking);
            builder.RegisterZoneRequestHandler<ZoneFillGroundTypeRandom>(Commands.ZoneFillGroundTypeRandom);



            builder.RegisterZoneRequestHandler<NpcListSafeSpawnPoint>(Commands.NpcListSafeSpawnPoint);
            builder.RegisterZoneRequestHandler<NpcPlaceSafeSpawnPoint>(Commands.NpcPlaceSafeSpawnPoint);
            builder.RegisterZoneRequestHandler<NpcAddSafeSpawnPoint>(Commands.NpcAddSafeSpawnPoint);
            builder.RegisterZoneRequestHandler<NpcSetSafeSpawnPoint>(Commands.NpcSetSafeSpawnPoint);
            builder.RegisterZoneRequestHandler<NpcDeleteSafeSpawnPoint>(Commands.NpcDeleteSafeSpawnPoint);
            builder.RegisterZoneRequestHandler<ZoneListPresences>(Commands.ZoneListPresences);
            builder.RegisterZoneRequestHandler<ZoneNpcFlockNew>(Commands.ZoneNpcFlockNew);
            builder.RegisterZoneRequestHandler<ZoneNpcFlockSet>(Commands.ZoneNpcFlockSet);
            builder.RegisterZoneRequestHandler<ZoneNpcFlockDelete>(Commands.ZoneNpcFlockDelete);
            builder.RegisterZoneRequestHandler<ZoneNpcFlockKill>(Commands.ZoneNpcFlockKill);
            builder.RegisterZoneRequestHandler<ZoneNpcFlockSetParameter>(Commands.ZoneNpcFlockSetParameter);

            builder.RegisterZoneRequestHandler<GetRifts>(Commands.GetRifts);
            builder.RegisterZoneRequestHandler<UseItem>(Commands.UseItem);
            builder.RegisterZoneRequestHandler<GateSetName>(Commands.GateSetName);

            builder.RegisterZoneRequestHandler<ProximityProbeRemove>(Commands.ProximityProbeRemove);

            builder.RegisterZoneRequestHandler<FieldTerminalInfo>(Commands.FieldTerminalInfo);

            builder.RegisterZoneRequestHandler<PBSFeedableInfo>(Commands.PBSFeedableInfo);
            builder.RegisterZoneRequestHandler<PBSFeedItemsHander>(Commands.PBSFeedItems);
            builder.RegisterZoneRequestHandler<PBSMakeConnection>(Commands.PBSMakeConnection);
            builder.RegisterZoneRequestHandler<PBSBreakConnection>(Commands.PBSBreakConnection);
            builder.RegisterZoneRequestHandler<PBSRenameNode>(Commands.PBSRenameNode);
            builder.RegisterZoneRequestHandler<PBSSetConnectionWeight>(Commands.PBSSetConnectionWeight);
            builder.RegisterZoneRequestHandler<PBSSetOnline>(Commands.PBSSetOnline);
            builder.RegisterZoneRequestHandler<PBSGetNetwork>(Commands.PBSGetNetwork);
            builder.RegisterZoneRequestHandler<PBSCheckDeployment>(Commands.PBSCheckDeployment);
            builder.RegisterZoneRequestHandler<PBSSetStandingLimit>(Commands.PBSSetStandingLimit);
            builder.RegisterZoneRequestHandler<PBSNodeInfo>(Commands.PBSNodeInfo);
            builder.RegisterZoneRequestHandler<PBSGetTerritories>(Commands.PBSGetTerritories);
            builder.RegisterZoneRequestHandler<PBSSetTerritoryVisibility>(Commands.PBSSetTerritoryVisibility);
            builder.RegisterZoneRequestHandler<PBSSetBaseDeconstruct>(Commands.PBSSetBaseDeconstruct);
            builder.RegisterZoneRequestHandler<PBSSetReinforceOffset>(Commands.PBSSetReinforceOffset);
            builder.RegisterZoneRequestHandler<PBSSetEffect>(Commands.PBSSetEffect);
            builder.RegisterZoneRequestHandler<ZoneDrawRamp>(Commands.ZoneDrawRamp);
            builder.RegisterZoneRequestHandler<ZoneSmooth>(Commands.ZoneSmooth);

            builder.RegisterZoneRequestHandler<GetRobotInfo>(Commands.GetRobotFittingInfo);
        }
    }
}
