using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.RemoteControl;

namespace Perpetuum.Modules.ModuleProperties
{
    public class RemoteCommandTranslatorDamageProperty : ModuleProperty
    {
        private readonly RemoteCommandTranslatorModule _module;

        public RemoteCommandTranslatorDamageProperty(RemoteCommandTranslatorModule module)
            : base(module, AggregateField.drone_remote_command_translation_damage_modifier)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            RemoteCommand remoteCommand = (RemoteCommand)_module.GetAmmo();
            ItemPropertyModifier modifier = module.GetPropertyModifier(AggregateField.drone_remote_command_translation_damage_modifier);
            remoteCommand?.ModifyDroneDamage(ref modifier);

            return modifier.Value;
        }
    }
}
