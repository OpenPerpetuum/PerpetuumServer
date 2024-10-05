using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Blobs;

namespace Perpetuum.Units.UnitProperties
{
    public class DetectionStrengthProperty : UnitProperty
    {
        public DetectionStrengthProperty(Unit owner)
            : base(
                  owner,
                  AggregateField.detection_strength,
                  AggregateField.detection_strength_modifier,
                  AggregateField.effect_detection_strength_modifier,
                  AggregateField.effect_dreadnought_detection_strength_modifier)
        {
        }

        protected override double CalculateValue()
        {
            double v = base.CalculateValue();

            IBlobableUnit blobableUnit = owner as IBlobableUnit;
            blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, 0.75);

            return v;
        }
    }
}
