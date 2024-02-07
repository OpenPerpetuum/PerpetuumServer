using System.Collections.Generic;

namespace Perpetuum.DataDumper {
    public partial class DataDumper
    {
        public class ModuleDataView : ItemDataView {
            public string ModuleTier { get; set; }
            public double ModuleCpu { get; set; }
            public double ModuleReactor { get; set; }
            public List<string> ExtensionsRequired { get; set; }
        }

    }
}
