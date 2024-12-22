using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;
using Perpetuum.Services.Sparks.Teleports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.RequestHandlers
{
    public class UseLotteryItem : IRequestHandler
    {
        private readonly IEntityServices entityServices;
        private readonly IAccountManager accountManager;
        private readonly IAccountRepository accountRepository;
        private readonly SparkTeleportHelper sparkTeleportHelper;

        public UseLotteryItem(
            IEntityServices entityServices,
            IAccountManager accountManager,
            IAccountRepository accountRepository,
            SparkTeleportHelper sparkTeleportHelper)
        {
            this.entityServices = entityServices;
            this.accountManager = accountManager;
            this.accountRepository = accountRepository;
            this.sparkTeleportHelper = sparkTeleportHelper;
        }

        public void HandleRequest(IRequest request)
        {
            long itemEid = request.Data.GetOrDefault<long>(k.itemEID);
            Entity item = entityServices.Repository.Load(itemEid);

            if (item is LotteryItem)
            {
                HandleLottery(request);
            }
            else if (item is EPBoost)
            {
                HandleEPBoost(request, itemEid);
            }
            else if (item is Paint) //TODO this is here until we can build a good category flag..
            {
                HandlePaint(request, itemEid);
            }
            else if (item is CalibrationProgramCapsule)
            {
                HandleCalibrationTemplateItem(request, itemEid);
            }
            else if (item is RespecToken)
            {
                HandleRespecToken(request, itemEid);
            }
            else if (item is SparkTeleportDevice)
            {
                HandleSparkTeleportDevice(request, itemEid);
            }
            else if (item is ServerWideEpBooster)
            {
                HandleServerWideEpBooster(request, itemEid);
            }
        }

        private void HandleLottery(IRequest request)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long itemEid = request.Data.GetOrDefault<long>(k.itemEID);
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;

                Container container = Container.GetWithItems(containerEid, character);
                LotteryItem lotteryItem = (LotteryItem)container.GetItemOrThrow(itemEid, true).Unstack(1);

                EntityDefault randomEd = lotteryItem.PickRandomItem();
                Item randomItem = (Item)entityServices.Factory.CreateWithRandomEID(randomEd);
                randomItem.Owner = character.Eid;

                container.AddItem(randomItem, true);
                entityServices.Repository.Delete(lotteryItem);
                container.Save();

                LogOpen(character, container, lotteryItem);
                LogRandomItemCreated(character, container, randomItem);

                Transaction.Current.OnCommited(() =>
                {
                    Dictionary<string, object> result = new Dictionary<string, object>
                    {
                        {k.container,container.ToDictionary()},
                        {k.item,randomItem.ToDictionary()}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        private void HandleEPBoost(IRequest request, long itemEid)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                Account account = accountManager.Repository
                    .Get(request.Session.AccountId)
                    .ThrowIfNull(ErrorCodes.AccountNotFound);

                Container container = Container.GetWithItems(containerEid, character);

                EPBoost containerItem = (EPBoost)container.GetItemOrThrow(itemEid, true).Unstack(1);

                containerItem.Activate(accountManager, account);

                entityServices.Repository.Delete(containerItem);
                container.Save();
                LogActivation(character, container, containerItem);

                Transaction.Current.OnCommited(() =>
                {
                    //Send custom message back in "Redeemables" dialog
                    Dictionary<string, object> boostDict = containerItem.ToDictionary();
                    boostDict[k.quantity] = -1;  //Indicate the consumption of item from stack
                    Dictionary<string, object> result = new Dictionary<string, object>
                    {
                        { k.container, container.ToDictionary() },
                        { k.item, boostDict }
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        private void HandleCalibrationTemplateItem(IRequest request, long itemEid)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                Container container = Container.GetWithItems(containerEid, character);
                container.ThrowIfNotType<PublicContainer>(ErrorCodes.ContainerHasToBeOnADockingBase); //Error for unpacking elsewhere

                CalibrationProgramCapsule ctCapsule = (CalibrationProgramCapsule)container.GetItemOrThrow(itemEid, true).Unstack(1);
                EntityDefault ctDef = ctCapsule.Activate();

                Item ctItem = (Item)entityServices.Factory.CreateWithRandomEID(ctDef);
                ctItem.Owner = character.Eid;

                container.AddItem(ctItem, false); // CTs dont stack
                entityServices.Repository.Delete(ctCapsule);
                container.Save();

                Transaction.Current.OnCommited(() =>
                {
                    Dictionary<string, object> result = new Dictionary<string, object>
                    {
                        { k.container, container.ToDictionary() },
                        { k.item, ctItem.ToDictionary() }
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        private static void LogOpen(Character character, Container container, LotteryItem lotteryItem)
        {
            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.LotteryOpen)
                .SetCharacter(character)
                .SetContainer(container)
                .SetItem(lotteryItem);

            character.LogTransaction(b);
        }

        private static void LogActivation(Character character, Container container, Item item)
        {
            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.ItemRedeem)
                .SetCharacter(character)
                .SetContainer(container)
                .SetItem(item);

            character.LogTransaction(b);
        }

        private static void LogRandomItemCreated(Character character, Container container, Item randomItem)
        {
            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.LotteryRandomItemCreated)
                .SetCharacter(character)
                .SetContainer(container)
                .SetItem(randomItem);

            character.LogTransaction(b);
        }

        private void HandlePaint(IRequest request, long paintEid)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                Container container = Container.GetWithItems(containerEid, character);
                container.ThrowIfNotType<RobotInventory>(ErrorCodes.RobotMustBeSelected); //TODO better error to indicate item not being activated in robot cargo

                Paint paintItem = (Paint)container.GetItemOrThrow(paintEid, true).Unstack(1);
                paintItem.Activate(container as RobotInventory, character);
                entityServices.Repository.Delete(paintItem);
                container.Save();

                Transaction.Current.OnCommited(() =>
                {
                    //Send custom message back in "Redeemables" dialog
                    Dictionary<string, object> paintDict = paintItem.ToDictionary();
                    paintDict[k.quantity] = -1;  //Indicate the consumption of item from stack
                    Dictionary<string, object> result = new Dictionary<string, object>
                    {
                        { k.container, container.ToDictionary() },
                        { k.item, paintDict}
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        private void HandleRespecToken(IRequest request, long itemEid)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                Account account = accountManager.Repository
                    .Get(request.Session.AccountId)
                    .ThrowIfNull(ErrorCodes.AccountNotFound);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                Container container = Container.GetWithItems(containerEid, character);
                RespecToken containerItem = (RespecToken)container
                    .GetItemOrThrow(itemEid, true)
                    .Unstack(1);

                containerItem.Activate(accountManager, account, character);

                entityServices.Repository.Delete(containerItem);
                container.Save();
                LogActivation(character, container, containerItem);

                Transaction.Current.OnCommited(() =>
                {
                    accountRepository.Update(account);
                });

                scope.Complete();
            }

            Dictionary<string, object> relogMessage = new Dictionary<string, object>
            {
                { k.message, "You will be automatically relogged in 5 seconds" },
                { k.translate, "relog_in_5_seconds" },
            };

            Message.Builder
                .SetCommand(Commands.ServerMessage)
                .WithData(relogMessage)
                .ToCharacter(request.Session.Character)
                .Send();

            TimeSpan delay = TimeSpan.FromSeconds(5);

            Task.Delay(delay).ContinueWith(t =>
            {
                using (TransactionScope scope = Db.CreateTransaction())
                {
                    Services.Sessions.ISession session = request.Session;
                    session.DeselectCharacter();
                    scope.Complete();
                }
            });
        }

        private void HandleSparkTeleportDevice(IRequest request, long itemEid)
        {
            int baseId = 0;
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                Account account = accountManager.Repository
                    .Get(request.Session.AccountId)
                    .ThrowIfNull(ErrorCodes.AccountNotFound);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                Container container = Container.GetWithItems(containerEid, character);
                SparkTeleportDevice containerItem = (SparkTeleportDevice)container
                    .GetItemOrThrow(itemEid, true)
                    .Unstack(1);

                baseId = containerItem.BaseId;

                entityServices.Repository.Delete(containerItem);
                container.Save();
                LogActivation(character, container, containerItem);

                Transaction.Current.OnCommited(() =>
                {
                    accountRepository.Update(account);
                });

                scope.Complete();
            }

            Dictionary<string, object> sparkTeleportData = new Dictionary<string, object>
            {
                { k.ID, baseId },
            };

            Request sparkTeleportRequest = new Request
            {
                Command = Commands.SparkTeleportUse,
                Session = request.Session,
                Data = sparkTeleportData
            };

            request.Session.HandleLocalRequest(sparkTeleportRequest);
        }

        private void HandleServerWideEpBooster(IRequest request, long itemEid)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Character character = request.Session.Character;
                Account account = accountManager.Repository
                    .Get(request.Session.AccountId)
                    .ThrowIfNull(ErrorCodes.AccountNotFound);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                Container container = Container.GetWithItems(containerEid, character);
                ServerWideEpBooster containerItem = (ServerWideEpBooster)container
                    .GetItemOrThrow(itemEid, true)
                    .Unstack(1);

                containerItem.Activate(request.Session);

                entityServices.Repository.Delete(containerItem);
                container.Save();
                LogActivation(character, container, containerItem);

                Transaction.Current.OnCommited(() =>
                {
                    accountRepository.Update(account);
                });

                scope.Complete();
            }
        }
    }
}