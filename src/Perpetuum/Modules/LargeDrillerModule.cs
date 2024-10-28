using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Modules
{
    public class LargeDrillerModule : DrillerModule
    {
        public LargeDrillerModule(CategoryFlags ammoCategoryFlags, RareMaterialHandler rareMaterialHandler, MaterialHelper materialHelper)
            : base(ammoCategoryFlags, rareMaterialHandler, materialHelper)
        {
        }

        protected override long GeneratedHeat => 5;

        public override void DoExtractMinerals(IZone zone)
        {
            ParentRobot.IncreaseOverheat(EffectType.effect_excavator);

            Position centralTile = ParentRobot.PositionWithHeight;
            MaterialType materialType;
            if (!(GetAmmo() is MiningAmmo ammo))
            {
                return;
            }

            materialType = ammo.MaterialType;

            MaterialInfo materialInfo = MaterialHelper.GetMaterialInfo(materialType);
            CheckEnablerEffect(materialInfo, centralTile);
            MineralLayer mineralLayer = zone.Terrain
                .GetMineralLayerOrThrow(materialInfo.Type);
            double materialAmount = materialInfo.Amount * MiningAmountModifier.Value;
            List<Position> mineralPositions = centralTile.GetEightNeighbours(ParentRobot.Zone.Size).ToList();
            mineralPositions.Add(centralTile);

            int emptyTilesCounter = 0;

            // make it parallel 
            foreach (Position position in mineralPositions)
            {
                List<ItemInfo> extractedMaterials = Extract(mineralLayer, position, (uint)materialAmount);
                if (extractedMaterials.Count == 0)
                {
                    emptyTilesCounter++;
                    _ = emptyTilesCounter
                        .ThrowIfEqual(9, ErrorCodes.NoMineralOnTile);

                    continue;
                }

                extractedMaterials
                    .AddRange(RareMaterialHandler.GenerateRareMaterials(materialInfo.EntityDefault.Definition));
                CreateBeam(position.Center, BeamState.AlignToTerrain);
                using (TransactionScope scope = Db.CreateTransaction())
                {
                    Debug.Assert(ParentRobot != null, "ParentRobot != null");
                    Robots.RobotInventory container = ParentRobot.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();
                    Player player = ParentRobot as Player;
                    Debug.Assert(player != null, "player != null");
                    foreach (ItemInfo material in extractedMaterials)
                    {
                        Item item = (Item)Factory.CreateWithRandomEID(material.Definition);
                        item.Owner = Owner;
                        item.Quantity = material.Quantity;
                        container.AddItem(item, true);
                        int drilledMineralDefinition = material.Definition;
                        int drilledQuantity = material.Quantity;
                        player.MissionHandler
                            .EnqueueMissionEventInfo(
                                new DrillMineralEventInfo(
                                    player,
                                    drilledMineralDefinition,
                        drilledQuantity,
                                    position));
                        player.Zone?.MiningLogHandler.EnqueueMiningLog(drilledMineralDefinition, drilledQuantity);
                    }

                    //save container
                    container.Save();
                    OnGathererMaterial(zone, player, (int)materialInfo.Type);
                    Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                    scope.Complete();
                }
            }
        }
    }
}
