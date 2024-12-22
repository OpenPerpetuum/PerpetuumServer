using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;
using System.Linq;

namespace Perpetuum.Zones.RemoteControl
{
    public class RemoteControlledCreature : SmartCreature
    {
        private const double SentryTurretCallForHelpArmorThreshold = 0.8;
        private UnitDespawnHelper despawnHelper;
        private readonly IStandingHandler standingHandler;
        private const double StandingLimit = 0.0;

        public event RemoteChannelEventHandler RemoteChannelDeactivated;

        public Robot CommandRobot { get; private set; }

        public double RemoteChannelBandwidthUsage { get; private set; }

        public TimeSpan DespawnTime
        {
            set => despawnHelper = UnitDespawnHelper.Create(this, value);
        }

        public bool IsInOperationalRange => CurrentPosition.IsInRangeOf2D(CommandRobot.CurrentPosition, HomeRange);

        public bool IsReceivedRetreatCommand
        {
            get
            {
                Effects.Effect translatedCommand =
                    EffectHandler.GetEffectsByType(EffectType.remote_command_translation).FirstOrDefault();

                return translatedCommand != null &&
                    translatedCommand.PropertyModifiers
                        .Any(x => x.Field == AggregateField.drone_remote_command_translation_retreat && x.Value >= 1);
            }
        }

        public override bool IsStationary => true;

        public override double CallForHelpArmorThreshold => SentryTurretCallForHelpArmorThreshold;

        public RemoteControlledCreature(IStandingHandler standingHandler)
        {
            this.standingHandler = standingHandler;
        }

        public void SetCommandRobot(Robot commandRobot)
        {
            CommandRobot = commandRobot;
        }

        public void SetBandwidthUsage(double value)
        {
            RemoteChannelBandwidthUsage = value;
        }

        public override void AddThreat(Unit hostile, Threat threat, bool spreadToGroup)
        {
            if (hostile.IsPlayer() && CommandRobot == (hostile as Player))
            {
                return;
            }

            base.AddThreat(hostile, threat, spreadToGroup);
        }

        public void Scoop()
        {
            RobotInventory cargo = CommandRobot.GetContainer();

            if (cargo != null)
            {
                Item packedDrone = null;
                if (ED.Options.PackedTurretId != 0)
                {
                    packedDrone = (Item)Factory.CreateWithRandomEID(ED.Options.PackedTurretId);
                    packedDrone.Quantity = 1;

                    if (cargo.IsEnoughCapacity(packedDrone))
                    {
                        cargo.AddItem(packedDrone, true);
                        States.Teleport = true;
                        RemoveFromZone();
                    }
                }
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            despawnHelper?.Update(time, this);
        }

        protected override void OnBeforeRemovedFromZone(IZone zone)
        {
            RemoteChannelDeactivated(this);
        }

        protected bool IsHostilePlayer(Player targetPlayer)
        {
            if (!(CommandRobot is Player player))
            {
                return true;
            }

            if (player == targetPlayer)
            {
                return false;
            }

            if (Zone.Configuration.IsAlpha && !player.HasPvpEffect && !targetPlayer.HasPvpEffect)
            {
                return false;
            }

            if (player == targetPlayer)
            {
                return false;
            }

            if (player.Gang != null && player.Gang.IsMember(targetPlayer.Character))
            {
                return false;
            }

            double corporationStanding = standingHandler.GetStanding(player.CorporationEid, targetPlayer.CorporationEid);

            if (corporationStanding > StandingLimit)
            {
                return false;
            }

            double personalStanding = standingHandler.GetStanding(Owner, targetPlayer.Character.Eid);

            return personalStanding <= StandingLimit;
        }
    }
}
