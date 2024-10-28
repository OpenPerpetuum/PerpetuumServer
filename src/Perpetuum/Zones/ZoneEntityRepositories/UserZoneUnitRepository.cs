using Perpetuum.Data;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using System.Collections.Generic;
using System.Data;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public class UserZoneUnitRepository : ZoneUnitReader, IZoneUnitRepository
    {
        private readonly IZone zone;

        public UserZoneUnitRepository(IZone zone, UnitHelper unitHelper) : base(unitHelper)
        {
            this.zone = zone;
        }

        public void Insert(Unit unit, Position position, string syncPrefix, bool runtime)
        {
            Db.Query()
                .CommandText("insert zoneuserentities (eid,zoneid,x,y,z,orientation) values (@eid,@zoneID,@x,@y,@z,@orientation)")
                .SetParameter("@eid", unit.Eid)
                .SetParameter("@zoneID", zone.Id)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@z", position.Z)
                .SetParameter("@orientation", (unit.Orientation * 255).Clamp(0, 255))
                .ExecuteNonQuery()
                .ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Delete(Unit unit)
        {
            int res = Db.Query()
                .CommandText("delete zoneuserentities where eid=@eid")
                .SetParameter("@eid", unit.Eid)
                .ExecuteNonQuery()
                .ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public void Update(Unit unit)
        {
            //ez meg nem kellett, de lehet
        }

        public override Dictionary<Unit, Position> GetAll()
        {
            List<IDataRecord> records = Db.Query()
                .CommandText("select * from zoneuserentities where zoneid=@zoneId")
                .SetParameter("@zoneId", zone.Id)
                .Execute();

            Dictionary<Unit, Position> result = new Dictionary<Unit, Position>();
            foreach (IDataRecord record in records)
            {
                Unit unit = CreateUnit(record);
                if (unit == null)
                {
                    continue;
                }

                double x = record.GetValue<double>("x");
                double y = record.GetValue<double>("y");
                Position p = new Position(x, y);
                result.Add(unit, p);
            }

            return result;
        }
    }
}