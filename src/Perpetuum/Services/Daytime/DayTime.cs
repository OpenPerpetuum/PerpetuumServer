using System;

namespace Perpetuum.Services.Daytime
{
    public static class DayTime
    {
        private static readonly int _nltTotal = 1000;
        //private static readonly int _sunrise = 150;
        //private static readonly int _sunset = 750;
        private static readonly double _nltDaySpan = TimeSpan.FromHours(3).TotalHours;
        public static DayTimeInfo GetCurrentDayTime()
        {
            return GetNLTForTime(DateTime.Now);
        }

        public static DayTimeInfo GetNLTForTime(DateTime time)
        {
            var currentDayTime = time.TimeOfDay.TotalHours;
            var nltDayFactor = (currentDayTime % _nltDaySpan) / _nltDaySpan;
            var nlt = (int)(nltDayFactor.Clamp() * _nltTotal);
            return new DayTimeInfo(nlt);
        }
    }
}
