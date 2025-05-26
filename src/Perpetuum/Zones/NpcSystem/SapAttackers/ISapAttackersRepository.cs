namespace Perpetuum.Zones.NpcSystem.SapAttackers
{
    public interface ISapAttackersRepository
    {
        INpcPresences CreateSapAttackersSpawn(int sapDefinition, int zoneId);
    }
}
