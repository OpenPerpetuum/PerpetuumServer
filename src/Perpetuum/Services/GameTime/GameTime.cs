using System;

namespace Perpetuum.Services.Daytime
{
    public static class GameTime
    {
        private static readonly int _gameDayLength = 1000;
        //private static readonly int _sunrise = 150;
        //private static readonly int _sunset = 750;
        private static readonly double _gameDaySpan = TimeSpan.FromHours(3).TotalHours;
        public static GameTimeInfo GetCurrentDayTime()
        {
            return GetNLTForTime(DateTime.Now);
        }

        public static GameTimeInfo GetNLTForTime(DateTime time)
        {
            var currentDayTime = time.TimeOfDay.TotalHours;
            var nltDayFactor = (currentDayTime % _gameDaySpan) / _gameDaySpan;
            var nlt = (int)(nltDayFactor.Clamp() * _gameDayLength);
            return new GameTimeInfo(nlt);
        }
    }
}
