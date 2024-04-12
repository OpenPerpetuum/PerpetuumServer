using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public class IndustrialValueManager : IIndustrialValueManager
    {
        private ImmutableDictionary<string, IndustrialTarget> _industrialTargets = ImmutableDictionary<string, IndustrialTarget>.Empty;

        public ImmutableSortedSet<IndustrialTarget> IndustrialTargets => _industrialTargets.Values.ToImmutableSortedSet();

        public bool IsValuable => !_industrialTargets.IsEmpty;

        public void AddOrUpdateIndustrialTargetWithValue(Position tile, double value)
        {
            IndustrialTarget newTarget = new IndustrialTarget(tile.Center);
            newTarget.AddIndustrialValue(new IndustrialValue(value));
            _ = ImmutableInterlocked.AddOrUpdate(
                ref _industrialTargets,
                tile.Center.ToString(),
                newTarget,
                (_, oldTarget) =>
                {
                    return newTarget;
                });
        }

        public bool Contains(Position tile)
        {
            return _industrialTargets.ContainsKey(tile.ToString());
        }

        public void Clear()
        {
            _industrialTargets = _industrialTargets.Clear();
        }

        public void Remove(Position tile)
        {
            _ = ImmutableInterlocked.TryRemove(ref _industrialTargets, tile.Center.ToString(), out _);
        }

        public string ToDebugString()
        {
            if (_industrialTargets.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            _ = sb.AppendLine();
            _ = sb.AppendLine("========== THREAT ==========");
            _ = sb.AppendLine();

            foreach (IndustrialTarget industrialTarget in _industrialTargets.Values.OrderByDescending(h => h.IndustrialValue))
            {
                _ = sb.AppendFormat("  {0} ({1}) => {2}", "Industrial Target", industrialTarget.Position.ToString(), industrialTarget.IndustrialValue);
                _ = sb.AppendLine();
            }

            _ = sb.AppendLine();
            _ = sb.AppendLine("============================");

            return sb.ToString();
        }
    }
}
