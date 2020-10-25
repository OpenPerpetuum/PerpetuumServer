using Perpetuum.Reactive;
using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services.Daytime
{
    public class DayTimeService : Process, IDayTimeService
    {
        private readonly Observable<DayTimeInfo> _observable;
        private DayTimeInfo _current;
        public DayTimeService()
        {
            _current = DayTime.GetCurrentDayTime();
            _observable = new AnonymousObservable<DayTimeInfo>(OnSubscribe);
        }

        private void RefreshCurrentDayTime()
        {
            _current = DayTime.GetCurrentDayTime();
        }

        public DayTimeInfo GetCurrentDayTime()
        {
            if (_current == null)
            {
                RefreshCurrentDayTime();
            }
            return _current;
        }

        private void OnSubscribe(IObserver<DayTimeInfo> observer)
        {
            observer.OnNext(GetCurrentDayTime());
        }

        public IDisposable Subscribe(IObserver<DayTimeInfo> observer)
        {
            return _observable.Subscribe(observer);
        }

        private void SendDayTimeNotification()
        {
            _observable.OnNext(GetCurrentDayTime());
        }

        public override void Update(TimeSpan time)
        {
            RefreshCurrentDayTime();
            SendDayTimeNotification();
        }
    }
}
