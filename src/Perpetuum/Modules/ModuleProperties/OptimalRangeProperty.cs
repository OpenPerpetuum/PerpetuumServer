using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.Weapons;

namespace Perpetuum.Modules.ModuleProperties
{
    public class OptimalRangeProperty : ModuleProperty
    {
        private readonly ActiveModule _module;

        public OptimalRangeProperty(ActiveModule module) : base(module, AggregateField.optimal_range)
        {
            _module = module;
            AddEffectModifier(AggregateField.effect_optimal_range_modifier);
            AddEffectModifier(AggregateField.drone_amplification_long_range_modifier);
            AddEffectModifier(AggregateField.effect_dreadnought_optimal_range_modifier);
        }

        protected override double CalculateValue()
        {
            ItemPropertyModifier optimalRange = ItemPropertyModifier.Create(AggregateField.optimal_range);
            Items.Ammos.Ammo ammo = _module.GetAmmo();

            if (module is MissileWeaponModule m)
            {
                if (ammo != null)
                {
                    optimalRange = ammo.OptimalRangePropertyModifier;

                    ItemPropertyModifier missileRangeMod = m.MissileRangeModifier.ToPropertyModifier();

                    missileRangeMod.Modify(ref optimalRange);
                    module.ApplyRobotPropertyModifiers(ref optimalRange);
                }
            }
            else
            {
                optimalRange = module.GetPropertyModifier(AggregateField.optimal_range);
                ammo?.ModifyOptimalRange(ref optimalRange);
            }

            ApplyEffectModifiers(ref optimalRange);

            return optimalRange.Value;
        }
    }
}
