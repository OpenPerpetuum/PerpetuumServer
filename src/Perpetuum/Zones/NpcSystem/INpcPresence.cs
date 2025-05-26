using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem
{
    public interface INpcPresence
    {
        DynamicPresence ActivePresence { get; }

        int PresenceId { get; }

        bool Spawned { get; }

        double Threshold { get; }

        int MinStability { get; }

        void SetActivePresence(DynamicPresence presence);

        bool IsActivePresence(Presence presence);

        /// <summary>
        /// Deactivate ActivePresence
        /// </summary>
        void DeactivatePresence();
    }
}