using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using System.Collections.Generic;
using System.Threading;

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
        private long overheatValue;
        private long oldOverheatValue;

        public event EffectEventHandler<bool> EffectChanged;

        public void Increase(long value = 1)
        {
            long currentOverheatValue = Interlocked.Read(ref overheatValue);
            Interlocked.Exchange(ref oldOverheatValue, currentOverheatValue);
            Interlocked.Add(ref overheatValue, value);
            ProcessOverheat();
        }

        public void Decrease(long value = 1)
        {
            long currentOverheatValue = Interlocked.Read(ref overheatValue);
            Interlocked.Exchange(ref oldOverheatValue, currentOverheatValue);
            Interlocked.Add(ref overheatValue, value * -1);
            long newValue = Interlocked.Read(ref overheatValue);
            if (newValue < 0)
            {
                Interlocked.Exchange(ref overheatValue, 0);
            }

            ProcessOverheat();
        }

        private void ProcessOverheat()
        {
            long value = Interlocked.Read(ref overheatValue);
            long oldValue = Interlocked.Read(ref oldOverheatValue);
            switch (value)
            {
                case long x when x >= 0 && x < 25 && oldValue >= 25:
                    RemoveAllOverheatEffects();

                    break;
                case long x when x >= 25 && x < 50 && (oldValue < 25 || oldValue >= 50):
                    RemoveAllOverheatEffects();
                    AddOverheatEffect(EffectType.effect_overheat_buildup_low);

                    break;
                case long x when x >= 50 && x < 75 && (oldValue < 50 || oldValue >= 75):
                    RemoveAllOverheatEffects();
                    AddOverheatEffect(EffectType.effect_overheat_buildup_medium);

                    break;
                case long x when x >= 75 && x < 100 && (oldValue < 75 || oldValue >= 100):
                    RemoveAllOverheatEffects();
                    AddOverheatEffect(EffectType.effect_overheat_buildup_high);

                    break;
                case long x when x >= 100 && x < 120 && oldValue < 100:
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
                case long x when x >= 120:
                    robot.Kill(
                        robot.IsPlayer()
                            ? robot as Player
                            : null);

                    break;
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
            EffectBuilder overheatBuildupBuilder = robot.NewEffectBuilder();
            overheatBuildupBuilder
                .SetType(effectType)
                .WithPropertyModifier(overHeatWeaponDamageModifier)
                .WithPropertyModifier(overHeatMiningAmountModifier);
            robot.ApplyEffect(overheatBuildupBuilder);
        }
    }
}
