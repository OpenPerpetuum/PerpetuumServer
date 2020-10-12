using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class WeatherWatcher : EventProcessor<EventMessage>
    {
        private IZone _zone;
        private Lazy<ZoneEffect> _goodWeather;
        private Lazy<ZoneEffect> _badWeather;
        public WeatherWatcher(IZone zone)
        {
            _zone = zone;
            _goodWeather = new Lazy<ZoneEffect>(GetGoodWeather);
            _badWeather = new Lazy<ZoneEffect>(GetBadWeather);
        }

        private ZoneEffect GetGoodWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_intrusion_detection_lvl1, true);
        }

        private ZoneEffect GetBadWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_intrusion_masking_lvl1, true);
        }

        public override void OnNext(EventMessage value)
        {
            if (value is WeatherEventMessage msg && msg.ZoneId==_zone.Id)
            {
                if (msg.Weather.Current > 200)
                {
                    _zone.AddZoneEffect(_badWeather.Value);
                }
                else
                {
                    _zone.RemoveZoneEffect(_badWeather.Value);
                }

                if (msg.Weather.Current < 100)
                {
                    _zone.AddZoneEffect(_goodWeather.Value);
                }
                else
                {
                    _zone.RemoveZoneEffect(_goodWeather.Value);
                }
            }
        }
    }
}
