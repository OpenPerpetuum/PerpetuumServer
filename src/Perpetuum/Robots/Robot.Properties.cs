using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Units;

namespace Perpetuum.Robots
{
    partial class Robot
    {
        private UnitOptionalProperty<int> decay;
        private UnitOptionalProperty<Color> tint;

        private ItemProperty powerGridMax;
        private ItemProperty powerGrid;
        private ItemProperty cpuMax;
        private ItemProperty cpu;
        private ItemProperty ammoReloadTime;
        private ItemProperty missileHitChance;
        private ItemProperty decayChance;
        private ItemProperty mineDetectionRange;
        private ItemProperty camouflage;

        private void InitProperties()
        {
            decay = new UnitOptionalProperty<int>(this, UnitDataType.Decay, k.decay, () => 255);
            decay.PropertyChanged += _ =>
            {
                camouflage.SetValue(CamouflageBonus());
            };
            OptionalProperties.Add(decay);

            tint = new UnitOptionalProperty<Color>(this,UnitDataType.Tint,k.tint,() => ED.Config.Tint);
            OptionalProperties.Add(tint);

            powerGridMax = new UnitProperty(this, AggregateField.powergrid_max, AggregateField.powergrid_max_modifier);
            AddProperty(powerGridMax);

            powerGrid = new PowerGridProperty(this);
            AddProperty(powerGrid);

            cpuMax = new UnitProperty(this, AggregateField.cpu_max, AggregateField.cpu_max_modifier);
            AddProperty(cpuMax);

            cpu = new CpuProperty(this);
            AddProperty(cpu);

            ammoReloadTime = new UnitProperty(this, AggregateField.ammo_reload_time, AggregateField.ammo_reload_time_modifier);
            AddProperty(ammoReloadTime);

            missileHitChance = new UnitProperty(this, AggregateField.missile_miss, AggregateField.missile_miss_modifier);
            AddProperty(missileHitChance);

            decayChance = new DecayChanceProperty(this);
            AddProperty(decayChance);

            mineDetectionRange = new UnitProperty(
                this,
                AggregateField.mine_detection_range,
                AggregateField.undefined,
                AggregateField.effect_mine_detection_range_modifier);
            AddProperty(mineDetectionRange);

            camouflage = new UnitProperty(this, AggregateField.stealth_strength_modifier);
            camouflage.PropertyChanged += _ =>
            { UpdateTypes |= UnitUpdateTypes.Stealth; };
            AddProperty(camouflage);
        }

        private double PowerGridMax
        {
            get { return powerGridMax.Value; }
        }

        public double PowerGrid
        {
            get { return powerGrid.Value; }
        }

        private double CpuMax
        {
            get { return cpuMax.Value; }
        }

        public double Cpu
        {
            get { return cpu.Value; }
        }

        public TimeSpan AmmoReloadTime
        {
            get { return TimeSpan.FromMilliseconds(ammoReloadTime.Value); }
        }

        public double MissileHitChance
        {
            get { return missileHitChance.Value; }
        }

        public int Decay
        {
            private get { return decay.Value; }
            set { decay.Value = value & 255; }
        }

        public Color Tint
        {
            get { return tint.Value; }
            set { tint.Value = value; }
        }

        public double MineDetectionRange
        {
            get { return mineDetectionRange.Value; }
        }

        public override double StealthStrength => base.StealthStrength + camouflage.Value;

        public override void UpdateRelatedProperties(AggregateField field)
        {
            foreach (var component in RobotComponents)
            {
                component.UpdateRelatedProperties(field);
            }

            base.UpdateRelatedProperties(field);
        }

        public override Dictionary<string, object> BuildPropertiesDictionary()
        {
            var result = new Dictionary<string,object>();

            foreach (var component in RobotComponents)
            {
                var d = component.BuildPropertiesDictionary();
                result.AddRange(d);
            }

            // hogy felulirja a defaultokat
            result.AddRange(base.BuildPropertiesDictionary());

            return result;
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            var modifier = base.GetPropertyModifier(field);

            foreach (var component in RobotComponents)
            {
                var m = component.GetPropertyModifier(field);
                m.Modify(ref modifier);
            }

            return modifier;
        }

        public bool CheckPowerGridForModule(Module module, bool removing=false)
        {
            return SimulateFitting(module, removing, PowerGridMax, TotalPowerGridUsage, AggregateField.powergrid_usage, AggregateField.powergrid_max_modifier);
        }

        public bool CheckCpuForModule(Module module, bool removing = false)
        {
            return SimulateFitting(module, removing, CpuMax, TotalCpuUsage, AggregateField.cpu_usage, AggregateField.cpu_max_modifier);
        }

        private bool SimulateFitting(Module module, bool removing, double max, double current, AggregateField usageField, AggregateField maxModField)
        {
            double moduleUsageEstimate = 0;
            var itemMod = module.BasePropertyModifiers.GetPropertyModifier(usageField);
            module.SimulateRobotPropertyModifiers(this, ref itemMod);
            itemMod.Modify(ref moduleUsageEstimate);
            moduleUsageEstimate = removing ? -moduleUsageEstimate : moduleUsageEstimate;
            if (removing && module.BasePropertyModifiers.GetPropertyModifier(maxModField).HasValue)
            {
                var mod = module.BasePropertyModifiers.GetPropertyModifier(maxModField).Value;
                max /= Math.Max(mod, 1);
            }
            return current + moduleUsageEstimate <= max;
        }

        public double TotalPowerGridUsage
        {
            get { return Modules.Sum(m => m.PowerGridUsage); }
        }

        public double TotalCpuUsage
        {
            get { return Modules.Sum(m => m.CpuUsage); }
        }

        private class DecayChanceProperty : UnitProperty
        {
            public DecayChanceProperty(Unit owner) : base(owner, AggregateField.decay_chance) { }

            protected override double CalculateValue()
            {
                var v = 20 / owner.SignatureRadius * 0.01;
                return v;
            }
        }

        private class PowerGridProperty : UnitProperty
        {
            private readonly Robot _owner;

            public PowerGridProperty(Robot owner)
                : base(owner, AggregateField.powergrid_current)
            {
                _owner = owner;
            }

            protected override double CalculateValue()
            {
                return _owner.PowerGridMax - _owner.TotalPowerGridUsage;
            }
        }

        private class CpuProperty : UnitProperty
        {
            private readonly Robot _owner;

            public CpuProperty(Robot owner)
                : base(owner, AggregateField.cpu_current)
            {
                _owner = owner;
            }

            protected override double CalculateValue()
            {
                return _owner.CpuMax - _owner.TotalCpuUsage;
            }
        }

        protected virtual double CamouflageBonus()
        {
            // Average value of color components.
            double average = (Tint.R + Tint.G + Tint.B) / 3.0;
            // Faction island color.
            double oneColor;

            // Bonus depending on the faction of the island.
            switch (Zone.Configuration.RaceId)
            {
                // Pelistal
                case 1:
                    oneColor = Tint.G;
                    break;
                // Nuimqol
                case 2:
                    oneColor = Tint.B;
                    break;
                // Thelodica
                case 3:
                    oneColor = Tint.R;
                    break;
                // Default for 0.00rF .
                default:
                    oneColor = average;
                    break;
            }

            // Main camouflage formula.
            return 25.974 * (oneColor - average) / Math.Pow(average + 12, 2) * (Decay / 255.0);
        }

        private void CamouflageUpdate()
        {
            camouflage.SetValue(CamouflageBonus());
        }
    }
}
