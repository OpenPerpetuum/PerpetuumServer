using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Items.Ammos
{
    public sealed class RemoteCommand : Ammo
    {
        private ItemProperty _droneDamageModifier = ItemProperty.None;
        private ItemProperty _droneArmorMaxModifier = ItemProperty.None;
        private ItemProperty _droneMiningAmountModifier = ItemProperty.None;
        private ItemProperty _droneHarvestingAmountModifier = ItemProperty.None;
        private ItemProperty _droneRemoteRepairAmountModifier = ItemProperty.None;
        private ItemProperty _droneRetreatConfirmation = ItemProperty.None;

        public override void Initialize()
        {
            if (!IsCategory(CategoryFlags.cf_missile_ammo))
            {
                _droneDamageModifier = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_damage_modifier_modifier);
                AddProperty(_droneDamageModifier);

                _droneArmorMaxModifier = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_armor_max_modifier_modifier);
                AddProperty(_droneArmorMaxModifier);

                _droneMiningAmountModifier = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_mining_amount_modifier_modifier);
                AddProperty(_droneMiningAmountModifier);

                _droneHarvestingAmountModifier = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_harvesting_amount_modifier_modifier);
                AddProperty(_droneHarvestingAmountModifier);

                _droneRemoteRepairAmountModifier = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_remote_repair_amount_modifier_modifier);
                AddProperty(_droneRemoteRepairAmountModifier);

                _droneRetreatConfirmation = new AmmoProperty<RemoteCommand>(this, AggregateField.drone_remote_command_translation_retreat_confirmation);
                AddProperty(_droneRetreatConfirmation);
            }
            base.Initialize();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void ModifyDroneDamage(ref ItemPropertyModifier property)
        {
            ItemPropertyModifier modifier = _droneDamageModifier.ToPropertyModifier();
            modifier.Modify(ref property);
        }

        public void ModifyDroneArmorMax(ref ItemPropertyModifier property)
        {
            ItemPropertyModifier modifier = _droneArmorMaxModifier.ToPropertyModifier();
            modifier.Modify(ref property);
        }

        public void ModifyDroneMiningAmount(ref ItemPropertyModifier property)
        {
            ItemPropertyModifier modifier = _droneMiningAmountModifier.ToPropertyModifier();
            modifier.Modify(ref property);
        }

        public void ModifyDroneHarvestingAmount(ref ItemPropertyModifier property)
        {
            ItemPropertyModifier modifier = _droneHarvestingAmountModifier.ToPropertyModifier();
            modifier.Modify(ref property);
        }

        public void ModifyDroneRemoteRepairAmount(ref ItemPropertyModifier property)
        {
            ItemPropertyModifier modifier = _droneRemoteRepairAmountModifier.ToPropertyModifier();
            modifier.Modify(ref property);
        }

        public void ConfirmRetreat(ref ItemPropertyModifier property)
        {

            ItemPropertyModifier modifier = _droneRetreatConfirmation.ToPropertyModifier();
            modifier.Modify(ref property);
        }
    }
}
