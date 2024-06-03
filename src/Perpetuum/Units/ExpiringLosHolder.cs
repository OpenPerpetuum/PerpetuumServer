using Perpetuum.Zones;
using System;

namespace Perpetuum.Units
{
    public class ExpiringLosHolder
    {
        public readonly LOSResult losResult;
        private readonly DateTime expiry;

        public ExpiringLosHolder(LOSResult losResult, TimeSpan lifetime)
        {
            this.losResult = losResult;
            expiry = DateTime.Now.Add(lifetime);
        }

        public bool Expired => DateTime.Now >= expiry;
    }
}
