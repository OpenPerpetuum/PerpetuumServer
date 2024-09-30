using Perpetuum.Modules.AdaptiveAlloy;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.CombatLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.DamageProcessors
{
    public class DamageProcessor
    {
        private readonly Unit unit;
        private Lazy<ShieldGeneratorModule> shield;
        private Lazy<AdaptiveAlloyModule> adaptiveAlloy;
        private readonly Queue<DamageInfo> damageInfos = new Queue<DamageInfo>();
        private bool processing;

        public CombatEventHandler<DamageTakenEventArgs> DamageTaken { private get; set; }

        public DamageProcessor(Unit unit)
        {
            this.unit = unit;
            OnRequipUnit();
        }

        public void OnRequipUnit()
        {
            shield = new Lazy<ShieldGeneratorModule>(() =>
            {
                Robot robot = unit as Robot;

                return robot?.Modules.OfType<ShieldGeneratorModule>().FirstOrDefault();
            });

            adaptiveAlloy = new Lazy<AdaptiveAlloyModule>(() =>
            {
                Robot robot = unit as Robot;

                return robot?.Modules.OfType<AdaptiveAlloyModule>().FirstOrDefault();
            });
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!unit.InZone || unit.IsAttackable != ErrorCodes.NoError || unit.States.Dead || unit.IsInvulnerable)
            {
                return;
            }

            lock (damageInfos)
            {
                if (!processing)
                {
                    processing = true;
                    _ = Task.Run(() => ProcessFirstDamage(damageInfo)).ContinueWith(t => processing = false);
                    return;
                }

                damageInfos.Enqueue(damageInfo);
            }
        }

        private void ProcessFirstDamage(DamageInfo info)
        {
            while (true)
            {
                ProcessDamage(info);

                lock (damageInfos)
                {
                    if (damageInfos.Count == 0)
                    {
                        return;
                    }

                    info = damageInfos.Dequeue();
                }
            }
        }

        private void ProcessDamage(DamageInfo damageInfo)
        {
            if (!unit.InZone || unit.IsAttackable != ErrorCodes.NoError || unit.States.Dead || unit.IsInvulnerable)
            {
                return;
            }

            double totalDamage = 0.0;
            double totalKers = 0.0;
            double totalAbsorbedDamage = 0.0;

            foreach (Damage damage in damageInfo.CalculateDamages(unit))
            {
                double partialDamage = damage.type == DamageType.Electric
                    ? CalculateAbsorbedDamage(damage.value, true, ref totalAbsorbedDamage)
                    : CalculateAbsorbedDamage(damage.value, false, ref totalAbsorbedDamage);
                if (partialDamage <= 0.0)
                {
                    continue;
                }

                double resist = unit.GetResistByDamageType(damage.type);
                partialDamage -= partialDamage * resist;

                double kers = CalculateKersValue(damage.type, partialDamage);
                if (kers > 0.0)
                {
                    unit.Core += kers;
                    totalKers += kers;
                }

                totalDamage += partialDamage;

                adaptiveAlloy.Value?.RegisterDamage(damage.type, partialDamage);
            }

            CombatEventHandler<DamageTakenEventArgs> h = DamageTaken;
            if (h == null)
            {
                return;
            }

            DamageTakenEventArgs e = new DamageTakenEventArgs
            {
                TotalDamage = totalDamage,
                TotalCoreDamage = totalAbsorbedDamage,
                TotalKers = totalKers,
                IsCritical = damageInfo.IsCritical,
                IsKillingBlow = false
            };

            h(damageInfo.attacker, e);
        }

        private double CalculateAbsorbedDamage(double damage, bool isPenetrating, ref double absorbed)
        {
            if (shield.Value == null || !unit.HasShieldEffect)
            {
                return damage;
            }

            double absorbtionModifier = shield.Value.AbsorbtionModifier;
            if (absorbtionModifier <= 0.0)
            {
                return damage;
            }

            double currCore = unit.Core;
            if (currCore < 1.0)
            {
                return damage;
            }

            // damage = 100
            // absorb = 1.2 / 0.8
            double coreDamage = damage * absorbtionModifier;
            if (currCore < coreDamage)
            {
                damage -= currCore / absorbtionModifier;
                absorbed += currCore;
                currCore = 0.0;
            }
            else
            {
                damage = isPenetrating
                    ? coreDamage
                    : 0.0;
                absorbed += coreDamage;
                currCore -= coreDamage;
            }

            unit.Core = currCore;

            return damage;
        }

        private double CalculateKersValue(DamageType damageType, double damage)
        {
            double kersModifier = unit.GetKersByDamageType(damageType);

            if (Math.Abs(kersModifier - 1.0) < double.Epsilon)
            {
                return 0.0;
            }

            double kers = damage * kersModifier;

            if (kers <= 0.0)
            {
                return 0.0;
            }

            double kersMod = (Math.Sin(unit.Core / unit.CoreMax * Math.PI) / 2) + 0.5;
            return kers * kersMod;
        }
    }
}