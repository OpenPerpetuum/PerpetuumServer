using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.ControlTower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Zones.PBS.DockingBases
{
    /// <summary>
    /// Player built docking base
    /// </summary>
    public class PBSDockingBase : DockingBase, IPBSObject, IStandingController
    {
        public PBSDockingBase(
            MarketHelper marketHelper,
            ICorporationManager corporationManager,
            IChannelManager channelManager,
            ICentralBank centralBank,
            IRobotTemplateRelations robotTemplateRelations,
            DockingBaseHelper dockingBaseHelper,
            SparkTeleportHelper sparkTeleportHelper,
            PBSObjectHelper<PBSDockingBase>.Factory pbsObjectHelperFactory)
            : base(channelManager, centralBank, robotTemplateRelations, dockingBaseHelper)
        {
            this.marketHelper = marketHelper;
            this.corporationManager = corporationManager;
            this.sparkTeleportHelper = sparkTeleportHelper;
            pbsObjectHelper = pbsObjectHelperFactory(this);
            pbsReinforceHandler = new PBSReinforceHandler<PBSDockingBase>(this);
            standingController = new PBSStandingController<PBSDockingBase>(this);
            pbsTerritorialVisibilityHelper = new PBSTerritorialVisibilityHelper(this);
        }

        private readonly MarketHelper marketHelper;
        private readonly ICorporationManager corporationManager;
        private readonly SparkTeleportHelper sparkTeleportHelper;
        private readonly PBSStandingController<PBSDockingBase> standingController;
        protected readonly PBSObjectHelper<PBSDockingBase> pbsObjectHelper;
        private readonly PBSReinforceHandler<PBSDockingBase> pbsReinforceHandler;
        private readonly PBSTerritorialVisibilityHelper pbsTerritorialVisibilityHelper;
        private Dictionary<string, object> cacheTerritoryDictionary;
        private DateTime lastTDRequest;
        private int bandwidthCapacity;
        private bool trashWasKilled;

        private bool IsFullyConstructed => pbsObjectHelper.IsFullyConstructed;

        public IPBSReinforceHandler ReinforceHandler => pbsReinforceHandler;

        public IPBSConnectionHandler ConnectionHandler => pbsObjectHelper.ConnectionHandler;

        public int ZoneIdCached { get; private set; }

        public ErrorCodes ModifyConstructionLevel(int amount, bool force = false)
        {
            return pbsObjectHelper.ModifyConstructionLevel(amount, force);
        }

        public int ConstructionLevelMax => pbsObjectHelper.ConstructionLevelMax;

        public int ConstructionLevelCurrent => pbsObjectHelper.ConstructionLevelCurrent;

        public bool IsOrphaned
        {
            get => pbsObjectHelper.IsOrphaned;
            set => pbsObjectHelper.IsOrphaned = value;
        }

        public event Action<Unit, bool> OrphanedStateChanged
        {
            add => pbsObjectHelper.OrphanedStateChanged += value;
            remove => pbsObjectHelper.OrphanedStateChanged -= value;
        }

        public void SendNodeUpdate(PBSEventType eventType = PBSEventType.nodeUpdate)
        {
            pbsObjectHelper.SendNodeUpdate(eventType);
        }

        public bool IsLootGenerating { get; set; }

        public double StandingLimit
        {
            get => standingController.StandingLimit;
            set => standingController.StandingLimit = value;
        }

        public bool StandingEnabled
        {
            get => standingController.Enabled;
            set => standingController.Enabled = value;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override ErrorCodes IsAttackable
        {
            get
            {
                //no reinforce
                if (pbsReinforceHandler.CurrentState.IsReinforced)
                {
                    return ErrorCodes.TargetIsNonAttackable_Reinforced;
                }

                //no connections
                //true if only production stuff is connected ONLY
                //anything else -> false -> kill that node first

                bool anyControlTower = pbsObjectHelper.ConnectionHandler.GetConnections().Any(c => c.TargetPbsObject is PBSControlTower);

                if (anyControlTower)
                {
                    return ErrorCodes.TargetIsNonAttackable_ControlTowerConnected;
                }

                return ErrorCodes.NoError; //itt tilos a base-t meghivni, mert az mar docking base
            }
        }

        public override bool IsLockable => true;

        public PBSDockingBaseVisibility DockingBaseMapVisibility
        {
            get => pbsTerritorialVisibilityHelper.DockingBaseMapVisibility();
            set => pbsTerritorialVisibilityHelper.SetDockingBaseVisibleOnMap(value);
        }


        public PBSDockingBaseVisibility NetworkMapVisibility
        {
            get => pbsTerritorialVisibilityHelper.NetworkMapVisibility();
            set => pbsTerritorialVisibilityHelper.SetNetworkVisibleOnTerritoryMap(value);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            pbsObjectHelper.Init();
            pbsReinforceHandler.Init();
            pbsTerritorialVisibilityHelper.Init();

            ZoneIdCached = zone.Id; //OPP: make sure this is set!

            //OPP: The following line is commented because it broke the BaseListFacilities command.
            // ClearChildren(); //ez azert kell, hogy a zonan ne legyenek gyerekei semmikepp
            Parent = 0; //ez azert kell, hogy a bazison levo kontenerek megtalaljak, mint root
            base.OnEnterZone(zone, enterType);
        }

        public override void OnLoadFromDb()
        {
            base.OnLoadFromDb();
            pbsObjectHelper.Init();
            pbsTerritorialVisibilityHelper.Init();
        }

        public override void OnInsertToDb()
        {
            pbsObjectHelper.Init();
            DynamicProperties.Update(k.creation, DateTime.Now);
            base.OnInsertToDb();
            Market market = GetMarket();
            marketHelper.InsertGammaPlasmaOrders(market);
            Logger.Info("A new PBSDockingbase is created " + this);
        }

        public void OnDockingBaseDeployed()
        {
            PBSHelper.SendPBSDockingBaseCreatedToProduction(Eid);
            ChannelManager.CreateChannel(ChannelType.Station, ChannelName);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> info = base.ToDictionary();
            IZone zone = Zone;
            if (zone != null)
            {
                pbsObjectHelper.AddToDictionary(info);
                pbsReinforceHandler.AddToDictionary(info);
                pbsTerritorialVisibilityHelper.AddToDictionary(info);
                info.Add(k.bandwidthLoad, GetBandwithLoad());
            }

            return info;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            //var info = base.GetDebugInfo();
            IDictionary<string, object> info = this.GetMiniDebugInfo();
            pbsObjectHelper.AddToDictionary(info);
            pbsReinforceHandler.AddToDictionary(info);
            pbsTerritorialVisibilityHelper.AddToDictionary(info);

            return info;
        }

        public override void OnUpdateToDb()
        {
            pbsReinforceHandler.OnSave();
            pbsObjectHelper.OnSave();
            pbsTerritorialVisibilityHelper.OnSave();
            base.OnUpdateToDb();
        }

        public override void OnDeleteFromDb()
        {
            //NO BASE CLASS CALL -> szandekos
            Logger.DebugInfo($"[{InfoString}] docking base on delete");
            Logger.DebugInfo($"[{InfoString}] zonaid jo, helperes cucc jon");
            PBSHelper.DeletePBSDockingBase(ZoneIdCached, this).ThrowIfError();
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            Logger.DebugInfo($"[{InfoString}] pbsbase remove from zone");
            pbsObjectHelper.RemoveFromZone(zone);
            PBSHelper.SendPBSDockingBaseDeleteToProduction(Eid);
            base.OnRemovedFromZone(zone);
        }

        protected override void OnDead(Unit killer)
        {
            IsLootGenerating = true;
            trashWasKilled = true; //signal trash
            Logger.DebugInfo($"[{InfoString}] loot generating -> true");
            IZone zone = Zone;
            pbsObjectHelper.DropLootToZoneFromBase(zone, this, killer);
            base.OnDead(killer);
        }

        public override ErrorCodes IsDockingAllowed(Character issuerCharacter)
        {
            return !IsFullyConstructed
                ? ErrorCodes.ObjectNotFullyConstructed
                : !OnlineStatus
                    ? ErrorCodes.NodeOffline
                    : !StandingEnabled
                        ? ErrorCodes.NoError
                        : !corporationManager.IsStandingMatch(Owner, issuerCharacter.CorporationEid, StandingLimit)
                            ? ErrorCodes.StandingTooLowForDocking
                            : ErrorCodes.NoError;
        }

        protected override void DoExplosion()
        {
            //NO base call!!!
        }

        public void SetOnlineStatus(bool state, bool checkNofBase, bool forcedByServer = false)
        {
            pbsObjectHelper.SetOnlineStatus(state, checkNofBase, forcedByServer);
        }

        public void TakeOver(long newOwner)
        {
            pbsObjectHelper.TakeOver(newOwner);
        }

        public bool OnlineStatus => pbsObjectHelper.OnlineStatus;

        protected override void OnUpdate(TimeSpan time)
        {
            pbsReinforceHandler.OnUpdate(time);
            pbsObjectHelper.OnUpdate(time);
            base.OnUpdate(time);
        }

        public Dictionary<string, object> GetTerritorialDictionary()
        {
            if (lastTDRequest == default || DateTime.Now.Subtract(lastTDRequest).TotalMinutes > 60)
            {
                lastTDRequest = DateTime.Now.AddMinutes(FastRandom.NextInt(10));

                Dictionary<string, object> ctd = GenerateTerritoryDictionary();
                cacheTerritoryDictionary = ctd;
            }

            return cacheTerritoryDictionary;
        }

        private Dictionary<string, object> GenerateTerritoryDictionary()
        {
            Dictionary<string, object> info = new Dictionary<string, object>
                           {
                               {k.corporationEID, Owner},
                               {k.x, CurrentPosition.intX},
                               {k.y, CurrentPosition.intY},
                           };

            Dictionary<string, object> nodes = pbsObjectHelper.ConnectionHandler.NetworkNodes
                                         .Cast<Unit>()
                                         .ToDictionary("n", unit =>
                                          new Dictionary<string, object>
                                          {
                                            {k.x, unit.CurrentPosition.intX},
                                            {k.y, unit.CurrentPosition.intY},
                                            {k.constructionRadius, unit.GetConstructionRadius()}
                                          });
            info.Add("nodes", nodes);
            return info;
        }

        public virtual ErrorCodes SetDeconstructionRight(Character issuer, bool state)
        {
            this.CheckAccessAndThrowIfFailed(issuer);

            CorporationRole role = Corporation.GetRoleFromSql(issuer);

            if (!role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO))
            {
                return ErrorCodes.InsufficientPrivileges;
            }

            if (!IsFullyConstructed)
            {
                return ErrorCodes.ObjectNotFullyConstructed;
            }

            if (state)
            {
                DynamicProperties.Update(k.allowDeconstruction, 1);
            }
            else
            {
                DynamicProperties.Remove(k.allowDeconstruction);
            }

            Save();

            return ErrorCodes.NoError;
        }

        /// <summary>
        /// If property is present then it's set to deconstruct
        /// </summary>
        /// <returns></returns>
        public virtual ErrorCodes IsDeconstructAllowed()
        {

            return DynamicProperties.Contains(k.allowDeconstruction) ? ErrorCodes.NoError : ErrorCodes.DockingBaseNotSetToDeconstruct;
        }

        public override double GetOwnerRefundMultiplier(TransactionType transactionType)
        {
            double multiplier = 0.0;
            switch (transactionType)
            {
                case TransactionType.hangarRent:
                case TransactionType.hangarRentAuto:
                    multiplier = 1.0;
                    break;
                case TransactionType.marketFee:
                    multiplier = 1.0;
                    break;
                case TransactionType.ProductionManufacture:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionResearch:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionMultiItemRepair:
                case TransactionType.ItemRepair:
                    multiplier = 0.75;
                    break;
                case TransactionType.ProductionPrototype:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionMassProduction:
                    multiplier = 0.5;
                    break;
                case TransactionType.MarketTax:
                    multiplier = 1.0;
                    break;
            }

            return multiplier;
        }

        public int GetBandwidthCapacity
        {
            get
            {
                if (bandwidthCapacity <= 0)
                {
                    if (ED.Config.bandwidthCapacity != null)
                    {
                        bandwidthCapacity = (int)ED.Config.bandwidthCapacity;
                    }
                    else
                    {
                        Logger.Error("no bandwidthCapacity defined for " + this);
                        bandwidthCapacity = 1000;
                    }
                }

                return bandwidthCapacity;
            }
        }

        private int GetBandwithLoad()
        {
            return pbsObjectHelper.ConnectionHandler.NetworkNodes
                .Where(n => !(n is PBSDockingBase))
                .Sum(n => n.GetBandwidthUsage());
        }

        public override bool IsOnGammaZone()
        {
            return true;
        }

        public override bool IsVisible(Character character)
        {
            if (DockingBaseMapVisibility == PBSDockingBaseVisibility.open)
            {
                return true;
            }

            Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);
            if (Owner == corporationEid)
            {
                if (DockingBaseMapVisibility == PBSDockingBaseVisibility.corporation)
                {
                    return true;
                }
                else if (DockingBaseMapVisibility == PBSDockingBaseVisibility.hidden)
                {
                    return role.IsAnyRole(
                        CorporationRole.CEO,
                        CorporationRole.DeputyCEO,
                        CorporationRole.viewPBS);
                }
            }

            return false;
        }

        public void TrashMe()
        {
            SystemContainer trash = SystemContainer.GetByName("pbs_trash");
            Parent = trash.Eid;
            Db.Query()
                .CommandText("INSERT dbo.pbstrash (baseeid, waskilled) VALUES (@baseeid, @waskilled)")
                .SetParameter("@baseeid", Eid)
                .SetParameter("@waskilled", trashWasKilled)
                .ExecuteNonQuery();

            Save();
        }

        public int GetNetworkNodeRange()
        {
            if (ED.Config.network_node_range != null)
            {
                return (int)ED.Config.network_node_range;
            }

            Logger.Error("no network_node_range defined for " + ED.Name);

            return 0;
        }

        public ErrorCodes DoCleanUpWork(int zone)
        {
            ErrorCodes ec = ErrorCodes.NoError;

            Logger.Info(" >>>>>>    docking base SQL DELETE Start ");

            //ezeket elkeszitjuk most mert kesobb nem lesz mar kontenere a base-nek
            Dictionary<string, object> infoHomeBaseCleared = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadHomeBaseCleared);
            Dictionary<string, object> infoBaseDeadWhileDocked = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadWhileDocked);
            Dictionary<string, object> infoBaseDeadWhileOnZone = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadWhileOnZone);

            //---market cleanup
            Market market = GetMarketOrThrow();

            int marketOrdersDeleted = Db.Query()
                .CommandText("delete marketitems where marketeid=@marketEID")
                .SetParameter("@marketEID", market.Eid)
                .ExecuteNonQuery();

            Logger.Info(marketOrdersDeleted + " market orders deleted from market: " + market.Eid + " base:" + Eid);

            //---spark teleport cleanup
            sparkTeleportHelper.DeleteAllSparkTeleports(this);

            //---------------------------------------------

            TrashMe();

            //itt lehet pucolni vagy logolni vagy valami
            //itt nem zonazunk, elintezzuk a bedokkolt playereket stb, natur sql
            //plugineknek szolni stb

            //----------ezeknek van beallitva homebasenek a bazis ami meghalt
            Character[] charactersHomeBaseCleared = Db.Query()
                .CommandText("select characterid from characters where homebaseeid=@eid and active=1 and inuse=1")
                .SetParameter("@eid", Eid)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0)))
                .ToArray();

            //clear homebase settings
            int homeBasesCleared = Db.Query()
                .CommandText("update characters set homebaseeid=null where homebaseeid=@eid")
                .SetParameter("@eid", Eid)
                .ExecuteNonQuery();

            Logger.Info(homeBasesCleared + " homebases cleared. for base:" + Eid);
            //------------------------------------------------------

            //clean up insured robots
            int insurancesCleared = Db.Query()
                .CommandText("cleanUpInsuranceByBaseEid")
                .SetParameter("@baseEid", Eid)
                .ExecuteScalar<int>();

            Logger.Info(insurancesCleared + " insurances clear for base: " + Eid);

            //ezek azok akik onnan jottek, vagy epp ott vannak bedokkolva
            IEnumerable<Character> affectedCharacters = GetCharacters();
            List<Tuple<Character, long, bool>> charactersToInform = new List<Tuple<Character, long, bool>>();
            foreach (Character affectedCharacter in affectedCharacters)
            {
                Character character = affectedCharacter;
                long? revertedBaseEid = null;
                long? homeBaseEid = character.HomeBaseEid;
                if (homeBaseEid != Eid)
                {
                    //ezeknek volt beallitva valami homebase, rakjuk oket oda
                    revertedBaseEid = homeBaseEid;
                }

                if (revertedBaseEid == null)
                {
                    //ezeknek nem volt oket faj alapjan deportaljuk
                    revertedBaseEid = DefaultCorporation.GetDockingBaseEid(character);
                }

                Logger.Info("reverted base for characterID:" + character.Id + " base:" + revertedBaseEid);
                //set reverted base
                character.CurrentDockingBaseEid = (long)revertedBaseEid;
                //be van dockolva, aktivchassis ugrott
                bool isDocked = character.IsDocked;
                if (isDocked)
                {
                    character.SetActiveRobot(null);
                }

                if (character.IsOnline)
                {
                    //ezalatt kilottek a base-t, informaljuk
                    charactersToInform.Add(new Tuple<Character, long, bool>(character, (long)revertedBaseEid, isDocked));
                }
            }

            Logger.Info(" sql administration for docking base delete is done. " + this);
            Logger.Info(" >>>>>>    docking base SQL DELETE   STOP ");

            Transaction.Current.OnCommited(() =>
            {
                ChannelManager.DeleteChannel(ChannelName);
                PBSHelper.SendBaseDestroyed(infoBaseDeadWhileDocked, infoBaseDeadWhileOnZone, charactersToInform);
                Message.Builder.SetCommand(Commands.PbsEvent).WithData(infoHomeBaseCleared).ToCharacters(charactersHomeBaseCleared).Send();
                SendNodeUpdate(PBSEventType.baseDead);
            });

            return ec;
        }
    }

}
