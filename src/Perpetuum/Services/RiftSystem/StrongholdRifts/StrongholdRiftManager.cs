using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;

namespace Perpetuum.Services.RiftSystem.StrongholdRifts
{
    /// <summary>
    /// RiftManager for Stronghold
    /// </summary>
    public class StrongholdRiftManager : IRiftManager
    {
        private readonly IZone _zone;
        private readonly IZoneManager _zoneManager;
        private readonly IEntityServices _entityServices;
        private readonly IEnumerable<StrongholdRiftLocation> _spawnLocations;

        public StrongholdRiftManager(IZone zone, IEntityServices entityServices, ICustomRiftConfigReader customRiftConfigReader, IZoneManager zoneManager)
        {
            _zone = zone;
            _spawnLocations = StrongholdRiftLocationRepository.GetAllInZone(zone, customRiftConfigReader);
            _entityServices = entityServices;
            _zoneManager = zoneManager;
        }

        private bool _spawnedAll = false;
        private bool _spawning = false;
        public void Update(TimeSpan time)
        {
            if (_spawnedAll || _spawning)
                return;

            SpawnAll();
        }

        private void SpawnAll()
        {
            _spawning = true;
            foreach (var location in _spawnLocations)
            {
                SpawnRift(location);
            }
            _spawnedAll = true;
            _spawning = false;
        }

        private void SpawnRift(StrongholdRiftLocation spawn)
        {
            var targetDestination = spawn.RiftConfig.TryGetValidDestination(_zoneManager);
            if (targetDestination == null)
                return;

            var zoneTarget = _zoneManager.GetZone(targetDestination.ZoneId);
            var targetPos = targetDestination.GetPosition(zoneTarget);
            var rift = (StrongholdExitRift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.STRONGHOLD_EXIT_RIFT);
            rift.AddToZone(_zone, spawn.Location, ZoneEnterType.NpcSpawn);
            rift.SetTarget(zoneTarget, targetPos);
            rift.SetConfig(spawn.RiftConfig);
            Logger.Info(string.Format("Rift spawned on zone {0} {1} ({2})", _zone.Id, rift.ED.Name, rift.CurrentPosition));
        }
    }
}
