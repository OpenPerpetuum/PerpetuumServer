using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Accounting
{
    public class MtProductHelper
    {
        private readonly IMtProductRepository mtProductRepository;

        public MtProductHelper(IMtProductRepository mtProductRepository)
        {
            this.mtProductRepository = mtProductRepository;
        }

        public MtProduct GetByAccountTransactionType(AccountTransactionType type)
        {
            return mtProductRepository.Get(type.ToString());
        }

        public IEnumerable<MtProduct> GetAllProducts()
        {
            return mtProductRepository.GetAll();
        }

        public Dictionary<string, object> GetProductInfos()
        {
            return GetAllProducts().ToDictionary(p => p.name, p => (object)p.price);
        }
    }
}
