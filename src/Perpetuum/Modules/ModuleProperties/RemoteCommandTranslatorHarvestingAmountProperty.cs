using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.RemoteControl;

namespace Perpetuum.Modules.ModuleProperties
{
    public class RemoteCommandTranslatorHarvestingAmountProperty : ModuleProperty
    {
        private readonly RemoteCommandTranslatorModule _module;

        public RemoteCommandTranslatorHarvestingAmountProperty(RemoteCommandTranslatorModule module)
            : base(module, AggregateField.drone_remote_command_translation_harvesting_amount_modifier)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            RemoteCommand remoteCommand = (RemoteCommand)_module.GetAmmo();
            ItemPropertyModifier modifier = module.GetPropertyModifier(AggregateField.drone_remote_command_translation_harvesting_amount_modifier);
            remoteCommand?.ModifyDroneHarvestingAmount(ref modifier);

            return modifier.Value;
        }
    }
}
