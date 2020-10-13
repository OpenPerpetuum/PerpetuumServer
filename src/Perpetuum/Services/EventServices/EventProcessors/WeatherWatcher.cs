using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.Weather;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class WeatherWatcher : EventProcessor<EventMessage>
    {
        private IZone _zone;
        private readonly Lazy<ZoneEffect> _goodWeather;
        private readonly Lazy<ZoneEffect> _badWeather;
        public WeatherWatcher(IZone zone)
        {
            _zone = zone;
            _goodWeather = new Lazy<ZoneEffect>(GetGoodWeather);
            _badWeather = new Lazy<ZoneEffect>(GetBadWeather);
        }

        private ZoneEffect GetGoodWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_good, true);
        }

        private ZoneEffect GetBadWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_bad, true);
        }

        private bool IsBadWeather(WeatherInfo info)
        {
            return info.Current > 200 && info.Next > 200;
        }

        private bool IsGoodWeather(WeatherInfo info)
        {
            return info.Current < 100 && info.Next < 100;
        }

        public override void OnNext(EventMessage value)
        {
            if (value is WeatherEventMessage msg && msg.ZoneId==_zone.Id)
            {
                if (IsBadWeather(msg.Weather))
                {
                    _zone.ZoneEffectHandler.AddEffect(_badWeather.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_badWeather.Value);
                }

                if (IsGoodWeather(msg.Weather))
                {
                    _zone.ZoneEffectHandler.AddEffect(_goodWeather.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_goodWeather.Value);
                }
            }
        }
    }
}
