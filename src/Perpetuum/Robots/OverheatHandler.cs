using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using System;
using System.Collections.Generic;

namespace Perpetuum.Robots
{
    public class OverheatHandler
    {
        private readonly Dictionary<EffectType, double> overheatBonuses = new Dictionary<EffectType, double>
        {
            { EffectType.effect_overheat_buildup_low, 1.01 },
            { EffectType.effect_overheat_buildup_medium, 1.03 },
            { EffectType.effect_overheat_buildup_high, 1.05 },
            { EffectType.effect_overheat_buildup_critical, 1.1 },
        };

        public OverheatHandler(Robot robot)
        {
            this.robot = robot;
        }

        private readonly Robot robot;
        private double overheatValue;
        private double oldOverheatValue;
        private readonly object @lock = new object();

        public event EffectEventHandler<bool> EffectChanged;

        public void Increase(double value = 1)
        {
            lock (@lock)
            {
                oldOverheatValue = overheatValue;
                overheatValue += value;
                ProcessOverheat();
            }
        }

        public void Decrease(double value = 1)
        {
            lock (@lock)
            {
                overheatValue = Math.Max(0, overheatValue - value);
                ProcessOverheat();
            }
        }

        private void ProcessOverheat()
        {
            lock (@lock)
            {
                switch (overheatValue)
                {
                    case double x when x >= 0 && x < 25 && oldOverheatValue >= 25:
                        RemoveAllOverheatEffects();

                        break;
                    case double x when x >= 25 && x < 50 && (oldOverheatValue < 25 || oldOverheatValue >= 50):
                        RemoveAllOverheatEffects();
                        AddOverheatEffect(EffectType.effect_overheat_buildup_low);

                        break;
                    case double x when x >= 50 && x < 75 && (oldOverheatValue < 50 || oldOverheatValue >= 75):
                        RemoveAllOverheatEffects();
                        AddOverheatEffect(EffectType.effect_overheat_buildup_medium);

                        break;
                    case double x when x >= 75 && x < 100 && (oldOverheatValue < 75 || oldOverheatValue >= 100):
                        RemoveAllOverheatEffects();
                        AddOverheatEffect(EffectType.effect_overheat_buildup_high);

                        break;
                    case double x when x >= 100 && x < 120 && oldOverheatValue < 100:
                        RemoveAllOverheatEffects();
                        AddOverheatEffect(EffectType.effect_overheat_buildup_critical);

                        if (robot.IsPlayer())
                        {
                            Character character = (robot as Player).Character;
                            Dictionary<string, object> relogMessage = new Dictionary<string, object>
                        {
                            { k.message, "reactor_overheat_critical" },
                            { k.type, 0 },
                            { k.recipients, character.Id },
                            { k.translate, 1 },
                        };
                            Message.Builder
                                .SetCommand(Commands.ServerMessage)
                                .WithData(relogMessage)
                                .ToCharacter(character)
                                .Send();
                        }

                        break;
                    case double x when x >= 120:
                        robot.Core = 0;

                        break;
                }
            }
        }

        private void RemoveAllOverheatEffects()
        {
            robot.EffectHandler.RemoveEffectsByType(EffectType.effect_overheat_buildup_low);
            robot.EffectHandler.RemoveEffectsByType(EffectType.effect_overheat_buildup_medium);
            robot.EffectHandler.RemoveEffectsByType(EffectType.effect_overheat_buildup_high);
            robot.EffectHandler.RemoveEffectsByType(EffectType.effect_overheat_buildup_critical);
        }

        private void AddOverheatEffect(EffectType effectType)
        {
            double bonusValue = overheatBonuses.ContainsKey(effectType)
                ? overheatBonuses[effectType]
                : 1;
            ItemPropertyModifier overHeatWeaponDamageModifier =
                new ItemPropertyModifier(AggregateField.effect_dreadnought_weapon_damage_modifier, AggregateFormula.Modifier, bonusValue);
            ItemPropertyModifier overHeatMiningAmountModifier =
                new ItemPropertyModifier(AggregateField.effect_excavator_mining_amount_modifier, AggregateFormula.Modifier, bonusValue);
            ItemPropertyModifier overHeatHarvestingAmountModifier =
                new ItemPropertyModifier(AggregateField.effect_excavator_harvesting_amount_modifier, AggregateFormula.Modifier, bonusValue);
            EffectBuilder overheatBuildupBuilder = robot.NewEffectBuilder();
            overheatBuildupBuilder
                .SetType(effectType)
                .WithPropertyModifier(overHeatWeaponDamageModifier)
                .WithPropertyModifier(overHeatMiningAmountModifier)
                .WithPropertyModifier(overHeatHarvestingAmountModifier);
            robot.ApplyEffect(overheatBuildupBuilder);
        }
    }
}
