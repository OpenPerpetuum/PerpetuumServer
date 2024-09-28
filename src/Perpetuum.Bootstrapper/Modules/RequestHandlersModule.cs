using Autofac;
using Autofac.Builder;
using Perpetuum.Host.Requests;
using Perpetuum.RequestHandlers;
using Perpetuum.RequestHandlers.AdminTools;
using Perpetuum.RequestHandlers.Characters;
using Perpetuum.RequestHandlers.Corporations;
using Perpetuum.RequestHandlers.Corporations.YellowPages;
using Perpetuum.RequestHandlers.Extensions;
using Perpetuum.RequestHandlers.FittingPreset;
using Perpetuum.RequestHandlers.Gangs;
using Perpetuum.RequestHandlers.Intrusion;
using Perpetuum.RequestHandlers.Mails;
using Perpetuum.RequestHandlers.Markets;
using Perpetuum.RequestHandlers.Production;
using Perpetuum.RequestHandlers.RobotTemplates;
using Perpetuum.RequestHandlers.Socials;
using Perpetuum.RequestHandlers.Sparks;
using Perpetuum.RequestHandlers.Standings;
using Perpetuum.RequestHandlers.TechTree;
using Perpetuum.RequestHandlers.Trades;
using Perpetuum.RequestHandlers.TransportAssignments;
using Perpetuum.RequestHandlers.Zone;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class RequestHandlersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterGeneric(typeof(RequestHandlerProfiler<>));

            RegisterRequestHandlerFactory<IRequest>(builder);
            RegisterRequestHandlerFactory<IZoneRequest>(builder);

            _ = RegisterRequestHandler<GetEnums>(builder, Commands.GetEnums);
            _ = RegisterRequestHandler<GetCommands>(builder, Commands.GetCommands);
            _ = RegisterRequestHandler<GetEntityDefaults>(builder, Commands.GetEntityDefaults).SingleInstance();
            _ = RegisterRequestHandler<GetAggregateFields>(builder, Commands.GetAggregateFields).SingleInstance();
            _ = RegisterRequestHandler<GetDefinitionConfigUnits>(builder, Commands.GetDefinitionConfigUnits).SingleInstance();
            _ = RegisterRequestHandler<GetEffects>(builder, Commands.GetEffects).SingleInstance();
            _ = RegisterRequestHandler<GetDistances>(builder, Commands.GetDistances);
            _ = RegisterRequestHandler<SignIn>(builder, Commands.SignIn);
            _ = RegisterRequestHandler<SignInSteam>(builder, Commands.SignInSteam);
            _ = RegisterRequestHandler<SignOut>(builder, Commands.SignOut);
            _ = RegisterRequestHandler<SteamListAccounts>(builder, Commands.SteamListAccounts);
            _ = RegisterRequestHandler<AccountConfirmEmail>(builder, Commands.AccountConfirmEmail);
            _ = RegisterRequestHandler<CharacterList>(builder, Commands.CharacterList);
            _ = RegisterRequestHandler<CharacterCreate>(builder, Commands.CharacterCreate);
            _ = RegisterRequestHandler<CharacterSelect>(builder, Commands.CharacterSelect);
            _ = RegisterRequestHandler<CharacterDeselect>(builder, Commands.CharacterDeselect);
            _ = RegisterRequestHandler<CharacterForceDeselect>(builder, Commands.CharacterForceDeselect);
            _ = RegisterRequestHandler<CharacterForceDisconnect>(builder, Commands.CharacterForceDisconnect);
            _ = RegisterRequestHandler<CharacterDelete>(builder, Commands.CharacterDelete);
            _ = RegisterRequestHandler<CharacterSetHomeBase>(builder, Commands.CharacterSetHomeBase);
            _ = RegisterRequestHandler<CharacterGetProfiles>(builder, Commands.CharacterGetProfiles);
            _ = RegisterRequestHandler<CharacterRename>(builder, Commands.CharacterRename);
            _ = RegisterRequestHandler<CharacterCheckNick>(builder, Commands.CharacterCheckNick);
            _ = RegisterRequestHandler<CharacterCorrectNick>(builder, Commands.CharacterCorrectNick);
            _ = RegisterRequestHandler<CharacterIsOnline>(builder, Commands.IsOnline);
            _ = RegisterRequestHandler<CharacterSettingsSet>(builder, Commands.CharacterSettingsSet);
            _ = RegisterRequestHandler<CharacterSetMoodMessage>(builder, Commands.CharacterSetMoodMessage);
            _ = RegisterRequestHandler<CharacterTransferCredit>(builder, Commands.CharacterTransferCredit);
            _ = RegisterRequestHandler<CharacterSetAvatar>(builder, Commands.CharacterSetAvatar);
            _ = RegisterRequestHandler<CharacterSetBlockTrades>(builder, Commands.CharacterSetBlockTrades);
            _ = RegisterRequestHandler<CharacterSetCredit>(builder, Commands.CharacterSetCredit);
            _ = RegisterRequestHandler<CharacterClearHomeBase>(builder, Commands.CharacterClearHomeBase);
            _ = RegisterRequestHandler<CharacterSettingsGet>(builder, Commands.CharacterSettingsGet);
            _ = RegisterRequestHandler<CharacterGetMyProfile>(builder, Commands.CharacterGetMyProfile);
            _ = RegisterRequestHandler<CharacterSearch>(builder, Commands.CharacterSearch);
            _ = RegisterRequestHandler<CharacterRemoveFromCache>(builder, Commands.CharacterRemoveFromCache);
            _ = RegisterRequestHandler<CharacterListNpcDeath>(builder, Commands.CharacterListNpcDeath);
            _ = RegisterRequestHandler<CharacterTransactionHistory>(builder, Commands.CharacterTransactionHistory);
            _ = RegisterRequestHandler<CharacterGetZoneInfo>(builder, Commands.CharacterGetZoneInfo);
            _ = RegisterRequestHandler<CharacterNickHistory>(builder, Commands.CharacterNickHistory);
            _ = RegisterRequestHandler<CharacterGetNote>(builder, Commands.CharacterGetNote);
            _ = RegisterRequestHandler<CharacterSetNote>(builder, Commands.CharacterSetNote);
            _ = RegisterRequestHandler<CharacterCorporationHistory>(builder, Commands.CharacterCorporationHistory);
            _ = RegisterRequestHandler<CharacterWizardData>(builder, Commands.CharacterWizardData).SingleInstance();
            _ = RegisterRequestHandler<CharactersOnline>(builder, Commands.GetCharactersOnline);
            _ = RegisterRequestHandler<ReimburseItemRequestHandler>(builder, Commands.ReimburseItem);
            _ = RegisterRequestHandler<Chat>(builder, Commands.Chat);
            _ = RegisterRequestHandler<GoodiePackList>(builder, Commands.GoodiePackList);
            _ = RegisterRequestHandler<GoodiePackRedeem>(builder, Commands.GoodiePackRedeem);
            _ = RegisterRequestHandler<Ping>(builder, Commands.Ping);
            _ = RegisterRequestHandler<Quit>(builder, Commands.Quit);
            _ = RegisterRequestHandler<SetMaxUserCount>(builder, Commands.SetMaxUserCount);
            _ = RegisterRequestHandler<SparkTeleportSet>(builder, Commands.SparkTeleportSet);
            _ = RegisterRequestHandler<SparkTeleportUse>(builder, Commands.SparkTeleportUse);
            _ = RegisterRequestHandler<SparkTeleportDelete>(builder, Commands.SparkTeleportDelete);
            _ = RegisterRequestHandler<SparkTeleportList>(builder, Commands.SparkTeleportList);
            _ = RegisterRequestHandler<SparkChange>(builder, Commands.SparkChange);
            _ = RegisterRequestHandler<SparkRemove>(builder, Commands.SparkRemove);
            _ = RegisterRequestHandler<SparkList>(builder, Commands.SparkList);
            _ = RegisterRequestHandler<SparkSetDefault>(builder, Commands.SparkSetDefault);
            _ = RegisterRequestHandler<SparkUnlock>(builder, Commands.SparkUnlock);
            _ = RegisterRequestHandler<Undock>(builder, Commands.Undock)
                ;
            _ = RegisterRequestHandler<ProximityProbeRegisterSet>(builder, Commands.ProximityProbeRegisterSet);
            _ = RegisterRequestHandler<ProximityProbeSetName>(builder, Commands.ProximityProbeSetName);
            _ = RegisterRequestHandler<ProximityProbeList>(builder, Commands.ProximityProbeList);
            _ = RegisterRequestHandler<ProximityProbeGetRegistrationInfo>(builder, Commands.ProximityProbeGetRegistrationInfo);

            _ = RegisterRequestHandler<IntrusionEnabler>(builder, Commands.IntrusionEnabler);
            _ = RegisterRequestHandler<AccountGetTransactionHistory>(builder, Commands.AccountGetTransactionHistory);
            _ = RegisterRequestHandler<AccountList>(builder, Commands.AccountList);

            _ = RegisterRequestHandler<AccountEpForActivityHistory>(builder, Commands.AccountEpForActivityHistory);
            _ = RegisterRequestHandler<RedeemableItemList>(builder, Commands.RedeemableItemList);
            _ = RegisterRequestHandler<RedeemableItemRedeem>(builder, Commands.RedeemableItemRedeem);
            _ = RegisterRequestHandler<RedeemableItemActivate>(builder, Commands.RedeemableItemActivate);
            _ = RegisterRequestHandler<CreateItemRequestHandler>(builder, Commands.CreateItem);
            _ = RegisterRequestHandler<TeleportList>(builder, Commands.TeleportList);
            _ = RegisterRequestHandler<TeleportConnectColumns>(builder, Commands.TeleportConnectColumns);
            _ = RegisterRequestHandler<EnableSelfTeleport>(builder, Commands.EnableSelfTeleport);
            _ = RegisterRequestHandler<ItemCount>(builder, Commands.ItemCount);
            _ = RegisterRequestHandler<SystemInfo>(builder, Commands.SystemInfo);
            _ = RegisterRequestHandler<TransferData>(builder, Commands.TransferData);

            _ = RegisterRequestHandler<BaseReown>(builder, Commands.BaseReown);
            _ = RegisterRequestHandler<BaseSetDockingRights>(builder, Commands.BaseSetDockingRights);
            _ = RegisterRequestHandler<BaseSelect>(builder, Commands.BaseSelect);
            _ = RegisterRequestHandler<BaseGetInfo>(builder, Commands.BaseGetInfo);
            _ = RegisterRequestHandler<BaseGetMyItems>(builder, Commands.BaseGetMyItems);
            _ = RegisterRequestHandler<BaseListFacilities>(builder, Commands.BaseListFacilities).SingleInstance();

            _ = RegisterRequestHandler<GetZoneInfo>(builder, Commands.GetZoneInfo);
            _ = RegisterRequestHandler<ItemCountOnZone>(builder, Commands.ItemCountOnZone);


            _ = RegisterRequestHandler<CorporationCreate>(builder, Commands.CorporationCreate);
            _ = RegisterRequestHandler<CorporationRemoveMember>(builder, Commands.CorporationRemoveMember);
            _ = RegisterRequestHandler<CorporationGetMyInfo>(builder, Commands.CorporationGetMyInfo);
            _ = RegisterRequestHandler<CorporationSetMemberRole>(builder, Commands.CorporationSetMemberRole);
            _ = RegisterRequestHandler<CorporationCharacterInvite>(builder, Commands.CorporationCharacterInvite);
            _ = RegisterRequestHandler<CorporationInviteReply>(builder, Commands.CorporationInviteReply);
            _ = RegisterRequestHandler<CorporationInfo>(builder, Commands.CorporationInfo);
            _ = RegisterRequestHandler<CorporationLeave>(builder, Commands.CorporationLeave);
            _ = RegisterRequestHandler<CorporationSearch>(builder, Commands.CorporationSearch);
            _ = RegisterRequestHandler<CorporationSetInfo>(builder, Commands.CorporationSetInfo);
            _ = RegisterRequestHandler<CorporationDropRoles>(builder, Commands.CorporationDropRoles);
            _ = RegisterRequestHandler<CorporationCancelLeave>(builder, Commands.CorporationCancelLeave);
            _ = RegisterRequestHandler<CorporationPayOut>(builder, Commands.CorporationPayOut);
            _ = RegisterRequestHandler<CorporationForceInfo>(builder, Commands.CorporationForceInfo);
            _ = RegisterRequestHandler<CorporationGetDelegates>(builder, Commands.CorporationGetDelegates);
            _ = RegisterRequestHandler<CorporationTransfer>(builder, Commands.CorporationTransfer);
            _ = RegisterRequestHandler<CorporationHangarListAll>(builder, Commands.CorporationHangarListAll);
            _ = RegisterRequestHandler<CorporationHangarListOnBase>(builder, Commands.CorporationHangarListOnBase);
            _ = RegisterRequestHandler<CorporationRentHangar>(builder, Commands.CorporationRentHangar);
            _ = RegisterRequestHandler<CorporationHangarPayRent>(builder, Commands.CorporationHangarPayRent);
            _ = RegisterRequestHandler<CorporationHangarLogSet>(builder, Commands.CorporationHangarLogSet);
            _ = RegisterRequestHandler<CorporationHangarLogClear>(builder, Commands.CorporationHangarLogClear);
            _ = RegisterRequestHandler<CorporationHangarLogList>(builder, Commands.CorporationHangarLogList);
            _ = RegisterRequestHandler<CorporationHangarSetAccess>(builder, Commands.CorporationHangarSetAccess);
            _ = RegisterRequestHandler<CorporationHangarClose>(builder, Commands.CorporationHangarClose);
            _ = RegisterRequestHandler<CorporationHangarSetName>(builder, Commands.CorporationHangarSetName);
            _ = RegisterRequestHandler<CorporationHangarRentPrice>(builder, Commands.CorporationHangarRentPrice);
            _ = RegisterRequestHandler<CorporationHangarFolderSectionCreate>(builder, Commands.CorporationHangarFolderSectionCreate);
            _ = RegisterRequestHandler<CorporationHangarFolderSectionDelete>(builder, Commands.CorporationHangarFolderSectionDelete);
            _ = RegisterRequestHandler<CorporationVoteStart>(builder, Commands.CorporationVoteStart);
            _ = RegisterRequestHandler<CorporationVoteList>(builder, Commands.CorporationVoteList);
            _ = RegisterRequestHandler<CorporationVoteDelete>(builder, Commands.CorporationVoteDelete);
            _ = RegisterRequestHandler<CorporationVoteCast>(builder, Commands.CorporationVoteCast);
            _ = RegisterRequestHandler<CorporationVoteSetTopic>(builder, Commands.CorporationVoteSetTopic);
            _ = RegisterRequestHandler<CorporationBulletinStart>(builder, Commands.CorporationBulletinStart);
            _ = RegisterRequestHandler<CorporationBulletinEntry>(builder, Commands.CorporationBulletinEntry);
            _ = RegisterRequestHandler<CorporationBulletinDelete>(builder, Commands.CorporationBulletinDelete);
            _ = RegisterRequestHandler<CorporationBulletinList>(builder, Commands.CorporationBulletinList);
            _ = RegisterRequestHandler<CorporationBulletinDetails>(builder, Commands.CorporationBulletinDetails);
            _ = RegisterRequestHandler<CorporationBulletinEntryDelete>(builder, Commands.CorporationBulletinEntryDelete);
            _ = RegisterRequestHandler<CorporationBulletinNewEntries>(builder, Commands.CorporationBulletinNewEntries);
            _ = RegisterRequestHandler<CorporationBulletinModerate>(builder, Commands.CorporationBulletinModerate);
            _ = RegisterRequestHandler<CorporationCeoTakeOverStatus>(builder, Commands.CorporationCeoTakeOverStatus);
            _ = RegisterRequestHandler<CorporationVolunteerForCeo>(builder, Commands.CorporationVolunteerForCeo);
            _ = RegisterRequestHandler<CorporationRename>(builder, Commands.CorporationRename);
            _ = RegisterRequestHandler<CorporationDonate>(builder, Commands.CorporationDonate);
            _ = RegisterRequestHandler<CorporationTransactionHistory>(builder, Commands.CorporationTransactionHistory);
            _ = RegisterRequestHandler<CorporationApply>(builder, Commands.CorporationApply);
            _ = RegisterRequestHandler<CorporationDeleteMyApplication>(builder, Commands.CorporationDeleteMyApplication);
            _ = RegisterRequestHandler<CorporationAcceptApplication>(builder, Commands.CorporationAcceptApplication);
            _ = RegisterRequestHandler<CorporationDeleteApplication>(builder, Commands.CorporationDeleteApplication);
            _ = RegisterRequestHandler<CorporationListMyApplications>(builder, Commands.CorporationListMyApplications);
            _ = RegisterRequestHandler<CorporationListApplications>(builder, Commands.CorporationListApplications);
            _ = RegisterRequestHandler<CorporationLogHistory>(builder, Commands.CorporationLogHistory);
            _ = RegisterRequestHandler<CorporationNameHistory>(builder, Commands.CorporationNameHistory);
            _ = RegisterRequestHandler<CorporationSetColor>(builder, Commands.CorporationSetColor);
            _ = RegisterRequestHandler<CorporationDocumentConfig>(builder, Commands.CorporationDocumentConfig).SingleInstance();
            _ = RegisterRequestHandler<CorporationDocumentTransfer>(builder, Commands.CorporationDocumentTransfer);
            _ = RegisterRequestHandler<CorporationDocumentList>(builder, Commands.CorporationDocumentList);
            _ = RegisterRequestHandler<CorporationDocumentCreate>(builder, Commands.CorporationDocumentCreate);
            _ = RegisterRequestHandler<CorporationDocumentDelete>(builder, Commands.CorporationDocumentDelete);
            _ = RegisterRequestHandler<CorporationDocumentOpen>(builder, Commands.CorporationDocumentOpen);
            _ = RegisterRequestHandler<CorporationDocumentUpdateBody>(builder, Commands.CorporationDocumentUpdateBody);
            _ = RegisterRequestHandler<CorporationDocumentMonitor>(builder, Commands.CorporationDocumentMonitor);
            _ = RegisterRequestHandler<CorporationDocumentUnmonitor>(builder, Commands.CorporationDocumentUnmonitor);
            _ = RegisterRequestHandler<CorporationDocumentRent>(builder, Commands.CorporationDocumentRent);
            _ = RegisterRequestHandler<CorporationDocumentRegisterList>(builder, Commands.CorporationDocumentRegisterList);
            _ = RegisterRequestHandler<CorporationDocumentRegisterSet>(builder, Commands.CorporationDocumentRegisterSet);
            _ = RegisterRequestHandler<CorporationInfoFlushCache>(builder, Commands.CorporationInfoFlushCache);
            _ = RegisterRequestHandler<CorporationGetReputation>(builder, Commands.CorporationGetReputation);
            _ = RegisterRequestHandler<CorporationMyStandings>(builder, Commands.CorporationMyStandings);
            _ = RegisterRequestHandler<CorporationSetMembersNeutral>(builder, Commands.CorporationSetMembersNeutral);
            _ = RegisterRequestHandler<CorporationRoleHistory>(builder, Commands.CorporationRoleHistory);
            _ = RegisterRequestHandler<CorporationMemberRoleHistory>(builder, Commands.CorporationMemberRoleHistory);




            _ = RegisterRequestHandler<YellowPagesSearch>(builder, Commands.YellowPagesSearch);
            _ = RegisterRequestHandler<YellowPagesSubmit>(builder, Commands.YellowPagesSubmit);
            _ = RegisterRequestHandler<YellowPagesGet>(builder, Commands.YellowPagesGet);
            _ = RegisterRequestHandler<YellowPagesDelete>(builder, Commands.YellowPagesDelete);


            _ = RegisterRequestHandler<AllianceGetDefaults>(builder, Commands.AllianceGetDefaults).SingleInstance();
            _ = RegisterRequestHandler<AllianceGetMyInfo>(builder, Commands.AllianceGetMyInfo);
            _ = RegisterRequestHandler<AllianceRoleHistory>(builder, Commands.AllianceRoleHistory);

            _ = RegisterRequestHandler<ExtensionTest>(builder, Commands.ExtensionTest);
            _ = RegisterRequestHandler<ExtensionGetAll>(builder, Commands.ExtensionGetAll).SingleInstance();
            _ = RegisterRequestHandler<ExtensionPrerequireList>(builder, Commands.ExtensionPrerequireList).SingleInstance();
            _ = RegisterRequestHandler<ExtensionCategoryList>(builder, Commands.ExtensionCategoryList).SingleInstance();
            _ = RegisterRequestHandler<ExtensionLearntList>(builder, Commands.ExtensionLearntList);
            _ = RegisterRequestHandler<ExtensionGetAvailablePoints>(builder, Commands.ExtensionGetAvailablePoints);
            _ = RegisterRequestHandler<ExtensionGetPointParameters>(builder, Commands.ExtensionGetPointParameters);
            _ = RegisterRequestHandler<ExtensionHistory>(builder, Commands.ExtensionHistory);
            _ = RegisterRequestHandler<ExtensionBuyForPoints>(builder, Commands.ExtensionBuyForPoints);
            _ = RegisterRequestHandler<ExtensionRemoveLevel>(builder, Commands.ExtensionRemoveLevel);
            _ = RegisterRequestHandler<ExtensionBuyEpBoost>(builder, Commands.ExtensionBuyEpBoost);
            _ = RegisterRequestHandler<ExtensionResetCharacter>(builder, Commands.ExtensionResetCharacter);
            _ = RegisterRequestHandler<ExtensionFreeLockedEp>(builder, Commands.ExtensionFreeLockedEp);
            _ = RegisterRequestHandler<ExtensionFreeAllLockedEpByCommand>(builder, Commands.ExtensionFreeAllLockedEpCommand); // For GameAdmin Channel Command
            _ = RegisterRequestHandler<ExtensionGive>(builder, Commands.ExtensionGive);
            _ = RegisterRequestHandler<ExtensionReset>(builder, Commands.ExtensionReset);
            _ = RegisterRequestHandler<ExtensionRevert>(builder, Commands.ExtensionRevert);

            _ = RegisterRequestHandler<ItemShopBuy>(builder, Commands.ItemShopBuy);
            _ = RegisterRequestHandler<ItemShopList>(builder, Commands.ItemShopList);
            _ = RegisterRequestHandler<GiftOpen>(builder, Commands.GiftOpen);
            _ = RegisterRequestHandler<GetHighScores>(builder, Commands.GetHighScores);
            _ = RegisterRequestHandler<GetMyHighScores>(builder, Commands.GetMyHighScores);
            _ = RegisterRequestHandler<ZoneSectorList>(builder, Commands.ZoneSectorList).SingleInstance();

            _ = RegisterRequestHandler<ListContainer>(builder, Commands.ListContainer);

            _ = RegisterRequestHandler<SocialGetMyList>(builder, Commands.SocialGetMyList);
            _ = RegisterRequestHandler<SocialFriendRequest>(builder, Commands.SocialFriendRequest);
            _ = RegisterRequestHandler<SocialConfirmPendingFriendRequest>(builder, Commands.SocialConfirmPendingFriendRequest);
            _ = RegisterRequestHandler<SocialDeleteFriend>(builder, Commands.SocialDeleteFriend);
            _ = RegisterRequestHandler<SocialBlockFriend>(builder, Commands.SocialBlockFriend);

            _ = RegisterRequestHandler<PBSGetReimburseInfo>(builder, Commands.PBSGetReimburseInfo);
            _ = RegisterRequestHandler<PBSSetReimburseInfo>(builder, Commands.PBSSetReimburseInfo);
            _ = RegisterRequestHandler<PBSGetLog>(builder, Commands.PBSGetLog);

            _ = RegisterRequestHandler<MineralScanResultList>(builder, Commands.MineralScanResultList);
            _ = RegisterRequestHandler<MineralScanResultMove>(builder, Commands.MineralScanResultMove);
            _ = RegisterRequestHandler<MineralScanResultDelete>(builder, Commands.MineralScanResultDelete);
            _ = RegisterRequestHandler<MineralScanResultCreateItem>(builder, Commands.MineralScanResultCreateItem);
            _ = RegisterRequestHandler<MineralScanResultUploadFromItem>(builder, Commands.MineralScanResultUploadFromItem);

            _ = RegisterRequestHandler<FreshNewsCount>(builder, Commands.FreshNewsCount);
            _ = RegisterRequestHandler<GetNews>(builder, Commands.GetNews);
            _ = RegisterRequestHandler<AddNews>(builder, Commands.AddNews);
            _ = RegisterRequestHandler<UpdateNews>(builder, Commands.UpdateNews);
            _ = RegisterRequestHandler<NewsCategory>(builder, Commands.NewsCategory).SingleInstance();

            _ = RegisterRequestHandler<EpForActivityDailyLog>(builder, Commands.EpForActivityDailyLog);
            _ = RegisterRequestHandler<GetMyKillReports>(builder, Commands.GetMyKillReports);
            _ = RegisterRequestHandler<UseLotteryItem>(builder, Commands.UseLotteryItem);
            _ = RegisterRequestHandler<ContainerMover>(builder, Commands.ContainerMover);


            _ = RegisterRequestHandler<MarketTaxChange>(builder, Commands.MarketTaxChange);
            _ = RegisterRequestHandler<MarketTaxLogList>(builder, Commands.MarketTaxLogList);
            _ = RegisterRequestHandler<MarketGetInfo>(builder, Commands.MarketGetInfo);
            _ = RegisterRequestHandler<MarketAddCategory>(builder, Commands.MarketAddCategory);
            _ = RegisterRequestHandler<MarketItemList>(builder, Commands.MarketItemList);
            _ = RegisterRequestHandler<MarketGetMyItems>(builder, Commands.MarketGetMyItems);
            _ = RegisterRequestHandler<MarketGetAveragePrices>(builder, Commands.MarketGetAveragePrices);
            _ = RegisterRequestHandler<MarketCreateBuyOrder>(builder, Commands.MarketCreateBuyOrder);
            _ = RegisterRequestHandler<MarketCreateSellOrder>(builder, Commands.MarketCreateSellOrder);
            _ = RegisterRequestHandler<MarketBuyItem>(builder, Commands.MarketBuyItem);
            _ = RegisterRequestHandler<MarketCancelItem>(builder, Commands.MarketCancelItem);
            _ = RegisterRequestHandler<MarketGetState>(builder, Commands.MarketGetState);
            _ = RegisterRequestHandler<MarketSetState>(builder, Commands.MarketSetState);
            _ = RegisterRequestHandler<MarketFlush>(builder, Commands.MarketFlush);
            _ = RegisterRequestHandler<MarketGetDefinitionAveragePrice>(builder, Commands.MarketGetDefinitionAveragePrice);
            _ = RegisterRequestHandler<MarketAvailableItems>(builder, Commands.MarketAvailableItems);
            _ = RegisterRequestHandler<MarketItemsInRange>(builder, Commands.MarketItemsInRange);
            _ = RegisterRequestHandler<MarketInsertStats>(builder, Commands.MarketInsertStats);
            _ = RegisterRequestHandler<MarketListFacilities>(builder, Commands.MarketListFacilities);
            _ = RegisterRequestHandler<MarketInsertAverageForCF>(builder, Commands.MarketInsertAverageForCF);
            _ = RegisterRequestHandler<MarketGlobalAveragePrices>(builder, Commands.MarketGlobalAveragePrices);
            _ = RegisterRequestHandler<MarketModifyOrder>(builder, Commands.MarketModifyOrder);
            _ = RegisterRequestHandler<MarketCreateGammaPlasmaOrders>(builder, Commands.MarketCreateGammaPlasmaOrders);
            _ = RegisterRequestHandler<MarketRemoveItems>(builder, Commands.MarketRemoveItems);
            _ = RegisterRequestHandler<MarketCleanUp>(builder, Commands.MarketCleanUp);



            _ = RegisterRequestHandler<TradeBegin>(builder, Commands.TradeBegin);
            _ = RegisterRequestHandler<TradeCancel>(builder, Commands.TradeCancel);
            _ = RegisterRequestHandler<TradeSetOffer>(builder, Commands.TradeSetOffer);
            _ = RegisterRequestHandler<TradeAccept>(builder, Commands.TradeAccept);
            _ = RegisterRequestHandler<TradeRetractOffer>(builder, Commands.TradeRetractOffer);


            _ = RegisterRequestHandler<GetRobotInfo>(builder, Commands.GetRobotInfo).OnActivated(e => e.Instance.ForFitting = false);
            _ = RegisterRequestHandler<GetRobotInfo>(builder, Commands.GetRobotFittingInfo);
            _ = RegisterRequestHandler<SelectActiveRobot>(builder, Commands.SelectActiveRobot);
            _ = RegisterRequestHandler<RequestStarterRobot>(builder, Commands.RequestStarterRobot);
            _ = RegisterRequestHandler<RobotEmpty>(builder, Commands.RobotEmpty);
            _ = RegisterRequestHandler<SetRobotTint>(builder, Commands.SetRobotTint);

            _ = RegisterRequestHandler<FittingPresetList>(builder, Commands.FittingPresetList);
            _ = RegisterRequestHandler<FittingPresetSave>(builder, Commands.FittingPresetSave);
            _ = RegisterRequestHandler<FittingPresetDelete>(builder, Commands.FittingPresetDelete);
            _ = RegisterRequestHandler<FittingPresetApply>(builder, Commands.FittingPresetApply);

            _ = RegisterRequestHandler<RobotTemplateAdd>(builder, Commands.RobotTemplateAdd);
            _ = RegisterRequestHandler<RobotTemplateUpdate>(builder, Commands.RobotTemplateUpdate);
            _ = RegisterRequestHandler<RobotTemplateDelete>(builder, Commands.RobotTemplateDelete);
            _ = RegisterRequestHandler<RobotTemplateList>(builder, Commands.RobotTemplateList);
            _ = RegisterRequestHandler<RobotTemplateBuild>(builder, Commands.RobotTemplateBuild);

            _ = RegisterRequestHandler<EquipModule>(builder, Commands.EquipModule);
            _ = RegisterRequestHandler<ChangeModule>(builder, Commands.ChangeModule);
            _ = RegisterRequestHandler<RemoveModule>(builder, Commands.RemoveModule);
            _ = RegisterRequestHandler<EquipAmmo>(builder, Commands.EquipAmmo);
            _ = RegisterRequestHandler<ChangeAmmo>(builder, Commands.ChangeAmmo);
            _ = RegisterRequestHandler<RemoveAmmo>(builder, Commands.UnequipAmmo);
            _ = RegisterRequestHandler<PackItems>(builder, Commands.PackItems);
            _ = RegisterRequestHandler<UnpackItems>(builder, Commands.UnpackItems);
            _ = RegisterRequestHandler<TrashItems>(builder, Commands.TrashItems);
            _ = RegisterRequestHandler<RelocateItems>(builder, Commands.RelocateItems);
            _ = RegisterRequestHandler<StackSelection>(builder, Commands.StackSelection);
            _ = RegisterRequestHandler<UnstackAmount>(builder, Commands.UnstackAmount);
            _ = RegisterRequestHandler<SetItemName>(builder, Commands.SetItemName);
            _ = RegisterRequestHandler<StackTo>(builder, Commands.StackTo);
            _ = RegisterRequestHandler<ServerMessage>(builder, Commands.ServerMessage);
            _ = RegisterRequestHandler<RequestInfiniteBox>(builder, Commands.RequestInfiniteBox);
            _ = RegisterRequestHandler<DecorCategoryList>(builder, Commands.DecorCategoryList);
            _ = RegisterRequestHandler<PollGet>(builder, Commands.PollGet);
            _ = RegisterRequestHandler<PollAnswer>(builder, Commands.PollAnswer);
            _ = RegisterRequestHandler<ForceDock>(builder, Commands.ForceDock);
            _ = RegisterRequestHandler<ForceDockAdmin>(builder, Commands.ForceDockAdmin);
            _ = RegisterRequestHandler<GetItemSummary>(builder, Commands.GetItemSummary);

            _ = RegisterRequestHandler<ProductionHistory>(builder, Commands.ProductionHistory);
            _ = RegisterRequestHandler<GetResearchLevels>(builder, Commands.GetResearchLevels).SingleInstance();
            _ = RegisterRequestHandler<ProductionComponentsList>(builder, Commands.ProductionComponentsList);
            _ = RegisterRequestHandler<ProductionRefine>(builder, Commands.ProductionRefine);
            _ = RegisterRequestHandler<ProductionRefineQuery>(builder, Commands.ProductionRefineQuery);
            _ = RegisterRequestHandler<ProductionCPRGInfo>(builder, Commands.ProductionCPRGInfo);
            _ = RegisterRequestHandler<ProductionCPRGForge>(builder, Commands.ProductionCPRGForge);
            _ = RegisterRequestHandler<ProductionCPRGForgeQuery>(builder, Commands.ProductionCPRGForgeQuery);
            _ = RegisterRequestHandler<ProductionGetCPRGFromLine>(builder, Commands.ProductionGetCprgFromLine);
            _ = RegisterRequestHandler<ProductionGetCPRGFromLineQuery>(builder, Commands.ProductionGetCprgFromLineQuery);
            _ = RegisterRequestHandler<ProductionLineSetRounds>(builder, Commands.ProductionLineSetRounds);
            _ = RegisterRequestHandler<ProductionPrototypeStart>(builder, Commands.ProductionPrototypeStart);
            _ = RegisterRequestHandler<ProductionPrototypeQuery>(builder, Commands.ProductionPrototypeQuery);
            _ = RegisterRequestHandler<ProductionInsuranceQuery>(builder, Commands.ProductionInsuranceQuery);
            _ = RegisterRequestHandler<ProductionInsuranceList>(builder, Commands.ProductionInsuranceList);
            _ = RegisterRequestHandler<ProductionInsuranceBuy>(builder, Commands.ProductionInsuranceBuy);
            _ = RegisterRequestHandler<ProductionInsuranceDelete>(builder, Commands.ProductionInsuranceDelete);
            _ = RegisterRequestHandler<ProductionMergeResearchKitsMulti>(builder, Commands.ProductionMergeResearchKitsMulti);
            _ = RegisterRequestHandler<ProductionMergeResearchKitsMultiQuery>(builder, Commands.ProductionMergeResearchKitsMultiQuery);
            _ = RegisterRequestHandler<ProductionQueryLineNextRound>(builder, Commands.ProductionQueryLineNextRound);
            _ = RegisterRequestHandler<ProductionReprocess>(builder, Commands.ProductionReprocess);
            _ = RegisterRequestHandler<ProductionReprocessQuery>(builder, Commands.ProductionReprocessQuery);
            _ = RegisterRequestHandler<ProductionRepair>(builder, Commands.ProductionRepair);
            _ = RegisterRequestHandler<ProductionRepairQuery>(builder, Commands.ProductionRepairQuery);
            _ = RegisterRequestHandler<ProductionResearch>(builder, Commands.ProductionResearch);
            _ = RegisterRequestHandler<ProductionResearchQuery>(builder, Commands.ProductionResearchQuery);
            _ = RegisterRequestHandler<ProductionInProgressHandler>(builder, Commands.ProductionInProgress);
            _ = RegisterRequestHandler<ProductionCancel>(builder, Commands.ProductionCancel);
            _ = RegisterRequestHandler<ProductionFacilityInfo>(builder, Commands.ProductionFacilityInfo);
            _ = RegisterRequestHandler<ProductionLineList>(builder, Commands.ProductionLineList);
            _ = RegisterRequestHandler<ProductionLineCalibrate>(builder, Commands.ProductionLineCalibrate);
            _ = RegisterRequestHandler<ProductionLineDelete>(builder, Commands.ProductionLineDelete);
            _ = RegisterRequestHandler<ProductionLineStart>(builder, Commands.ProductionLineStart);
            _ = RegisterRequestHandler<ProductionFacilityDescription>(builder, Commands.ProductionFacilityDescription);
            _ = RegisterRequestHandler<ProductionInProgressCorporation>(builder, Commands.ProductionInProgressCorporation);
            //admin 
            _ = RegisterRequestHandler<ProductionRemoveFacility>(builder, Commands.ProductionRemoveFacility);
            _ = RegisterRequestHandler<ProductionSpawnComponents>(builder, Commands.ProductionSpawnComponents);
            _ = RegisterRequestHandler<ProductionScaleComponentsAmount>(builder, Commands.ProductionScaleComponentsAmount);
            _ = RegisterRequestHandler<ProductionUnrepairItem>(builder, Commands.ProductionUnrepairItem);
            _ = RegisterRequestHandler<ProductionFacilityOnOff>(builder, Commands.ProductionFacilityOnOff);
            _ = RegisterRequestHandler<ProductionForceEnd>(builder, Commands.ProductionForceEnd);
            _ = RegisterRequestHandler<ProductionServerInfo>(builder, Commands.ProductionServerInfo);
            _ = RegisterRequestHandler<ProductionSpawnCPRG>(builder, Commands.ProductionSpawnCPRG);
            _ = RegisterRequestHandler<ProductionGetInsurance>(builder, Commands.ProductionGetInsurance);
            _ = RegisterRequestHandler<ProductionSetInsurance>(builder, Commands.ProductionSetInsurance);

            _ = RegisterRequestHandler<CreateCorporationHangarStorage>(builder, Commands.CreateCorporationHangarStorage);
            _ = RegisterRequestHandler<DockAll>(builder, Commands.DockAll);
            _ = RegisterRequestHandler<ReturnCorporationOwnderItems>(builder, Commands.ReturnCorporateOwnedItems);

            _ = RegisterRequestHandler<RelayOpen>(builder, Commands.RelayOpen);
            _ = RegisterRequestHandler<RelayClose>(builder, Commands.RelayClose);
            _ = RegisterRequestHandler<ZoneSaveLayer>(builder, Commands.ZoneSaveLayer);
            _ = RegisterRequestHandler<ZoneRemoveObject>(builder, Commands.ZoneRemoveObject);
            _ = RegisterRequestHandler<ZoneDebugLOS>(builder, Commands.ZoneDebugLOS);
            _ = RegisterRequestHandler<ZoneSetBaseDetails>(builder, Commands.ZoneSetBaseDetails);
            _ = RegisterRequestHandler<ZoneSelfDestruct>(builder, Commands.ZoneSelfDestruct);
            _ = RegisterRequestHandler<ZoneSOS>(builder, Commands.ZoneSOS);
            _ = RegisterRequestHandler<ZoneCopyGroundType>(builder, Commands.ZoneCopyGroundType); //OPP

            _ = RegisterRequestHandler<ZoneGetZoneObjectDebugInfo>(builder, Commands.ZoneGetZoneObjectDebugInfo);
            _ = RegisterRequestHandler<ZoneDrawBlockingByEid>(builder, Commands.ZoneDrawBlockingByEid);


            _ = RegisterRequestHandler<GangCreate>(builder, Commands.GangCreate);
            _ = RegisterRequestHandler<GangDelete>(builder, Commands.GangDelete);
            _ = RegisterRequestHandler<GangLeave>(builder, Commands.GangLeave);
            _ = RegisterRequestHandler<GangKick>(builder, Commands.GangKick);
            _ = RegisterRequestHandler<GangInfo>(builder, Commands.GangInfo);
            _ = RegisterRequestHandler<GangSetLeader>(builder, Commands.GangSetLeader);
            _ = RegisterRequestHandler<GangSetRole>(builder, Commands.GangSetRole);
            _ = RegisterRequestHandler<GangInvite>(builder, Commands.GangInvite);
            _ = RegisterRequestHandler<GangInviteReply>(builder, Commands.GangInviteReply);

            _ = RegisterRequestHandler<TechTreeInfo>(builder, Commands.TechTreeInfo);
            _ = RegisterRequestHandler<TechTreeUnlock>(builder, Commands.TechTreeUnlock);
            _ = RegisterRequestHandler<TechTreeResearch>(builder, Commands.TechTreeResearch);
            _ = RegisterRequestHandler<TechTreeDonate>(builder, Commands.TechTreeDonate);
            _ = RegisterRequestHandler<TechTreeGetLogs>(builder, Commands.TechTreeGetLogs);


            _ = RegisterRequestHandler<TransportAssignmentSubmit>(builder, Commands.TransportAssignmentSubmit);
            _ = RegisterRequestHandler<TransportAssignmentList>(builder, Commands.TransportAssignmentList);
            _ = RegisterRequestHandler<TransportAssignmentCancel>(builder, Commands.TransportAssignmentCancel);
            _ = RegisterRequestHandler<TransportAssignmentTake>(builder, Commands.TransportAssignmentTake);
            _ = RegisterRequestHandler<TransportAssignmentLog>(builder, Commands.TransportAssignmentLog);
            _ = RegisterRequestHandler<TransportAssignmentContainerInfo>(builder, Commands.TransportAssignmentContainerInfo);
            _ = RegisterRequestHandler<TransportAssignmentRunning>(builder, Commands.TransportAssignmentRunning);
            _ = RegisterRequestHandler<TransportAssignmentRetrieve>(builder, Commands.TransportAssignmentRetrieve);
            _ = RegisterRequestHandler<TransportAssignmentListContent>(builder, Commands.TransportAssignmentListContent);
            _ = RegisterRequestHandler<TransportAssignmentGiveUp>(builder, Commands.TransportAssignmentGiveUp);
            _ = RegisterRequestHandler<TransportAssignmentDeliver>(builder, Commands.TransportAssignmentDeliver);


            _ = RegisterRequestHandler<SetStanding>(builder, Commands.SetStanding);
            _ = RegisterRequestHandler<ForceStanding>(builder, Commands.ForceStanding);
            _ = RegisterRequestHandler<ForceFactionStandings>(builder, Commands.ForceFactionStandings);
            _ = RegisterRequestHandler<GetStandingForDefaultCorporations>(builder, Commands.GetStandingForDefaultCorporations);
            _ = RegisterRequestHandler<GetStandingForDefaultAlliances>(builder, Commands.GetStandingForDefaultAlliances);
            _ = RegisterRequestHandler<StandingList>(builder, Commands.StandingList);
            _ = RegisterRequestHandler<StandingHistory>(builder, Commands.StandingHistory);
            _ = RegisterRequestHandler<ReloadStandingForCharacter>(builder, Commands.ReloadStandingForCharacter);

            _ = RegisterRequestHandler<MailList>(builder, Commands.MailList);
            _ = RegisterRequestHandler<MailUsedFolders>(builder, Commands.MailUsedFolders);
            _ = RegisterRequestHandler<MailSend>(builder, Commands.MailSend);
            _ = RegisterRequestHandler<MailDelete>(builder, Commands.MailDelete);
            _ = RegisterRequestHandler<MailOpen>(builder, Commands.MailOpen);
            _ = RegisterRequestHandler<MailMoveToFolder>(builder, Commands.MailMoveToFolder);
            _ = RegisterRequestHandler<MailDeleteFolder>(builder, Commands.MailDeleteFolder);
            _ = RegisterRequestHandler<MailNewCount>(builder, Commands.MailNewCount);
            _ = RegisterRequestHandler<MassMailOpen>(builder, Commands.MassMailOpen);
            _ = RegisterRequestHandler<MassMailDelete>(builder, Commands.MassMailDelete);
            _ = RegisterRequestHandler<MassMailSend>(builder, Commands.MassMailSend);
            _ = RegisterRequestHandler<MassMailList>(builder, Commands.MassMailList);
            _ = RegisterRequestHandler<MassMailNewCount>(builder, Commands.MassMailNewCount);


            _ = RegisterRequestHandler<ServerShutDownState>(builder, Commands.ServerShutDownState);
            _ = RegisterRequestHandler<ServerShutDown>(builder, Commands.ServerShutDown);
            _ = RegisterRequestHandler<ServerShutDownCancel>(builder, Commands.ServerShutDownCancel);

            //RegisterZoneRequestHandlers();

            //Admin tool commands
            _ = RegisterRequestHandler<GetAccountsWithCharacters>(builder, Commands.GetAccountsWithCharacters);
            _ = RegisterRequestHandler<AccountGet>(builder, Commands.AccountGet);
            _ = RegisterRequestHandler<AccountUpdate>(builder, Commands.AccountUpdate);
            _ = RegisterRequestHandler<AccountCreate>(builder, Commands.AccountCreate);
            _ = RegisterRequestHandler<ChangeSessionPassword>(builder, Commands.ChangeSessionPassword);
            _ = RegisterRequestHandler<AccountBan>(builder, Commands.AccountBan);
            _ = RegisterRequestHandler<AccountUnban>(builder, Commands.AccountUnban);
            _ = RegisterRequestHandler<AccountDelete>(builder, Commands.AccountDelete);
            _ = RegisterRequestHandler<ServerInfoGet>(builder, Commands.ServerInfoGet);
            _ = RegisterRequestHandler<ServerInfoSet>(builder, Commands.ServerInfoSet);

            // Open account commands
            _ = RegisterRequestHandler<AccountOpenCreate>(builder, Commands.AccountOpenCreate);

            // Event GM Commands
            _ = RegisterRequestHandler<EPBonusEvent>(builder, Commands.EPBonusSet);
        }

        private void RegisterRequestHandlerFactory<T>(ContainerBuilder builder) where T : IRequest
        {
            _ = builder.Register<RequestHandlerFactory<T>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return command =>
                {
                    return ctx.IsRegisteredWithKey<IRequestHandler<T>>(command) ? ctx.ResolveKeyed<IRequestHandler<T>>(command) : null;
                };
            });
        }

        private IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<TRequestHandler, TRequest>(ContainerBuilder builder, Command command)
            where TRequestHandler : IRequestHandler<TRequest>
            where TRequest : IRequest
        {
            IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> res = builder.RegisterType<TRequestHandler>();

            _ = builder.Register(c =>
            {
                return c.Resolve<RequestHandlerProfiler<TRequest>>(new TypedParameter(typeof(IRequestHandler<TRequest>), c.Resolve<TRequestHandler>()));
            }).Keyed<IRequestHandler<TRequest>>(command);

            return res;
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<T>(ContainerBuilder builder, Command command) where T : IRequestHandler<IRequest>
        {
            return RegisterRequestHandler<T, IRequest>(builder, command);
        }
    }
}
