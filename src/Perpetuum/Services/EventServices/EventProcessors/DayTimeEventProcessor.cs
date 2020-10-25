using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class GameTimeEventProcessor : EventProcessor<EventMessage>
    {
        private IZone _zone;
        private readonly Lazy<ZoneEffect> _dayEffect;
        private readonly Lazy<ZoneEffect> _nightEffect;
        public GameTimeEventProcessor(IZone zone)
        {
            _zone = zone;
            _dayEffect = new Lazy<ZoneEffect>(GetDayEffect);
            _nightEffect = new Lazy<ZoneEffect>(GetNightEffect);
        }

        private ZoneEffect GetDayEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_day, true);
        }

        private ZoneEffect GetNightEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_night, true);
        }

        private static readonly int _sunrise = 150;
        private static readonly int _sunset = 750;

        private bool IsDay(GameTimeMessage msg)
        {
            return msg.TimeInfo.GameTimeStamp < 500 && msg.TimeInfo.GameTimeStamp > 300;
        }

        private bool IsNight(GameTimeMessage msg)
        {
            return msg.TimeInfo.GameTimeStamp < 50 || msg.TimeInfo.GameTimeStamp > 850;
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

