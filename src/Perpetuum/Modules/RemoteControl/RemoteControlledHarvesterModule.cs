using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;

namespace Perpetuum.Modules
{
    public class RemoteControlledHarvesterModule : HarvesterModule
    {
        public RemoteControlledHarvesterModule(PlantHarvester.Factory plantHarvesterFactory) : base(CategoryFlags.undefined, plantHarvesterFactory)
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
                DoHarvesting(Zone);
            }
        }
    }
}
