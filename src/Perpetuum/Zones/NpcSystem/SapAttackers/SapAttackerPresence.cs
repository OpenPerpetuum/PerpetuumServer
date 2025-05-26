using Perpetuum.Zones.NpcSystem.Presences;
using System;

namespace Perpetuum.Zones.NpcSystem.SapAttackers
{
    public class SapAttackerPresence : INpcPresence
    {
        public int PresenceId { get; }
        public int MinStability { get; }
        public bool Spawned { get; private set; }
        public DynamicPresence ActivePresence { get; private set; }

        public double Threshold => throw new NotImplementedException();

        public SapAttackerPresence(int presenceID, int stability)
        {
            PresenceId = presenceID;
            MinStability = stability;
        }

        public override string ToString()
        {
            return $"{MinStability}:{PresenceId} Spawned? {Spawned}";
        }

        public void SetActivePresence(DynamicPresence presence)
        {
            ActivePresence = presence;
            Spawned = true;
        }

        public bool IsActivePresence(Presence presence)
        {
            return ReferenceEquals(ActivePresence, presence);
        }

        public void DeactivatePresence()
        {
            ActivePresence = null;
        }
    }
}
