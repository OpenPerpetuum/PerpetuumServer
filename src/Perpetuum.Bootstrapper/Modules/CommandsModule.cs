using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Module = Autofac.Module;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class CommandsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            foreach (Command command in GetCommands())
            {
                _ = builder.RegisterInstance(command).As<Command>().Keyed<Command>(command.Text.ToUpper());
            }

            _ = builder.Register<Func<string, Command>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return commandText =>
                {
                    commandText = commandText.ToUpper();
                    return ctx.IsRegisteredWithKey<Command>(commandText) ? ctx.ResolveKeyed<Command>(commandText) : null;
                };
            });
        }

        public static IEnumerable<Command> GetCommands()
        {
            return typeof(Commands).GetFields(BindingFlags.Static | BindingFlags.Public).Select(info => (Command)info.GetValue(null));
        }
    }
}
