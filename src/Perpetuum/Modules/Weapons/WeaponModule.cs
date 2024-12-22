using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perpetuum.Modules.Weapons
{
    public class WeaponModule : ActiveModule
    {
        private readonly ModuleAction action;

        public ModuleProperty DamageModifier { get; }

        public ModuleProperty Accuracy { get; }

        public WeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            action = new ModuleAction(this);
            Accuracy = new ModuleProperty(this, AggregateField.accuracy);
            AddProperty(Accuracy);
            Accuracy.AddEffectModifier(AggregateField.drone_amplification_accuracy_modifier);
            cycleTime
                .AddEffectModifier(AggregateField.effect_weapon_cycle_time_modifier);
            cycleTime
                .AddEffectModifier(AggregateField.drone_amplification_cycle_time_modifier);
            cycleTime
                .AddEffectModifier(AggregateField.effect_dreadnought_weapon_cycle_time_modifier);
            DamageModifier = new ModuleProperty(this, AggregateField.damage_modifier);
            AddProperty(DamageModifier);
            DamageModifier.AddEffectModifier(AggregateField.drone_amplification_damage_modifier);
            DamageModifier.AddEffectModifier(AggregateField.drone_remote_command_translation_damage_modifier);
            DamageModifier.AddEffectModifier(AggregateField.effect_dreadnought_weapon_damage_modifier);

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
                case AggregateField.drone_amplification_damage_modifier:
                case AggregateField.drone_remote_command_translation_damage_modifier:
                    {
                        DamageModifier.Update();

                        return;
                    }
                case AggregateField.drone_amplification_accuracy_modifier:
                    {
                        Accuracy.Update();

                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override void OnAction()
        {
            action.DoAction();
            ParentRobot.IncreaseOverheatByValue(
                EffectType.effect_dreadnought,
                GeneratedHeat);
        }

        protected virtual bool CheckAccuracy(Unit victim)
        {
            double rnd = FastRandom.NextDouble();
            bool isMiss = rnd * Accuracy.Value > victim.SignatureRadius;

            return isMiss;
        }

        protected virtual IDamageBuilder GetDamageBuilder()
        {
            return DamageInfo.Builder
                .WithAttacker(ParentRobot)
                .WithOptimalRange(OptimalRange)
                .WithFalloff(Falloff)
                .WithDamages(GetCleanDamages());
        }

        private IEnumerable<Damage> GetCleanDamages()
        {
            WeaponAmmo ammo = (WeaponAmmo)GetAmmo();

            return ammo != null ? ammo.GetCleanDamages() : new Damage[0];
        }

        private class ModuleAction : ILockVisitor
        {
            private readonly WeaponModule _weapon;

            public ModuleAction(WeaponModule weapon)
            {
                _weapon = weapon;
            }

            public void DoAction()
            {
                _weapon.ParentRobot.HasShieldEffect.ThrowIfTrue(ErrorCodes.ShieldIsActive);

                Lock currentLock = _weapon.GetLock();

                currentLock?.AcceptVisitor(this);
            }

            public void VisitLock(Lock @lock)
            {

            }

            public void VisitUnitLock(UnitLock unitLock)
            {
                Unit victim = unitLock.Target;

                victim.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
                victim.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

                ErrorCodes err = victim.IsAttackable;

                if (err != ErrorCodes.NoError)
                {
                    throw new PerpetuumException(err);
                }

                victim.IsInvulnerable.ThrowIfTrue(ErrorCodes.TargetIsInvulnerable);
                _weapon.ConsumeAmmo();

                LOSResult result = _weapon.GetLineOfSight(victim);

                if (result.hit)
                {
                    DoDamageToPosition(result.position);
                    _weapon.OnError(ErrorCodes.LOSFailed);

                    return;
                }

                double distance = _weapon.ParentRobot.GetDistance(victim);
                double bulletTime = _weapon.GetAmmo().BulletTime;
                int flyTime = (int)(distance / bulletTime * 1000);
                int beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);
                bool miss = _weapon.CheckAccuracy(victim);

                if (miss)
                {
                    _ = _weapon.CreateBeam(victim, BeamState.Miss, beamTime, bulletTime);
                    _weapon.OnError(ErrorCodes.AccuracyCheckFailed);

                    return;
                }

                int delay = _weapon.CreateBeam(victim, BeamState.Hit, beamTime, bulletTime);

                flyTime += delay;

                IDamageBuilder builder = _weapon.GetDamageBuilder();

                _ = Task.Delay(flyTime).ContinueWith(t => victim.TakeDamage(builder.Build()));
            }

            public void VisitTerrainLock(TerrainLock terrainLock)
            {
                Position location = terrainLock.Location;

                _weapon.ConsumeAmmo();

                BlockingInfo blockingInfo = _weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(terrainLock.Location) ?? BlockingInfo.None;

                location = location.AddToZ(Math.Min(blockingInfo.Height, 20));

                LOSResult losResult = _weapon.GetLineOfSight(location);

                if (losResult.hit && !location.IsEqual2D(losResult.position))
                {
                    location = losResult.position;
                    _weapon.OnError(ErrorCodes.LOSFailed);
                }

                DoDamageToPosition(location);
            }

            private void DoDamageToPosition(Position location)
            {
                double distance = _weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
                double bulletTime = _weapon.GetAmmo().BulletTime;
                int flyTime = (int)(distance / bulletTime * 1000);
                int beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);

                flyTime += _weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime);

                double damage = _weapon.GetDamageBuilder().Build().CalculatePlantDamages();

                if (damage <= 0.0)
                {
                    return;
                }

                IZone zone = _weapon.Zone;

                if (zone == null)
                {
                    return;
                }

                _ = Task.Delay(flyTime).ContinueWith(t => DealDamageToPosition(zone, location, damage));
            }

            private static void DealDamageToPosition(IZone zone, Position location, double damage)
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    zone.DamageToPlantOnArea(Area.FromRadius(location, 1), damage / 2.0);
                }
            }
        }
    }
}