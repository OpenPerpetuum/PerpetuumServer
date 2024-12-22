using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.RemoteControl;

namespace Perpetuum.Modules.ModuleProperties
{
    public class RemoteCommandTranslatorArmorMaxProperty : ModuleProperty
    {
        private readonly RemoteCommandTranslatorModule _module;

        public RemoteCommandTranslatorArmorMaxProperty(RemoteCommandTranslatorModule module)
            : base(module, AggregateField.drone_remote_command_translation_armor_max_modifier)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            RemoteCommand remoteCommand = (RemoteCommand)_module.GetAmmo();
            ItemPropertyModifier modifier = module.GetPropertyModifier(AggregateField.drone_remote_command_translation_armor_max_modifier);
            remoteCommand?.ModifyDroneArmorMax(ref modifier);

            return modifier.Value;
        }
    }
}
