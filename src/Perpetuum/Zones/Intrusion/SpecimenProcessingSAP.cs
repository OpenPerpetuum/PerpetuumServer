using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Transactions;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Intrusion target which can be completed by submitting items to the SAP
    /// </summary>
    public class SpecimenProcessingSAP : SAP
    {
        private class SiegeItem
        {
            public readonly int definition;
            public readonly IntRange quantity;

            public SiegeItem(int definition, IntRange quantity)
            {
                this.definition = definition;
                this.quantity = quantity;
            }
        }

        private static readonly IDictionary<int, SiegeItem> _specimenProcessingItems; //the items the specimen processing might require


        private const int SUBMIT_ITEM_RANGE = 7;
        private static readonly TimeSpan _submitItemCooldown = TimeSpan.FromMinutes(1.5);

        private static readonly IntRange _requiredItems = new IntRange(5, 6);

        private readonly IList<ItemInfo> _itemInfos;
        private readonly ConcurrentDictionary<int, PlayerItemProgress> _playerItemProgresses = new ConcurrentDictionary<int, PlayerItemProgress>();

        static SpecimenProcessingSAP()
        {
            _specimenProcessingItems = Database.CreateCache<int, SiegeItem>("siegeitems", "id", r =>
            {
                int definition = r.GetValue<int>("definition");
                int minQty = r.GetValue<int>("minquantity");
                int maxQty = r.GetValue<int>("maxquantity");

                return new SiegeItem(definition, new IntRange(minQty, maxQty));
            });

        }

        public SpecimenProcessingSAP() : base(BeamType.attackpoint_item_enter, BeamType.attackpoint_item_out)
        {
            int itemsCount = FastRandom.NextInt(_requiredItems);
            _itemInfos = GenerateSpecimenProcessingItemList(itemsCount);
        }

        /// <summary>
        /// Generates required item's list for the specimen processing SAP
        /// </summary>
        private static IList<ItemInfo> GenerateSpecimenProcessingItemList(int count = 5)
        {
            List<ItemInfo> result = new List<ItemInfo>();
            while (result.Count < count)
            {
                KeyValuePair<int, SiegeItem> randomItemInfo = _specimenProcessingItems.RandomElement();
                SiegeItem siegeItem = randomItemInfo.Value;
                int randomQty = FastRandom.NextInt(siegeItem.quantity);
                ItemInfo itemInfo = new ItemInfo(siegeItem.definition, randomQty);
                result.Add(itemInfo);
            }

            return result;
        }


        protected override int MaxScore => _itemInfos.Count;


        private ItemInfo GetItemInfo(int index)
        {
            return index >= _itemInfos.Count ? default : _itemInfos[index];
        }

        public void SubmitItem(Player player, long itemEid)
        {
            IsInRangeOf3D(player, SUBMIT_ITEM_RANGE).ThrowIfFalse(ErrorCodes.AttackPointIsOutOfRange);

            Site.IntrusionInProgress.ThrowIfFalse(ErrorCodes.SiegeAlreadyExpired);

            PlayerItemProgress progress = _playerItemProgresses.GetOrAdd(player.Character.Id, new PlayerItemProgress());

            progress.nextSubmitTime.ThrowIfGreater(DateTime.Now, ErrorCodes.SiegeSubmitItemOverload);

            RobotInventory container = player.GetContainer();
            Debug.Assert(container != null, "container != null");
            container.EnlistTransaction();

            Item item = container.GetItemOrThrow(itemEid);

            ItemInfo requestedItemInfo = GetItemInfo(progress.index);

            requestedItemInfo.Definition.ThrowIfNotEqual(item.Definition, ErrorCodes.SiegeDefinitionNotSupported);

            int neededQty = requestedItemInfo.Quantity - progress.quantity;

            int submittedQty = UpdateOrDeleteItem(item, neededQty);

            container.Save();

            if (submittedQty > 0)
            {
                Transaction.Current.OnCompleted(c => UpdateProgess(player, container, submittedQty, requestedItemInfo, progress));
            }
        }

        private void UpdateProgess(Player player, RobotInventory container, int submittedQty, ItemInfo requestedItemInfo, PlayerItemProgress progress)
        {
            progress.quantity += submittedQty;

            if (progress.quantity >= requestedItemInfo.Quantity)
            {
                progress.index++;
                progress.quantity = 0;

                IncrementPlayerScore(player, 1);
            }

            progress.nextSubmitTime = DateTime.Now + _submitItemCooldown;

            container.SendUpdateToOwnerAsync();

            if (progress.index >= _itemInfos.Count)
            {
                return;
            }

            SendProgressToPlayer(player.Character);
        }

        public void SendProgressToPlayer(Character character)
        {
            int currentIndex = 0;
            int submittedQty = 0;
            DateTime nextSubmitTime = default;

            if (_playerItemProgresses.TryGetValue(character.Id, out PlayerItemProgress itemProgress))
            {
                currentIndex = itemProgress.index;
                submittedQty = itemProgress.quantity;
                nextSubmitTime = itemProgress.nextSubmitTime;
            }

            ItemInfo itemInfo = GetItemInfo(currentIndex);
            int maxScore = MaxScore;
            int currentScore = GetPlayerScore(character);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { k.eid,Eid },
                { k.definition, itemInfo.Definition },
                { k.quantity, itemInfo.Quantity },
                { k.current, submittedQty },
                { k.submitInterval,(int)_submitItemCooldown.TotalMinutes },
                { k.nextSubmitTime,nextSubmitTime },
                { k.maxScore,maxScore },
                { k.currentScore,currentScore }
            };

            Message.Builder.SetCommand(Commands.IntrusionSapItemInfo).WithData(data).WrapToResult().ToCharacter(character).Send();
        }


        private static int UpdateOrDeleteItem(Item item, int neededQty)
        {
            int itemQty = item.Quantity;
            int submittedQty = 0;

            if (itemQty > neededQty)
            {
                item.Quantity -= neededQty;
                submittedQty = neededQty;
            }
            else
            {
                Repository.Delete(item);
                submittedQty = itemQty;
            }

            return submittedQty;
        }

        private class PlayerItemProgress
        {
            public int index;
            public int quantity;
            public DateTime nextSubmitTime;
        }

        protected override void AppendTopScoresToPacket(Packet packet, int count)
        {
            AppendPlayerTopScoresToPacket(this, packet, count);
        }
    }
}