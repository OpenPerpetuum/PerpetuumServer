using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem
{
    public interface INpcPresences
    {
        INpcPresence GetNextPresence(double threshold);

        INpcPresence GetNextPresence(int minStability);

        bool HasActivePresence(Presence presence);

        INpcPresence GetActivePresence(Presence presence);

        INpcPresence[] GetAllActivePresences();
    }
}
