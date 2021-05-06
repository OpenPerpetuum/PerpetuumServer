using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleHarvesterDataView : ActiveModuleDataView {
        public double ModuleMiningModifier { get; set; }
        public int AmmoCapacity { get; set; }

        public ModuleHarvesterDataView(GathererModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            ModuleMiningModifier = item.GetBasePropertyModifier(AggregateField.mining_amount_modifier).Value;
            AmmoCapacity = item.AmmoCapacity;
        }
    }
}
