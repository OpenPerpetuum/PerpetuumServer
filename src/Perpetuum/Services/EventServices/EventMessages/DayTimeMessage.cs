using Perpetuum.Services.Daytime;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class DayTimeMessage : EventMessage
    {
        public DayTimeInfo TimeInfo { get; private set; }
        public DayTimeMessage(DayTimeInfo time)
        {
            TimeInfo = time;
        }
    }
}
