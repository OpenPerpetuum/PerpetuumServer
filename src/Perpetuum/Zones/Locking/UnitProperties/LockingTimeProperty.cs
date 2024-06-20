using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs;

namespace Perpetuum.Zones.Locking.UnitProperties
{
    public class LockingTimeProperty : UnitProperty
    {
        public LockingTimeProperty(Unit owner)
            : base(
                owner,
                AggregateField.locking_time,
                AggregateField.locking_time_modifier,
                AggregateField.effect_sensor_booster_locking_time_modifier,
                AggregateField.effect_sensor_dampener_locking_time_modifier,
                AggregateField.effect_locking_time_modifier,
                AggregateField.drone_amplification_locking_time_modifier)
        { }

        protected override double CalculateValue()
        {
            double v = base.CalculateValue();
            IBlobableUnit blobableUnit = owner as IBlobableUnit;
            blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, -5);

            return v;
        }
    }
}
