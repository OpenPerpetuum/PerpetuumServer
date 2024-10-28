using Autofac;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Effects.ZoneEffects;
using System;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class EffectsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<EffectBuilder>();

            _ = builder.RegisterType<ZoneEffectHandler>().As<IZoneEffectHandler>();

            _ = builder.Register<Func<IZone, IZoneEffectHandler>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return zone => new ZoneEffectHandler(zone);
            });

            _ = builder.RegisterType<InvulnerableEffect>().Keyed<Effect>(EffectType.effect_invulnerable);
            _ = builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_eccm);
            _ = builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_stealth);

            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_recharge_time);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_critical_hit_chance);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_time);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_signature_radius);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_fast_extraction);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_usage_gathering);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_siege);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_speed);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_repaired_amount);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_range);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_ewar_optimal);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_armor_max);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_shield_absorbtion_ratio);
            _ = builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_excavator);

            // NOX effects

            _ = builder.RegisterType<NoxEffect>().Keyed<Effect>(EffectType.nox_effect_repair_negation);
            _ = builder.RegisterType<NoxEffect>().Keyed<Effect>(EffectType.nox_effect_shield_negation);
            _ = builder.RegisterType<NoxEffect>().Keyed<Effect>(EffectType.nox_effect_teleport_negation);

            // intrusion effects

            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl1);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl2);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl3);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_signals_lvl4_combined);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_industrial_lvl4_combined);
            _ = builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_engineering_lvl4_combined);

            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl3);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl1);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl2);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl3);

            // New Bonuses - OPP
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta_bonus);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta2_bonus);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha_bonus);
            _ = builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha2_bonus);

            _ = builder.Register<EffectFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();

                return effectType =>
                {
                    return !ctx.IsRegisteredWithKey<Effect>(effectType) ? new Effect() : ctx.ResolveKeyed<Effect>(effectType);
                };
            });
        }
    }
}
