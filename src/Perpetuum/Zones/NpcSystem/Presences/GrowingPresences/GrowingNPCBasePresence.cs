using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;

namespace Perpetuum.Zones.NpcSystem.Presences.GrowingPresences
{
    public class GrowingNPCBasePresence : GrowingPresence
    {
        public GrowingNPCBasePresence(IZone zone, IPresenceConfiguration configuration, IEscalatingPresenceFlockSelector selector) : base(zone, configuration, selector) { }
        protected override void InitStateMachine()
        {
            CurrentGrowthLevel = FastRandom.NextInt(9);
            StackFSM = new StackFSM();
            StackFSM.Push(new NPCBaseSpawnState(this));
        }
    }
}
