using Perpetuum.Zones.Teleporting.Strategies;

namespace Perpetuum.Bootstrapper
{
    internal class TeleportStrategyFactories : ITeleportStrategyFactories
    {
        public TeleportWithinZone.Factory TeleportWithinZoneFactory { get; set; }
        public TeleportToAnotherZone.Factory TeleportToAnotherZoneFactory { get; set; }
        public TrainingExitStrategy.Factory TrainingExitStrategyFactory { get; set; }
    }
}
