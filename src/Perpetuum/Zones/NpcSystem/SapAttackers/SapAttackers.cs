using Perpetuum.Zones.NpcSystem.Presences;
using System;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.SapAttackers
{
    public class SapAttackers : INpcPresences
    {
        private readonly INpcPresence[] _presences;

        public SapAttackers(INpcPresence[] presences)
        {
            _presences = presences.OrderBy(s => Array.IndexOf(presences, s.MinStability)).ToArray();
        }

        public INpcPresence GetNextPresence(int stability)
        {
            for (int i = _presences.Length - 1; i >= 0; i--)
            {
                if (stability - _presences[i].MinStability < stability)
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

        public INpcPresence GetActivePresence(Presence presence)
        {
            return _presences.Single(w => w.IsActivePresence(presence));
        }

        public INpcPresence[] GetAllActivePresences()
        {
            return _presences.Where(w => !w.IsActivePresence(null)).ToArray();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SapAttackerSpawn {");
            for (int i = 0; i < _presences.Length; i++)
            {
                sb.AppendLine(_presences[i].ToString());
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        public INpcPresence GetNextPresence(double threshold)
        {
            throw new NotImplementedException();
        }
    }
}
