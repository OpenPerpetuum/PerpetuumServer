using Perpetuum.ExportedTypes;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Intrusion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Perpetuum.Services.Relics
{
    public class ZoneRelicManager : AbstractRelicManager
    {
        //Spawn time params
        private readonly TimeSpan RESPAWN_RANDOM_WINDOW = TimeSpan.FromHours(1);
        private readonly TimeSpan _respawnRate = TimeSpan.FromHours(1.5);

        private readonly Random _random;

        private readonly IEnumerable<RelicSpawnInfo> _spawnInfos;

        //Beam Draw refresh
        private readonly TimeSpan _relicRefreshRate = TimeSpan.FromSeconds(19.95);

        //DB-accessing objects
        private readonly RelicZoneConfigRepository relicZoneConfigRepository;
        private readonly RelicSpawnInfoRepository relicSpawnInfoRepository;

        //Child RelicManagers
        private readonly IList<OutpostRelicManager> outpostRelicManagers = new List<OutpostRelicManager>();

        private readonly RiftSpawnPositionFinder _spawnPosFinder;

        private readonly IZone _zone;
        protected override IZone Zone => _zone;
        private readonly ReaderWriterLockSlim _lock;
        protected override ReaderWriterLockSlim Lock => _lock;

        public ZoneRelicManager(IZone zone)
        {
            _lock = new ReaderWriterLockSlim();
            _random = new Random();
            _relics = new List<IRelic>();
            _zone = zone;
            _spawnPosFinder = new PveRiftSpawnPositionFinder(zone);
            if (zone.Configuration.Terraformable)
            {
                _spawnPosFinder = new PvpRiftSpawnPositionFinder(zone);
            }
            // init repositories and extract data
            relicZoneConfigRepository = new RelicZoneConfigRepository(zone);
            relicSpawnInfoRepository = new RelicSpawnInfoRepository(zone);
            relicLootGenerator = new RelicLootGenerator();

            //Get Zone Relic-Configuration data
            RelicZoneConfig config = relicZoneConfigRepository.GetZoneConfig();
            _max_relics = config.GetMax();
            _respawnRate = config.GetTimeSpan();
            _respawnRandomized = RollNextSpawnTime();

            _spawnInfos = relicSpawnInfoRepository.GetAll();
        }

        public override void Start()
        {
            base.Start();
            List<Outpost> outposts = _zone.Units.OfType<Outpost>().ToList();
            foreach (Outpost outpost in outposts)
            {
                outpostRelicManagers.Add(new OutpostRelicManager(outpost));
            }
            foreach (OutpostRelicManager childManagers in outpostRelicManagers)
            {
                childManagers.Start();
            }
        }

        public override void Stop()
        {
            foreach (OutpostRelicManager childManagers in outpostRelicManagers)
            {
                childManagers.Stop();
            }
            base.Stop();
        }

        public override void Update(TimeSpan time)
        {
            base.Update(time);
            foreach (OutpostRelicManager childManagers in outpostRelicManagers)
            {
                childManagers.Update(time);
            }
        }

        protected override IRelic MakeRelic(RelicInfo info, Position position)
        {
            return Relic.BuildAndAddToZone(info, _zone, position, relicLootGenerator.GenerateLoot(info));
        }

        protected override TimeSpan RollNextSpawnTime()
        {
            double randomFactor = _random.NextDouble() - 0.5;
            double minutesToAdd = RESPAWN_RANDOM_WINDOW.TotalMinutes * randomFactor;

            return _respawnRate.Add(TimeSpan.FromMinutes(minutesToAdd));
        }


        protected override RelicInfo GetNextRelicType()
        {
            IEnumerable<RelicSpawnInfo> spawnRates = _spawnInfos;
            double sumRate = spawnRates.Sum(r => r.GetRate());
            double minRate = 0.0;
            double chance = _random.NextDouble();
            RelicInfo info = null;
            foreach (RelicSpawnInfo spawnRate in spawnRates)
            {
                double rate = spawnRate.GetRate() / sumRate;
                double maxRate = rate + minRate;

                if (minRate < chance && chance <= maxRate)
                {
                    info = spawnRate.GetRelicInfo();
                    break;
                }
                minRate += rate;
            }
            return info;
        }

        protected override Point FindRelicPosition(RelicInfo info)
        {
            if (info.HasStaticPosistion) //If the relic spawn info has a valid static position defined - use that
            {
                return info.GetPosition().ToPoint();
            }
            return _spawnPosFinder.FindSpawnPosition(); //Else use random-walkable
        }

        protected override List<Dictionary<string, object>> DoGetRelicListDictionary()
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (OutpostRelicManager childManagers in outpostRelicManagers)
            {
                list.AddMany(childManagers.GetRelicListDictionary());
            }
            list.AddMany(_relics.Select(r => r.ToDebugDictionary()).ToList());
            return list;
        }

        protected override void RefreshBeam(IRelic relic)
        {
            RelicInfo info = relic.GetRelicInfo();
            int level = info.GetLevel();
            int faction = info.GetFaction();
            Position position = relic.GetPosition();
            BeamType factionalBeamType = BeamType.orange_20sec;
            switch (faction)
            {
                case 0:
                    factionalBeamType = BeamType.orange_20sec;
                    break;
                case 1:
                    factionalBeamType = BeamType.green_20sec;
                    break;
                case 2:
                    factionalBeamType = BeamType.blue_20sec;
                    break;
                case 3:
                    factionalBeamType = BeamType.red_20sec;
                    break;
                default:
                    factionalBeamType = BeamType.orange_20sec;
                    break;
            }

            Position p = _zone.FixZ(position);
            BeamBuilder beamBuilder = Beam.NewBuilder().WithType(BeamType.artifact_radar).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);
            beamBuilder = Beam.NewBuilder().WithType(BeamType.nature_effect).WithTargetPosition(position)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(_relicRefreshRate);
            _zone.CreateBeam(beamBuilder);
            for (int i = 0; i < level; i++)
            {
                beamBuilder = Beam.NewBuilder().WithType(factionalBeamType).WithTargetPosition(p.AddToZ((3.5 * i) + 1.0))
                    .WithState(BeamState.Hit)
                    .WithDuration(_relicRefreshRate);
                _zone.CreateBeam(beamBuilder);
            }
        }
    }
}
