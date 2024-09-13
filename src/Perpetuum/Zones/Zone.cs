using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Log;
using Perpetuum.Network;
using Perpetuum.Players;
using Perpetuum.Services.HighScores;
using Perpetuum.Services.Relics;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Strongholds;
using Perpetuum.Services.Weather;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Decors;
using Perpetuum.Zones.Effects.ZoneEffects;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.Turrets;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.ZoneEntityRepositories;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Perpetuum.Zones
{
    public abstract class Zone : Threading.Process.Process, IZone
    {
        private ImmutableHashSet<ZoneSession> _sessions = ImmutableHashSet<ZoneSession>.Empty;
        private ImmutableDictionary<long, Unit> _units = ImmutableDictionary<long, Unit>.Empty;
        private ImmutableDictionary<long, Player> _players = ImmutableDictionary<long, Player>.Empty;

        public int Id => Configuration.Id;
        public Size Size => Configuration.Size;

        public IDecorHandler DecorHandler { get; set; }

        public ZoneConfiguration Configuration { get; set; }

        public ITerrain Terrain { get; set; }
        public CorporationHandler CorporationHandler { get; set; }
        public IPlantHandler PlantHandler { get; set; }
        public IBeamService Beams { get; set; }
        public IWeatherService Weather { get; set; }
        public IEnvironmentHandler Environment { get; set; }
        public IPresenceManager PresenceManager { get; set; }
        public ISafeSpawnPointsRepository SafeSpawnPoints { get; set; }
        public PBSHighwayHandler HighwayHandler { get; set; }
        public TerraformHandler TerraformHandler { get; set; }
        public IZoneUnitService UnitService { get; set; }
        public MiningLogHandler MiningLogHandler { get; set; }
        public HarvestLogHandler HarvestLogHandler { get; set; }
        public ZoneSession.Factory ZoneSessionFactory { get; set; }
        public IZoneEffectHandler ZoneEffectHandler { get; set; }

        [CanBeNull]
        public IRiftManager RiftManager { private get; set; }

        [CanBeNull]
        public IRelicManager RelicManager { get; set; }

        [CanBeNull]
        public IStrongholdPlayerStateManager PlayerStateManager { get; set; }

        public IZoneEnterQueueService EnterQueueService { get; set; }

        public IHighScoreService HighScores { get; set; }

        private readonly IGangManager _gangManager;

        public TcpListener Listener { get; set; }

        private readonly SessionlessPlayerTimeout _sessionlessPlayerTimeout;
        public bool IsLayerEditLocked { get; set; }

        protected Zone(ISessionManager sessionManager, IGangManager gangManager)
        {
            IsLayerEditLocked = true;
            sessionManager.CharacterDeselected += OnCharacterDeselected;
            _gangManager = gangManager;
            _gangManager.GangMemberJoined += OnGangMemberJoined;
            _gangManager.GangMemberRemoved += OnGangMemberRemoved;
            _gangManager.GangDisbanded += OnGangDisbanded;
            _sessionlessPlayerTimeout = new SessionlessPlayerTimeout(this);
        }

        private void OnCharacterDeselected(ISession session, Character character)
        {
            EnterQueueService.RemovePlayer(character);

            ZoneSession zoneSession = GetSessionByCharacter(character);
            zoneSession?.Disconnect();
        }

        public override void Start()
        {
            Listener.Start(OnConnectionAccepted);
            RelicManager?.Start();
        }

        public override void Stop()
        {
            Listener.Stop();

            //Emit stop signal to child services
            RelicManager?.Stop();

            // players
            SaveUnitsToDb<Player>();
            SaveUnitsToDb<PBSDockingBase>();
            SaveUnitsToDb<PBSTurret>();
            SaveUnitsToDb<PBSObject>();
            SaveUnitsToDb<ProximityDeviceBase>();
        }

        private void OnGangMemberJoined(Gang gang, Character character)
        {
            if (this.TryGetPlayer(character, out Player player))
            {
                player.Gang = gang;
            }
        }

        private void OnGangMemberRemoved(Gang gang, Character character)
        {
            if (this.TryGetPlayer(character, out Player player))
            {
                player.Gang = null;
            }
        }

        private void OnGangDisbanded(Gang gang)
        {
            foreach (Player player in Players)
            {
                if (player.Gang == gang)
                {
                    player.Gang = null;
                }
            }
        }

        public void LoadUnits()
        {
            foreach (KeyValuePair<Unit, Position> kvp in UnitService.GetAll())
            {
                Unit unit = kvp.Key;
                Position position = kvp.Value;
                unit.AddToZone(this, position);
            }
        }

        private void UpdateSessions(TimeSpan time)
        {
            foreach (ZoneSession session in _sessions)
            {
                session.Update(time);
            }

            _sessionlessPlayerTimeout.Update(time);
        }

        [CanBeNull]
        public ZoneSession GetSessionByCharacter(Character character)
        {
            return character == Character.None ? null : _sessions.FirstOrDefault(s => s.Character == character);
        }

        private void OnConnectionAccepted(Socket socket)
        {
            ZoneSession session = ZoneSessionFactory(this, socket);
            session.Stopped += OnSessionStopped;
            _ = ImmutableInterlocked.Update(ref _sessions, s => s.Add(session));
            session.Start();
        }

        private void OnSessionStopped(IZoneSession session)
        {
            if (session.Id == 0)
            {
                return;
            }

            _ = ImmutableInterlocked.Update(ref _sessions, s => s.Remove((ZoneSession)session));
        }

        public void SetGang(Player player)
        {
            player.Gang = _gangManager.GetGangByMember(player.Character);
        }

        public void AddUnit(Unit unit)
        {
            if (!ImmutableInterlocked.TryAdd(ref _units, unit.Eid, unit))
            {
                return;
            }

            if (unit is Player player)
            {
                _ = ImmutableInterlocked.TryAdd(ref _players, player.Eid, player);
                StrongholdPlayerStateManager.OnPlayerAddToZone(this, player);
            }

            unit.Updated += OnUnitUpdated;
            unit.Dead += OnUnitDead;

            ZoneEffectHandler.OnEnterZone(unit);
            Logger.Info($"Unit entered to zone. zone:{Id} eid = {unit.InfoString} ({unit.CurrentPosition})");
        }

        private void OnUnitDead(Unit killer, Unit victim)
        {
            // alapesetben ez a player kapja
            if (!(killer is Player killerPlayer))
            {
                return;
            }

            ITaggable taggable = victim as ITaggable;
            // ha taggelve volt akkor az kapja
            Player tagger = taggable?.GetTagger();
            if (tagger != null)
            {
                killerPlayer = tagger;
            }

            _ = HighScores.UpdateHighScoreAsync(killerPlayer, victim);
        }

        public void RemoveUnit(Unit unit)
        {
            if (!ImmutableInterlocked.TryRemove(ref _units, unit.Eid, out Unit u))
            {
                return;
            }

            if (u is Player player)
            {
                _ = ImmutableInterlocked.TryRemove(ref _players, player.Eid, out player);
                PlayerStateManager?.OnPlayerExitZone(player);
            }

            u.Updated -= OnUnitUpdated;
            Logger.Info($"Unit exited from zone. zone:{Id} eid = {u.InfoString} ({u.CurrentPosition})");
        }

        private ImmutableHashSet<Unit> _updatedUnits = ImmutableHashSet<Unit>.Empty;

        private void ProcessUpdatedUnits()
        {
            ImmutableHashSet<Unit> updatedUnits;

            if ((updatedUnits = Interlocked.CompareExchange(ref _updatedUnits, ImmutableHashSet<Unit>.Empty, _updatedUnits)) == ImmutableHashSet<Unit>.Empty)
            {
                return;
            }

            foreach (KeyValuePair<long, Unit> kvp in _units)
            {
                Unit targetUnit = kvp.Value;
                foreach (Unit sourceUnit in updatedUnits)
                {
                    if (sourceUnit == targetUnit)
                    {
                        continue;
                    }

                    sourceUnit.UpdateVisibilityOf(targetUnit);
                    targetUnit.UpdateVisibilityOf(sourceUnit);

                    if (Configuration.Protected)
                    {
                        continue;
                    }

                    IBlobableUnit bSource = sourceUnit as IBlobableUnit;
                    bSource?.BlobHandler.UpdateBlob(targetUnit);
                }
            }
        }

        private void OnUnitUpdated(Unit unit, UnitUpdatedEventArgs e)
        {
            bool visibilityUpdated = (e.UpdateTypes & UnitUpdateTypes.Visibility) > 0;
            if (!visibilityUpdated)
            {
                return;
            }

            _ = ImmutableInterlocked.Update(ref _updatedUnits, h => h.Add(unit));
        }

        public IEnumerable<Unit> Units => _units.Values;

        public IEnumerable<Player> Players => _players.Values;

        public Unit GetUnit(long eid)
        {
            return _units.GetValueOrDefault(eid);
        }

        public Player GetPlayer(long eid)
        {
            return _players.GetValueOrDefault(eid);
        }

        private readonly IntervalTimer _updateUnitsTimer = new IntervalTimer(500);

        private Action<TimeSpan> _updateProfiler;

        [Conditional("DEBUG")]
        private void MeasureUpdate(TimeSpan time)
        {
            Action<TimeSpan> profiler = _updateProfiler ?? (_updateProfiler = Profiler.CreateUpdateProfiler(TimeSpan.FromSeconds(30), e =>
            {
                Logger.Info($"zone {Id} update average: {e.TotalMilliseconds} ms");
            }));

            profiler(time);
        }

        public override void Update(TimeSpan time)
        {
            UpdateSessions(time);

            _updateUnitsTimer.Update(time).IsPassed(ProcessUpdatedUnits);

            UpdateUnits(time);

            RiftManager?.Update(time);
            RelicManager?.Update(time);
            MiningLogHandler.Update(time);
            HarvestLogHandler.Update(time);
            MeasureUpdate(time);
        }

        private void UpdateUnits(TimeSpan time)
        {
            foreach (KeyValuePair<long, Unit> kvp in _units)
            {
                kvp.Value.Update(time);
            }
        }

        public override string ToString()
        {
            return $"(id:{Id})";
        }

        private void SaveUnitsToDb<T>() where T : Unit
        {
            List<T> units = _units.Values.OfType<T>().ToList();
            foreach (T unit in units)
            {
                Entity.Repository.ForceUpdate(unit);
            }
        }

        public ILogger<ChatLogEvent> ChatLogger { get; set; }

        public void Enter(Character character, Command replyCommand)
        {
            Logger.Info("[Zone Enter] start request. character:" + character.Id + " reply:" + replyCommand);

            try
            {

                if (this.TryGetPlayer(character, out Player player))
                {
                    Logger.Info("[Zone Enter] player on zone. player: " + player.Eid + " character:" + character.Id + " reply:" + replyCommand);
                    EnterQueueService.SendReplyCommand(character, player, replyCommand);
                    return;
                }

                if (replyCommand == Commands.TeleportUse)
                {
                    EnterQueueService.LoadPlayerAndSendReply(character, replyCommand);
                    return;
                }

                EnterQueueService.EnqueuePlayer(character, replyCommand);
            }
            finally
            {
                Logger.Info("[Zone Enter] end request. character:" + character.Id + " reply:" + replyCommand);
            }
        }
    }
}