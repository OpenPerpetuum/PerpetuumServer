using Perpetuum.Reactive;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;

namespace Perpetuum.Services.Daytime
{
    public class DayObserver : Observer<DayTimeInfo>
    {
        private readonly EventListenerService _listener;
        public DayObserver(EventListenerService listener)
        {
            _listener = listener;
        }

        public override void OnNext(DayTimeInfo info)
        {
            _listener.PublishMessage(new DayTimeMessage(info));
        }
    }
}
