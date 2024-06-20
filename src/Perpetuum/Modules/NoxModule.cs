using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Modules
{
    public sealed class NoxModule : EffectModule
    {
        private readonly EffectType effectType;
        private readonly ItemProperty noxEffectEnhancerRadiusModifier;
        private readonly ItemProperty effectNegator;

        public NoxModule(EffectType effectType, AggregateField effectModifier)
        {
            this.effectType = effectType;
            noxEffectEnhancerRadiusModifier = new ModuleProperty(this, AggregateField.nox_effect_enhancer_radius_modifier);
            AddProperty(noxEffectEnhancerRadiusModifier);
            effectNegator = new ModuleProperty(this, effectModifier);
            AddProperty(effectNegator);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            if (ParentIsPlayer())
            {
                if (!ConsumePlasma())
                {
                    State.SwitchTo(ModuleStateType.Idle);

                    return;
                }
                if (!Zone.Configuration.IsAlpha)
                {
                    (ParentRobot as Player).ApplyPvPEffect();
                }
            }

            base.OnAction();
        }

        private bool ConsumePlasma()
        {
            Robots.RobotInventory cargo = ParentRobot
                .GetContainer();

            Item plasmaToConsume = cargo.GetItemByDefinition(ED.Options.PlasmaDefinition);

            if (plasmaToConsume == null || plasmaToConsume.Quantity < ED.Options.PlasmaConsumption)
            {
                OnError(ErrorCodes.PlasmaNotFound);

                return false;
            }

            _ = cargo.RemoveItem(plasmaToConsume, ED.Options.PlasmaConsumption);

            return true;
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            _ = effectBuilder
                .SetType(effectType)
                .SetOwnerToSource()
                .WithPropertyModifier(effectNegator.ToPropertyModifier())
                .WithRadiusModifier(noxEffectEnhancerRadiusModifier.Value);
        }
    }
}
