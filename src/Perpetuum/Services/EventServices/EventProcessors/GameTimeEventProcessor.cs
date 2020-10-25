using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class GameTimeEventProcessor : EventProcessor<EventMessage>
    {
        private const int NIGHT_END = 100;
        //private const int SUNRISE = 150;
        private const int DAY_START = 200;
        private const int DAY_END = 700;
        //private const int SUNSET = 750;
        private const int NIGHT_START = 800;

        private readonly IZone _zone;
        private readonly Lazy<ZoneEffect> _dayEffect;
        private readonly Lazy<ZoneEffect> _nightEffect;
        public GameTimeEventProcessor(IZone zone)
        {
            _zone = zone;
            _dayEffect = new Lazy<ZoneEffect>(CreateDayEffect);
            _nightEffect = new Lazy<ZoneEffect>(CreateNightEffect);
        }

        private ZoneEffect CreateDayEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_day, true);
        }

        private ZoneEffect CreateNightEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_night, true);
        }

        private bool IsDay(GameTimeMessage msg)
        {
            return msg.TimeInfo.GameTimeStamp > DAY_START && msg.TimeInfo.GameTimeStamp < DAY_END;
        }

        private bool IsNight(GameTimeMessage msg)
        {
            return msg.TimeInfo.GameTimeStamp > NIGHT_START || msg.TimeInfo.GameTimeStamp < NIGHT_END;
        }

        public override void OnNext(EventMessage value)
        {
            if (value is GameTimeMessage msg)
            {
                if (IsNight(msg))
                {
                    _zone.ZoneEffectHandler.AddEffect(_nightEffect.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_nightEffect.Value);
                }

                if (IsDay(msg))
                {
                    _zone.ZoneEffectHandler.AddEffect(_dayEffect.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_dayEffect.Value);
                }
            }
        }
    }
}

