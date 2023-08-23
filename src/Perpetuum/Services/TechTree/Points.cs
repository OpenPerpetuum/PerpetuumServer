using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.TechTree
{
    public struct Points
    {
        public static readonly Points Empty = new Points(TechTreePointType.undefined, 0);

        public readonly TechTreePointType type;
        public readonly int amount;

        public Points(TechTreePointType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {"pointType",(int)type},
                    {"amount",amount}
                };
        }
    }
}