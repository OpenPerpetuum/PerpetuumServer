using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Players.ExtensionMethods;
using Perpetuum.Robots;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Services.Looting
{
    public class LootContainer : Unit
    {
        public static readonly TimeSpan DespawnTime = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<Character, int> _pinTryCounts = new ConcurrentDictionary<Character, int>();

        private const int LOOT_RANGE = 10;
        private UnitDespawnHelper _despawnHelper;
        private readonly ILootItemRepository _itemRepository;
        private readonly Looters _looters;
        private readonly IBuilder<Packet> _lootListPacketBuilder;

        private readonly IntervalTimer _timerResetOwner = new IntervalTimer(TimeSpan.FromMinutes(5));
        protected readonly object syncObject = new object();

        private readonly IDynamicProperty<int> _pinCode;

        public LootContainer(ILootItemRepository lootItemRepository)
        {
            _looters = new Looters(this);
            _itemRepository = lootItemRepository;

            _lootListPacketBuilder = new LootListPacketBuilder(this, _itemRepository);

            _pinCode = DynamicProperties.GetProperty<int>(k.pinCode);
        }

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
            _despawnHelper.CanApplyDespawnEffect = OnCanApplyDespawnEffect;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public int PinCode
        {
            protected get => _pinCode.Value; set => _pinCode.Value = value;
        }

        public override ErrorCodes IsAttackable => ErrorCodes.TargetIsNonAttackable;

        private bool OnCanApplyDespawnEffect(Unit unit)
        {
            return _looters.Count > 0;
        }

        public IEnumerable<LootItem> GetLootItems()
        {
            return _itemRepository.GetAll(this);
        }

        public void AddLoots(IEnumerable<LootItem> items)
        {
            foreach (LootItem item in items)
            {
                AddLoot(item);
            }
        }

        protected void AddLoot(LootItem item)
        {
            if (item.Quantity == 0)
            {
                return;
            }

            _itemRepository.AddWithStack(this, item);
        }

        public void SendLootListToPlayer(Player player, int pinCode)
        {
            HasAccess(player, pinCode);

            Zone.CreateBeam(BeamType.loot_bolt, b => b.WithSource(player)
                .WithTarget(this)
                .WithState(BeamState.Hit).WithDuration(1000));
            player.Session.SendPacket(_lootListPacketBuilder);

            _looters.Add(player);
        }

        protected virtual void HasAccess(Player looter, int pinCode)
        {
            IsInLootRange(looter).ThrowIfFalse(ErrorCodes.LootContainerOutOfRange);

            Character owner = this.GetOwnerAsCharacter();

            if (owner == Character.None || // van owner?
                 owner == looter.Character || // ugyanaz akar-e lootolni aki a gazdi
                 Gang.CompareGang(looter.Character, owner) // ugyanabban a gangben vannak-e
                )
            {
                return;
            }

            if (!IsFieldContainer() && !looter.IsInDefaultCorporation())
            {
                if (looter.CorporationEid == owner.CorporationEid)
                {
                    return;
                }
            }

            CheckPinCode(looter.Character, pinCode);
        }

        private bool IsInLootRange(Player player) { return IsInRangeOf3D(player, LOOT_RANGE); }

        private void CheckPinCode(Character looter, int pinCode)
        {
            _pinTryCounts.GetOrDefault(looter).ThrowIfGreaterOrEqual(3, ErrorCodes.AccessDenied);

            if (LootHelper.PinToString(PinCode) != LootHelper.PinToString(pinCode))
            {
                _pinTryCounts.AddOrUpdate(looter, 1, (c, current) => ++current);
                throw new PerpetuumException(ErrorCodes.AccessDenied);
            }

            _pinTryCounts[looter] = 0;
        }

        private bool IsFieldContainer() { return this is FieldContainer; }

        protected void SendLootListToLooters()
        {
            SendPacketToLooters(_lootListPacketBuilder);
        }

        protected void SendPacketToLooters(IBuilder<Packet> builder)
        {
            _looters.GetLooters().SendPacket(builder);
        }

        public void ReleaseLootContainer(Player player)
        {
            _looters.Remove(player);
        }

        public void TakeLoots(Player player, int pinCode, IList<KeyValuePair<Guid, int>> items)
        {
            HasAccess(player, pinCode);

            lock (syncObject)
            {
                BeamBuilder takeLootBeamBuilder = Beam.NewBuilder().WithType(BeamType.loot_bolt)
                                                           .WithSource(player)
                                                           .WithTarget(this)
                                                           .WithState(BeamState.Hit)
                                                           .WithDuration(TimeSpan.FromSeconds(1));

                Zone.CreateBeam(takeLootBeamBuilder);

                using (TransactionScope scope = Db.CreateTransaction())
                {
                    RobotInventory container = player.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();
                    List<Item> lootedItems = new List<Item>();

                    LootContainerProgressInfoPacketBuilder progressPacketBuilder = new LootContainerProgressInfoPacketBuilder(container, this, items.Count);

                    foreach (KeyValuePair<Guid, int> kvp in items)
                    {
                        try
                        {
                            Guid lootId = kvp.Key;
                            int reqQty = kvp.Value;

                            LootItem lootItem = _itemRepository.Get(this, lootId);
                            if (lootItem == null)
                            {
                                continue;
                            }

                            if (lootItem.Quantity < reqQty)
                            {
                                reqQty = lootItem.Quantity;
                            }

                            Item item = CreateWithRandomEid(lootItem.ItemInfo);
                            item.Owner = player.Owner;
                            item.Quantity = reqQty;
                            item.IsRepackaged = lootItem.ItemInfo.IsRepackaged;

                            if (!container.IsEnoughCapacity(item))
                            {
                                continue;
                            }

                            //ha serult akkor legyen serult
                            item.Health = lootItem.ItemInfo.Health;

                            lock (container)
                            {
                                container.AddItem(item, true);
                            }

                            lootItem.Quantity -= reqQty;

                            if (lootItem.Quantity <= 0)
                            {
                                _itemRepository.Delete(this, lootItem);
                            }
                            else
                            {
                                _itemRepository.Update(this, lootItem);
                            }

                            lootedItems.Add(item);
                        }
                        finally
                        {
                            SendPacketToLooters(progressPacketBuilder);
                            progressPacketBuilder.Increase();
                        }
                    }

                    container.Save();

                    Transaction.Current.OnCompleted(c =>
                    {
                        container.SendUpdateToOwnerAsync();

                        OnTakeLoots(player, lootedItems);

                        if (CanRemoveIfEmpty() && _itemRepository.IsEmpty(this))
                        {
                            RemoveFromZone();
                        }
                        else
                        {
                            SendLootListToLooters();
                        }

                        SendPacketToLooters(progressPacketBuilder);
                    });

                    scope.Complete();
                }
            }
        }

        private void OnTakeLoots(Player player, IEnumerable<Item> lootedItems)
        {
            TransactionLogEventBuilder b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.TakeLoot).SetCharacter(player.Character).SetContainer(Eid);

            int displayOrder = GetMissionDisplayOrder();
            Guid missionGuid = GetMissionGuid();

            foreach (Item item in lootedItems)
            {
                b.SetItem(item);
                player.Character.LogTransaction(b);

                if (this is FieldContainer)
                {
                    continue;
                }

                //#if DEBUG
                //                Logger.Info(">>>>> ENQUEUE LOOTING >>>>> " + player.Character.Id + " " + item.ED.Name + " qty:" + item.Quantity);
                //#endif

                player.MissionHandler.EnqueueMissionEventInfo(new LootMissionEventInfo(player, item, CurrentPosition, missionGuid, displayOrder));

            }
        }

        protected virtual bool CanRemoveIfEmpty()
        {
            return true;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _looters.Update(time);
            _despawnHelper.Update(time, this);

            if (IsFieldContainer())
            {
                return;
            }

            _timerResetOwner.Update(time);

            if (_timerResetOwner.Passed)
            {
                ResetOwner();
            }
        }

        private void ResetOwner()
        {
            if (Owner == 0L)
            {
                return;
            }

            Db.CreateTransactionAsync(scope =>
            {
                Owner = 0;
                Save();
            });
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            Db.CreateTransactionAsync(scope =>
            {
                _itemRepository.DeleteAll(this);
                zone.UnitService.RemoveUserUnit(this);
            }).ContinueWith(t =>
            {
                base.OnRemovedFromZone(zone);
            });
        }

        protected class LootContainerProgressInfoPacketBuilder : IBuilder<Packet>
        {
            private readonly LootContainer _container;
            private readonly int _maxCount;
            private readonly RobotInventory _robotInventory;
            private int _currentCount;

            public LootContainerProgressInfoPacketBuilder(RobotInventory robotInventory, LootContainer container, int maxCount)
            {
                _container = container;
                _robotInventory = robotInventory;
                _maxCount = maxCount;
            }

            public Packet Build()
            {
                Packet packet = new Packet(ZoneCommand.LootContainerProgressInfo);

                packet.AppendLong(_robotInventory.Eid);
                packet.AppendLong(_container.Eid);
                packet.AppendInt(_maxCount);
                packet.AppendInt(_currentCount);

                return packet;
            }

            public void Increase()
            {
                _currentCount++;
            }
        }

        private class LootListPacketBuilder : IBuilder<Packet>
        {
            private readonly LootContainer _container;
            private readonly ILootItemRepository _itemRepository;

            public LootListPacketBuilder(LootContainer container, ILootItemRepository itemRepository)
            {
                _container = container;
                _itemRepository = itemRepository;
            }

            public Packet Build()
            {
                Packet packet = new Packet(ZoneCommand.LootList);

                packet.AppendLong(_container.Eid);

                List<LootItem> loots = _itemRepository.GetAll(_container).ToList();
                packet.AppendInt(loots.Count);

                foreach (LootItem lootItem in loots)
                {
                    lootItem.AppendToPacket(packet);
                }

                return packet;
            }
        }

        private class Looters
        {
            private readonly LootContainer _lootContainer;
            private readonly ConcurrentDictionary<long, Player> _looters = new ConcurrentDictionary<long, Player>();
            private readonly TimerAction _action;

            public Looters(LootContainer lootContainer)
            {
                _lootContainer = lootContainer;
                _action = new TimerAction(CleanUpLooters, TimeSpan.FromSeconds(1000));
            }

            public int Count => _looters.Count;

            public IEnumerable<Player> GetLooters()
            {
                return _looters.Values;
            }

            public void Add(Player player)
            {
                _looters[player.Eid] = player;
            }

            public void Remove(Player player)
            {
                _looters.Remove(player.Eid);
            }

            public void Update(TimeSpan time)
            {
                _action.Update(time);
            }

            private void CleanUpLooters()
            {
                foreach (KeyValuePair<long, Player> kvp in _looters)
                {
                    Player player = kvp.Value;
                    bool isInZone = player.InZone;
                    bool isInLootRange = _lootContainer.IsInLootRange(player);

                    if (isInZone && isInLootRange)
                    {
                        continue;
                    }

                    _looters.Remove(kvp.Key);
                }
            }
        }

        public static LootContainerBuilder Create()
        {
            return new LootContainerBuilder();
        }

        public class LootContainerBuilder
        {
            private static readonly Dictionary<LootContainerType, string> _containerTypeToName = new Dictionary<LootContainerType, string>
            {
                {LootContainerType.LootOnly,DefinitionNames.LOOT_CONTAINER_OBJECT},
                {LootContainerType.Field,DefinitionNames.FIELD_CONTAINER},
                {LootContainerType.Mission,DefinitionNames.MISSION_CONTAINER}
            };

            private readonly List<LootItem> _lootItems = new List<LootItem>();

            private LootContainerType _containerType;
            private Player _ownerPlayer;
            private int _pinCode;
            private BeamType _enterBeamType;

            internal LootContainerBuilder()
            {
                _containerType = LootContainerType.LootOnly;
                _pinCode = FastRandom.NextInt(1, 9999);
                _enterBeamType = BeamType.undefined;
            }

            public LootContainerBuilder SetType(LootContainerType type)
            {
                _containerType = type;
                return this;
            }

            public LootContainerBuilder SetOwner(Player player)
            {
                _ownerPlayer = player;
                return this;
            }

            public LootContainerBuilder SetPinCode(int pinCode)
            {
                _pinCode = pinCode;
                return this;
            }

            public LootContainerBuilder SetEnterBeamType(BeamType beamType)
            {
                _enterBeamType = beamType;
                return this;
            }

            public LootContainerBuilder AddLoot(int definition, int quantity)
            {
                return AddLoot(LootItemBuilder.Create(definition).SetQuantity(quantity));
            }

            public LootContainerBuilder AddLoot(IBuilder<LootItem> builder)
            {
                AddLoot(builder.Build());
                return this;
            }

            public LootContainerBuilder AddLoot(ILootGenerator lootGenerator)
            {
                AddLoot(lootGenerator.Generate());
                return this;
            }

            public LootContainerBuilder AddLoot(IEnumerable<LootItem> lootItems)
            {
                _lootItems.AddRange(lootItems);
                return this;
            }

            public LootContainerBuilder AddLoot(LootItem lootItem)
            {
                _lootItems.Add(lootItem);
                return this;
            }

            [CanBeNull]
            public LootContainer BuildAndAddToZone(IZone zone, Position position)
            {
                if (_lootItems.Count == 0)
                {
                    return null;
                }

                LootContainer container = Build(zone, position);
                if (container == null)
                {
                    return null;
                }

                Transaction.Current.OnCommited(() =>
                {
                    BeamBuilder beamBuilder = Beam.NewBuilder().WithType(_enterBeamType).WithSource(_ownerPlayer)
                        .WithTarget(container)
                        .WithState(BeamState.Hit)
                        .WithDuration(TimeSpan.FromSeconds(5));

                    container.AddToZone(zone, position, ZoneEnterType.Default, beamBuilder);
                });

                return container;
            }

            [CanBeNull]
            public LootContainer Build(IZone zone, Position position)
            {
                string definitionName = _containerTypeToName.GetOrDefault(_containerType);
                LootContainer container = (LootContainer)CreateUnitWithRandomEID(definitionName);
                if (container == null)
                {
                    return null;
                }

                container.PinCode = _pinCode;

                if (_ownerPlayer != null)
                {
                    container.Owner = _ownerPlayer.Owner;
                }

                container.Initialize();

                container.AddLoots(_lootItems.Where(l => !l.ItemInfo.IsRepackaged));

                IEnumerable<LootItem> stackedLoots = _lootItems.Where(l => l.ItemInfo.IsRepackaged)
                                        .GroupBy(l => l.ItemInfo.Definition)
                                        .Select(grp => LootItemBuilder.Create(grp.Key).AsRepackaged().SetQuantity(grp.Sum(l => l.Quantity)).Build());

                container.AddLoots(stackedLoots);

                IEnumerable<IGrouping<string, LootItem>> plasmaByType = container
                    .GetLootItems()
                    .Where(x => x.ItemInfo.EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_reactor_plasma))
                    .GroupBy(x => x.ItemInfo.EntityDefault.Name);

                foreach (IGrouping<string, LootItem> plasma in plasmaByType)
                {
                    using (TransactionScope scope = Db.CreateTransaction())
                    {
                        Db.Query()
                            .CommandText("exec sp_RecordPlasmaGathered @gathered_on, @plasma_type, @quantity")
                            .SetParameter("@gathered_on", DateTime.UtcNow)
                            .SetParameter("@plasma_type", plasma.Key)
                            .SetParameter("@quantity", plasma.Sum(x => x.ItemInfo.Quantity))
                            .ExecuteNonQuery();
                        scope.Complete();
                    }
                }

                IEnumerable<IGrouping<string, LootItem>> fragmentsByType = container
                    .GetLootItems()
                    .Where(x => x.ItemInfo.EntityDefault.CategoryFlags.IsAny(new CategoryFlags[] { CategoryFlags.cf_robotshards, CategoryFlags.cf_research_kits, CategoryFlags.cf_reactor_cores }))
                    .GroupBy(x => x.ItemInfo.EntityDefault.Name);

                foreach (IGrouping<string, LootItem> fragment in fragmentsByType)
                {
                    using (TransactionScope scope = Db.CreateTransaction())
                    {
                        Db.Query()
                            .CommandText("exec sp_RecordResourceGathered @gathered_on, @resource_name, @quantity")
                            .SetParameter("@gathered_on", DateTime.UtcNow)
                            .SetParameter("@resource_name", fragment.Key)
                            .SetParameter("@quantity", fragment.Sum(x => x.ItemInfo.Quantity))
                            .ExecuteNonQuery();
                        scope.Complete();
                    }
                }

                zone.UnitService.AddUserUnit(container, position);
                return container;
            }
        }
    }
}