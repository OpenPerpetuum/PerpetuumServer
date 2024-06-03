using Perpetuum.Zones;

namespace Perpetuum.Units
{
    public interface IUnitVisibility
    {
        Unit Target { get; }

        LOSResult GetLineOfSight(bool ballistic);
    }
}
