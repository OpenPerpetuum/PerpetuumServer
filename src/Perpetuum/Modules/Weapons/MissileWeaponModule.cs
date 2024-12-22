using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Units;
using Perpetuum.Zones.RemoteControl;

namespace Perpetuum.Modules.Weapons
{
    public class MissileWeaponModule : WeaponModule
    {
        private readonly ModuleProperty propertyExplosionRadius;
        public readonly ModuleProperty MissileRangeModifier;
        public readonly ModuleProperty MissileFalloffModifier;

        public MissileWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            propertyExplosionRadius = new ExplosionRadiusProperty(this);
            AddProperty(propertyExplosionRadius);
            MissileRangeModifier = new ModuleProperty(this, AggregateField.module_missile_range_modifier);
            MissileRangeModifier.AddEffectModifier(AggregateField.effect_missile_range_modifier);
            AddProperty(MissileRangeModifier);
            MissileFalloffModifier = new ModuleProperty(this, AggregateField.module_missile_falloff_modifier);
            AddProperty(MissileFalloffModifier);
            propertyExplosionRadius.AddEffectModifier(AggregateField.drone_amplification_accuracy_modifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.explosion_radius:
                case AggregateField.explosion_radius_modifier:
                case AggregateField.drone_amplification_accuracy_modifier:
                    {
                        propertyExplosionRadius.Update();
                        return;
                    }
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                    {
                        MissileRangeModifier.Update();
                        return;
                    }
                case AggregateField.module_missile_falloff_modifier:
                    {
                        MissileFalloffModifier.Update();
                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override bool CheckAccuracy(Unit victim)
        {
            double rnd = FastRandom.NextDouble();
            bool isMiss = rnd > ParentRobot.MissileHitChance;
            return isMiss;
        }

        protected override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder().WithExplosionRadius(propertyExplosionRadius.Value);
        }

        private class ExplosionRadiusProperty : ModuleProperty
        {
            private new readonly MissileWeaponModule module;

            public ExplosionRadiusProperty(MissileWeaponModule module) : base(module, AggregateField.explosion_radius)
            {
                this.module = module;
            }

            protected override double CalculateValue()
            {
                WeaponAmmo ammo = (WeaponAmmo)module.GetAmmo();
                if (ammo == null)
                {
                    return 0.0;
                }

                ItemPropertyModifier property = ammo.GetExplosionRadius();
                module.ApplyRobotPropertyModifiers(ref property);
                ApplyEffectModifiers(ref property);

                return property.Value;
            }
        }
    }
}