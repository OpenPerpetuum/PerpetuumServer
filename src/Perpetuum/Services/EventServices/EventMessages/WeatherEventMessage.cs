using Perpetuum.Services.Weather;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class WeatherEventMessage : EventMessage
    {
        public WeatherInfo Weather { get; private set; }
        public WeatherEventMessage(WeatherInfo weather)
        {
            Weather = weather;
        }
    }
}
