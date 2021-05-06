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
    public class ModuleShieldGeneratorDataView : ActiveModuleDataView {

        public double module_shield_radius { get; set; }
        public double module_absorption_ratio { get; set; }

        public ModuleShieldGeneratorDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            module_shield_radius = item.GetBasePropertyModifier(AggregateField.shield_radius).Value;
            module_absorption_ratio = Math.Round(1 / item.GetBasePropertyModifier(AggregateField.shield_absorbtion).Value, 3);
        }
    }
}
