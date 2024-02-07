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
    public class ModuleAuxAccumulatorDataView : ModuleDataView {
        public double ModuleAccumulatorCapacity { get; set; }

        public ModuleAuxAccumulatorDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

                ModuleAccumulatorCapacity = item.GetBasePropertyModifier(AggregateField.core_max).Value;
        }
    }
}
