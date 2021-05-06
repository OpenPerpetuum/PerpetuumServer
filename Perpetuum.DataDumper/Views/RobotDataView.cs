using Perpetuum.ExportedTypes;
using Perpetuum.Modules;
using Perpetuum.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class RobotDataView : ItemDataView {
        // Main Stats
        public double Speed { get; set; }
        public double Armor { get; set; }
        public double Accumulator { get; set; }
        public double Cargo { get; set; }
        public double Cpu { get; set; }
        public double Reactor { get; set; }
        public double SlopeCapacity { get; set; }

        // Slots
        public int SlotsHead { get; set; }
        public int SlotsLeg { get; set; }
        public int SlotsChassis { get; set; }
        public int SlotsGuns { get; set; }
        public int SlotsMissiles { get; set; }
        public int SlotsIndustrial { get; set; }
        public int SlotsMisc { get; set; }

        // Defense
        public double SurfaceSize { get; set; }
        public double ResistanceChem { get; set; }
        public double ResistanceKinetic { get; set; }
        public double ResistanceSeismic { get; set; }
        public double ResistanceThermal { get; set; }
        public double ResistanceDemobilizer { get; set; }

        // Targeting
        public double SensorStrength { get; set; }
        public double LockingRange { get; set; }
        public double LockingTime { get; set; }
        public double MaxLockedTargets { get; set; }
        public double SignalDetection { get; set; }
        public double SignalMasking { get; set; }

        // Misc
        public double AccumulatorRecharge { get; set; }
        public double AccumulatorStability { get; set; }
        public double InterferenceEmission { get; set; }
        public double InterferenceRadius { get; set; }
        public double InterferenceMin { get; set; }
        public double InterferenceMax { get; set; }

        public RobotDataView(Robot input, DataDumper dumper) {
            dumper.InitItemView(this, input);

            var dictionaryData = input.ToDictionary();

            RobotHead head = input.GetRobotComponent<RobotHead>();
            RobotChassis chassis = input.GetRobotComponent<RobotChassis>();
            RobotLeg legs = input.GetRobotComponent<RobotLeg>();
            RobotInventory inventory = input.Components.OfType<RobotInventory>().SingleOrDefault();

            var allSlotFlags = head.ED.Options.SlotFlags
                                    .Concat(chassis.ED.Options.SlotFlags)
                                    .Concat(legs.ED.Options.SlotFlags).ToList();

            var typeFlags = new List<SlotFlags>();

            foreach (var slotFlags in allSlotFlags) {
                var currentSlotTypes = EnumHelper<SlotFlags>.MaskToList((SlotFlags)Convert.ToInt64(slotFlags)).ToList()
                                    .Intersect(SLOT_TYPE_FLAGS).ToList();

                typeFlags.AddRange(currentSlotTypes);
            }
            

            // main Stats
            Speed = (double)input.Properties.SingleOrDefault(x=> x.Field == AggregateField.speed_max).Value * 36; // needs to be multiplied by 36
            Armor = input.ArmorMax;
            Accumulator = input.CoreMax;
            Cargo = (double) inventory.GetCapacityInfo()["capacity"];
            Cpu = input.Cpu;
            // The Max property is what we want, but it's not public and since this is a fake robot it will equal max
            Reactor = input.PowerGrid;
            
            if (input.Slope == 4) {
                SlopeCapacity = 45;
            } else if (input.Slope == 5) {
                SlopeCapacity= 51;
            } else if (input.Slope == 6) {
                SlopeCapacity= 56;
            } else {
                SlopeCapacity = input.Slope;
            }

            // Slots
            SlotsHead = head.MaxSlots;
            SlotsChassis = chassis.MaxSlots;
            SlotsLeg = legs.MaxSlots;
            SlotsGuns = typeFlags.Where(x => x == SlotFlags.turret).Count();
            SlotsMissiles = typeFlags.Where(x => x == SlotFlags.missile).Count();
            SlotsIndustrial = typeFlags.Where(x => x == SlotFlags.industrial).Count();
            SlotsMisc = typeFlags.Where(x => x == SlotFlags.ew_and_engineering).Count();

            // Defense
            SurfaceSize = input.SignatureRadius;
            ResistanceChem = input.GetPropertyModifier(AggregateField.resist_chemical).Value;
            ResistanceKinetic = input.GetPropertyModifier(AggregateField.resist_kinetic).Value;
            ResistanceSeismic = input.GetPropertyModifier(AggregateField.resist_explosive).Value;
            ResistanceThermal = input.GetPropertyModifier(AggregateField.resist_thermal).Value;
            ResistanceDemobilizer = 0; // input.GetPropertyModifier(AggregateField.effect_massivness_speed_max_modifier).Value;


            // Targeting
            SensorStrength = input.SensorStrength; // input.GetPropertyModifier(AggregateField.sensor_strength).Value;

            //  Same as input.GetPropertyModifier(AggregateField.locking_range).Value * 10;
            LockingRange = input.MaxTargetingRange * 10; 
            LockingTime = input.GetPropertyModifier(AggregateField.locking_time).Value / 1000;
            MaxLockedTargets = input.GetPropertyModifier(AggregateField.locked_targets_max).Value;
            SignalDetection = input.DetectionStrength;
            SignalMasking = input.StealthStrength;

            // Misc
            AccumulatorRecharge = input.CoreRechargeTime.TotalSeconds;
            AccumulatorStability = input.ReactorRadiation;
            InterferenceEmission = input.GetPropertyModifier(AggregateField.blob_emission).Value; // Same as input.BlobEmission but it's not public
            InterferenceRadius = input.GetPropertyModifier(AggregateField.blob_emission_radius).Value * 10;  // Same as input.BlobEmissionRadius but it's not public
            InterferenceMin = input.GetPropertyModifier(AggregateField.blob_level_low).Value;
            InterferenceMax = input.GetPropertyModifier(AggregateField.blob_level_high).Value;

            // Required Extensions
            var testExt = input.ExtensionBonusEnablerExtensions.ToList();
            var test2 = input.ED.EnablerExtensions; // Extensions required (Extension + Level)
            var test3 = input.RobotComponents.SelectMany(component => component.ExtensionBonuses).ToList(); // Bonuses from extension

            // Extension Bonuses
        }
    }
}
