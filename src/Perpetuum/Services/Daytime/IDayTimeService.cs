using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services.Daytime
{
    public interface IDayTimeService : IObservable<DayTimeInfo>, IProcess
    {
        [NotNull]
        DayTimeInfo GetCurrentDayTime();
    }
}
