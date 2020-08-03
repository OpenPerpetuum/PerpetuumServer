﻿using Perpetuum.Players;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting.Strategies;
using System.Linq;

namespace Perpetuum.Services.RiftSystem.StrongholdRifts
{
    public class StrongholdExitRift : Portal
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private IZone _destinationZone;
        private DockingBase _base;

        public StrongholdExitRift(ITeleportStrategyFactories teleportStrategyFactories, IZoneManager zoneManager)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
            _destinationZone = zoneManager.GetZone(8);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _base = _destinationZone.Units.OfType<DockingBase>().First();
            base.OnEnterZone(zone, enterType);
        }

        public override void UseItem(Player player)
        {
            base.UseItem(player);

            var teleport = _teleportStrategyFactories.TeleportToAnotherZoneFactory(_destinationZone);
            teleport.TargetPosition = UndockSpawnPositionSelector.SelectSpawnPosition(_base);
            teleport.DoTeleportAsync(player);
        }
    }
}
