namespace Perpetuum.Zones.NpcSystem.AI
{
    public class MiningIndustrialTurretAI : IndustrialAI
    {
        public MiningIndustrialTurretAI(SmartCreature smartCreature)
            : base(smartCreature)
        {
            smartCreature.LookingForMiningTargets();
        }
    }
}
