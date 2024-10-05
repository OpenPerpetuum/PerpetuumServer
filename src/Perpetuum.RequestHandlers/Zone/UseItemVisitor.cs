using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Gates;

namespace Perpetuum.RequestHandlers.Zone
{
    public class UseItemVisitor : IEntityVisitor<Unit>, IEntityVisitor<Gate>, IEntityVisitor<Robot>
    {
        private readonly IZone _zone;
        private readonly Character _character;

        public UseItemVisitor(IZone zone, Character character)
        {
            _zone = zone;
            _character = character;
        }

        public void Visit(Unit unit)
        {
            if (unit is IUsableItem usable)
            {
                //fallback, can only be used if the player is on the zone
                //zone packet used in this case

                if (_zone.TryGetPlayer(_character, out Player player))
                {
                    usable.UseItem(player);
                }
            }
        }

        /// <summary>
        /// Gate can be used from anywhere
        /// </summary>
        /// <param name="gate"></param>
        public void Visit(Gate gate)
        {
            gate.UseGateWithCharacter(_character, _character.CorporationEid);
        }

        public void Visit(Robot robot)
        {
            robot.Kill();
        }
    }
}