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
    public class ModuleArmorHardenerDataView : ActiveModuleDataView {

        public double ModuleResistActiveChemical { get; set; }
        public double ModuleResistPassiveChemical { get; set; }
        public double ModuleResistActiveKinetic { get; set; }
        public double ModuleResistPassiveKinetic { get; set; }
        public double ModuleResistActiveSeismic { get; set; }
        public double ModuleResistPassiveSeismic { get; set; }
        public double ModuleResistActiveThermal { get; set; }
        public double ModuleResistPassiveThermal { get; set; }
        public string ModuleResistType { get; set; }
        public double ModuleResistPassive { get; set; }
        public double ModuleResistActive { get; set; }

        public ModuleArmorHardenerDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            FillModuleData(item);
        }

        public ModuleArmorHardenerDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            FillModuleData(item);
        }

        private void FillModuleData(Module module) {
            ModuleResistActiveChemical = module.GetBasePropertyModifier(AggregateField.effect_resist_chemical).Value;
            ModuleResistPassiveChemical = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            ModuleResistActiveKinetic = module.GetBasePropertyModifier(AggregateField.effect_resist_kinetic).Value;
            ModuleResistPassiveKinetic = module.GetBasePropertyModifier(AggregateField.resist_kinetic).Value;
            ModuleResistActiveSeismic = module.GetBasePropertyModifier(AggregateField.effect_resist_explosive).Value;
            ModuleResistPassiveSeismic = module.GetBasePropertyModifier(AggregateField.resist_explosive).Value;
            ModuleResistActiveThermal = module.GetBasePropertyModifier(AggregateField.effect_resist_thermal).Value;
            ModuleResistPassiveThermal = module.GetBasePropertyModifier(AggregateField.resist_thermal).Value;

            var passives = new List<Tuple<double, double, string>> {
                                            new Tuple<double, double, string>(ModuleResistPassiveChemical,ModuleResistActiveChemical, "Chemical"),
                                            new Tuple<double, double, string>(ModuleResistPassiveKinetic, ModuleResistActiveKinetic, "Kinetic"),
                                            new Tuple<double, double, string>(ModuleResistPassiveSeismic, ModuleResistActiveSeismic, "Seismic"),
                                            new Tuple<double, double, string>(ModuleResistPassiveThermal, ModuleResistActiveThermal, "Thermal")
                                        };

            if (passives.Where(x => x.Item1 != 0).Count() > 1) {
                ModuleResistType = "All";
                ModuleResistPassive = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            } else {
                var activeType = passives.Where(x => x.Item1 != 0).Single();
                ModuleResistType = activeType.Item3;
                ModuleResistPassive = activeType.Item1;
                ModuleResistActive = activeType.Item2;
            }
        }
    }

}
