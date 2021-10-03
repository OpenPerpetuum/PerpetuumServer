using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;
using System.Linq;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;

namespace Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence
{
    /// <summary>
    /// A non-roaming ExpiringPresence that would spawning with Roaming rules
    /// </summary>
    public class RandomSpawningExpiringPresence : ExpiringPresence, IRoamingPresence
    {
        public StackFSM StackFSM { get; protected set; }
        public Position SpawnOrigin { get; set; }
        public IRoamingPathFinder PathFinder { get; set; }
        public override Area Area => Configuration.Area;
        public Point CurrentRoamingPosition { get; set; }

        public RandomSpawningExpiringPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);

            InitStateMachine();
        }

        protected virtual void InitStateMachine()
        {
            StackFSM = new StackFSM();
            StackFSM.Push(new StaticSpawnState(this));
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            StackFSM?.Update(time);
        }

        protected override void OnPresenceExpired()
        {
            StackFSM?.Clear();
            foreach (var flock in Flocks)
            {
                flock.RemoveAllMembersFromZone(true);
            }
            ResetDynamicDespawnTimer();
            InitStateMachine();
            base.OnPresenceExpired();
        }

        public void OnSpawned()
        {
            ResetDynamicDespawnTimer();
        }
    }

    public class GrowingPresence : RandomSpawningExpiringPresence
    {
        public TimeSpan GrowTime { get; private set; }
        public IEscalatingPresenceFlockSelector Selector { get; private set; }
        public GrowingPresence(IZone zone, IPresenceConfiguration configuration, IEscalatingPresenceFlockSelector selector) : base(zone, configuration)
        {
            Selector = selector;
            if (Configuration.GrowthSeconds != null)
                GrowTime = TimeSpan.FromSeconds((int)Configuration.GrowthSeconds);
        }

        protected override void InitStateMachine()
        {
            StackFSM = new StackFSM();
            StackFSM.Push(new GrowSpawnState(this));
        }

        public override void LoadFlocks()
        {
            var flockConfigs = Selector.GetFlocksForPresenceLevel(this, 0);
            foreach (var config in flockConfigs)
            {
                CreateAndAddFlock(config);
            }
        }

        protected override void OnPresenceExpired()
        {
            base.OnPresenceExpired();
            ClearFlocks();
            LoadFlocks();
        }
    }

    public class StaticSpawnState : SpawnState
    {
        private readonly int BASE_RADIUS = 300;
        private readonly int PLAYER_RADIUS = 150;
        public StaticSpawnState(IRoamingPresence presence, int playerMinDist = 200) : base(presence, playerMinDist) { }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new NullRoamingState(_presence));
        }

        protected override bool IsInRange(Position position, int range)
        {
            var zone = _presence.Zone;
            if (zone.Configuration.IsGamma && zone.IsUnitWithCategoryInRange(CategoryFlags.cf_pbs_docking_base, position, BASE_RADIUS))
                return true;
            else if (zone.GetStaticUnits().OfType<DockingBase>().WithinRange2D(position, BASE_RADIUS).Any())
                return true;
            else if (zone.GetStaticUnits().OfType<Teleport>().WithinRange2D(position, PLAYER_RADIUS).Any())
                return true;
            else if (zone.PresenceManager.GetPresences().OfType<RandomSpawningExpiringPresence>().Where(p => p.SpawnOrigin.IsInRangeOf2D(position, BASE_RADIUS)).Any())
                return true;

            return zone.Players.WithinRange2D(position, range).Any();
        }
    }

    public class GrowSpawnState : StaticSpawnState
    {
        private readonly GrowingPresence _growingPresence;
        public GrowSpawnState(GrowingPresence presence, int playerMinDist = 200) : base(presence, playerMinDist)
        {
            _growingPresence = presence;
        }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new GrowthState(_growingPresence));
        }
    }

    public class NPCBaseGrowState : GrowSpawnState
    {
        public NPCBaseGrowState(GrowingPresence presence, int playerMinDist = 200) : base(presence, playerMinDist) { }


        protected override bool IsValidSpawnPosition(Position position, int range)
        {
            if(!_presence.Zone.Size.Contains(position.intX, position.intY))
            {
                return false;
            }
            else if(_presence.Zone.Terrain.Controls.GetValue(position.intX, position.intY).IsAnyTerraformProtected)
            {
                return false;
            }
            return IsInRange(position, range);
        }

        protected override Position FindSpawnPosition()
        {
            return _presence.PathFinder.FindSpawnPosition(_presence).ToPosition();
        }
    }

    public class GrowthState : NullRoamingState
    {
        private readonly GrowingPresence _growingPresence;
        private readonly TimeTracker _timer;
        private int _currentLevel = 0;
        public GrowthState(GrowingPresence presence) : base(presence)
        {
            _growingPresence = presence;
            _timer = new TimeTracker(_growingPresence.GrowTime);
        }

        public override void Update(TimeSpan time)
        {
            if (IsRunningTask)
                return;

            var members = GetAllMembers();
            if (IsDeadAndExiting(members))
                return;

            if (!NextWaveReady(time))
                return;

            RunTask(() => SpawnNextWave(), t => { });
        }

        private bool NextWaveReady(TimeSpan time)
        {
            if (!CheckTimer(time))
                return false;

            _currentLevel++;
            return true;
        }

        private bool CheckTimer(TimeSpan time)
        {
            _timer.Update(time);
            if (!_timer.Expired)
                return false;

            _timer.Reset();
            return true;
        }

        private void SpawnNextWave()
        {
            var flockConfigs = _growingPresence.Selector.GetFlocksForPresenceLevel(_growingPresence, _currentLevel);
            foreach (var config in flockConfigs)
            {
                var flock = _growingPresence.CreateAndAddFlock(config);
                flock.SpawnAllMembers();
            }
        }
    }
}
