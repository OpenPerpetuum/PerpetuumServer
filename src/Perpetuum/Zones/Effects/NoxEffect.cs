using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Effects
{
    public class NoxEffect : AuraEffect
    {
        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            IEnumerable<Unit> affectedUnits = zone.GetUnitsWithinRange2D(Owner.CurrentPosition, Radius);
            affectedUnits = Owner is Npc npc
                ? affectedUnits.Where(u => u is Player)
                : zone.Configuration.IsAlpha
                    ? affectedUnits.Where(u => u is Npc || (u is Player player && player.HasPvpEffect))
                    : affectedUnits.Where(u => u is Npc || (u is Player player && !player.HasNoTeleportWhilePVP));

            return affectedUnits;
        }
    }
}
