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
    public class ModuleRemoteArmorRepairerDataView : ActiveModuleDataView {

        public double module_repair_amount { get; set; }
        public double module_optimal_range { get; set; }

        public ModuleRemoteArmorRepairerDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            module_optimal_range = item.GetBasePropertyModifier(AggregateField.optimal_range).Value * 10;
            module_repair_amount = item.GetBasePropertyModifier(AggregateField.armor_repair_amount).Value;
        }
    }
}