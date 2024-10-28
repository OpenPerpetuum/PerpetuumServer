using Perpetuum.EntityFramework;
using Perpetuum.Zones.Training;

namespace Perpetuum.Units.DockingBases
{
    public class UndockSpawnPositionSelector : IEntityVisitor<DockingBase>, IEntityVisitor<TrainingDockingBase>
    {
        private Position spawnPosition;

        public static Position SelectSpawnPosition(DockingBase dockingBase)
        {
            UndockSpawnPositionSelector selector = new UndockSpawnPositionSelector();
            dockingBase.AcceptVisitor(selector);

            return selector.spawnPosition.Center;
        }

        public void Visit(DockingBase dockingBase)
        {
            int minRange = dockingBase.Size;
            int maxRange = minRange + dockingBase.SpawnRange;
            int radius = FastRandom.NextInt(minRange, maxRange);
            double angle = FastRandom.NextDouble();
            spawnPosition = dockingBase.CurrentPosition.OffsetInDirection(angle, radius);
        }

        public void Visit(TrainingDockingBase dockingBase)
        {
            spawnPosition = dockingBase.SpawnPosition.GetRandomPositionInRange2D(0, dockingBase.SpawnRange);
        }
    }
}
