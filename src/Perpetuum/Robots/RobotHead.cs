using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Robots
{
    public class RobotHead : RobotComponent
    {
        public RobotHead(IExtensionReader extensionReader) : base(RobotComponentType.Head, extensionReader)
        {
        }
    }
}
