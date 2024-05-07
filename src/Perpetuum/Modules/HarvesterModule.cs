using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Modules
{
    public class HarvesterModule : GathererModule
    {
        private const int MAX_EP_PER_DAY = 1440;
        private readonly PlantHarvester.Factory _plantHarvesterFactory;
        private readonly HarvestingAmountModifierProperty _harverstingAmountModifier;

        public HarvesterModule(CategoryFlags ammoCategoryFlags, PlantHarvester.Factory plantHarvesterFactory) : base(ammoCategoryFlags, true)
        {
            _plantHarvesterFactory = plantHarvesterFactory;
            _harverstingAmountModifier = new HarvestingAmountModifierProperty(this);
            AddProperty(_harverstingAmountModifier);
            cycleTime.AddEffectModifier(AggregateField.effect_harvesting_cycle_time_modifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            base.UpdateProperty(field);

            switch (field)
            {
                case AggregateField.harvesting_amount_modifier:
                case AggregateField.effect_harvesting_amount_modifier:
                    {
                        _harverstingAmountModifier.Update();

                        break;
                    }
            }
        }

        protected override int CalculateEp(int materialType)
        {

            HarvesterModule[] activeGathererModules = this is RemoteControlledHarvesterModule
                ? ParentRobot.ActiveModules.OfType<RemoteControlledHarvesterModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray()
                : ParentRobot.ActiveModules.OfType<HarvesterModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray();

            if (activeGathererModules.Length == 0)
            {
                return 0;
            }

            TimeSpan avgCycleTime = activeGathererModules.Select(m => m.CycleTime).Average();
            double t = TimeSpan.FromDays(1).Divide(avgCycleTime);
            double chance = (double)MAX_EP_PER_DAY / t;

            chance /= activeGathererModules.Length;

            double rand = FastRandom.NextDouble();

            return rand <= chance ? 1 : 0;
        }

        protected override void OnAction()
        {
            IZone zone = Zone;

            if (zone == null)
            {
                return;
            }

            DoHarvesting(zone);
            ConsumeAmmo();
        }

        public void DoHarvesting(IZone zone)
        {
            TerrainLock terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);

            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (TransactionScope scope = Db.CreateTransaction())
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    Zones.Terrains.Materials.Plants.PlantInfo plantInfo = zone.Terrain.Plants.GetValue(terrainLock.Location);
                    double amountModifier = _harverstingAmountModifier.GetValueByPlantType(plantInfo.type);

                    IPlantHarvester plantHarvester = _plantHarvesterFactory(zone, amountModifier);

                    System.Collections.Generic.IEnumerable<ItemInfo> harvestedPlants = plantHarvester.HarvestPlant(terrainLock.Location);

                    Debug.Assert(ParentRobot != null, "ParentRobot != null");

                    Robots.RobotInventory container = ParentRobot.GetContainer();

                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();

                    Player player = ParentRobot is RemoteControlledCreature remoteControlledCreature &&
                        remoteControlledCreature.CommandRobot is Player ownerPlayer
                        ? ownerPlayer
                        : ParentRobot as Player;

                    Debug.Assert(player != null, "player != null");

                    foreach (ItemInfo extractedMaterial in harvestedPlants)
                    {
                        Item item = (Item)Factory.CreateWithRandomEID(extractedMaterial.Definition);

                        item.Owner = Owner;
                        item.Quantity = extractedMaterial.Quantity;
                        container.AddItem(item, true);

                        int extractedHarvestDefinition = extractedMaterial.Definition;
                        int extractedQuantity = extractedMaterial.Quantity;

                        player.MissionHandler.EnqueueMissionEventInfo(new HarvestPlantEventInfo(player, extractedHarvestDefinition, extractedQuantity, terrainLock.Location));
                        player.Zone?.HarvestLogHandler.EnqueueHarvestLog(extractedHarvestDefinition, extractedQuantity);
                    }

                    container.Save();
                    OnGathererMaterial(zone, player, (int)plantInfo.type);
                    Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                    scope.Complete();
                }
            }
        }
    }
}