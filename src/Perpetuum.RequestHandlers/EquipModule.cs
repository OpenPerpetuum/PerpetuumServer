using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Robots;
using System.Collections.Generic;
using System.Transactions;

namespace Perpetuum.RequestHandlers
{
    public class EquipModule : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public EquipModule(IEntityRepository entityRepository, RobotHelper robotHelper)
        {
            _entityRepository = entityRepository;
            _robotHelper = robotHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (TransactionScope scope = Db.CreateTransaction())
            {
                Accounting.Characters.Character character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                long containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                Container container = Container.GetWithItems(containerEid, character).ThrowIfNull(ErrorCodes.ContainerNotFound);
                container.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

                long robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                Robot robot = _robotHelper.LoadRobotOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);
                robot.Initialize(character);

                int slot = request.Data.GetOrDefault<int>(k.slot);
                RobotComponentType componentType = request.Data.GetOrDefault<string>(k.robotComponent).ToEnum<RobotComponentType>();
                RobotComponent component = robot.GetRobotComponentOrThrow(componentType);
                component.MakeSlotFree(slot, container);
                long moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
                Module module = (Module)container.GetItemOrThrow(moduleEid).Unstack(1);
                component.EquipModuleOrThrow(module, slot, robot.Definition);

                robot.Initialize(character);
                robot.Save();
                container.Save();

                Transaction.Current.OnCompleted(completed =>
                {
                    Dictionary<string, object> result = new Dictionary<string, object>
                    {
                        {k.robot, robot.ToDictionary()},
                        {k.container, container.ToDictionary()}
                    };
                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });

                scope.Complete();
            }
        }
    }
}