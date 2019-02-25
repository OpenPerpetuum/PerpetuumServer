using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Threading;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Services.Relics
{

    public class Relic : Unit
    {
        private RelicInfo _info;
        private IZone _zone;
        private bool _alive;

        private const double ACTIVATION_RANGE = 3; //30m
        private const double RESPAWN_PROXIMITY = 10.0 * ACTIVATION_RANGE;

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(4);

        private RelicLootItems _loots;

        [CanBeNull]
        public static Relic BuildAndAddToZone(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems)
        {
            var relic = (Relic)CreateUnitWithRandomEID(DefinitionNames.RELIC);
            if (relic == null)
                return null;
            relic.Init(info, zone, position, lootItems);
            zone.AddUnit(relic);
            return relic;
        }

        public void Init(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems)
        {
            _info = info;
            _zone = zone;
            CurrentPosition = _zone.GetPosition(position);
            SetAlive(true);
            SetLoots(lootItems);
        }

        public void SetLoots(RelicLootItems lootItems)
        {
            using (_lock.Write(THREAD_TIMEOUT))
                _loots = lootItems;
        }

        public RelicInfo GetRelicInfo()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return _info;
        }

        public Position GetPosition()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return CurrentPosition;
        }

        public void SetAlive(bool isAlive)
        {
            using (_lock.Write(THREAD_TIMEOUT))
                _alive = isAlive;
        }

        public bool IsAlive()
        {
            using (_lock.Read(THREAD_TIMEOUT))
                return _alive;
        }

        private UnitDespawnHelper _despawnHelper;

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _despawnHelper?.Update(time, this);
            base.OnUpdate(time);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            SetAlive(false);
            base.OnRemovedFromZone(zone);
        }


        protected internal override void UpdatePlayerVisibility(Player player)
        {
            if (GetPosition().TotalDistance2D(player.CurrentPosition) < ACTIVATION_RANGE && IsAlive())
            {
                PopRelic(player);
            }
        }

        private void PopRelic(Player player)
        {
            //Set flag on relic for removal
            SetAlive(false);

            //Compute loots
            if (_loots == null)
                return;

            //Compute EP
            var ep = GetRelicInfo().GetEP();
            if (_zone.Configuration.Type == ZoneType.Pvp) ep *= 2;
            if (_zone.Configuration.Type == ZoneType.Training) ep = 0;

            //Fork task to make the lootcan and log the ep
            Task.Run(() =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    LootContainer.Create().SetOwner(player).SetEnterBeamType(BeamType.loot_bolt).AddLoot(_loots.LootItems).BuildAndAddToZone(_zone, CurrentPosition);
                    if (ep > 0) player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Artifact, ep);
                    scope.Complete();
                }
            });
        }
    }

}
