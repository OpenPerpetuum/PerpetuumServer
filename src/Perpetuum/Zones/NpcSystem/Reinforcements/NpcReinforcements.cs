using Perpetuum.Zones.NpcSystem.Presences;
using System;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcements : INpcReinforcements
    {
        private const double ThresholdDelta = 0.1;

        // Sorted array of OreNpcData by threshold
        private readonly INpcReinforcementWave[] _presences;

        public NpcReinforcements(INpcReinforcementWave[] presences)
        {
            _presences = presences.OrderBy(s => Array.IndexOf(presences, s.Threshold)).ToArray();
        }

        /// <summary>
        /// Get the next unspawned presence in the list with the next greatest threshold
        /// </summary>
        /// <param name="threshold">percentage expressed as [0.0-1.0]</param>
        /// <returns>INpcReinforcementWave or null</returns>
        public INpcReinforcementWave GetNextPresence(double threshold)
        {
            for (int i = _presences.Length - 1; i >= 0; i--)
            {
                if (threshold - _presences[i].Threshold < ThresholdDelta && _presences[i].Threshold - threshold > ThresholdDelta)
                {
                    return _presences[i].Spawned ? null : _presences[i];
                }
            }
            return null;
        }

        public bool HasActivePresence(Presence presence)
        {
            return _presences.Any(w => w.IsActivePresence(presence));
        }

        public INpcReinforcementWave GetActiveWaveOfPresence(Presence presence)
        {
            return _presences.Single(w => w.IsActivePresence(presence));
        }

        public INpcReinforcementWave[] GetAllActiveWaves()
        {
            return _presences.Where(w => !w.IsActivePresence(null)).ToArray();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ReinforceSpawn {");
            for (int i = 0; i < _presences.Length; i++)
            {
                sb.AppendLine(_presences[i].ToString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
