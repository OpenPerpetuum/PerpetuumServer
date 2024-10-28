using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.Channels;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Units.DockingBases
{
    public class DockingBase : Unit
    {
        private readonly ICentralBank centralBank;
        private readonly IRobotTemplateRelations robotTemplateRelations;
        private readonly DockingBaseHelper dockingBaseHelper;

        public DockingBase(
            IChannelManager channelManager,
            ICentralBank centralBank,
            IRobotTemplateRelations robotTemplateRelations,
            DockingBaseHelper dockingBaseHelper)
        {
            ChannelManager = channelManager;
            this.centralBank = centralBank;
            this.robotTemplateRelations = robotTemplateRelations;
            this.dockingBaseHelper = dockingBaseHelper;
        }

        protected IChannelManager ChannelManager { get; }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override ErrorCodes IsAttackable => ErrorCodes.TargetIsNonAttackable;

        public override bool IsLockable => false;

        private string WelcomeMessage => DynamicProperties.GetOrAdd<string>(k.welcome);

        public Position SpawnPosition => ED.Options.SpawnPosition;

        public int SpawnRange => ED.Options.SpawnRange;

        public int Size => ED.Options.Size;

        private int DockingRange => ED.Options.DockingRange;

        public bool IsInDockingRange(Player player)
        {
            return IsInRangeOf3D(player, DockingRange);
        }

        public virtual ErrorCodes IsDockingAllowed(Character issuerCharacter)
        {
            return ErrorCodes.NoError;
        }

        public override void OnDeleteFromDb()
        {
            Zone.UnitService.RemoveDefaultUnit(this, false);
            base.OnDeleteFromDb();
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> baseDict = base.ToDictionary();

            baseDict.Add(k.dockRange, DockingRange);
            baseDict.Add(k.welcome, WelcomeMessage);

            try
            {
                Dictionary<string, object> publicContainerInfo = GetPublicContainer().ToDictionary();
                publicContainerInfo.Add(k.noItemsSent, 1);
                baseDict.Add(k.publicContainer, publicContainerInfo);
            }
            catch (Exception)
            {
                Logger.Warning("trouble with docking base: " + Eid + " but transaction saved");
            }

            return baseDict;
        }

        public Dictionary<string, object> GetDockingBaseDetails()
        {
            Dictionary<string, object> info = ToDictionary();
            info[k.px] = CurrentPosition.intX;
            info[k.py] = CurrentPosition.intY;
            info[k.zone] = Zone.Id;

            return info;
        }

        public virtual double GetOwnerRefundMultiplier(TransactionType transactionType)
        {
            return 0;
        }

        public string ChannelName => $"base_{Eid}";

        public void DockIn(Character character, TimeSpan undockDelay, ZoneExitType zoneExitType)
        {
            DockIn(character, undockDelay);

            Transaction.Current.OnCommited(() =>
            {
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {k.result, new Dictionary<string, object>
                    {
                        {k.baseEID, Eid},
                        {k.reason,(byte)zoneExitType}
                    }}};

                Message.Builder.SetCommand(Commands.Dock).WithData(data).ToCharacter(character).Send();
            });
        }

        public void DockIn(Character character, TimeSpan undockDelay)
        {
            character.NextAvailableUndockTime = DateTime.Now + undockDelay;
            character.CurrentDockingBaseEid = Eid;
            character.IsDocked = true;
            character.ZoneId = null;
            character.ZonePosition = null;

            Transaction.Current.OnCommited(() => TryJoinChannel(character));
        }

        protected IEnumerable<Character> GetCharacters()
        {
            return Db.Query()
                .CommandText("select characterid from characters where baseeid=@eid and active=1")
                .SetParameter("@eid", Eid)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0)))
                .ToArray();
        }

        public PublicContainer GetPublicContainerWithItems(Character character)
        {
            PublicContainer publicContainer = GetPublicContainer();
            publicContainer.ReloadItems(character);

            return publicContainer;
        }

        public PublicContainer GetPublicContainer()
        {
            return dockingBaseHelper.GetPublicContainer(this);
        }

        [NotNull]
        public Market GetMarketOrThrow()
        {
            return GetMarket().ThrowIfNull(ErrorCodes.MarketNotFound);
        }

        [CanBeNull]
        public Market GetMarket()
        {
            return dockingBaseHelper.GetMarket(this);
        }

        [CanBeNull]
        public ItemShop GetItemShop()
        {
            return dockingBaseHelper.GetItemShop(this);
        }

        public IEnumerable<ProductionFacility> GetProductionFacilities()
        {
            return dockingBaseHelper.GetProductionFacilities(this);
        }

        public PublicCorporationHangarStorage GetPublicCorporationHangarStorage()
        {
            return dockingBaseHelper.GetPublicCorporationHangarStorage(this);
        }

        [CanBeNull]
        public Robot CreateStarterRobotForCharacter(Character character, bool setActive = false)
        {
            PublicContainer container = GetPublicContainerWithItems(character);
            // keresunk egy arkhet,ha van akkor csondben kilepunk
            RobotTemplate template = robotTemplateRelations.GetStarterMaster(CanCreateEquippedStartRobot);
            if (container.GetItems(true).Any(i => i.Definition == template.EntityDefault.Definition))
            {
                return null;
            }

            // ha nincs akkor legyartunk egyet
            Robot robot = template.Build();
            robot.Owner = character.Eid;
            robot.Initialize(character);
            robot.Repair();
            container.AddItem(robot, true);
            container.Save();

            if (setActive)
            {
                character.SetActiveRobot(robot);
            }

            return robot;
        }

        protected virtual bool CanCreateEquippedStartRobot => Zone?.Configuration.Protected ?? false;

        protected virtual void JoinChannel(Character character)
        {
            ChannelManager.JoinChannel(ChannelName, character, ChannelMemberRole.Undefined, null);
        }

        public void LeaveChannel(Character character)
        {
            ChannelManager.LeaveChannel(ChannelName, character);
        }

        /// <summary>
        /// Check and joins/leave character in/from the docking base chat, if possible.
        /// </summary>
        /// <param name="character">Character being checked.</param>
        public void TryJoinChannel(Character character)
        {
            if (IsDockingAllowed(character) == ErrorCodes.NoError)
            {
                JoinChannel(character);

                return;
            }

            LeaveChannel(character);
        }

        public static bool Exists(long baseEid)
        {
            return Db.Query().CommandText("select eid from zoneentities where eid=@baseEid").SetParameter("@baseEid", baseEid).ExecuteScalar<long>() > 0 ||
                Db.Query().CommandText("select eid from zoneuserentities where eid=@baseEid").SetParameter("@baseEid", baseEid).ExecuteScalar<long>() > 0;
        }

        public virtual bool IsOnGammaZone()
        {
            return false;
        }

        public virtual bool IsVisible(Character character)
        {
            return true;
        }

        public void AddCentralBank(TransactionType transactionType, double amount)
        {
            amount = Math.Abs(amount);
            double centralBankShare = amount;
            Corporation profitingOwner = ProfitingOwnerSelector.GetProfitingOwner(this);
            if (profitingOwner != null)
            {
                double multiplier = GetOwnerRefundMultiplier(transactionType);
                if (multiplier > 0.0)
                {
                    double shareFromOwnership = amount * multiplier;
                    centralBankShare = amount * (1 - multiplier);
                    Logger.Info("corpEID: " + profitingOwner.Eid + " adding to wallet: " + shareFromOwnership + " as docking base owner facility payback.");
                    IntrusionHelper.AddOwnerIncome(profitingOwner.Eid, shareFromOwnership);
                }
            }

            centralBank.AddAmount(centralBankShare, transactionType);
        }
    }
}
