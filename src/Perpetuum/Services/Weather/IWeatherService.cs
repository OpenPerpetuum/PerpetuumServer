using System;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Weather
{
    public interface IWeatherService : IObservable<WeatherInfo>, IProcess
    {
        [NotNull]
        WeatherInfo GetCurrentWeather();
        void SetCurrentWeather(WeatherInfo weather);
    }
}