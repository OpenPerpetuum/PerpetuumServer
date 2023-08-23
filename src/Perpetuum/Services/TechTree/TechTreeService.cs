using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Services.TechTree
{
    public class TechTreeService : ITechTreeService
    {
        private readonly ITechTreeInfoService _infoService;

        public TechTreeService(ITechTreeInfoService infoService)
        {
            _infoService = infoService;
        }

        public void NodeUnlocked(long owner, TechTreeNode node)
        {

        }

        public IEnumerable<TechTreeNode> GetUnlockedNodes(long owner)
        {
            var nodes = _infoService.GetNodes();
            return Db.Query()
                .CommandText("select definition from techtreeunlockednodes where owner = @owner")
                .SetParameter("@owner", owner)
                .Execute()
                .Select(r => nodes[r.GetValue<int>(0)]).ToArray();
        }

        public void AddInfoToDictionary(long owner, IDictionary<string, object> dictionary)
        {
            dictionary["techTree"] = GetInfo(owner);
        }

        private IDictionary<string, object> GetInfo(long owner)
        {
            var info = new Dictionary<string, object>
                {
                    {"unlockedNodes", GetUnlockedNodes(owner).Select(n => n.Definition).ToArray()},
                };

            var points = new TechTreePointsHandler(owner);
            points.AddAvailablePointsToDictionary(info);
            return info;
        }
    }
}