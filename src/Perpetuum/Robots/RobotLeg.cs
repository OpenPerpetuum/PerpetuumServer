using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Robots
{
    public class RobotLeg : RobotComponent
    {
        public RobotLeg(IExtensionReader extensionReader) : base(RobotComponentType.Leg, extensionReader)
        {
        }
    }
}
