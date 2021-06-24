using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System;
using System.Linq;
using System.Drawing;
using Perpetuum.ExportedTypes;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Log;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences.SpecialPresences
{
    /// <summary>
    /// A non-roaming ExpiringPresence that would spawning with Roaming rules
    /// </summary>
    public class SpecialPresence : ExpiringPresence, IRoamingPresence
    {
        public StackFSM StackFSM { get; }
        public Position SpawnOrigin { get; set; }
        public IRoamingPathFinder PathFinder { get; set; }
        public override Area Area => Configuration.Area;
        public Point CurrentRoamingPosition { get; set; }

        public SpecialPresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);

            StackFSM = new StackFSM();
            StackFSM.Push(new StaticSpawnState(this));
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            StackFSM.Update(time);
        }

        protected override void OnPresenceExpired()
        {
            base.OnPresenceExpired();
            ResetDynamicDespawnTimer();
            foreach (var flock in Flocks)
            {
                flock.RemoveAllMembersFromZone(true);
            }
        }

        public void OnSpawned()
        {
            ResetDynamicDespawnTimer();
        }
    }

    public class StaticSpawnState : SpawnState
    {
        private readonly int BASE_RADIUS = 300;
        private readonly int PLAYER_RADIUS = 150;
        public StaticSpawnState(IRoamingPresence presence, int playerMinDist = 200) : base(presence, playerMinDist) { }

        public override void Enter()
        {
            Logger.DebugWarning($"ENTERED StaticSpawnState");
            base.Enter();
        }

        protected override void OnSpawned()
        {
            _presence.OnSpawned();
            _presence.StackFSM.Push(new NullRoamingState(_presence));
        }

        private bool IsInRange(Position position)
        {
            var zone = _presence.Zone;
            if (zone.Configuration.IsGamma && zone.IsUnitWithCategoryInRange(CategoryFlags.cf_pbs_docking_base, position, BASE_RADIUS))
                return true;
            else if (zone.GetStaticUnits().OfType<DockingBase>().WithinRange2D(position, BASE_RADIUS).Any())
                return true;
            else if (zone.GetStaticUnits().OfType<Teleport>().WithinRange2D(position, PLAYER_RADIUS).Any())
                return true;

            return zone.Players.WithinRange2D(position, PLAYER_RADIUS).Any();
        }

        protected override void SpawnFlocks()
        {
            Position spawnPosition;
            bool anyPlayersAround;
            int maxRetries = 100;

            do
            {
                spawnPosition = _presence.PathFinder.FindSpawnPosition(_presence).ToPosition();
                anyPlayersAround = IsInRange(spawnPosition);
                maxRetries--;
            } while (anyPlayersAround && maxRetries > 0);

            if (anyPlayersAround)
            {
                _presence.Log("FAILED to resolve spawn position out of range of players: " + spawnPosition);
                return;
            }
            Logger.DebugWarning($"GAMMA BASE PRES SPAWNING! @: {spawnPosition}");

            DoSpawning(spawnPosition);
        }
    }

    public class NullRoamingState : IState
    {
        protected readonly IRoamingPresence _presence;

        public NullRoamingState(IRoamingPresence presence)
        {
            _presence = presence;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }

        protected Npc[] GetAllMembers()
        {
            return _presence.Flocks.GetMembers().ToArray();
        }

        protected bool IsDeadAndExiting(Npc[] members)
        {
            if (members.Length <= 0)
            {
                _presence.StackFSM.Pop();
                return true;
            }
            return false;
        }

        public virtual void Update(TimeSpan time)
        {
            var members = GetAllMembers();
            IsDeadAndExiting(members);
            if(members.Length > 0)
                Logger.DebugWarning($"GAMMA BASE PRES @: {members[0].CurrentPosition} CHILLIN");
        }
    }
}
