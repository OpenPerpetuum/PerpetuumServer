namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        public class ActiveModuleDataView : ModuleDataView {
            // These are nullable because some items may be
            // in a group of active modules but are themselves
            // passive and we don't want to show 0 for them
            public double? ModuleAccumulator { get; set; }
            public double? ModuleCycle { get; set; }
            public double? ModuleOptimalRange { get; set; }
        }

    }
}
