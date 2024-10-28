using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;

namespace Perpetuum.Zones.Blobs.BlobEmitters
{
    public class BlobEmitter : IBlobEmitter
    {
        private readonly ItemProperty blobEmission;
        private readonly ItemProperty blobEmissionRadius;

        public BlobEmitter(Unit unit)
        {
            blobEmission = new UnitProperty(
                unit,
                AggregateField.blob_emission,
                AggregateField.blob_emission_modifier,
                AggregateField.effect_blob_emission_modifier,
                AggregateField.effect_dreadnought_blob_emission_modifier);
            unit.AddProperty(blobEmission);

            blobEmissionRadius = new UnitProperty(
                unit,
                AggregateField.blob_emission_radius,
                AggregateField.blob_emission_radius_modifier,
                AggregateField.effect_blob_emission_radius_modifier);
            unit.AddProperty(blobEmissionRadius);
        }

        public double BlobEmission => blobEmission.Value;

        public double BlobEmissionRadius => blobEmissionRadius.Value;
    }
}