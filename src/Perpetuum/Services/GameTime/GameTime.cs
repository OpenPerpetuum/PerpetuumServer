using System;

namespace Perpetuum.Services.Daytime
{
    public static class GameTime
    {
        private const int DAY_LENGTH = 1000;
        private static readonly double _gameDaySpan = TimeSpan.FromHours(3).TotalHours;
        public static GameTimeInfo GetCurrentDayTime()
        {
            return GetNLTForTime(DateTime.Now);
        }

        public static GameTimeInfo GetNLTForTime(DateTime time)
        {
            var currentDayTime = time.TimeOfDay.TotalHours;
            var nltDayFactor = (currentDayTime % _gameDaySpan) / _gameDaySpan;
            var nlt = (int)(nltDayFactor.Clamp() * DAY_LENGTH);
            return new GameTimeInfo(nlt);
        }
    }
}
