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
    public class ModuleERPDataView : ActiveModuleDataView {

        public double ModuleRecoveryChemical { get; set; }
        public double ModuleRecoveryKinetic { get; set; }
        public double ModuleRecoverySeismic { get; set; }
        public double ModuleRecoveryThermal { get; set; }

        public ModuleERPDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            ModuleRecoveryChemical = item.GetBasePropertyModifier(AggregateField.chemical_damage_to_core_modifier).Value * 100;
            ModuleRecoveryKinetic = item.GetBasePropertyModifier(AggregateField.kinetic_damage_to_core_modifier).Value * 100;
            ModuleRecoverySeismic = item.GetBasePropertyModifier(AggregateField.explosive_damage_to_core_modifier).Value * 100;
            ModuleRecoveryThermal = item.GetBasePropertyModifier(AggregateField.thermal_damage_to_core_modifier).Value * 100;
        }
    }
}
