using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Teleporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.FieldEffectGenerators
{
    public class FieldEccmEffectGenerator : Unit
    {
        public FieldEccmEffectGenerator(EffectType effectType)
        {
            _effectType = effectType;
            effectEccmStrengthModifier = new UnitProperty(this, AggregateField.effect_field_sensor_strength_modifier);
            AddProperty(effectEccmStrengthModifier);
            effectReactorStabilityModifier = new UnitProperty(this, AggregateField.effect_field_reactor_radiation_modifier);
            AddProperty(effectReactorStabilityModifier);
        }

        private UnitDespawnHelper _despawnHelper;
        private readonly EffectType _effectType;
        private readonly ItemProperty effectEccmStrengthModifier;
        private readonly ItemProperty effectReactorStabilityModifier;

        private int _emitRadius;

        private int EmitRadius
        {
            get
            {
                if (_emitRadius <= 0)
                {
                    if (ED.Config.emitRadius != null)
                    {
                        _emitRadius = (int)ED.Config.emitRadius;
                    }
                    else
                    {
                        Logger.Error("no emitradius defined for " + this);
                        _emitRadius = 10;
                    }
                }

                return _emitRadius;
            }
        }

        public override ErrorCodes IsAttackable => ErrorCodes.NoError;

        public override bool IsLockable => true;

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
        }

        public void ApplyFieldEffect()
        {
            Effects.EffectBuilder builder = NewEffectBuilder()
                .SetSource(this)
                .SetType(_effectType)
                .WithPropertyModifier(effectEccmStrengthModifier.ToPropertyModifier())
                .WithPropertyModifier(effectReactorStabilityModifier.ToPropertyModifier())
                .WithTargetSelector(zone => GetTargetUnits());
            ApplyEffect(builder);
        }

        protected IEnumerable<Unit> GetTargetUnits()
        {
            return GetTargetsByPosition();
        }

        protected virtual IEnumerable<Unit> GetTargetsByPosition()
        {
            foreach (Robot unit in Zone.Units.OfType<Robot>().WithinRange(CurrentPosition, EmitRadius))
            {
                yield return unit;
            }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            if (enterType == ZoneEnterType.Deploy)
            {
                ApplyFieldEffect();
            }

            base.OnEnterZone(zone, enterType);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _despawnHelper.Update(time, this);
        }

        public virtual void CheckDeploymentAndThrow(IZone zone, Position spawnPosition)
        {
            zone.Units
                .OfType<DockingBase>()
                .WithinRange(spawnPosition, DistanceConstants.MOBILE_TELEPORT_MIN_DISTANCE_TO_DOCKINGBASE)
                .Any()
                .ThrowIfTrue(ErrorCodes.MobileTeleportsAreNotDeployableNearBases);
            zone.Units
                .OfType<Teleport>()
                .WithinRange(spawnPosition, DistanceConstants.MOBILE_TELEPORT_MIN_DISTANCE_TO_TELEPORT)
                .Any()
                .ThrowIfTrue(ErrorCodes.TeleportIsInRange);
        }
    }
}
