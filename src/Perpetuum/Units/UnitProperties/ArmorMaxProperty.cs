using Perpetuum.ExportedTypes;

namespace Perpetuum.Units.UnitProperties
{
    public class ArmorMaxProperty : UnitProperty
    {
        public ArmorMaxProperty(Unit owner)
            : base(
                  owner,
                  AggregateField.armor_max,
                  AggregateField.armor_max_modifier,
                  AggregateField.effect_armor_max_modifier,
                  AggregateField.drone_amplification_armor_max_modifier,
                  AggregateField.drone_remote_command_translation_armor_max_modifier)
        { }

        protected override void OnAfterPropertyChanging(double newValue, double oldValue)
        {
            double difference = newValue - oldValue;
            owner.Armor += difference;
        }
    }
}
