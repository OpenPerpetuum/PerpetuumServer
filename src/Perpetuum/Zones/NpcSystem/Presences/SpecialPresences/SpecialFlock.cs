using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences.SpecialPresences
{
    public class SpecialFlock : RoamingFlock
    {
        public SpecialFlock(IFlockConfiguration configuration, Presence presence) : base(configuration, presence) { }

        protected override bool IsPresenceInSpawningState()
        {
            if (Presence is SpecialPresence pres)
            {
                return pres.StackFSM.Current is StaticSpawnState;
            }
            return base.IsPresenceInSpawningState();
        }

        protected override Position GetSpawnPosition(Position spawnOrigin)
        {
            if (Presence is SpecialPresence pres)
            {
                spawnOrigin = pres.SpawnOrigin.Clamp(Presence.Zone.Size);
            }
            return base.GetSpawnPosition(spawnOrigin);
        }
    }
}
