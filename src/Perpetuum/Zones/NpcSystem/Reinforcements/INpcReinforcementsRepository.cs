using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    /// <summary>
    /// DB lookup interface and factory for INpcReinforcements
    /// </summary>
    public interface INpcReinforcementsRepository
    {
        INpcPresences CreateOreNPCSpawn(MaterialType materialType, int zoneId);
        INpcPresences CreateNpcBossAddSpawn(NpcBossInfo npcBossInfo, int zoneId);
    }
}
