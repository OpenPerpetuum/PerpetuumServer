using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules.EffectModules
{
    public abstract class EffectModule : ActiveModule
    {
        private readonly EffectToken _token = EffectToken.NewToken();

        protected EffectModule() : this(false) 
        {
            
        }

        protected EffectModule(bool ranged) : base(ranged)
        {
        }

        protected override void OnStateChanged(IState state)
        {
            var moduleState = (IModuleState) state;

            if (moduleState.Type == ModuleStateType.Idle)
            {
                if (ED.AttributeFlags.SelfEffect)
                {
                    ParentRobot?.EffectHandler.RemoveEffectByToken(_token);
                }
            }

            base.OnStateChanged(state);
        }

        protected override void OnAction()
        {
            Unit target;

            if (ED.AttributeFlags.SelfEffect)
            {
                target = ParentRobot;
            }
            else
            {
                var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);
                target = unitLock.Target;
            }

            if (!CanApplyEffect(target))
                return;

            target.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
            target.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

            OnApplyingEffect(target);

            var effectBuilder = target.NewEffectBuilder();
            SetupEffect(effectBuilder);
            effectBuilder.WithToken(_token);
            target.ApplyEffect(effectBuilder);
        }

        protected virtual bool CanApplyEffect(Unit target)
        {
            return true;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected virtual ItemPropertyModifier GetRangeModifiedProperty(ItemProperty property, Unit target)
        {
            var maxValue = property.Value;
            var rangeModValue = ModifyValueByOptimalRange(target, maxValue).Clamp(0, maxValue);
            return ItemPropertyModifier.Create(property.Field, rangeModValue);
        }

        protected virtual void OnApplyingEffect(Unit target) {}

        protected virtual void SetupEffect(EffectBuilder effectBuilder) { }
        protected virtual void SetupEffect(EffectBuilder effectBuilder, Unit target)
        {
            SetupEffect(effectBuilder);
        }
    }
}