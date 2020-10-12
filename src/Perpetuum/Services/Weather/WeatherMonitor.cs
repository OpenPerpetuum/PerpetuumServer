using Perpetuum.Reactive;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using System;

namespace Perpetuum.Services.Weather
{

    public class WeatherEventListener : Observer<WeatherInfo>
    {

        private IZone _zone;
        private EventListenerService _listener;
        public WeatherEventListener(EventListenerService listener, IZone zone)
        {
            _zone = zone;
            _listener = listener;
        }

        public override void OnNext(WeatherInfo info)
        {
            _listener.PublishMessage(new WeatherEventMessage(info, _zone.Id));
        }
    }

    public class WeatherMonitor : Observer<WeatherInfo>
    {
        private Action<WeatherInfo> _onNext;
        public WeatherMonitor(Action<WeatherInfo> onNext)
        {
            _onNext = onNext;
        }

        public override void OnNext(WeatherInfo info)
        {
            _onNext(info);
        }

        protected override void OnDispose()
        {
            _onNext = null;
            base.OnDispose();
        }
    }
}
