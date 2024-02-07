using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class AmmoWeaponDataView : ItemDataView {
        public double? DamageChemical { get; set; }
        public double? DamageKinetic { get; set; }
        public double? DamageSeismic { get; set; }
        public double? DamageThermal { get; set; }
        public double? DamageToxic { get; set; }
        public double DamageTotal { get => (DamageChemical ?? 0) + (DamageKinetic ?? 0) + (DamageSeismic ?? 0) + (DamageThermal ?? 0); }
        public double? OptimalRange { get; set; }
        public double? ExplosionRadius { get; set; }
        public string ModifierRange { get; set; }
        public string ModifierFalloff { get; set; }

        public AmmoWeaponDataView(Ammo item, DataDumper dumper) {
            dumper.InitItemView(this, item);

            var dictionaryData = item.ToDictionary();

            DamageChemical = item.GetBasePropertyModifier(AggregateField.damage_chemical).Value;
            DamageKinetic = item.GetBasePropertyModifier(AggregateField.damage_kinetic).Value;
            DamageSeismic = item.GetBasePropertyModifier(AggregateField.damage_explosive).Value;
            DamageThermal = item.GetBasePropertyModifier(AggregateField.damage_thermal).Value;
            DamageToxic = item.GetBasePropertyModifier(AggregateField.damage_toxic).Value;

            ModifierFalloff = (item.GetBasePropertyModifier(AggregateField.falloff).Value * 10).ToString();
            ExplosionRadius = item.GetBasePropertyModifier(AggregateField.explosion_radius).Value;

            // Empty out the 0's
            if (DamageChemical == 0) { DamageChemical = null; };
            if (DamageKinetic == 0) { DamageKinetic = null; };
            if (DamageSeismic == 0) { DamageSeismic = null; };
            if (DamageThermal == 0) { DamageThermal = null; };
            if (DamageToxic == 0) { DamageToxic = null; };

            if (ModifierFalloff == "0") { ModifierFalloff = null; }; // keeping this as a string for now because other modules may use it as a modifier?
            if (ExplosionRadius == 0) { ExplosionRadius = null; };

            // This will get the property even if it doesn't explicitly exist
            // It will be set to the default value, but the HasValue will tell
            // us if it's something other than the default

            ModifierRange = GetModifierString(item.GetBasePropertyModifier(AggregateField.optimal_range_modifier));

            var rangeProp = item.GetBasePropertyModifier(AggregateField.optimal_range);

            if (rangeProp.HasValue) {
                OptimalRange = rangeProp.Value * 10;
            }
        }

    }


}
