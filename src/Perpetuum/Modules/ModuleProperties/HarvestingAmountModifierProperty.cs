using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Modules.ModuleProperties
{
    public class HarvestingAmountModifierProperty : ModuleProperty
    {
        public HarvestingAmountModifierProperty(GathererModule module) : base(module, AggregateField.harvesting_amount_modifier)
        {
            AddEffectModifier(AggregateField.effect_harvesting_amount_modifier);
            AddEffectModifier(AggregateField.drone_amplification_harvesting_amount_modifier);
        }

        public double GetValueByPlantType(PlantType plantType)
        {
            AggregateField modifier = AggregateField.undefined;

            switch (plantType)
            {
                case PlantType.RustBush:
                    {
                        modifier = AggregateField.harvesting_amount_helioptris_modifier;

                        break;
                    }
                case PlantType.SlimeRoot:
                    {
                        modifier = AggregateField.harvesting_amount_triandlus_modifier;

                        break;
                    }
                case PlantType.ElectroPlant:
                    {
                        modifier = AggregateField.harvesting_amount_electroplant_modifier;

                        break;
                    }
                case PlantType.TreeIron:
                    {
                        modifier = AggregateField.harvesting_amount_prismocitae_modifier;

                        break;
                    }
            }

            Items.ItemPropertyModifier property = ToPropertyModifier();

            if (module.ParentRobot != null && modifier != AggregateField.undefined)
            {
                Items.ItemPropertyModifier mod = module.ParentRobot.GetPropertyModifier(modifier);

                mod.Modify(ref property);
            }

            return property.Value;
        }

        protected override double CalculateValue()
        {
            if (module.ParentRobot == null)
            {
                return 1.0;
            }

            Items.ItemPropertyModifier p = module.ParentRobot.GetPropertyModifier(AggregateField.harvesting_amount_modifier);

            ApplyEffectModifiers(ref p);
            module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.drone_amplification_harvesting_amount_modifier, ref p);

            return p.Value;
        }
    }
}
