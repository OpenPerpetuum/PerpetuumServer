using Perpetuum.ExportedTypes;

namespace Perpetuum.Modules.ModuleProperties
{
    public class MiningAmountModifierProperty : ModuleProperty
    {
        private new readonly GathererModule module;

        public MiningAmountModifierProperty(GathererModule module)
        : base(module, AggregateField.mining_amount_modifier)
        {
            this.module = module;
            AddEffectModifier(AggregateField.effect_mining_amount_modifier);
            AddEffectModifier(AggregateField.drone_amplification_mining_amount_modifier);
            AddEffectModifier(AggregateField.drone_remote_command_translation_mining_amount_modifier);
            AddEffectModifier(AggregateField.effect_excavator_mining_amount_modifier);
        }

        protected override double CalculateValue()
        {
            if (base.module.ParentRobot == null)
            {
                return 1.0;
            }

            Items.ItemPropertyModifier m = base.module.ParentRobot.GetPropertyModifier(AggregateField.mining_amount_modifier);
            MiningAmmo ammo = (MiningAmmo)module.GetAmmo();

            ammo?.ApplyMiningAmountModifier(ref m);
            base.module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.effect_mining_amount_modifier, ref m);
            base.module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.drone_amplification_mining_amount_modifier, ref m);
            base.module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.drone_remote_command_translation_mining_amount_modifier, ref m);
            base.module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.effect_excavator_mining_amount_modifier, ref m);

            return m.Value;
        }
    }
}
