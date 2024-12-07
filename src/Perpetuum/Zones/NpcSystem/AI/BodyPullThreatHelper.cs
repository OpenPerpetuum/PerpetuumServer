using Perpetuum.EntityFramework;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Units;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Teleporting;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class BodyPullThreatHelper :
        IEntityVisitor<Player>,
        IEntityVisitor<AreaBomb>,
        IEntityVisitor<Npc>,
        IEntityVisitor<SentryTurret>,
        IEntityVisitor<CombatDrone>,
        IEntityVisitor<Portal>,
        IEntityVisitor<SupportDrone>
    {
        private readonly SmartCreature smartCreature;

        public BodyPullThreatHelper(SmartCreature smartCreature)
        {
            this.smartCreature = smartCreature;
        }

        public void Visit(Player player)
        {
            if (smartCreature.Behavior.Type != BehaviorType.Aggressive &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone)
            {
                return;
            }

            if (player.HasTeleportSicknessEffect &&
                !(smartCreature is RemoteControlledCreature))
            {
                return;
            }

            if (smartCreature is RemoteControlledCreature remoteControlledCreature &&
                (remoteControlledCreature.CommandRobot is Player) &&
                player.Zone.Configuration.IsAlpha &&
                !player.HasPvpEffect &&
                !remoteControlledCreature.CommandRobot.HasPvpEffect)
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == player.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(player))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(player, new Threat(ThreatType.Bodypull, threat));
        }

        public void Visit(AreaBomb bomb)
        {
            if (!smartCreature.IsInAggroRange(bomb))
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            Flocks.ISmartCreatureGroup group = smartCreature.Group;

            if (group != null && group.Members.Any(m => m.ThreatManager.Contains(bomb)))
            {
                return;
            }

            double threat = Threat.BODY_PULL;

            if (smartCreature.ThreatManager.IsThreatened)
            {
                Hostile h = smartCreature.ThreatManager.GetMostHatedHostile();

                if (h != null)
                {
                    threat = h.Threat * 100;
                }
            }

            smartCreature.AddThreat(bomb, new Threat(ThreatType.Bodypull, threat + FastRandom.NextDouble(0, 5)));
        }

        public void Visit(Npc npc)
        {
            ProcessNpcThreats(npc);
        }

        public void Visit(SentryTurret sentryTurret)
        {
            ProcessNpcThreats(sentryTurret);
        }

        public void Visit(CombatDrone combatDrone)
        {
            ProcessNpcThreats(combatDrone);
        }

        public void Visit(Portal portal)
        {
            ProcessNpcThreats(portal);
        }

        public void Visit(MobileTeleport teleport)
        {
            ProcessNpcThreats(teleport);
        }

        public void Visit(SupportDrone supportDrone)
        {
            ProcessNpcThreats(supportDrone);
        }

        private void ProcessNpcThreats(Unit unit)
        {
            if (smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone &&
                smartCreature.ED.Options.Faction == unit.ED.Options.Faction)
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule || m is RemoteArmorRepairModule))
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == unit.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(unit))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(unit, new Threat(ThreatType.Bodypull, threat));
        }
    }
}
