using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class WeatherWatcher : EventProcessor<EventMessage>
    {
        private IZone _zone;
        private ZoneEffect _goodWeather;
        private ZoneEffect _badWeather;
        public WeatherWatcher(IZone zone)
        {
            _zone = zone;
            _goodWeather = new ZoneEffect(_zone.Id, EffectType.effect_intrusion_detection_lvl1, true);
            _badWeather = new ZoneEffect(_zone.Id, EffectType.effect_intrusion_masking_lvl1, true);
        }

        public override void OnNext(EventMessage value)
        {
            if (value is WeatherEventMessage msg)
            {
                if (msg.Weather.Current > 200)
                {
                    _zone.AddZoneEffect(_badWeather);
                }
                else
                {
                    _zone.RemoveZoneEffect(_badWeather);
                }

                if (msg.Weather.Current < 100)
                {
                    _zone.AddZoneEffect(_goodWeather);
                }
                else
                {
                    _zone.RemoveZoneEffect(_goodWeather);
                }
            }
        }
    }
}
