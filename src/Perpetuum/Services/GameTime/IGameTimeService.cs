using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services.Daytime
{
    public interface IGameTimeService : IObservable<GameTimeInfo>, IProcess
    {
        [NotNull]
        GameTimeInfo GetCurrentDayTime();
    }
}
