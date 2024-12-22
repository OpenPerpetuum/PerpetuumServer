using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.RemoteControl;

namespace Perpetuum.Modules.ModuleProperties
{
    public class RemoteCommandTranslatorMiningAmountProperty : ModuleProperty
    {
        private readonly RemoteCommandTranslatorModule _module;

        public RemoteCommandTranslatorMiningAmountProperty(RemoteCommandTranslatorModule module)
            : base(module, AggregateField.drone_remote_command_translation_mining_amount_modifier)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            RemoteCommand remoteCommand = (RemoteCommand)_module.GetAmmo();
            ItemPropertyModifier modifier = module.GetPropertyModifier(AggregateField.drone_remote_command_translation_mining_amount_modifier);
            remoteCommand?.ModifyDroneMiningAmount(ref modifier);

            return modifier.Value;
        }
    }
}
