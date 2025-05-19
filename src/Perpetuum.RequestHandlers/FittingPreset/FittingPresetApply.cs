using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Robots;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public class FittingPresetApply : FittingPresetRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public FittingPresetApply(IEntityRepository entityRepository, RobotHelper robotHelper)
        {
            _entityRepository = entityRepository;
            _robotHelper = robotHelper;
        }

        public override void HandleRequest(IRequest request)
        {
            using (System.Transactions.TransactionScope scope = Db.CreateTransaction())
            {
                int id = request.Data.GetOrDefault<int>(k.ID);
                long robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                bool forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

                Accounting.Characters.Character character = request.Session.Character;
                Robots.Fitting.IFittingPresetRepository repo = GetFittingPresetRepository(character, forCorporation);
                Robots.Fitting.FittingPreset preset = repo.Get(id);
                Robot robot = _robotHelper.LoadRobotForCharacter(robotEid, character);
                robot.ED.ThrowIfNotEqual(preset.Robot, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                Container container = Container.GetWithItems(containerEid, character);
                robot.EmptyRobot(character, container, false);
                robot.Initialize(character);

                foreach (IGrouping<RobotComponentType, Robots.Fitting.FittingPreset.ModuleInfo> moduleInfos in preset.Modules.GroupBy(i => i.Component))
                {
                    RobotComponent component = robot.GetRobotComponent(moduleInfos.Key).ThrowIfNull(ErrorCodes.ItemNotFound);

                    foreach (Robots.Fitting.FittingPreset.ModuleInfo moduleInfo in moduleInfos)
                    {
                        Module module = container.GetItems().OfType<Module>().FirstOrDefault(m => m.ED == moduleInfo.Module);
                        if (module == null)
                        {
                            continue;
                        }

                        module = (Module)module.Unstack(1);

                        if (module is ActiveModule activeModule && moduleInfo.Ammo != EntityDefault.None)
                        {
                            Ammo ammo = (Ammo)container.GetAndRemoveItemByDefinition(moduleInfo.Ammo.Definition, activeModule.AmmoCapacity);
                            if (ammo != null)
                            {
                                activeModule.SetAmmo(ammo);
                            }
                        }

                        component.EquipModuleOrThrow(module, moduleInfo.Slot, robot.Definition);
                    }
                }

                robot.Initialize(character);

                robot.Save();
                container.Save();

                Dictionary<string, object> result = new Dictionary<string, object>
                {
                    {k.robot, robot.ToDictionary()},
                    {k.container, container.ToDictionary()}
                };
                Message.Builder.FromRequest(request).WithData(result).Send();

                scope.Complete();
            }
        }
    }
}