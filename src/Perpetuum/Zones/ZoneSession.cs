using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Common.Loggers;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.IDGenerators;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Network;
using Perpetuum.Players;
using Perpetuum.Reactive;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Weather;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Terraforming;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Transactions;

namespace Perpetuum.Zones
{
    public class ZoneSession : IZoneSession
    {
        public static readonly IZoneSession None = new NullZoneSession();
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();

        private readonly IZone _zone;
        private readonly ISessionManager _sessionManager;
        private readonly EncryptedTcpConnection _connection;

        public Character Character { get; private set; } = Character.None;
        private Player _player;
        public DateTime DisconnectTime { get; private set; }
        private DateTime _lastReceivedPacketTime;

        public int Id { get; set; }

        private TerrainUpdateNotifier _terrainUpdateNotifier;

        public delegate ZoneSession Factory(IZone zone, Socket socket);

        public ZoneSession(IZone zone, Socket socket, ISessionManager sessionManager)
        {
            Id = _idGenerator.GetNextID();
            _zone = zone;
            _connection = new EncryptedTcpConnection(socket);
            _connection.Received += OnReceived;
            _connection.Disconnected += OnDisconnected;
            _sessionManager = sessionManager;
        }

        public void Start()
        {
            _connection.Receive();
        }

        public void Stop()
        {
            OnStopped();
        }

        public event Action<ZoneSession> Stopped;

        private void OnStopped()
        {
            if (_beamsMonitor != null)
            {
                _beamsMonitor.Dispose();
                _beamsMonitor = null;
            }

            if (_weatherMonitor != null)
            {
                _weatherMonitor.Dispose();
                _weatherMonitor = null;
            }

            Stopped?.Invoke(this);
        }

        public void SendPacket(Packet packet)
        {
            _connection.Send(packet.ToArray());
        }

        public void SendPacket(IBuilder<Packet> packetBuilder)
        {
            if (packetBuilder == null)
            {
                return;
            }

            Packet packet = packetBuilder.Build();
            SendPacket(packet);
        }

        public AccessLevel AccessLevel { get; private set; }

        private void OnDisconnected(ITcpConnection connection)
        {
            Player player = _player;
            if (player == null)
            {
                OnStopped();
            }
            else
            {
                DisconnectTime = DateTime.Now; //for the logs
                LogoutRequest(false);
            }

            WriteFQLog($"Player disconnected. characterId:{(Character != Character.None ? Character.Id : 0)}");
        }

        private void OnReceived(ITcpConnection connection, byte[] packetData)
        {
            _lastReceivedPacketTime = DateTime.Now;
            TimeSpan executeTime = GlobalTimer.Elapsed;
            bool cancelLogout = true;

            Packet packet = new Packet(packetData);

            try
            {
                switch (packet.Command)
                {
                    case ZoneCommand.AuthUnit:
                        {
                            HandleAuth(packet);
                            break;
                        }
                    case ZoneCommand.ClientUpdate:
                        {
                            HandleClientUpdate(packet);

                            break;
                        }
                    case ZoneCommand.MoveForward:
                        {
                            HandleMoveForward(packet);

                            break;
                        }
                    case ZoneCommand.Ping: { HandlePing(packet); break; }
                    case ZoneCommand.ClosingSocket:
                        {
                            cancelLogout = false;
                            HandleClosingSocket(packet);
                            break;
                        }
                    case ZoneCommand.ControlCommand: { HandleControlCommand(packet); break; }
                    case ZoneCommand.DeployItem: { HandleDeployItem(packet); break; }
                    case ZoneCommand.EnablePVP: { HandleEnablePvp(packet); break; }
                    case ZoneCommand.GangDoodle: { HandleGangDoodle(packet); break; }
                    case ZoneCommand.GetLayer: { HandleGetLayer(packet); break; }
                    case ZoneCommand.SetLayer: { HandleSetLayer(packet); break; }
                    case ZoneCommand.GetLootList: { HandleGetLootList(packet); break; }
                    case ZoneCommand.TakeLoot: { HandleTakeLoot(packet); break; }
                    case ZoneCommand.PutLoot: { HandlePutLoot(packet); break; }
                    case ZoneCommand.ReleaseLoot: { HandleReleaseLoot(packet); break; }
                    case ZoneCommand.LoadAmmo: { HandleLoadAmmo(packet); break; }
                    case ZoneCommand.UnloadAmmo: { HandleUnloadAmmo(packet); break; }
                    case ZoneCommand.LocalChat: { HandleLocalChat(packet); break; }
                    case ZoneCommand.LockTerrain: { HandleLockTerrain(packet); break; }
                    case ZoneCommand.LockUnit: { HandleLockUnit(packet); break; }
                    case ZoneCommand.SetPrimaryLock: { HandleSetPrimaryLock(packet); break; }
                    case ZoneCommand.RemoveLock: { HandleRemoveLock(packet); break; }
                    case ZoneCommand.GetTerrainLockParameters: { HandleGetTerrainLockParameters(packet); break; }
                    case ZoneCommand.SetTerrainLockParameters: { HandleSetTerrainLockParameters(packet); break; }


                    case ZoneCommand.Logout:
                        {
                            cancelLogout = false;
                            HandleLogout(packet);
                            break;
                        }
                    case ZoneCommand.GetModuleInfo: { HandleGetModuleInfo(packet); break; }
                    case ZoneCommand.ModuleUse: { HandleModuleUse(packet); break; }
                    case ZoneCommand.ModuleUseByCategoryFlag: { HandleModuleUseByCategoryFlags(packet); break; }
                    case ZoneCommand.UseItem: { HandleUseItem(packet); break; }
                    case ZoneCommand.GetMyRobotInfo: { HandleGetMyRobotInfo(packet); break; }

                }
            }
            catch (Exception ex)
            {
                if (ex is PerpetuumException gex)
                {
                    packet.Error = gex.error;
                    LogGenxyException(packet, gex);
                }
                else
                {
                    packet.Error = ErrorCodes.ServerError;
                }
            }

            TimeSpan workTime = GlobalTimer.Elapsed - executeTime;
            packet.WorkTime = (int)workTime.TotalMilliseconds;
            SendPacket(packet);

            if (cancelLogout)
            {
                CancelLogout(true);
            }
        }

        private void WriteFQLog(string message)
        {
            string info = string.Empty;

            Player player = _player;
            if (player != null)
            {
                info = player.InfoString;
            }

            LogEvent e = new LogEvent
            {
                LogType = LogType.Info,
                Tag = "FQ",
                Message = $"{info} - {message}"
            };

            Logger.Log(e);
        }

        private void WritePacketLog(Packet packet, string message = null)
        {
            Player player = _player;
            player?.WriteFQLog($"({packet.Command}) {message}");
        }

        [Conditional("DEBUG")]
        private void LogGenxyException(Packet packet, PerpetuumException gex)
        {
            string playerInfo = _player?.InfoString;
            LogEvent e = new LogEvent
            {
                LogType = LogType.Error,
                Tag = "ZPACKET",
                Message = $"command:{packet.Command} zone:{_zone.Id} player:{playerInfo} ex:{gex}"
            };

            Logger.Log(e);
        }

        private BeamsMonitor _beamsMonitor;
        private WeatherMonitor _weatherMonitor;

        private void HandleAuth(Packet packet)
        {
            packet.ReadInt(); // mar nem kell
            int count = (int)(packet.Length - packet.Position) - sizeof(long);
            byte[] encrypted = packet.ReadBytes(count);

            Character character = ZoneTicket.GetCharacterFromEncryptedTicket(encrypted);
            character.ThrowIfEqual(null, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            ISession characterSession = _sessionManager.GetByCharacter(character);

            if (characterSession == null || !characterSession.IsAuthenticated ||
                !characterSession.RemoteEndPoint.Address.Equals(_connection.RemoteEndPoint.Address))
            {
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            }

            Logger.Info($"Socket authentication successful. zone: {_zone.Id} character: {character.Id}");
            Character = character;
            AccessLevel = character.AccessLevel;

            if (!_zone.TryGetPlayer(character, out Player player))
            {
                // nincs kint a terepen ezert betoltjuk
                player = Player.LoadPlayerAndAddToZone(_zone, character);
            }

            ZoneSession session = player.Session as ZoneSession;
            session?.OnStopped();

            _beamsMonitor = new BeamsMonitor(this);
            _beamsMonitor.Subscribe(_zone.Beams);

            _weatherMonitor = new WeatherMonitor(OnWeatherUpdated);
            _weatherMonitor.Subscribe(_zone.Weather);

            _terrainUpdateNotifier = CreateTerrainNotifier(player);

            player.SetSession(this);
            player.SendInitSelf();
            player.ApplyTeleportSicknessEffect();
            player.ApplyInvulnerableEffect();

            _player = player;
        }

        private void OnWeatherUpdated(WeatherInfo weather)
        {
            SendPacket(weather.CreateUpdatePacket());
        }

        private TerrainUpdateNotifier CreateTerrainNotifier(Player player)
        {
            LayerType[] layerTypes = _zone.Configuration.Terraformable ? new[] { LayerType.Altitude, LayerType.Blocks, LayerType.Control, LayerType.Plants } :
                                                                 new[] { LayerType.Blocks, LayerType.Plants, LayerType.Control };

            return new TerrainUpdateNotifier(_zone, player, layerTypes);
        }

        private void HandleClientUpdate(Packet packet)
        {
            Player player = _player;
            if (player == null)
            {
                return;
            }

            player.States.InMoveable.ThrowIfTrue(ErrorCodes.InvalidMovement);
            Position position = packet.ReadPosition();
            float speed = (float)packet.ReadByte() / 255;
            float direction = (float)packet.ReadByte() / 255;

            if (!player.TryMove(position))
            {
                throw new PerpetuumException(ErrorCodes.InvalidMovement);
            }

            player.CurrentSpeed = speed;
            player.Direction = direction;
        }

        private void HandleMoveForward(Packet packet)
        {
            double direction = (double)packet.ReadUShort() / ushort.MaxValue;
            double speed = (double)packet.ReadUShort() / ushort.MaxValue;

            _player.Direction = direction;
            _player.CurrentSpeed = speed;
        }

        private static void HandlePing(Packet packet)
        {
            packet.PeekLong(5);
            packet.PutLong(13, (long)GlobalTimer.Elapsed.TotalMilliseconds);
        }

        private void HandleClosingSocket(Packet packet)
        {
            WritePacketLog(packet);
            Disconnect();
        }

        private void HandleControlCommand(Packet packet)
        {
        }

        private void HandleLockUnit(Packet packet)
        {
            long targetEid = packet.ReadLong();
            bool isPrimary = packet.ReadByte() != 0;

            WritePacketLog(packet, $"target = {targetEid} primary = {isPrimary}");
            _player.AddLock(targetEid, isPrimary);
        }

        private void HandleLockTerrain(Packet packet)
        {
            int x = packet.ReadInt();
            int y = packet.ReadInt();
            packet.ReadInt(); // z
            double z = _zone.GetZ(x, y);
            Position location = new Position(x + 0.5, y + 0.5, z);

            bool isPrimary = packet.ReadByte() != 0;

            WritePacketLog(packet, $"target = {location} primary = {isPrimary}");
            TerrainLock terrainLock = new TerrainLock(_player, location) { Primary = isPrimary };

            _player.AddLock(terrainLock);
        }

        private void HandleGetTerrainLockParameters(Packet packet)
        {
            long id = packet.ReadLong();

            TerrainLock terrainLock = _player.GetLock(id).ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLock);
            TerrainLockParametersPacketBuilder builder = new TerrainLockParametersPacketBuilder(terrainLock);
            _player.Session.SendPacket(builder);
        }

        private void HandleSetTerrainLockParameters(Packet packet)
        {
            long id = packet.ReadLong();
            TerraformType terraformType = (TerraformType)packet.ReadByte();
            TerraformDirection terraformDirection = (TerraformDirection)packet.ReadByte();
            int radius = packet.ReadByte();
            int falloff = packet.ReadByte();

            TerrainLock terrainLock = _player.GetLock(id).ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLock);

            terrainLock.TerraformType = terraformType;
            terrainLock.TerraformDirection = terraformDirection;
            terrainLock.Radius = radius;
            terrainLock.Falloff = falloff;

            TerrainLockParametersPacketBuilder builder = new TerrainLockParametersPacketBuilder(terrainLock);
            _player.Session.SendPacket(builder);
        }

        private void HandleDeployItem(Packet packet)
        {
            long itemEid = packet.ReadLong();
            int argsCount = packet.ReadInt();
            BinaryStream binaryStream = new BinaryStream(packet.ReadBytes(argsCount));

            _player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInTeleportSickness);

            using (TransactionScope scope = Db.CreateTransaction())
            {
                RobotInventory container = _player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                Item item = container.GetItemOrThrow(itemEid);

                ItemDeployerBase itemDeployer = item.ThrowIfNotType<ItemDeployerBase>(ErrorCodes.DefinitionNotSupported);
                if (itemDeployer is FieldContainerCapsule capsule)
                {
                    capsule.PinCode = binaryStream.ReadInt();
                }

                itemDeployer.Deploy(_zone, _player);

                if (item.ED.AttributeFlags.Consumable)
                {
                    Item tmpItem = container.RemoveItem(item, 1).ThrowIfNull(ErrorCodes.ItemNotFound);
                    Entity.Repository.Delete(tmpItem);
                    container.Save();
                }

                Transaction.Current.OnCommited(() => container.SendUpdateToOwner());
                scope.Complete();
            }
        }

        private void HandleEnablePvp(Packet packet)
        {
            _zone.Configuration.Type.ThrowIfEqual(ZoneType.Training, ErrorCodes.NoPvpInTraining);
            Logger.Info($"Pvp enabled. zone:{_zone.Id} player:{_player.InfoString}");

            WritePacketLog(packet);
            _player.ApplyPvPEffect();
        }

        private void HandleGangDoodle(Packet packet)
        {
            Groups.Gangs.Gang gang = _player.Gang;
            if (gang == null)
            {
                return;
            }

            packet.ReadLong();
            byte[] doodleData = packet.ReadBytes(8);

            using (Packet doodlePacket = new Packet(ZoneCommand.GangDoodle))
            {
                doodlePacket.AppendLong(_player.Eid);
                doodlePacket.AppendByteArray(doodleData);
                _zone.SendPacketToGang(gang, doodlePacket, _player.Eid);
            }
        }

        private void HandleGetLayer(Packet packet)
        {

            _player.Session.AccessLevel.IsAdminOrGm().ThrowIfFalse(ErrorCodes.AccessDenied);

            LayerType layerType = (LayerType)packet.ReadByte();
            MaterialType materialType = (MaterialType)packet.ReadByte();
            int x1 = packet.ReadInt();
            int y1 = packet.ReadInt();
            int x2 = packet.ReadInt();
            int y2 = packet.ReadInt();
            Area area = new Area(x1, y1, x2, y2);

            WritePacketLog(packet, $"type = {layerType} mtype = {materialType} area = {area}");

            Packet p = _zone.Terrain.BuildLayerUpdatePacket(layerType, area);
            if (p != null)
            {
                _player.Session.SendPacket(p);
            }
        }

        private void HandleGetLootList(Packet packet)
        {
            int pinCode = packet.ReadInt();
            long containerEid = packet.ReadLong();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            LootContainer container = _zone.GetUnit(containerEid) as LootContainer;
            container?.SendLootListToPlayer(_player, pinCode);
        }

        private void HandleGetModuleInfo(Packet packet)
        {
            RobotComponentType robotComponentType = (RobotComponentType)packet.ReadByte();
            int slot = packet.ReadByte();

            WritePacketLog(packet, $"rc = {robotComponentType} s = {slot}");

            RobotComponent robotComponent = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            Module module = robotComponent.GetModule(slot);

            using (Packet infoPacket = module.BuildModuleInfoPacket())
            {
                _player.Session.SendPacket(infoPacket);
            }
        }

        private void HandleLoadAmmo(Packet packet)
        {
            int ammoDefinition = packet.ReadInt();
            RobotComponentType robotComponentType = (RobotComponentType)packet.ReadByte();
            int slot = packet.ReadByte();

            WritePacketLog(packet, $"d = {ammoDefinition} rc = {robotComponentType} s = {slot}");

            RobotComponent component = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            ActiveModule module = component.GetModule(slot).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);

            if (!module.IsAmmoable)
            {
                return;
            }

            Ammo ammo = module.GetAmmo();

            if (ammoDefinition == 0)
            {
                if (ammo != null)
                {
                    module.State.UnloadAmmo();
                }
            }
            else
            {
                if (ammo?.Definition == ammoDefinition && ammo.Quantity == module.AmmoCapacity)
                {
                    return;
                }

                module.CheckLoadableAmmo(ammoDefinition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);

                if (module.ParentRobot is Player player)
                {
                    Ammo tmpAmmo = (Ammo)Entity.Factory.CreateWithRandomEID(ammoDefinition);
                    tmpAmmo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
                }

                module.State.LoadAmmo(ammoDefinition);
            }
        }

        private const int MAX_MESSAGE_LENGTH = 200;

        private void HandleLocalChat(Packet packet)
        {
            _player.Character.GlobalMuted.ThrowIfTrue(ErrorCodes.CharacterIsMuted);

            packet.Skip(4);

            string message = packet.ReadUtf8String();

            WriteLocalChatLog(_player, message);

            if (message.Length > MAX_MESSAGE_LENGTH)
            {
                message = message.Substring(0, MAX_MESSAGE_LENGTH);
            }

            using (Packet chatPacket = new Packet(ZoneCommand.LocalChat))
            {
                chatPacket.AppendInt(_player.Character.Id);
                chatPacket.AppendUtf8String(message);

                _player.SendPacketToWitnessPlayers(chatPacket, true);
            }
        }

        private void WriteLocalChatLog(Player sender, string message)
        {
            Collections.Spatial.CellCoord cell = sender.CurrentPosition.ToCellCoord();
            _zone.ChatLogger.LogMessage(sender.Character, $"[{cell}] {message}");
        }

        private void HandleLogout(Packet packet)
        {
            WritePacketLog(packet);

            _player.States.Combat.ThrowIfTrue(ErrorCodes.RobotInCombat);
            _player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);

            LogoutRequest(true);
        }

        private void HandleModuleUse(Packet packet)
        {
            long lockId = packet.ReadLong();
            RobotComponentType robotComponentType = (RobotComponentType)packet.ReadByte();
            int slot = packet.ReadByte();
            ModuleStateType moduleState = (ModuleStateType)packet.ReadByte();

            WritePacketLog(packet, $"lockId = {lockId} rc = {robotComponentType} s = {slot} state = {moduleState}");

            RobotComponent component = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            ActiveModule module = component.GetModule(slot).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);

            if (module.IsAmmoable)
            {
                Ammo ammo = module.GetAmmo();
                if (ammo == null || ammo.Definition == 0)
                {
                    _player.SendModuleProcessError(module, ErrorCodes.AmmoNotFound);
                    return;
                }
            }

            module.Lock = _player.GetLock(lockId);
            module.State.SwitchTo(moduleState);
        }

        private void HandleModuleUseByCategoryFlags(Packet packet)
        {
            long lockId = packet.ReadLong();
            CategoryFlags cf = (CategoryFlags)packet.ReadLong();
            ModuleStateType moduleState = (ModuleStateType)packet.ReadByte();

            WritePacketLog(packet, $"lockId = {lockId} cf = {cf} state = {moduleState}");

            foreach (ActiveModule module in _player.ActiveModules)
            {
                if (!module.IsCategory(cf))
                {
                    continue;
                }

                if (module.IsAmmoable)
                {
                    Ammo ammo = module.GetAmmo();
                    if (ammo == null || ammo.Quantity == 0)
                    {
                        continue;
                    }
                }

                Lock lockTarget = module.ED.AttributeFlags.PrimaryLockedTarget ? _player.GetPrimaryLock().ThrowIfNull(ErrorCodes.PrimaryLockTargetNotFound) :
                    _player.GetLock(lockId).ThrowIfNull(ErrorCodes.LockTargetNotFound);

                module.Lock = lockTarget;

                try
                {
                    module.State.SwitchTo(moduleState);
                }
                catch (PerpetuumException gex)
                {
                    _player.SendModuleProcessError(module, gex.error);
                }
            }
        }

        private void HandlePutLoot(Packet packet)
        {
            int pinCode = packet.ReadInt();
            long containerEid = packet.ReadLong();
            int count = packet.ReadInt();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid} count = {count}");

            List<KeyValuePair<long, int>> items = new List<KeyValuePair<long, int>>();

            for (int i = 0; i < count; i++)
            {
                long itemEid = packet.ReadLong();
                int qty = packet.ReadInt();
                items.Add(new KeyValuePair<long, int>(itemEid, qty));
            }

            FieldContainer container = _zone.GetUnit(containerEid) as FieldContainer;
            container?.PutLoots(_player, pinCode, items);
        }

        private void HandleReleaseLoot(Packet packet)
        {
            int pinCode = packet.ReadInt();
            long containerEid = packet.ReadLong();
            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            LootContainer container = _zone.GetUnit(containerEid) as LootContainer;
            container?.ReleaseLootContainer(_player);
        }

        private void HandleRemoveLock(Packet packet)
        {
            long lockId = packet.ReadLong();
            WritePacketLog(packet, $"lockId = {lockId}");
            _player.CancelLock(lockId);
        }

        private void HandleSetLayer(Packet packet)
        {
            _player.Session.AccessLevel.IsAdminOrGm().ThrowIfFalse(ErrorCodes.AccessDenied);
            _zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);

            using (new TerrainUpdateMonitor(_zone))
            {
                _zone.Terrain.UpdateAreaFromPacket(packet);
            }
        }

        private void HandleSetPrimaryLock(Packet packet)
        {
            long lockId = packet.ReadLong();
            WritePacketLog(packet, $"lockId = {lockId}");
            _player.SetPrimaryLock(lockId);
        }

        private void HandleTakeLoot(Packet packet)
        {
            int pinCode = packet.ReadInt();
            long containerEid = packet.ReadLong();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            List<KeyValuePair<Guid, int>> items = new List<KeyValuePair<Guid, int>>();

            while (!packet.AtEnd())
            {
                Guid lootId = packet.ReadGuid();
                int count = packet.ReadInt();
                items.Add(new KeyValuePair<Guid, int>(lootId, count));
            }

            LootContainer container = _zone.GetUnit(containerEid) as LootContainer;
            container?.TakeLoots(_player, pinCode, items);
        }

        private void HandleUnloadAmmo(Packet packet)
        {
            RobotComponentType robotComponent = (RobotComponentType)packet.ReadByte();
            int slot = packet.ReadByte();

            WritePacketLog(packet, $"rc = {robotComponent} s = {slot}");

            RobotComponent component = _player.GetRobotComponent(robotComponent);
            if (!(component?.GetModule(slot) is ActiveModule module))
            {
                return;
            }

            using (TransactionScope scope = Db.CreateTransaction())
            {
                RobotInventory container = _player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();
                module.UnequipAmmoToContainer(container);

                module.Save();
                container.Save();

                Transaction.Current.OnCompleted(c =>
                {
                    container.SendUpdateToOwner();
                });

                scope.Complete();
            }
        }

        private void HandleUseItem(Packet packet)
        {
            long itemEid = packet.ReadLong();

            IUsableItem usableItem = _zone.GetUnit(itemEid) as IUsableItem;
            usableItem?.UseItem(_player);
        }

        private void HandleGetMyRobotInfo(Packet packet)
        {
            RobotInfoPacketBuilder builder = new RobotInfoPacketBuilder(_player);
            _player.Session.SendPacket(builder);
        }

        [CanBeNull]
        private IntervalTimer _logoutTimer;
        private bool _safeLogout;
        private readonly object _logoutSync = new object();

        private static readonly TimeSpan _pveLogoutTime = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _pvpLogoutTime = TimeSpan.FromMinutes(2);

        private void LogoutRequest(bool safeLogout)
        {
            Player player = _player;
            if (player == null)
            {
                return;
            }

            lock (_logoutSync)
            {
                if (_logoutTimer != null)
                {
                    return;
                }

                _safeLogout = safeLogout;

                if (player.HasPvpEffect)
                {
                    player.StopAllModules();
                }

                TimeSpan logoutTime = player.IsInSafeArea ? _pveLogoutTime : _pvpLogoutTime;

                Effects.Effect pvpEffect = player.EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();
                IntervalTimer effectTimer = pvpEffect?.Timer;
                if (effectTimer != null)
                {
                    logoutTime = logoutTime.Max(effectTimer.Remaining);
                }

                _logoutTimer = new IntervalTimer(logoutTime);

                // mennyi ido mulva
                SendStartLogoutPacket(_logoutTimer);
            }
        }

        private void SendStartLogoutPacket([NotNull] IntervalTimer logoutTimer)
        {
            Packet packet = new Packet(ZoneCommand.StartLogout);
            packet.AppendInt((int)logoutTimer.Interval.TotalMilliseconds);
            SendPacket(packet);
        }

        private void SendCancelLogoutPacket()
        {
            Packet packet = new Packet(ZoneCommand.CancelLogout);
            SendPacket(packet);
        }

        public void CancelLogout()
        {
            CancelLogout(false);
        }

        private void CancelLogout(bool force, bool sendPacket = true)
        {
            if (_logoutTimer == null || (!force && !_safeLogout))
            {
                return;
            }

            _safeLogout = false;
            _logoutTimer = null;

            if (!sendPacket)
            {
                return;
            }
            // itt is kuldunk packetet,h megszakadt
            SendCancelLogoutPacket();
        }

        public void ResetLogoutTimer()
        {
            if (_logoutTimer == null)
            {
                return;
            }

            _logoutTimer.Reset();
            SendStartLogoutPacket(_logoutTimer);
        }

        public void SendTerrainData()
        {
            _terrainUpdateNotifier.ForceUpdateGrids();
        }

        public void SendBeam(IBuilder<Beam> builder)
        {
            SendBeam(builder.Build());
        }

        public void SendBeam(Beam beam)
        {
            if (beam.Type == BeamType.undefined)
            {
                return;
            }

            SendPacket(new BeamPacketBuilder(beam));
        }

        public void EnqueueLayerUpdates(IReadOnlyCollection<TerrainUpdateInfo> infos)
        {
            _terrainUpdateNotifier.EnqueueNewUpdates(infos);
        }

        private bool _isInLogout;

        private void UpdateLogout(TimeSpan time)
        {
            if (_logoutTimer == null)
            {
                return;
            }

            _logoutTimer.Update(time);

            if (!_logoutTimer.Passed)
            {
                return;
            }

            _logoutTimer = null;

            if (_isInLogout)
            {
                return;
            }

            _isInLogout = true;

            Task.Run(() => LogoutPlayer());
        }

        private void LogoutPlayer()
        {
            Character character = Character;

            using (TransactionScope scope = Db.CreateTransaction())
            {
                _player.Save();
                character.ZoneId = _zone.Id;
                character.ZonePosition = _player.CurrentPosition;

                _player.RemoveFromZone();
                _player.SetSession(None);

                _sessionManager.DeselectCharacter(character);
                scope.Complete();
            }

            Disconnect();
            OnStopped();
        }

        public void Disconnect()
        {
            LogoutRequest(false);
            _connection?.Disconnect();
        }

        public TimeSpan InactiveTime => DateTime.Now.Subtract(_lastReceivedPacketTime);

        public void Update(TimeSpan time)
        {
            _terrainUpdateNotifier?.Update();
            _beamsMonitor?.Update();

            UpdateLogout(time);
        }

        private class BeamsMonitor : Observer<Beam>
        {
            private readonly ZoneSession _session;
            private readonly ConcurrentQueue<Beam> _beams = new ConcurrentQueue<Beam>();

            public BeamsMonitor(ZoneSession session)
            {
                _session = session;
            }

            public override void OnNext(Beam beam)
            {
                _beams.Enqueue(beam);
            }

            public void Update()
            {
                Player player = _session._player;
                if (player == null)
                {
                    return;
                }

                while (_beams.TryDequeue(out Beam beam))
                {
                    _session.SendBeamIfVisible(beam);
                }
            }

            protected override void OnDispose()
            {
                _beams.Clear();
                base.OnDispose();
            }
        }

        public void SendBeamIfVisible(Beam beam)
        {
            Player player = _player;
            if (player == null)
            {
                return;
            }

            if (player.IsInRangeOf3D(beam.SourcePosition, beam.Visibility) || player.IsInRangeOf3D(beam.TargetPosition, beam.Visibility))
            {
                SendBeam(beam);
            }
        }
    }
}