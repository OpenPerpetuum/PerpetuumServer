using Perpetuum.Players;
using Perpetuum.Units;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    public class ZoneEffectHandler : IZoneEffectHandler
    {
        private const byte _ = 0;
        private readonly IZone _zone;
        private readonly Lazy<ConcurrentDictionary<ZoneEffect, byte>> effects;

        public event Action<ZoneEffect> EffectAdded;
        public event Action<ZoneEffect> EffectRemoved;
        public ZoneEffectHandler(IZone zone)
        {
            _zone = zone;
            effects = new Lazy<ConcurrentDictionary<ZoneEffect, byte>>(InitCollection);
        }

        private ConcurrentDictionary<ZoneEffect, byte> InitCollection()
        {
            var dict = new ConcurrentDictionary<ZoneEffect, byte>();
            foreach (var zoneEffect in ZoneEffectReader.GetStaticZoneEffects(_zone))
            {
                dict.Add(zoneEffect, _);
            }
            return dict;
        }

        public void AddEffect(ZoneEffect zoneEffect)
        {
            if (zoneEffect != null && effects.Value.TryAdd(zoneEffect, _))
            {
                EffectAdded?.Invoke(zoneEffect);
            }
        }

        public void RemoveEffect(ZoneEffect zoneEffect)
        {
            if (zoneEffect != null && effects.Value.TryRemove(zoneEffect, out byte b))
            {
                EffectRemoved?.Invoke(zoneEffect);
            }
        }

        private IEnumerable<ZoneEffect> GetEffects()
        {
            return effects.Value.Keys;
        }

        private bool CheckCanApply(Unit unit, ZoneEffect zoneEffect)
        {
            if (unit.EffectHandler.ContainsEffect(zoneEffect.Effect)) //dont apply if already on
            {
                return false;
            }
            else if (unit is Player && zoneEffect.PlayerOnly) //apply if for player and is player
            {
                return true;
            }
            //apply if not player and is not for player
            return !zoneEffect.PlayerOnly;
        }

        public void ApplyZoneEffects(Unit unit)
        {
            foreach (var zoneEffect in GetEffects())
            {
                if (CheckCanApply(unit, zoneEffect))
                {
                    var builder = unit.NewEffectBuilder().SetType(zoneEffect.Effect).SetOwnerToSource();
                    unit.ApplyEffect(builder);
                }
            }
        }
    }
}
