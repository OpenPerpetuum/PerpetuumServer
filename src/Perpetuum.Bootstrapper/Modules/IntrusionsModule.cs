using Autofac;
using Perpetuum.RequestHandlers.Intrusion;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class IntrusionsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterRequestHandler<BaseGetOwnershipInfo>(Commands.BaseGetOwnershipInfo);
            builder.RegisterRequestHandler<IntrusionGetPauseTime>(Commands.IntrusionGetPauseTime);
            builder.RegisterRequestHandler<IntrusionSetPauseTime>(Commands.IntrusionSetPauseTime);
            builder.RegisterRequestHandler<IntrusionUpgradeFacility>(Commands.IntrusionUpgradeFacility);
            builder.RegisterRequestHandler<SetIntrusionSiteMessage>(Commands.SetIntrusionSiteMessage);
            builder.RegisterRequestHandler<GetIntrusionLog>(Commands.GetIntrusionLog);
            builder.RegisterRequestHandler<GetIntrusionStabilityLog>(Commands.GetIntrusionStabilityLog);
            builder.RegisterRequestHandler<GetStabilityBonusThresholds>(Commands.GetStabilityBonusThresholds);
            builder.RegisterRequestHandler<GetIntrusionSiteInfo>(Commands.GetIntrusionSiteInfo);
            builder.RegisterRequestHandler<GetIntrusionPublicLog>(Commands.GetIntrusionPublicLog);
            builder.RegisterRequestHandler<GetIntrusionMySitesLog>(Commands.GetIntrusionMySitesLog);

            builder.RegisterZoneRequestHandler<IntrusionSAPGetItemInfo>(Commands.IntrusionSAPGetItemInfo);
            builder.RegisterZoneRequestHandler<IntrusionSAPSubmitItem>(Commands.IntrusionSAPSubmitItem);
            builder.RegisterZoneRequestHandler<IntrusionSiteSetEffectBonus>(Commands.IntrusionSiteSetEffectBonus);
            builder.RegisterZoneRequestHandler<IntrusionSetDefenseThreshold>(Commands.IntrusionSetDefenseThreshold);
        }
    }
}
