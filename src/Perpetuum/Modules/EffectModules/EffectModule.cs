using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules.EffectModules
{
    public abstract class EffectModule : ActiveModule
    {
        protected EffectModule() : this(false)
        {

        }

        protected EffectModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, false)
        {

        }

        protected EffectModule(bool ranged) : base(ranged)
        {
        }

        public EffectToken Token { get; } = EffectToken.NewToken();

        protected override void OnStateChanged(IState state)
        {
            IModuleState moduleState = (IModuleState)state;

            if (moduleState.Type == ModuleStateType.Idle)
            {
                if (ED.AttributeFlags.SelfEffect)
                {
                    ParentRobot?.EffectHandler.RemoveEffectByToken(Token);
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
                UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);
                target = unitLock.Target;
            }

            if (!CanApplyEffect(target))
            {
                return;
            }

            target.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
            target.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

            OnApplyingEffect(target);

            EffectBuilder effectBuilder = target.NewEffectBuilder();
            SetupEffect(effectBuilder);
            effectBuilder.WithToken(Token);
            target.ApplyEffect(effectBuilder);
        }

        protected virtual bool CanApplyEffect(Unit target)
        {
            return true;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected virtual void OnApplyingEffect(Unit target) { }

        protected abstract void SetupEffect(EffectBuilder effectBuilder);
    }
}