using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.StateMachines;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using System.Linq;

namespace Perpetuum.Modules.RemoteControl
{
    public class RemoteCommandTranslatorModule : EffectModule
    {
        private readonly ModuleProperty droneDamage;
        private readonly ModuleProperty droneArmorMax;
        private readonly ModuleProperty droneMiningAmount;
        private readonly ModuleProperty droneHarvestingAmount;
        private readonly ModuleProperty droneRemoteRepairAmount;
        private readonly ModuleProperty droneRetreat;

        public RemoteCommandTranslatorModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            droneDamage = new RemoteCommandTranslatorDamageProperty(this);
            AddProperty(droneDamage);

            droneArmorMax = new RemoteCommandTranslatorArmorMaxProperty(this);
            AddProperty(droneArmorMax);

            droneMiningAmount = new RemoteCommandTranslatorMiningAmountProperty(this);
            AddProperty(droneMiningAmount);

            droneHarvestingAmount = new RemoteCommandTranslatorHarvestingAmountProperty(this);
            AddProperty(droneHarvestingAmount);

            droneRemoteRepairAmount = new RemoteCommandTranslatorRemoteRepairAmountProperty(this);
            AddProperty(droneRemoteRepairAmount);

            droneRetreat = new RemoteCommandTranslatorRetreatProperty(this);
            AddProperty(droneRetreat);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnStateChanged(IState state)
        {
            IModuleState moduleState = (IModuleState)state;

            if (moduleState.Type == ModuleStateType.Idle)
            {
                RemoteControllerModule remoteController =
                    (RemoteControllerModule)ParentRobot?.ActiveModules.FirstOrDefault(x => x is RemoteControllerModule);

                if (remoteController != null)
                {
                    foreach (Unit drone in remoteController.ActiveDrones)
                    {
                        drone.EffectHandler.RemoveEffectByToken(Token);
                    }
                }
            }

            base.OnStateChanged(state);
        }

        protected override void OnAction()
        {
            RemoteControllerModule remoteController =
                (RemoteControllerModule)ParentRobot?.ActiveModules.FirstOrDefault(x => x is RemoteControllerModule);
            double operationalRange = remoteController?.OperationalRange ?? 0;

            if (remoteController != null)
            {
                foreach (Unit drone in remoteController.ActiveDrones)
                {
                    OnApplyingEffect(drone);
                    EffectBuilder effectBuilder = drone.NewEffectBuilder();
                    effectBuilder
                        .WithRadius(operationalRange);
                    SetupEffect(effectBuilder);
                    effectBuilder.WithToken(Token);

                    drone.ApplyEffect(effectBuilder);
                }
            }
        }

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            _ = effectBuilder
                .SetType(EffectType.remote_command_translation)
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_damage_modifier,
                        AggregateFormula.Modifier,
                        droneDamage.Value))
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_armor_max_modifier,
                        AggregateFormula.Modifier,
                        droneArmorMax.Value))
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_mining_amount_modifier,
                        AggregateFormula.Modifier,
                        droneMiningAmount.Value))
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_harvesting_amount_modifier,
                        AggregateFormula.Modifier,
                        droneHarvestingAmount.Value))
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_remote_repair_amount_modifier,
                        AggregateFormula.Modifier,
                        droneRemoteRepairAmount.Value))
                .WithPropertyModifier(
                    new ItemPropertyModifier(
                        AggregateField.drone_remote_command_translation_retreat,
                        AggregateFormula.Modifier,
                        droneRetreat.Value));
        }
    }
}
