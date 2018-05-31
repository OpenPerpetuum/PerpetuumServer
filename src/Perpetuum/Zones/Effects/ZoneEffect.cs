using System.Collections.Generic;
using System.Linq;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{

    /// <summary>
    /// Zone based effect
    /// </summary>
    public class ZoneEffect : AuraEffect
    {

        protected override void OnTick()
        {
            var remove = false;

            try
            {
                var zone = Owner.Zone;
                var sourceIsInZone = Owner.InZone;
                var sourceIsDead = Owner.States.Dead;
                var containsAuraEffect = Owner.EffectHandler.ContainsToken(Token);

                // nincsen a terepen vagy nincs mar rajta az effect
                if (zone == null || !sourceIsInZone || sourceIsDead || !containsAuraEffect)
                {
                    remove = true;
                    return;
                }

                // ha ez az eredeti effect akkor keres targeteket
                if (Owner is Player)
                {
                    Unit unit = Owner as Unit;
                    var effectBuilder = unit.NewEffectBuilder();
                    SetupEffect(effectBuilder);
                    unit.ApplyEffect(effectBuilder);
                }
                else
                {
                    remove = true;
                }
            }
            finally
            {
                if (remove)
                {
                    OnRemoved();
                }
            }
        }

        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            return zone.Players.Where(p => p.InZone);
        }

    }
}