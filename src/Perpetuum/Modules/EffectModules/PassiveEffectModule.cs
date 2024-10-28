using Perpetuum.EntityFramework;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules.EffectModules
{
    public abstract class PassiveEffectModule : Module
    {
        private readonly EffectToken token = EffectToken.NewToken();
        private bool renewRequired = false;

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void Update()
        {
            if (renewRequired)
            {
                RemoveEffect();
                renewRequired = false;
            }

            if (ParentRobot.EffectHandler.GetEffectByToken(token) == null)
            {
                ApplyEffect();
            }
        }

        protected void SetRenewRequired()
        {
            renewRequired = true;
        }

        protected virtual bool CanApplyEffect()
        {
            return true;
        }

        protected virtual void OnApplyingEffect() { }

        protected abstract void SetupEffect(EffectBuilder effectBuilder);

        private void ApplyEffect()
        {
            if (!CanApplyEffect())
            {
                return;
            }

            ParentRobot.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
            ParentRobot.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

            OnApplyingEffect();

            EffectBuilder effectBuilder = ParentRobot.NewEffectBuilder();
            SetupEffect(effectBuilder);
            effectBuilder.WithToken(token);
            ParentRobot.ApplyEffect(effectBuilder);
        }

        private void RemoveEffect()
        {
            ParentRobot.EffectHandler.RemoveEffectByToken(token);
        }
    }
}
