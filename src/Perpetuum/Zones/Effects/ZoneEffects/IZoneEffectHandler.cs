using Perpetuum.Players;
using Perpetuum.Units;
using System;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// Handles application of any/all ZoneEffects
    /// </summary>
    public interface IZoneEffectHandler
    {
        event Action<ZoneEffect> EffectAdded;
        event Action<ZoneEffect> EffectRemoved;
        void ApplyZoneEffects(Unit unit);
        void RemoveEffect(ZoneEffect zoneEffect);
        void AddEffect(ZoneEffect zoneEffect);
    }
}
