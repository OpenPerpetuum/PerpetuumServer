using Autofac;
using Perpetuum.Items.Templates;
using Perpetuum.Robots;

namespace Perpetuum.Bootstrapper.Modules
{
    internal class RobotTemplatesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.Register<RobotTemplateFactory>(x =>
            {
                IRobotTemplateRelations relations = x.Resolve<IRobotTemplateRelations>();
                return definition =>
                {
                    return relations.GetRelatedTemplateOrDefault(definition);
                };
            });

            _ = builder.RegisterType<RobotTemplateReader>().AsSelf().As<IRobotTemplateReader>();
            _ = builder.Register(x =>
            {
                return new CachedRobotTemplateReader(x.Resolve<RobotTemplateReader>());
            }).AsSelf().As<IRobotTemplateReader>().SingleInstance().OnActivated(e => e.Instance.Init());

            _ = builder.RegisterType<RobotTemplateRepository>().As<IRobotTemplateRepository>();
            _ = builder.RegisterType<RobotTemplateRelations>().As<IRobotTemplateRelations>().SingleInstance().OnActivated(e =>
            {
                e.Instance.Init();
            });

            _ = builder.RegisterType<RobotTemplateServices>().As<IRobotTemplateServices>().PropertiesAutowired().SingleInstance();

            _ = builder.RegisterType<HybridRobotBuilder>();

            _ = builder.RegisterType<RobotHelper>();
        }
    }
}
