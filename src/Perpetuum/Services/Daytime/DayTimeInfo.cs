namespace Perpetuum.Services.Daytime
{
    public class DayTimeInfo
    {
        public int NLT { get; private set; }
        public DayTimeInfo(int nlt)
        {
            NLT = nlt;
        }
    }
}
