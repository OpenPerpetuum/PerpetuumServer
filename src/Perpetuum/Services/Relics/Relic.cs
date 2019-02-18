using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Threading;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Services.Relics
{
    public class Relic : Unit
    {
        private int _id;
        private RelicInfo _info;
        private IZone _zone;
        private bool _alive;

        private const double ACTIVATION_RANGE = 3; //30m
        private const double RESPAWN_PROXIMITY = 10.0 * ACTIVATION_RANGE;

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(4);

        private RelicLootItems _loots;

        public Relic()
        {

        }

        public Relic(int id, RelicInfo info, IZone zone, Position position)
        {
            _id = id;
            _info = info;
            _zone = zone;
            CurrentPosition = position;
            SetAlive(true);
        }

        public void InitUnitProperties(Unit unit)
        {
            this.BasePropertyModifiers = unit.BasePropertyModifiers;
            this.ED = unit.ED;
            this.Eid = unit.Eid;
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
                    LootContainer.Create().SetOwner(player).SetEnterBeamType(BeamType.loot_bolt).AddLoot(_loots.LootItems).BuildAndAddToZone(_zone, _loots.Position);
                    if (ep > 0) player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Artifact, ep);
                    scope.Complete();
                }
            });
        }
    }



    public class RelicRepository
    {
        private RelicReader _relicReader;
        private RelicLootReader _relicLootReader;
        private IZone _zone;

        public RelicRepository(IZone zone)
        {
            _relicReader = new RelicReader(zone);
            _relicLootReader = new RelicLootReader();
            _zone = zone;
        }

        public IEnumerable<Relic> GetAll()
        {
            return _relicReader.GetAll();
        }

        public IEnumerable<Relic> GetAllOfType(RelicInfo info)
        {
            return _relicReader.GetAllWithInfo(info);
        }

        public IEnumerable<IRelicLoot> GetRelicLoots(RelicInfo info)
        {
            return _relicLootReader.GetRelicLoots(info);
        }

    }


    public class RelicLootReader
    {

        protected IRelicLoot CreateRelicLootFromRecord(IDataRecord record)
        {
            return new RelicLoot(record);
        }

        public IEnumerable<IRelicLoot> GetRelicLoots(RelicInfo info)
        {
            var loots = Db.Query().CommandText("SELECT definition,minquantity,maxquantity,chance,relicinfoid,packed FROM relicloots WHERE relicinfoid = @relicInfoId")
                .SetParameter("@relicInfoId", info.GetID())
                .Execute()
                .Select(CreateRelicLootFromRecord);

            var resultList = new List<IRelicLoot>();
            foreach (var loot in loots)
            {
                resultList.Add(loot);
            }
            return resultList;
        }

    }

    public class RelicReader
    {
        private IZone _zone;

        public RelicReader(IZone zone)
        {
            _zone = zone;
        }

        protected Relic CreateRelicFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var relicinfoid = record.GetValue<int>("relicinfoid");
            var zoneid = record.GetValue<int>("zoneid");
            var x = record.GetValue<int>("x");
            var y = record.GetValue<int>("y");

            var info = RelicInfo.GetByIDFromDB(relicinfoid);

            var relic = new Relic(id, info, _zone, new Position(x, y));

            return relic;
        }

        public IEnumerable<Relic> GetAllWithInfo(RelicInfo info)
        {
            var relics = Db.Query().CommandText("SELECT id, relicinfoid, zoneid x, y FROM relics WHERE zoneid = @zoneId AND relicinfoid = @relicInfoId")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@relicInfoId", info.GetID())
                .Execute()
                .Select(CreateRelicFromRecord);

            var resultList = new List<Relic>();
            foreach (var relic in relics)
            {
                resultList.Add(relic);
            }
            return resultList;
        }

        public IEnumerable<Relic> GetAll()
        {
            var relics = Db.Query().CommandText("SELECT id, relicinfoid, zoneid x, y FROM relics WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(CreateRelicFromRecord);

            var resultList = new List<Relic>();
            foreach (var relic in relics)
            {
                resultList.Add(relic);
            }
            return resultList;
        }
    }
}
