using Perpetuum.Accounting.Characters;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Teleporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.LandMines
{
    public class LandMineDeployer : ItemDeployer
    {
        private readonly IEntityServices _entityServices;

        public LandMineDeployer(IEntityServices entityServices) : base(entityServices)
        {
            _entityServices = entityServices;
        }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            //zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);

            PrivateCorporation corporation = player.Character.GetPrivateCorporationOrThrow();

            int maxLandMines = 100; // corporation.GetMaximumProbeAmount();
            corporation.GetLandMineEids().Count().ThrowIfGreaterOrEqual(maxLandMines, ErrorCodes.MaximumAmountOfProbesReached);

            LandMine landMine = (LandMine)_entityServices.Factory.CreateWithRandomEID(DeployableItemEntityDefault);
            landMine.CheckDeploymentAndThrow(zone, spawnPosition); //Enforce min-distance separations
            landMine.Owner = corporation.Eid;
            ZoneStorage zoneStorage = zone.Configuration.GetStorage();
            landMine.Parent = zoneStorage.Eid;
            landMine.Save();

            zone.UnitService.AddUserUnit(landMine, spawnPosition);

            List<Character> initialMembers = corporation.GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO).Select(cm => cm.character).ToList();
            initialMembers.Add(player.Character);

            landMine.Init(initialMembers.Distinct());
            landMine.SetDespawnTime(ProximityProbeDespawnTime);

            return landMine;
        }

        protected override ErrorCodes CanDeploy(IZone zone, Unit unit, Position spawnPosition, Player player)
        {
            /*
            if (zone.Configuration.Protected)
            {
                return ErrorCodes.OnlyUnProtectedZonesAllowed;
            }
            */

            if (!zone.Configuration.Terraformable)
            {
                if (zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_BASE).Any())
                {
                    return ErrorCodes.NotDeployableNearObject;
                }

                if (zone.Units.OfType<TeleportColumn>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_TELEPORT).Any())
                {
                    return ErrorCodes.NotDeployableNearObject;
                }
            }
            else
            {
                if (zone.Units.OfType<LandMine>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_LANDMINE).Any())
                {
                    return ErrorCodes.TooCloseToOtherDevice;
                }
            }

            return base.CanDeploy(zone, unit, spawnPosition, player);
        }

        private TimeSpan ProximityProbeDespawnTime
        {
            get
            {
                Items.ItemPropertyModifier m = GetPropertyModifier(AggregateField.despawn_time);

                return TimeSpan.FromMilliseconds((int)m.Value);
            }
        }
    }
}
