using Autofac;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.ProductionNodes;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class PbsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterGeneric(typeof(PBSObjectHelper<>));
            _ = builder.RegisterGeneric(typeof(PBSReinforceHandler<>));
            _ = builder.RegisterType<PBSProductionFacilityNodeHelper>();
        }
    }
}
