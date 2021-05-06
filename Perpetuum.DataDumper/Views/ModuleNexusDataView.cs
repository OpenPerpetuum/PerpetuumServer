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
    public class ModuleNexusDataView : ActiveModuleDataView {
        public string NexusCategory { get; set; }
        public string NexusPropertyName { get; set; }
        public string NexusPropertyValue { get; set; }
        public double ModuleEffectRadius { get; set; }
        public string ModuleEffectRadiusModifier { get; set; }

        static List<AggregateField> fixedModifiers = new List<AggregateField> {
                                        AggregateField.core_usage, AggregateField.cpu_usage,
                                        AggregateField.cycle_time, AggregateField.default_effect_range,
                                        AggregateField.powergrid_usage, AggregateField.effect_enhancer_aura_radius_modifier };

        private string GetModuleType(GangModule item) {
            if (item.IsCategory(CategoryFlags.cf_gang_assist_speed)) {
                return "Velocity";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_information)) {
                return "Farlock";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_industry)) {
                return "Industrial";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_siege)) {
                return "Assault";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_defense)) {
                return "Armor";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_ewar)) {
                return "EW";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_shared_dataprocessing)) {
                return "Lock Booster";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_coordinated_manuevering)) {
                return "Evasive";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_maintance)) {
                return "Repairer";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_precision_firing)) {
                return "Critical Hit";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_core_management)) {
                return "Recharger";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_fast_extracting)) {
                return "Fast Extractor";
            } else if (item.IsCategory(CategoryFlags.cf_gang_assist_shield_calculations)) {
                return "Shield";
            } else {
                return "";
            }
        }

        public ModuleNexusDataView(GangModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            var specialProperty = item.BasePropertyModifiers.All.Where(x => !fixedModifiers.Contains(x.Field)).Single();

            NexusCategory = GetModuleType(item);

            NexusPropertyName = dumper.GetLocalizedName(specialProperty.Field.ToString());
            NexusPropertyValue = GetModifierString(specialProperty);

            ModuleEffectRadius = item.GetBasePropertyModifier(AggregateField.default_effect_range).Value;

            var radiusMod = item.GetBasePropertyModifier(AggregateField.effect_enhancer_aura_radius_modifier);

            if (radiusMod.HasValue) {
                ModuleEffectRadiusModifier = GetModifierString(radiusMod);
            }
        }
    }
}
