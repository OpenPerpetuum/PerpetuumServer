using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public class TechTreePointsHandler
    {
        private readonly long _owner;

        public TechTreePointsHandler(Character character) : this(character.Eid) { }

        public TechTreePointsHandler(long owner)
        {
            _owner = owner;
        }

        public void UpdatePoints(TechTreePointType pointType, Func<int, int> pointsUpdater)
        {
            var record = Db.Query()
                .CommandText("select top 1 id,amount from techtreepoints (UPDLOCK) where owner = @owner and pointType = @pointType")
                .SetParameter("@owner", _owner)
                .SetParameter("@pointType", (int)pointType)
                .ExecuteSingleRow();

            var id = 0;
            var currentPoints = 0;

            if (record != null)
            {
                id = record.GetValue<int>(0);
                currentPoints = record.GetValue<int>("amount");
            }

            var updatedPoints = pointsUpdater(currentPoints);

            if (updatedPoints < 0)
                updatedPoints = 0;

            if (updatedPoints == currentPoints)
                return;

            if (id > 0)
            {
                Db.Query().CommandText("update techtreepoints set amount = @amount where id = @id")
                    .SetParameter("@id", id)
                    .SetParameter("@amount", updatedPoints)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
            }
            else
            {
                Db.Query().CommandText("insert into techtreepoints (owner,pointType,amount) values (@owner,@pointType,@amount)")
                    .SetParameter("@owner", _owner)
                    .SetParameter("@pointType", (int)pointType)
                    .SetParameter("@amount", updatedPoints)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
            }
        }

        public void AddAvailablePointsToDictionary(IDictionary<string, object> dictionary, string key = "points")
        {
            dictionary[key] = GetAvailablePoints().ToDictionary("p", p => p.ToDictionary());
        }

        private IEnumerable<Points> GetAvailablePoints()
        {
            return Db.Query()
                .CommandText("select pointType,amount from techtreepoints (UPDLOCK) where owner = @owner")
                .SetParameter("@owner", _owner)
                .Execute()
                .Select(r => new Points(r.GetValue<TechTreePointType>(0), r.GetValue<int>(1)));
        }
    }
}