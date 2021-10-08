using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;
using Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence;
using System;

namespace Perpetuum.Zones.NpcSystem.Presences.GrowingPresences
{
    public class GrowingPresence : RandomSpawningExpiringPresence
    {
        public TimeSpan GrowTime { get; private set; }
        public IEscalatingPresenceFlockSelector Selector { get; private set; }
        public int CurrentGrowthLevel { get; private set; }
        public GrowingPresence(IZone zone, IPresenceConfiguration configuration, IEscalatingPresenceFlockSelector selector) : base(zone, configuration)
        {
            Selector = selector;
            if (Configuration.GrowthSeconds != null)
                GrowTime = TimeSpan.FromSeconds((int)Configuration.GrowthSeconds);
        }

        protected override void InitStateMachine()
        {
            CurrentGrowthLevel = FastRandom.NextInt(9);
            StackFSM = new StackFSM();
            StackFSM.Push(new NPCBaseGrowState(this));
        }

        public override void LoadFlocks()
        {
            for (var i = 0; i <= CurrentGrowthLevel; i++)
            {
                var flockConfigs = Selector.GetFlocksForPresenceLevel(this, i);
                foreach (var config in flockConfigs)
                {
                    CreateAndAddFlock(config);
                }
            }
        }

        public void OnWaveSpawn(int level)
        {
            CurrentGrowthLevel = Math.Max(CurrentGrowthLevel, level);
        }

        protected override void OnPresenceExpired()
        {
            base.OnPresenceExpired();
            ClearFlocks();
            CurrentGrowthLevel = 0;
            LoadFlocks();
        }
    }
}
