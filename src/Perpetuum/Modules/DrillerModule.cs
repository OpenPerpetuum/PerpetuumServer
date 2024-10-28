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
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Transactions;

namespace Perpetuum.Modules
{
    public class DrillerModule : GathererModule
    {
        private const int MAX_EP_PER_DAY = 1440;
        private readonly ISet<int> LIQUIDS = new HashSet<int>(new int[]
        {
            (int)MaterialType.Crude,
            (int)MaterialType.Liquizit,
            (int)MaterialType.Epriton
        });

        protected readonly MaterialHelper MaterialHelper;
        protected readonly ItemProperty MiningAmountModifier;
        protected readonly RareMaterialHandler RareMaterialHandler;

        public DrillerModule(CategoryFlags ammoCategoryFlags, RareMaterialHandler rareMaterialHandler, MaterialHelper materialHelper)
            : base(ammoCategoryFlags, true)
        {
            RareMaterialHandler = rareMaterialHandler;
            MaterialHelper = materialHelper;
            MiningAmountModifier = new MiningAmountModifierProperty(this);
            AddProperty(MiningAmountModifier);
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
            switch (field)
            {
                case AggregateField.mining_amount_modifier:
                case AggregateField.effect_mining_amount_modifier:
                case AggregateField.drone_amplification_mining_amount_modifier:
                case AggregateField.effect_excavator_mining_amount_modifier:
                    {
                        MiningAmountModifier.Update();

                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        public List<ItemInfo> Extract(MineralLayer layer, Point location, uint amount)
        {
            if (!layer.HasMineral(location))
            {
                return new List<ItemInfo>();
            }

            MineralExtractor extractor = new MineralExtractor(location, amount, MaterialHelper);
            layer.AcceptVisitor(extractor);

            return new List<ItemInfo>(extractor.Items);
        }

        protected override void OnAction()
        {
            IZone zone = Zone;
            if (zone != null)
            {
                DoExtractMinerals(zone);
            }

            ConsumeAmmo();
            ParentRobot.IncreaseOverheatByValue(
                EffectType.effect_excavator,
                GeneratedHeat);
        }

        protected override int CalculateEp(int materialType)
        {
            DrillerModule[] activeGathererModules = this is RemoteControlledDrillerModule
                ? ParentRobot.ActiveModules.OfType<RemoteControlledDrillerModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray()
                : ParentRobot.ActiveModules.OfType<DrillerModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray();
            if (activeGathererModules.Length == 0)
            {
                return 0;
            }

            TimeSpan avgCycleTime = activeGathererModules.Select(m => m.CycleTime).Average();
            TimeSpan t = TimeSpan.FromDays(1).Divide(avgCycleTime);
            double chance = (double)MAX_EP_PER_DAY / t.Ticks;
            chance /= activeGathererModules.Length;
            if (LIQUIDS.Contains(materialType))
            {
                chance /= 2.0;
            }

            double rand = FastRandom.NextDouble();

            return rand <= chance
                ? 1
                : 0;
        }

        public virtual void DoExtractMinerals(IZone zone)
        {
            TerrainLock terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);
            MaterialType materialType;
            if (ParentRobot is RemoteControlledCreature)
            {
                materialType = zone.Terrain.GetMaterialTypeAtPosition(terrainLock.Location);
            }
            else
            {
                if (!(GetAmmo() is MiningAmmo ammo))
                {
                    return;
                }

                materialType = ammo.MaterialType;
            }

            MaterialInfo materialInfo = MaterialHelper.GetMaterialInfo(materialType);
            CheckEnablerEffect(materialInfo, terrainLock);
            MineralLayer mineralLayer = zone.Terrain
                .GetMineralLayerOrThrow(
                    materialInfo.Type,
                    (PerpetuumException ex) =>
                        (ParentRobot as RemoteControlledCreature)
                            .ProcessIndustrialTarget(terrainLock.Location.Center, 0));
            double materialAmount = materialInfo.Amount * MiningAmountModifier.Value;
            List<ItemInfo> extractedMaterials = Extract(mineralLayer, terrainLock.Location, (uint)materialAmount);
            _ = extractedMaterials.Count
                .ThrowIfEqual(
                    0,
                    ErrorCodes.NoMineralOnTile,
                    (PerpetuumException ex) =>
                    {
                        RemoteControlledCreature creature = ParentRobot as RemoteControlledCreature;
                        creature?.ProcessIndustrialTarget(terrainLock.Location.Center, 0);
                    });
            extractedMaterials
                .AddRange(RareMaterialHandler.GenerateRareMaterials(materialInfo.EntityDefault.Definition));
            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);
            using (TransactionScope scope = Db.CreateTransaction())
            {
                Debug.Assert(ParentRobot != null, "ParentRobot != null");
                Robots.RobotInventory container = ParentRobot.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();
                Player player = ParentRobot is RemoteControlledCreature remoteControlledCreature &&
                    remoteControlledCreature.CommandRobot is Player ownerPlayer
                    ? ownerPlayer
                    : ParentRobot as Player;
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
                                terrainLock.Location));
                    player.Zone?.MiningLogHandler.EnqueueMiningLog(drilledMineralDefinition, drilledQuantity);
                }

                //save container
                container.Save();
                OnGathererMaterial(zone, player, (int)materialInfo.Type);
                Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                scope.Complete();
            }

            ParentRobot.IncreaseOverheat(EffectType.effect_excavator);
        }

        protected void CheckEnablerEffect(MaterialInfo materialInfo, Position position)
        {
            if (!Zone.Configuration.Terraformable)
            {
                return;
            }

            if (!materialInfo.EnablerExtensionRequired)
            {
                return;
            }

            bool containsEnablerEffect =
                ParentRobot.EffectHandler.ContainsEffect(EffectCategory.effcat_pbs_mining_tower_effect) ||
                    (ParentRobot is RemoteControlledCreature rcu &&
                        rcu.CommandRobot is Player player &&
                        player.EffectHandler.ContainsEffect(EffectCategory.effcat_pbs_mining_tower_effect));
            containsEnablerEffect
                .ThrowIfFalse(
                    ErrorCodes.MiningEnablerEffectRequired,
                        (PerpetuumException ex) =>
                            (ParentRobot as RemoteControlledCreature)
                                .ProcessIndustrialTarget(position.Center, 0));
        }

        protected void CheckEnablerEffect(MaterialInfo materialInfo, TerrainLock terrainLock)
        {
            CheckEnablerEffect(materialInfo, terrainLock.Location);
        }
    }
}