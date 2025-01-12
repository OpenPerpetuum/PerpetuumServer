using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;
using System;

namespace Perpetuum.Zones.FieldEffectGenerators
{
    public class FieldEffectGeneratorDeployer : ItemDeployer
    {
        public FieldEffectGeneratorDeployer(IEntityServices entityServices) : base(entityServices)
        {
        }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            FieldEffectGenerator fieldEffectGenerator = (FieldEffectGenerator)base.CreateDeployableItem(zone, spawnPosition, player);
            fieldEffectGenerator.CheckDeploymentAndThrow(zone, spawnPosition);
            fieldEffectGenerator.SetDespawnTime(FieldEffectGeneratorDespawnTime);

            return fieldEffectGenerator;
        }

        private TimeSpan FieldEffectGeneratorDespawnTime
        {
            get
            {
                Items.ItemPropertyModifier m = GetPropertyModifier(AggregateField.despawn_time);

                return TimeSpan.FromMilliseconds((int)m.Value);
            }
        }
    }
}
