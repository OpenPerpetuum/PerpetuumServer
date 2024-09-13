using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Robots
{
    public class RobotChassis : RobotComponent
    {
        public RobotChassis(IExtensionReader extensionReader) : base(RobotComponentType.Chassis, extensionReader)
        {
        }
    }
}
