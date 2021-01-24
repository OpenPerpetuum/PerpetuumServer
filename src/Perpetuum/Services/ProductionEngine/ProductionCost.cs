using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionCost
    {
        public long? categoryFlag;
        public int? tierType;
        public int? tierLevel;
        public double costModifier;
    }

    public interface IProductionCostReader
    {
        IEnumerable<ProductionCost> ProductionCost { get; }
        ProductionCost GetProductionCostByED(EntityDefault ed);
        double GetProductionCostModByED(EntityDefault ed);
    }

    public class ProductionCostReader : IProductionCostReader
    {
        private const double MIN = 1.0;
        private const double MAX = 10.0;

        public IEnumerable<ProductionCost> ProductionCost
        {
            get { return _costTable.Values; }
        }
        private readonly IDictionary<int, ProductionCost> _costTable;
        public ProductionCostReader()
        {
            _costTable = Database.CreateCache<int, ProductionCost>("productioncost", "id", r =>
            {
                var cost = new ProductionCost
                {
                    categoryFlag = r.GetValue<long?>(k.category),
                    tierType = r.GetValue<int?>(k.tierType),
                    tierLevel = r.GetValue<int?>(k.tierLevel),
                    costModifier = r.GetValue<double>("costmodifier")
                };
                return cost;
            });
        }

        public ProductionCost GetProductionCostByED(EntityDefault ed)
        {
            var matchScores = ProductionCost.GroupBy(c =>
                (((CategoryFlags)(c.categoryFlag ?? 0) == ed.CategoryFlags) ? 5 : 0) +
                (((TierType)(c.tierType ?? 0) == ed.Tier.type) ? 1 : 0) +
                (((c.tierLevel ?? 0) == ed.Tier.level) ? 3 : 0));

            var bestMatchScore = matchScores.Max(x => x.Key);
            return matchScores.FirstOrDefault(x => x.Key == bestMatchScore).Select(g => g).FirstOrDefault();
        }

        public double GetProductionCostModByED(EntityDefault ed)
        {
            var prodCost = GetProductionCostByED(ed) ?? (new ProductionCost { costModifier = MIN });
            return prodCost.costModifier.Clamp(MIN, MAX);
        }
    }
}