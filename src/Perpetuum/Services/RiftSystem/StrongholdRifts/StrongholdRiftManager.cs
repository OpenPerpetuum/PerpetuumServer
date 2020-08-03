using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Perpetuum.Services.RiftSystem.StrongholdRifts
{
    public class StrongholdRiftManager : IRiftManager
    {
        private readonly IZone _zone;
        private readonly IEntityServices _entityServices;
        private IEnumerable<StrongholdRiftLocation> _spawnLocations;
        private readonly object _lock = new object();

        public StrongholdRiftManager(IZone zone, IEntityServices entityServices)
        {
            _zone = zone;
            _spawnLocations = StrongholdRiftLocationRepository.GetAllInZone(zone).ToConcurrentQueue();
            _entityServices = entityServices;
        }

        public void Update(TimeSpan time)
        {
            IEnumerable<StrongholdRiftLocation> unspawnedRiftLocations;
            lock (_lock)
            {
                unspawnedRiftLocations = _spawnLocations.Where(p => !p.Spawned).ToArray();
            }

            foreach (var location in unspawnedRiftLocations)
            {
                SpawnRift(location);
            }
        }

        private void SpawnRift(StrongholdRiftLocation spawn)
        {
            // Exit already spawned here .. shouldn't happen
            if (_zone.Units.WithinRange2D(spawn.Location, 1).OfType<StrongholdRiftLocation>().Any())
                return;

            var rift = (StrongholdExitRift)_entityServices.Factory.CreateWithRandomEID(DefinitionNames.STRONGHOLD_EXIT_RIFT);
            rift.RemovedFromZone += OnRiftRemovedFromZone;
            rift.AddToZone(_zone, spawn.Location, ZoneEnterType.NpcSpawn);
            spawn.Spawned = true;
            Logger.Info(string.Format("Rift spawned on zone {0} {1} ({2})", _zone.Id, rift.ED.Name, rift.CurrentPosition));
        }

        // Also shouldnt happen, they are invulnerable
        private void OnRiftRemovedFromZone(Unit unit)
        {
            lock (_lock)
                _spawnLocations.Where(spawn => unit.CurrentPosition.IsWithinRangeOf2D(spawn.Location, 1)).SingleOrDefault().Spawned = false;
        }

    }

}
