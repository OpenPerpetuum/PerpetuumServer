using Perpetuum.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Accounting
{
    public class MtProductRepository : IMtProductRepository
    {
        public MtProduct Get(string name)
        {
            IDataRecord record = Db.Query()
                .CommandText("select * from mtproductprices where productkey=@key")
                .SetParameter("@key", name.ToLower())
                .ExecuteSingleRow();

            if (record == null)
            {
                return MtProduct.None;
            }

            MtProduct p = CreateMtProductFromRecord(record);

            return p;
        }

        public IEnumerable<MtProduct> GetAll()
        {
            return Db.Query()
                .CommandText("select * from mtproductprices")
                .Execute()
                .Select(CreateMtProductFromRecord).ToArray();
        }

        private static MtProduct CreateMtProductFromRecord(IDataRecord record)
        {
            MtProduct p = new MtProduct
            {
                name = record.GetValue<string>("productkey"),
                price = record.GetValue<int>("price")
            };

            return p;
        }
    }
}