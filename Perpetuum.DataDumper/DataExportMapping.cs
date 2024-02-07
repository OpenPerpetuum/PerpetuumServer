using Perpetuum.DataDumper.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    public class DataExportMapping {
        public string TableName { get; set; }
        public Type ViewType { get; set; }
        public string Category { get; set; }

        public DataExportMapping(string _tableName, Type _type, string _category) {
            TableName = _tableName;
            ViewType = _type;
            Category = _category;
        }

        public override string ToString() {
            return ViewType.Name.ToString();
        }

        public static List<DataExportMapping> Mappings = new List<DataExportMapping>() {
                                // These universal ones are strange because they're stored in a different category
                                // even though in the game UI they're the same thing and they have basically the same stats
                                // Also the univeral one is a regular module while the others are Active
                                new DataExportMapping("ModuleMinerStats", typeof(ModuleMinerDataView), "cf_drillers"),
                                new DataExportMapping("ModuleHarvesterStats", typeof(ModuleHarvesterDataView), "cf_harvesters"),
                                new DataExportMapping("ModuleArmorHardenerStats", typeof(ModuleArmorHardenerDataView), "cf_armor_hardeners"),
                                new DataExportMapping("ModuleShieldGeneratorStats", typeof(ModuleShieldGeneratorDataView), "cf_shield_generators"),
                                new DataExportMapping("ModuleERPStats", typeof(ModuleERPDataView), "cf_kers"),
                                new DataExportMapping("ModuleRemArmorRepairerStats", typeof(ModuleRemoteArmorRepairerDataView), "cf_remote_armor_repairers"),
                                new DataExportMapping("ModuleArmorPlateStats", typeof(ModuleArmorPlateDataView), "cf_armor_plates"),
                                new DataExportMapping("SparkStats", typeof(SparkDataView), ""),
                                new DataExportMapping("ModuleArmorRepairerStats", typeof(ModuleArmorRepairerDataView), "cf_armor_repair_systems"),
                                new DataExportMapping("AmmoWeaponStats", typeof(AmmoWeaponDataView), "cf_ammo"),
                                new DataExportMapping("ModuleWeaponStats", typeof(ModuleWeaponDataView), "cf_weapons"),
                                new DataExportMapping("RobotStats", typeof(RobotDataView), "cf_robot"),
                                new DataExportMapping("ModuleAuxAccumulatorStats", typeof(ModuleAuxAccumulatorDataView), "cf_core_batteries"),
                                new DataExportMapping("ModuleEnergyTransfererStats", typeof(ModuleEnergyTransfererDataView), "cf_energy_transferers"),
                                new DataExportMapping("ModuleEnergyDrainerStats", typeof(ModuleEnergyDrainerDataView), "cf_energy_vampires"),
                                new DataExportMapping("ModuleEnergyNeutralizerStats", typeof(ModuleEnergyNeutralizerDataView), "cf_energy_neutralizers"),
                                new DataExportMapping("ModuleEnergyInjectorStats", typeof(ModuleEnergyInjectorDataView), "cf_core_boosters"),
                                new DataExportMapping("ModuleDemobilizerStats", typeof(ModuleDemobilizerDataView), "cf_webber"),
                                new DataExportMapping("ModuleNexusStats", typeof(ModuleNexusDataView), "cf_gang_assist_modules")

                            };

    }
}
