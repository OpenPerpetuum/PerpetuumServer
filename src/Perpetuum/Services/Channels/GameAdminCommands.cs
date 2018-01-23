using Perpetuum.Accounting.Characters;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Services.Channels
{
    public class GameAdminCommands
    {
        public GameAdminCommands()
        {

        }

        // obviously everything coming in from the in-game chat is a string.
        // we have to take that string and chop it up, work out what command is being executed
        // then parse/cast/convert arguments as necessary.
        public void ParseAdminCommand(Character sender, string text, IRequest request, Channel channel, ISessionManager sessionManager)
        {
            string[] command = text.Split(new char[] { ',' });

            if (command[0] == "#shutdown")
            {
                DateTime shutdownin = DateTime.Now;
                int minutes = 1;
                if (!int.TryParse(command[2], out minutes))
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                shutdownin = shutdownin.AddMinutes(minutes);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "message", command[1] },
                    { "date", shutdownin }
                };

                string cmd = string.Format("serverShutDown:relay:{0}", GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#shutdowncancel")
            {
                string cmd = string.Format("serverShutDownCancel:relay:null");
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#jumpto")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int zone);
                err = !int.TryParse(command[2], out int x);
                err = !int.TryParse(command[3], out int y);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "zoneID" , zone },
                    { "x" , x },
                    { "y" , y }
                };

                string cmd = string.Format("jumpAnywhere:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#moveplayer")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int characterID);
                err = !int.TryParse(command[2], out int x);
                err = !int.TryParse(command[3], out int y);
                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "characterID" , characterID },
                    { "x" , x },
                    { "y" , y }
                };

                string cmd = string.Format("zoneMoveUnit:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzonecleanobstacleblocking")
            {
                string cmd = string.Format("zoneCleanObstacleBlocking:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzonedrawblockingbyeid")
            {
                bool err = false;
                err = !Int64.TryParse(command[1], out Int64 eid);

                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "eid", eid }
                };
                
                string cmd = string.Format("zoneDrawBlockingByEid:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneremoveobjectbyeid")
            {
                bool err = false;
                err = !Int64.TryParse(command[1], out Int64 eid);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "target", eid }
                };

                string cmd = string.Format("zoneRemoveObject:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonecreateisland")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int lvl);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "low", lvl }
                };

                string cmd = string.Format("zoneCreateIsland:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneplacewall")
            {
                string cmd = string.Format("zonePlaceWall:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneclearwalls")
            {
                string cmd = string.Format("zoneClearWalls:zone_{0}:null", sender.ZoneId);
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#currentzoneadddecor")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int definition);
                err = !int.TryParse(command[2], out int x);
                err = !int.TryParse(command[3], out int y);
                err = !int.TryParse(command[4], out int z);
                err = !double.TryParse(command[5], out double qx);
                err = !double.TryParse(command[6], out double qy);
                err = !double.TryParse(command[7], out double qz);
                err = !double.TryParse(command[8], out double qw);
                err = !double.TryParse(command[9], out double scale);
                err = !int.TryParse(command[10], out int cat);

                if (err)
                {
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", x*255 },
                    { "y", y*255 },
                    { "z", z*255 },
                    { "quaternionX", qx },
                    { "quaternionY", qy },
                    { "quaternionZ", qz },
                    { "quaternionW", qw },
                    { "scale", scale },
                    { "category", cat }
                };

                string cmd = string.Format("zoneDecorAdd:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonedeletedecor")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int idno);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "ID", idno }
                };

                string cmd = string.Format("zoneDecorDelete:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zoneclearlayer")
            {

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "layerName", command[1] }
                };

                string cmd = string.Format("zoneClearLayer:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonesetplantspeed")
            {
                bool err = false;
                err = !int.TryParse(command[1], out int speed);

                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "speed", speed }
                };

                string cmd = string.Format("zoneSetPlantsSpeed:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

            if (command[0] == "#zonesetplantmode")
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "mode", command[1] }
                };

                string cmd = string.Format("zoneSetPlantsMode:zone_{0}:{1}", sender.ZoneId, GenxyConverter.Serialize(dictionary));
                request.Session.HandleLocalRequest(request.Session.CreateLocalRequest(cmd));
            }

        }
    }
}
