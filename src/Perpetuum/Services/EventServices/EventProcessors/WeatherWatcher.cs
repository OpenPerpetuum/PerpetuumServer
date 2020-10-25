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
        private const int BAD_WEATHER = 200;
        private const int GOOD_WEATHER = 100;

        private readonly IZone _zone;
        private readonly Lazy<ZoneEffect> _goodWeather;
        private readonly Lazy<ZoneEffect> _badWeather;
        public WeatherWatcher(IZone zone)
        {
            _zone = zone;
            _goodWeather = new Lazy<ZoneEffect>(CreateGoodWeather);
            _badWeather = new Lazy<ZoneEffect>(CreateBadWeather);
        }

        private ZoneEffect CreateGoodWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_good, true);
        }

        private ZoneEffect CreateBadWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_bad, true);
        }

        private bool IsBadWeather(WeatherInfo info)
        {
            return info.Current > BAD_WEATHER && info.Next > BAD_WEATHER;
        }

        private bool IsGoodWeather(WeatherInfo info)
        {
            return info.Current < GOOD_WEATHER && info.Next < GOOD_WEATHER;
        }

        public override void OnNext(EventMessage value)
        {
            if (value is WeatherEventMessage msg && msg.ZoneId == _zone.Id)
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
