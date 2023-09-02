using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Perpetuum.Modules.Weapons
{
    public class HellfireWeaponModule : ActiveModule
    {
        private readonly ModuleAction _action;
        public ModuleProperty DamageModifier { get; }
        public ModuleProperty Accuracy { get; }
        // --------------------
        private readonly ItemProperty _propertyExplosionRadius;
        public readonly ModuleProperty MissileRangeModifier;
        public readonly ModuleProperty MissileFalloffModifier;

        public HellfireWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            _action = new ModuleAction(this);

            DamageModifier = new ModuleProperty(this, AggregateField.damage_modifier);
            AddProperty(DamageModifier);
            Accuracy = new ModuleProperty(this, AggregateField.accuracy);
            AddProperty(Accuracy);

            cycleTime.AddEffectModifier(AggregateField.effect_weapon_cycle_time_modifier);

            // --------------------

            _propertyExplosionRadius = new ExplosionRadiusProperty(this);
            AddProperty(_propertyExplosionRadius);
            MissileRangeModifier = new ModuleProperty(this, AggregateField.module_missile_range_modifier);
            MissileRangeModifier.AddEffectModifier(AggregateField.effect_missile_range_modifier);
            AddProperty(MissileRangeModifier);
            MissileFalloffModifier = new ModuleProperty(this, AggregateField.module_missile_falloff_modifier);
            AddProperty(MissileFalloffModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.explosion_radius:
                case AggregateField.explosion_radius_modifier:
                    {
                        _propertyExplosionRadius.Update();
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

        protected bool CheckAccuracy(Unit victim)
        {
            var rnd = FastRandom.NextDouble();
            var isMiss = rnd > ParentRobot.MissileHitChance;
            return isMiss;
        }

        protected IDamageBuilder GetDamageBuilder()
        {
            return DamageInfo.Builder.WithAttacker(ParentRobot)
                                .WithOptimalRange(OptimalRange)
                                .WithFalloff(Falloff)
                                .WithDamages(GetCleanDamages())
                                .WithExplosionRadius(_propertyExplosionRadius.Value);
        }

        private class ExplosionRadiusProperty : ModuleProperty
        {
            private readonly HellfireWeaponModule _module;

            public ExplosionRadiusProperty(HellfireWeaponModule module) : base(module, AggregateField.explosion_radius)
            {
                _module = module;
            }

            protected override double CalculateValue()
            {
                var ammo = (WeaponAmmo)_module.GetAmmo();
                if (ammo == null)
                    return 0.0;

                var property = ammo.GetExplosionRadius();
                _module.ApplyRobotPropertyModifiers(ref property);
                return property.Value;
            }
        }

        // ------------------------------

        protected override void OnAction()
        {
            _action.DoAction();
        }

        private class ModuleAction : ILockVisitor
        {
            private readonly HellfireWeaponModule _weapon;

            public ModuleAction(HellfireWeaponModule weapon)
            {
                _weapon = weapon;
            }

            public void DoAction()
            {
                _weapon.ParentRobot.HasShieldEffect.ThrowIfTrue(ErrorCodes.ShieldIsActive);

                var currentLock = _weapon.GetLock();
                currentLock?.AcceptVisitor(this);
            }

            public void VisitLock(Lock @lock)
            {

            }

            public void VisitUnitLock(UnitLock unitLock)
            {
                var victim = unitLock.Target;

                victim.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
                victim.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

                var err = victim.IsAttackable;
                if (err != ErrorCodes.NoError)
                    throw new PerpetuumException(err);

                victim.IsInvulnerable.ThrowIfTrue(ErrorCodes.TargetIsInvulnerable);

                _weapon.ConsumeAmmo();

                var result = _weapon.GetLineOfSight(victim);
                if (result.hit)
                {
                    DoDamageToPosition(result.position);
                    _weapon.OnError(ErrorCodes.LOSFailed);
                    return;
                }

                var distance = _weapon.ParentRobot.GetDistance(victim);
                var bulletTime = _weapon.GetAmmo().BulletTime;
                var flyTime = (int)((distance / bulletTime) * 1000);
                var beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);

                var miss = _weapon.CheckAccuracy(victim);
                if (miss)
                {
                    _weapon.CreateBeam(victim, BeamState.Miss, beamTime, bulletTime);
                    _weapon.OnError(ErrorCodes.AccuracyCheckFailed);
                    return;
                }

                var delay = _weapon.CreateBeam(victim, BeamState.Hit, beamTime, bulletTime);
                flyTime += delay;

                var builder = _weapon.GetDamageBuilder();
                Task.Delay(flyTime).ContinueWith(t => victim.TakeDamage(builder.Build()));
            }

            public void VisitTerrainLock(TerrainLock terrainLock)
            {
                var location = terrainLock.Location;

                while (_weapon.GetAmmo().Quantity > 0)
                {
                    _weapon.ConsumeAmmo();

                    /*
                    var dX = (new Random().NextDouble() - 0.5) * 10;
                    var dY = (new Random().NextDouble() - 0.5) * 10;

                    var closeToTargetLocation = new Position(location.X + dX, location.Y + dY);
                    */

                    var closeToTargetLocation = location.GetRandomPositionInRange2D(0, 5);

                    var blockingInfo = _weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(closeToTargetLocation) ?? BlockingInfo.None;
                    closeToTargetLocation = closeToTargetLocation.AddToZ(Math.Min(blockingInfo.Height, 20));

                    var losResult = _weapon.GetLineOfSight(closeToTargetLocation);
                    if (losResult.hit && !closeToTargetLocation.IsEqual2D(losResult.position))
                    {
                        closeToTargetLocation = losResult.position;
                        _weapon.OnError(ErrorCodes.LOSFailed);
                    }

                    DoDamageToPosition(closeToTargetLocation);
                }
            }

            private void DoDamageToPosition(Position location)
            {
                var distance = _weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
                var bulletTime = _weapon.GetAmmo().BulletTime;
                var flyTime = (int)((distance / bulletTime) * 1000);

                var beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);
                flyTime += _weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime);

                var damage = _weapon.GetDamageBuilder().Build().CalculatePlantDamages();

                if (damage <= 0.0)
                    return;

                var zone = _weapon.Zone;
                if (zone == null)
                    return;

                Task.Delay(flyTime).ContinueWith(t => DealDamageToPosition(zone, location, damage));
            }

            private static void DealDamageToPosition(IZone zone, Position location, double damage)
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    zone.DamageToPlantOnArea(Area.FromRadius(location, 1), damage / 2.0);
                }
            }
        }

        private IEnumerable<Damage> GetCleanDamages()
        {
            var ammo = (WeaponAmmo)GetAmmo();
            return ammo != null ? ammo.GetCleanDamages() : new Damage[0];
        }
    }
}
