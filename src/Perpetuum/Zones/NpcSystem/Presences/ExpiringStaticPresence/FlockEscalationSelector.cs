using Perpetuum.Data;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences.GrowingPresences;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence
{
    public interface IEscalatingPresenceFlockSelector
    {
        IFlockConfiguration[] GetFlocksForPresenceLevel(GrowingPresence presence, int level);
    }
    public class EscalatingPresenceFlockSelector : IEscalatingPresenceFlockSelector
    {
        private readonly IEscalatingFlocksReader _reader;
        private readonly Random _random;
        public EscalatingPresenceFlockSelector(IEscalatingFlocksReader reader)
        {
            _reader = reader;
            _random = new Random();
        }
        public IFlockConfiguration[] GetFlocksForPresenceLevel(GrowingPresence presence, int level)
        {
            var infos = _reader.GetByPresence(presence).Where(info => info.Level == level);
            var flocks = new List<IFlockConfiguration>();
            foreach (var info in infos)
            {
                if (info.Chance >= _random.NextDouble())
                {
                    flocks.Add(presence.FlockConfigurationRepository.Get(info.FlockId));
                }
            }
            return flocks.ToArray();
        }
    }

    public class EscalationInfo
    {
        public int FlockId { get; set; }
        public int Level { get; set; }
        public double Chance { get; set; }
    }

    public interface IEscalatingFlocksReader
    {
        EscalationInfo[] GetByPresence(Presence presence);
    }

    public class EscalatingFlocksReader : IEscalatingFlocksReader
    {
        private ILookup<int, EscalationInfo> _flockInfos;

        public void Init()
        {
            _flockInfos = Db.Query().CommandText("select * from npcescalactions")
                .Execute()
                .Select(r =>
                {
                    return new
                    {
                        presenceID = r.GetValue<int>("presenceid"),
                        info = new EscalationInfo
                        {
                            FlockId = r.GetValue<int>("flockid"),
                            Level = r.GetValue<int>("level"),
                            Chance = r.GetValue<double>("chance")
                        }
                    };
                }).ToLookup(x => x.presenceID, x => x.info);
        }

        public EscalationInfo[] GetByPresence(Presence presence)
        {
            return _flockInfos.GetOrEmpty(presence.Configuration.ID);
        }
    }
}
