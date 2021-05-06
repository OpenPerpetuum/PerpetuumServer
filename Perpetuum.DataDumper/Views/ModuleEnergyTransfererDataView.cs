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
    public class ModuleEnergyTransfererDataView : ActiveModuleDataView {
        public double ModuleEnergyTransfer { get; set; }

        public ModuleEnergyTransfererDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            ModuleEnergyTransfer = item.GetBasePropertyModifier(AggregateField.energy_transfer_amount).Value;
        }
    }
}
