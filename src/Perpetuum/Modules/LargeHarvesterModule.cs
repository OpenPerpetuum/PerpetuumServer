using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Modules
{
    public sealed class LargeHarvesterModule : HarvesterModule
    {
        public LargeHarvesterModule(CategoryFlags ammoCategoryFlags, PlantHarvester.Factory plantHarvesterFactory)
            : base(ammoCategoryFlags, plantHarvesterFactory)
        {
        }

        public override void DoHarvesting(IZone zone)
        {
            Position centralTile = ParentRobot.PositionWithHeight.OffsetInDirection(ParentRobot.Direction, 3);
            List<Position> mineralPositions = centralTile.GetTwentyFourNeighbours(ParentRobot.Zone.Size).ToList();
            mineralPositions.Add(centralTile);

            int emptyTilesCounter = 0;

            List<(string resourceName, int quantity)> resourceStats = new List<(string resourceName, int quantity)>();

            // make it parallel 
            foreach (Position position in mineralPositions)
            {
                using (TransactionScope scope = Db.CreateTransaction())
                {
                    using (new TerrainUpdateMonitor(zone))
                    {
                        PlantInfo plantInfo = zone.Terrain.Plants.GetValue(position);
                        if (plantInfo.type == PlantType.NotDefined ||
                            Zone.Configuration.PlantRules.GetPlantRule(plantInfo.type) == null ||
                            plantInfo.material <= 0)
                        {
                            emptyTilesCounter++;
                            _ = emptyTilesCounter
                                .ThrowIfEqual(25, ErrorCodes.NoPlantOnTile);

                            continue;
                        }

                        CreateBeam(position, BeamState.AlignToTerrain);

                        double amountModifier = _harverstingAmountModifier.GetValueByPlantType(plantInfo.type);
                        IPlantHarvester plantHarvester = _plantHarvesterFactory(zone, amountModifier);
                        IEnumerable<ItemInfo> harvestedPlants = plantHarvester.HarvestPlant(position);


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
                            player.MissionHandler.EnqueueMissionEventInfo(new HarvestPlantEventInfo(player, extractedHarvestDefinition, extractedQuantity, position));
                            player.Zone?.HarvestLogHandler.EnqueueHarvestLog(extractedHarvestDefinition, extractedQuantity);

                            resourceStats.Add((extractedMaterial.EntityDefault.Name, extractedMaterial.Quantity));
                        }

                        container.Save();
                        OnGathererMaterial(zone, player, (int)plantInfo.type);
                        Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                        scope.Complete();
                    }
                }
            }

            foreach (var (resourceName, quantity) in resourceStats)
            {
                try
                {
                    Db.Query()
                        .CommandText("exec sp_RecordResourceGathered @gathered_on, @resource_name, @quantity")
                        .SetParameter("@gathered_on", DateTime.UtcNow)
                        .SetParameter("@resource_name", resourceName)
                        .SetParameter("@quantity", quantity)
                        .ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }

            GenerateHeat(EffectType.effect_excavator, 6);
        }
    }
}
