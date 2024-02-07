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
    public class ModuleArmorPlateDataView : ModuleDataView {
        public double ModuleHp { get; set; }
        public double ModuleSurfaceHit { get; set; }
        public double ModuleDemobResist { get; set; }

        public ModuleArmorPlateDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            ModuleHp = item.GetBasePropertyModifier(AggregateField.armor_max).Value;
            ModuleDemobResist = item.GetBasePropertyModifier(AggregateField.massiveness).Value * 100;
            ModuleSurfaceHit = item.GetBasePropertyModifier(AggregateField.signature_radius).Value;
        }
    }
}
