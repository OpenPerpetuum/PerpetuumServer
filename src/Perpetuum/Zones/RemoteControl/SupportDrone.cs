using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.Standing;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Zones.RemoteControl
{
    public class SupportDrone : CombatDrone
    {
        public override bool IsStationary => false;

        public SupportDrone(IStandingHandler standingHandler)
            : base(standingHandler)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override bool IsHostile(Player player)
        {
            return true;
        }

        internal override bool IsHostile(Npc npc)
        {
            return false;
        }

        internal override bool IsHostile(CombatDrone drone)
        {
            return true;
        }

        internal override bool IsHostile(IndustrialDrone drone)
        {
            return true;
        }

        internal override bool IsHostile(SupportDrone drone)
        {
            return true;
        }

        internal override bool IsHostile(SentryTurret turret)
        {
            return true;
        }

        internal override bool IsHostile(Rift rift)
        {
            return false;
        }

        internal override bool IsHostile(Gate gate)
        {
            return false;
        }

        internal override bool IsHostile(AreaBomb bomb)
        {
            return false;
        }

        internal override bool IsHostile(IndustrialTurret turret)
        {
            return true;
        }

        internal override bool IsHostile(Portal portal)
        {
            return false;
        }

        internal override bool IsHostile(MobileTeleport teleport)
        {
            return false;
        }
    }
}
