using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;


namespace Perpetuum.Services.Channels.ChatCommands
{
    public class AdminCommandRouter
    {
        private readonly GlobalConfiguration _config;
        private readonly ISessionManager _sessionManager;
        public AdminCommandRouter(GlobalConfiguration configuration, ISessionManager sessionManager)
        {
            _config = configuration;
            _sessionManager = sessionManager;
        }

        public void TryParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (IsAdminCommand(sender, text))
            {
                ParseAdminCommand(sender, text, request, channel, channelManager);
            }
        }

        private bool IsAdminCommand(Character sender, string message)
        {
            return message.StartsWith("#") && sender.AccessLevel == AccessLevel.admin;
        }

        private void ParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (!IsAdminCommand(sender, text))
                return;

            string[] command = text.Split(new char[] { ',' });

            var data = AdminCommandData.Create(sender, command, request, channel, channelManager, _sessionManager, _config.EnableDev);

            // channel is not secured. must be secured first.
            if (channel.Type != ChannelType.Admin)
            {
                if (command[0] == "#secure")
                {
                    AdminCommandHandlers.Secure(data);
                    return;
                }
                channel.SendMessageToAll(_sessionManager, sender, "Channel must be secured before sending commands.");
                return;
            }

            ServerCommands(data);
        }

        private void ServerCommands(AdminCommandData data)
        {
            switch (data.Command.Name)
            {
                case "#unsecure": { AdminCommandHandlers.UnSecure(data); break; }
                case "#shutdown": { AdminCommandHandlers.Shutdown(data); break; }
                case "#shutdowncancel": { AdminCommandHandlers.ShutdownCancel(data); break; }
                case "#jumpto": { AdminCommandHandlers.JumpTo(data); break; }
                case "#moveplayer": { AdminCommandHandlers.MovePlayer(data); break; }
                case "#giveitem": { AdminCommandHandlers.GiveItem(data); break; }
                case "#getlockedtileproperties": { AdminCommandHandlers.GetLockedTileProperties(data); break; }
                case "#setvisibility": { AdminCommandHandlers.SetVisibility(data); break; }
                case "#zonedrawstatmap": { AdminCommandHandlers.ZoneDrawStatMap(data); break; }
                case "#listplayersinzone": { AdminCommandHandlers.ListAllPlayersInZone(data); break; }
                case "#countofplayers": { AdminCommandHandlers.CountOfPlayers(data); break; }
                case "#addtochannel": { AdminCommandHandlers.AddToChannel(data); break; }
                case "#removefromchannel": { AdminCommandHandlers.RemoveFromChannel(data); break; }
                case "#listrifts": { AdminCommandHandlers.ListRifts(data); break; }
                case "#flagplayernameoffensive": { AdminCommandHandlers.FlagPlayerNameOffensive(data); break; }
                case "#renamecorp": { AdminCommandHandlers.RenameCorp(data); break; }
                case "#unlockallep": { AdminCommandHandlers.UnlockAllEP(data); break; }
                case "#epbonusset": { AdminCommandHandlers.EPBonusSet(data); break; }
                case "#listrelics": { AdminCommandHandlers.ListRelics(data); break; }
                #region devcmds
                case "#currentzonecleanobstacleblocking": { AdminCommandHandlers.ZoneCleanObstacleBlocking(data); break; }
                case "#currentzonedrawblockingbyeid": { AdminCommandHandlers.ZoneDrawBlockingByEid(data); break; }
                case "#currentzoneremoveobjectbyeid": { AdminCommandHandlers.ZoneRemoveObjectByEid(data); break; }
                case "#zonecreateisland": { AdminCommandHandlers.ZoneCreateIsland(data); break; }
                case "#currentzoneplacewall": { AdminCommandHandlers.ZonePlaceWall(data); break; }
                case "#currentzoneclearwalls": { AdminCommandHandlers.ZoneClearWalls(data); break; }
                case "#currentzoneadddecor": { AdminCommandHandlers.ZoneAddDecor(data); break; }
                case "#adddecortolockedtile": { AdminCommandHandlers.ZoneAddDecorToLockedTile(data); break; }
                case "#zonedeletedecor": { AdminCommandHandlers.ZoneDeleteDecor(data); break; }
                case "#zoneclearlayer": { AdminCommandHandlers.ZoneClearLayer(data); break; }
                case "#zonesetplantspeed": { AdminCommandHandlers.ZoneSetPlantSpeed(data); break; }
                case "#zonesetplantmode": { AdminCommandHandlers.ZoneSetPlantMode(data); break; }
                case "#currentzonerestoreoriginalgamma": { AdminCommandHandlers.ZoneRestoreOriginalGamma(data); break; }
                case "#zonedrawblockingbydefinition": { AdminCommandHandlers.ZoneDrawBlockingByDefinition(data); break; }
                case "#addblockingtotiles": { AdminCommandHandlers.ZoneAddBlockingToLockedTiles(data); break; }
                case "#removeblockingfromtiles": { AdminCommandHandlers.ZoneRemoveBlockingToLockedTiles(data); break; }
                case "#zonedecorlock": { AdminCommandHandlers.ZoneLockDecor(data); break; }
                case "#zonetileshighway": { AdminCommandHandlers.ZoneSetTilesHighway(data); break; }
                case "#zonetilesconcretea": { AdminCommandHandlers.ZoneSetTilesConcreteA(data); break; }
                case "#zonetilesconcreteb": { AdminCommandHandlers.ZoneSetTilesConcreteB(data); break; }
                case "#zonetilesroaming": { AdminCommandHandlers.ZoneSetTilesNPCRoaming(data); break; }
                case "#zonetilesPBSTerraformProtected": { AdminCommandHandlers.ZoneSetTilesTerraformProtectCombo(data); break; }
                case "#savelayers": { AdminCommandHandlers.SaveLayers(data); break; }
                case "#zoneislandblock": { AdminCommandHandlers.ZoneIslandBlock(data); break; }
                case "#zonecreategarden": { AdminCommandHandlers.ZoneCreateGarden(data); break; }
                case "#testmissions": { AdminCommandHandlers.TestMissions(data); break; }
                case "#spawnrelic": { AdminCommandHandlers.SpawnRelic(data); break; }
                #endregion
                default: { AdminCommandHandlers.Unknown(data); break; }
            }
        }
    }
}
