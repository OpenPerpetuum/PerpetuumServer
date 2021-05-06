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
    public class ModuleWeaponDataView : ActiveModuleDataView {
        public string AmmoType { get; set; }
        public int AmmoCapacity { get; set; }
        public double ModuleDamage { get; set; }
        public double ModuleFalloff { get; set; }
        public double ModuleHitDispersion { get; set; }
        public string MissileOptimalRange { get; set; }
        
        private List<SlotFlags> SlotFlagValues;
        public string SlotFlags {
            get {
                return String.Join(";", SlotFlagValues.Select(x => x.ToString()));
            }
            set {
                SlotFlagValues = EnumHelper<SlotFlags>.MaskToList((SlotFlags)Convert.ToInt64(value)).ToList();
            }
        }
        public string SlotType {
            get {
                return SlotFlagValues.Intersect(SLOT_TYPE_FLAGS).SingleOrDefault().ToString();
            }
        }

        public string SlotSize {
            get {
                return SlotFlagValues.Intersect(SLOT_SIZE_FLAGS).SingleOrDefault().ToString();
            }
        }

        public string SlotLocation {
            get {
                return SlotFlagValues.Intersect(SLOT_LOCATION_FLAGS).SingleOrDefault().ToString();
            }
        }

        public ModuleWeaponDataView(WeaponModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            var dictionaryData = item.ToDictionary();

            dictionaryData["ammoCategoryFlags"] = (CategoryFlags)dictionaryData["ammoCategoryFlags"];

            AmmoType = dumper.GetLocalizedName(((CategoryFlags)dictionaryData["ammoCategoryFlags"]).ToString()).Replace(";undefined", "");
            SlotFlags = item.ModuleFlag.ToString();
            AmmoCapacity = item.AmmoCapacity;
            ModuleDamage = item.DamageModifier.Value * 100;
            ModuleFalloff = item.Properties.Single(x => x.Field == AggregateField.falloff).Value * 10;
            ModuleHitDispersion = item.Accuracy.Value;

            var rangeModifierProperty = item.GetBasePropertyModifier(AggregateField.module_missile_range_modifier);

            if (rangeModifierProperty.HasValue) {
                string directionSymbol = "+";

                if (rangeModifierProperty.Value < 0) {
                    directionSymbol = "-";
                }

                MissileOptimalRange = directionSymbol + (rangeModifierProperty.Value - 1) * 100 + "%";

            }
        }

    }
}
