using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.RemoteControl;

namespace Perpetuum.Modules.ModuleProperties
{
    public class RemoteCommandTranslatorRetreatProperty : ModuleProperty
    {
        private readonly RemoteCommandTranslatorModule _module;

        public RemoteCommandTranslatorRetreatProperty(RemoteCommandTranslatorModule module)
            : base(module, AggregateField.drone_remote_command_translation_retreat)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            RemoteCommand remoteCommand = (RemoteCommand)_module.GetAmmo();
            ItemPropertyModifier modifier = module.GetPropertyModifier(AggregateField.drone_remote_command_translation_retreat);
            remoteCommand?.ConfirmRetreat(ref modifier);

            return modifier.Value;
        }
    }
}
