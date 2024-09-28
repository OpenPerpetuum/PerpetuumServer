using Autofac;
using Perpetuum.Common.Loggers;
using Perpetuum.Log;
using Perpetuum.Log.Formatters;
using Perpetuum.Log.Loggers;
using Perpetuum.Services.Channels;
using Perpetuum.Zones.CombatLogs;
using System;
using System.IO;
using System.Runtime.Caching;
using System.Text;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class LoggersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.Register(x =>
            {
                return new LoggerCache(new MemoryCache("LoggerCache"))
                {
                    Expiration = TimeSpan.FromHours(1)
                };
            }).As<ILoggerCache>().SingleInstance();

            _ = builder.Register<ChannelLoggerFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return name =>
                {
                    FileLogger<ChatLogEvent> fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke("channels", name);
                    return new ChannelLogger(fileLogger);
                };
            });

            _ = builder.RegisterGeneric(typeof(FileLogger<>));

            _ = builder.Register<Func<string, string, FileLogger<ChatLogEvent>>>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (directory, filename) =>
                {
                    ChatLogFormatter formatter = new ChatLogFormatter();
                    return ctx.Resolve<FileLogger<ChatLogEvent>.Factory>().Invoke(formatter, () => Path.Combine("chatlogs", directory, filename, DateTime.Now.ToString("yyyy-MM-dd"), $"{filename.RemoveSpecialCharacters()}.txt"));
                };
            });

            _ = builder.Register<ChatLoggerFactory>(x =>
            {
                IComponentContext ctx = x.Resolve<IComponentContext>();
                return (directory, filename) =>
                {
                    FileLogger<ChatLogEvent> fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke(directory, filename);
                    return fileLogger;
                };
            });


            _ = builder.Register(c =>
            {
                DefaultLogEventFormatter defaultFormater = new DefaultLogEventFormatter();

                DelegateLogEventFormatter<LogEvent, string> formater = new DelegateLogEventFormatter<LogEvent, string>(e =>
                {
                    string formatedEvent = defaultFormater.Format(e);

                    if (!(e.ThrownException is PerpetuumException gex))
                    {
                        return formatedEvent;
                    }

                    StringBuilder sb = new StringBuilder(formatedEvent);

                    _ = sb.AppendLine();
                    _ = sb.AppendLine();
                    _ = sb.AppendFormat("Error = {0}\n", gex.error);

                    if (gex.Data.Count > 0)
                    {
                        _ = sb.AppendFormat("Data: {0}", gex.Data.ToDictionary().ToDebugString());
                    }

                    return sb.ToString();
                });

                FileLogger<LogEvent> fileLogger = c.Resolve<FileLogger<LogEvent>.Factory>().Invoke(formater, () => Path.Combine("logs", DateTime.Now.ToString("yyyy-MM-dd"), "hostlog.txt"));
                fileLogger.BufferSize = 100;
                fileLogger.AutoFlushInterval = TimeSpan.FromSeconds(10);

                return new CompositeLogger<LogEvent>(fileLogger, new ColoredConsoleLogger(formater));
            }).As<ILogger<LogEvent>>();

            _ = builder.RegisterType<CombatLogger>();
            _ = builder.RegisterType<CombatLogHelper>();
            _ = builder.RegisterType<CombatSummary>();
            _ = builder.RegisterType<CombatLogSaver>().As<ICombatLogSaver>();

            _ = builder.RegisterGeneric(typeof(DbLogger<>));
        }
    }
}
