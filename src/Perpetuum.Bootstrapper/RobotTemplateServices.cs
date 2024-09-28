using Perpetuum.Items.Templates;

namespace Perpetuum.Bootstrapper
{
    internal class RobotTemplateServices : IRobotTemplateServices
    {
        public IRobotTemplateReader Reader { get; set; }
        public IRobotTemplateRelations Relations { get; set; }
    }
}
