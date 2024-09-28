using Autofac;
using Autofac.Builder;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Looting;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Bootstrapper
{
    internal static class ContainerBuilderExtensions
    {
        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<T>(this ContainerBuilder builder, Command command) where T : IRequestHandler<IRequest>
        {
            return RegisterRequestHandler<T, IRequest>(builder, command);
        }

        public static void RegisterFlock<T>(this ContainerBuilder builder, PresenceType presenceType) where T : Flock
        {
            _ = builder.RegisterType<T>().Keyed<Flock>(presenceType).OnActivated(e =>
            {
                e.Instance.EntityService = e.Context.Resolve<IEntityServices>();
                e.Instance.LootService = e.Context.Resolve<ILootService>();
            });
        }

        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterPresence<T>(this ContainerBuilder builder, PresenceType presenceType) where T : Presence
        {
            return builder.RegisterType<T>().Keyed<Presence>(presenceType).PropertiesAutowired();
        }

        public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterZoneRequestHandler<T>(this ContainerBuilder builder, Command command) where T : IRequestHandler<IZoneRequest>
        {
            return RegisterRequestHandler<T, IZoneRequest>(builder, command);
        }

        private static IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<TRequestHandler, TRequest>(ContainerBuilder builder, Command command)
            where TRequestHandler : IRequestHandler<TRequest>
            where TRequest : IRequest
        {
            IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> res = builder.RegisterType<TRequestHandler>();

            _ = builder.Register(c =>
            {
                return c.Resolve<RequestHandlerProfiler<TRequest>>(new TypedParameter(typeof(IRequestHandler<TRequest>), c.Resolve<TRequestHandler>()));
            }).Keyed<IRequestHandler<TRequest>>(command);

            return res;
        }
    }
}
