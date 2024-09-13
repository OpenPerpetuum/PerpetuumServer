using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Modules
{
    public class RemoteControlledDrillerModule : DrillerModule
    {
        public RemoteControlledDrillerModule(RareMaterialHandler rareMaterialHandler, MaterialHelper materialHelper)
            : base(CategoryFlags.undefined, rareMaterialHandler, materialHelper)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            if (Zone != null)
            {
                DoExtractMinerals(Zone);
            }
        }
    }
}
