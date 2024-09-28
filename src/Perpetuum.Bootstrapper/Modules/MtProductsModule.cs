using Autofac;
using Perpetuum.Accounting;
using Perpetuum.RequestHandlers;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class MtProductsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterType<MtProductRepository>().As<IMtProductRepository>();
            _ = builder.RegisterType<MtProductHelper>();
            builder.RegisterRequestHandler<MtProductPriceList>(Commands.MtProductPriceList);
        }
    }
}
