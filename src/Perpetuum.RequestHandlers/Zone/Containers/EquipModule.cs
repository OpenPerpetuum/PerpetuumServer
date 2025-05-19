using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class EquipModule : ZoneChangeModule
    {
        public override void DoChange(IZoneRequest request, Player player, Container container)
        {
            RobotComponentType componentType = request.Data.GetOrDefault<string>(k.robotComponent).ToEnum<RobotComponentType>();
            RobotComponent component = player.GetRobotComponentOrThrow(componentType);
            int slot = request.Data.GetOrDefault<int>(k.slot);
            component.GetModule(slot).ThrowIfNotNull(ErrorCodes.UsedSlot); //OPP: explicitly prohibit this to make this simpler
            // component.MakeSlotFree(slot, container); // Big nope

            long moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
            Module module = (Module)container.GetItemOrThrow(moduleEid).Unstack(1);
            module.CheckEnablerExtensionsAndThrowIfFailed(player.Character);

            //Perform pre-fit check for fitting legality
            player.CheckEnergySystemAndThrowIfFailed(module);

            Robot robot = player;

            component.EquipModuleOrThrow(module, slot, robot.Definition);
        }
    }
}