namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HarvestingIndustrialTurretAI : IndustrialAI
    {
        public HarvestingIndustrialTurretAI(SmartCreature smartCreature) : base(smartCreature)
        {
            smartCreature.LookingForHarvestingTargets();
        }
    }
}
