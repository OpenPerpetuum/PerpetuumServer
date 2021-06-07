using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Timers;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Units
{
    public delegate bool UnitDespawnerCanApplyEffect(Unit unit);
    public delegate void UnitDespawnStrategy(Unit unit);

    public class UnitDespawnHelper
    {
        public UnitDespawnerCanApplyEffect CanApplyDespawnEffect { protected get; set; }
        public UnitDespawnStrategy DespawnStrategy { protected get; set; }

        public virtual EffectType DespawnEffect
        {
            get { return EffectType.effect_despawn_timer; }
        }

        protected readonly TimeSpan _despawnTime;
        protected readonly EffectToken _effectToken = EffectToken.NewToken();
        protected readonly IntervalTimer _timer = new IntervalTimer(650);

        private bool _detectedEffectApplied = false;

        protected UnitDespawnHelper(TimeSpan despawnTime)
        {
            _despawnTime = despawnTime;
        }

        private bool HasEffect(Unit unit)
        {
            return unit.EffectHandler.ContainsToken(_effectToken);
        }

        protected bool EffectLive(Unit unit)
        {
            var effectRunning = HasEffect(unit);
            if (!_detectedEffectApplied)
            {
                _detectedEffectApplied = effectRunning;
            }
            return _detectedEffectApplied == effectRunning;
        }

        public virtual void Update(TimeSpan time, Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                TryReApplyDespawnEffect(unit);

                if (EffectLive(unit))
                    return;

                CanApplyDespawnEffect = null;
                if (DespawnStrategy != null)
                {
                    DespawnStrategy(unit);
                }
                else
                {
                    unit.States.Teleport = true; //kis villam visual amikor kiszedi
                    unit.RemoveFromZone();
                }
            });
        }

        private void TryReApplyDespawnEffect(Unit unit)
        {
            var canApplyDespawnEffect = CanApplyDespawnEffect;
            if (canApplyDespawnEffect == null)
                return;

            var applyDespawnEffect = canApplyDespawnEffect(unit);
            if (!applyDespawnEffect)
                return;

            ApplyDespawnEffect(unit);
        }

        public void ApplyDespawnEffect(Unit unit)
        {
            var effectBuilder = unit.NewEffectBuilder().SetType(DespawnEffect).WithDuration(_despawnTime).WithToken(_effectToken);
            unit.ApplyEffect(effectBuilder);
            _detectedEffectApplied = false;
        }

        public override string ToString()
        {
            return $"DespawnTime: {_despawnTime}";
        }

        public static UnitDespawnHelper Create(Unit unit, TimeSpan despawnTime)
        {
            var helper = new UnitDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }
    }
}