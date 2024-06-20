using Perpetuum.ExportedTypes;

namespace Perpetuum.Modules.ModuleProperties
{
    public class MiningAmountModifierProperty : ModuleProperty
    {
        private readonly GathererModule _module;

        public MiningAmountModifierProperty(GathererModule module) : base(module, AggregateField.mining_amount_modifier)
        {
            _module = module;
            AddEffectModifier(AggregateField.effect_mining_amount_modifier);
            AddEffectModifier(AggregateField.drone_amplification_mining_amount_modifier);
        }

        protected override double CalculateValue()
        {
            if (module.ParentRobot == null)
            {
                return 1.0;
            }

            Items.ItemPropertyModifier m = module.ParentRobot.GetPropertyModifier(AggregateField.mining_amount_modifier);
            MiningAmmo ammo = (MiningAmmo)_module.GetAmmo();

            ammo?.ApplyMiningAmountModifier(ref m);
            module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.effect_mining_amount_modifier, ref m);
            module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.drone_amplification_mining_amount_modifier, ref m);

            return m.Value;
        }
    }
}
