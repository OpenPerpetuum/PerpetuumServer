using Perpetuum.EntityFramework;

namespace Perpetuum.Bootstrapper
{
    internal class EntityAggregateServices : IEntityServices
    {
        public IEntityFactory Factory { get; set; }
        public IEntityDefaultReader Defaults { get; set; }
        public IEntityRepository Repository { get; set; }
    }
}
