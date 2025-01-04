using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{
    public class MissionLocation
    {
        public readonly int id;

        public long LocationEid => _locationEntity.Eid;

        public double X => MyPosition.X;

        public double Y => MyPosition.Y;

        public readonly int maxMissionLevel;

        public MissionAgent Agent { get; private set; }
        public int RaceId => Zone.Configuration.RaceId;
        public Position MyPosition { get; private set; }
        public int zoneId { get; set; }
        public IZone Zone => ZoneManager.GetZone(zoneId);
        public ZoneConfiguration ZoneConfig => Zone?.Configuration ?? ZoneConfiguration.None;
        private Entity _locationEntity; //docking base or field terminal

        private static MissionDataCache _missionDataCache;
        public static IZoneManager ZoneManager { get; set; }

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }


        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {k.ID, id},
                {k.agentID, Agent.id},
                {k.eid, LocationEid},
                {k.zoneID, ZoneConfig.Id},
                {k.x, X},
                {k.y, Y},
                {"maxLevel", maxMissionLevel}

            };

            return result;
        }

        public static MissionLocation FromRecord(IDataRecord record)
        {
            int id = record.GetValue<int>("id");
            int agentId = record.GetValue<int>("agentid");
            long locationEid = record.GetValue<long>("locationeid");
            int zoneId = record.GetValue<int>("zoneid");
            double x = record.GetValue<double>("x");
            double y = record.GetValue<double>("y");
            int maxMissionLevel = record.GetValue<int>("maxmissionlevel");

            MissionLocation location = new MissionLocation(id, maxMissionLevel)
            {
                Agent = _missionDataCache.GetAgent(agentId),
                MyPosition = new Position(x, y),
                _locationEntity = Entity.Repository.Load(locationEid),
                zoneId = zoneId
            };

            return location;
        }


        private MissionLocation(int id, int maxMissionLevel)
        {
            this.id = id;
            this.maxMissionLevel = maxMissionLevel;

        }

        public override string ToString()
        {
            return $"ID:{id} zoneId:{ZoneConfig.Id} x:{X} y:{Y} eid:{LocationEid}";
        }

        public Container GetContainer
        {
            get
            {
                ContainerLocator containerLocator = new ContainerLocator();
                _locationEntity.AcceptVisitor(containerLocator);
                return containerLocator.container;
            }
        }

        private class ContainerLocator : IEntityVisitor<DockingBase>, IEntityVisitor<FieldTerminal>
        {
            public Container container;

            public void Visit(DockingBase dockingBase)
            {
                container = dockingBase.GetPublicContainer();
            }

            public void Visit(FieldTerminal terminal)
            {
                container = terminal.GetPublicContainer();
            }
        }



        public static void Insert(long locationEid, int agentId, int zoneId, double x, double y, int missionLevel)
        {
            const string query = @"
INSERT dbo.missionlocations
        ( agentid ,
          locationeid ,
          zoneid ,
          x ,
          y ,
          maxmissionlevel
        )
VALUES  ( @agentId,
          @locationEid,
          @zoneId,
          @x,
          @y,
          @missionLevel
        )
";

            int res =
            Db.Query().CommandText(query)
                .SetParameter("@agentId", agentId)
                .SetParameter("@locationEid", locationEid)
                .SetParameter("@zoneId", zoneId)
                .SetParameter("@x", x)
                .SetParameter("@y", y)
                .SetParameter("@missionLevel", missionLevel)
                .ExecuteNonQuery();

            (res == 0).ThrowIfTrue(ErrorCodes.SQLInsertError);

        }


        public List<Mission> GetSolvableRandomMissionsAtLocation()
        {

            if (!_missionDataCache.GetMissionIdsByAgent(Agent, out int[] missionIdsByAgent))
            {
                //no random mission defined for this agent
                return new List<Mission>();
            }

            List<int> safeToResolveIds = _missionDataCache.GetAllResolveInfos.Where(i => i.locationId == id && i.IsSafeToResolve).Select(m => m.missionId).ToList();

            return _missionDataCache.GetAllMissions
                .Where(m =>
                    m.behaviourType == MissionBehaviourType.Random &&
                    missionIdsByAgent.Contains(m.id) &&
                    m.listable &&
                    safeToResolveIds.Contains(m.id)
                    ).ToList();
        }

        /// <summary>
        /// map category to issuerAlliance's proper corp ww, ii , ss  etc....
        /// </summary>
        /// <param name="missionCategory"></param>
        /// <param name="issuerCorporationEid"></param>
        /// <param name="issuerAllianceEid"></param>
        public void GetIssuerCorporationByCategory(MissionCategory missionCategory, out long issuerCorporationEid, out long issuerAllianceEid)
        {
            issuerAllianceEid = Agent.OwnerAlliance.Eid;

            string corpNamePostFix = MissionDataCache.GetCorporationPostFixByMissionCategory(missionCategory);

            long corporationEid =
            DefaultCorporationDataCache.GetPureCorporationFromAllianceByPostFix(issuerAllianceEid, corpNamePostFix);

            issuerCorporationEid = corporationEid;

#if DEBUG
            if (MissionResolveTester.isTestMode)
            {
                return;
            }

            string allianceName = DefaultCorporationDataCache.GetAllianceName(issuerAllianceEid);
            string corporationName = DefaultCorporationDataCache.GetCorporationName(issuerCorporationEid);

            Logger.Info(missionCategory + " " + allianceName + " mapped to " + corporationName);

#endif



        }

        /*
            1	usa_great_corp
            2	eu_great_corp
            3	asia_great_corp
         */
        public int GetRaceSpecificCoinDefinition()
        {
            //TODO: Fixme: I am a hack for Syndicatification
            if (zoneId == 0 || zoneId == 2 || zoneId == 8) //For New Virginia, Daoden and Hershfield
            {
                return EntityDefault.GetByName(DefinitionNames.UNIVERSAL_MISSION_COIN).Definition;
            }
            //--end hack--
            switch (RaceId)
            {
                case 1:
                    return EntityDefault.GetByName(DefinitionNames.TM_MISSION_COIN).Definition;

                case 2:
                    return EntityDefault.GetByName(DefinitionNames.ICS_MISSION_COIN).Definition;

                case 3:
                    return EntityDefault.GetByName(DefinitionNames.ASI_MISSION_COIN).Definition;

                default:
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);

            }

        }

        public void DeleteFromDb()
        {
            //related mission data cleanup
            int missionsHere =
            Db.Query().CommandText("DELETE dbo.missiontolocation WHERE locationid=@locationId")
                .SetParameter("@locationId", id)
                .ExecuteNonQuery();

            Logger.Info(missionsHere + " missions were disconnected from location " + this);

            //delete myself
            Db.Query().CommandText("DELETE dbo.missionlocations WHERE id=@locationId")
                .SetParameter("@locationId", id)
                .ExecuteNonQuery();

            Logger.Info("mission location deleted. " + this);
        }

        public void UpdatePositionById(Position position)
        {
            string qs = "update missionlocations set x=@x,y=@y  where id=@id";

            int res =
            Db.Query().CommandText(qs)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@id", id)
                .ExecuteNonQuery();


            (res == 1).ThrowIfFalse(ErrorCodes.SQLUpdateError);
        }
    }


}
