using Perpetuum.Reactive;
using System;

namespace Perpetuum.Services.Weather
{
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
