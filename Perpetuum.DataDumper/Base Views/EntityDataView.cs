using System.Collections.Generic;

namespace Perpetuum.DataDumper {
    public class EntityDataView {
        public string ItemName { get; set; } // This should actually be renamed...
        public string ItemKey { get; set; }
        public List<string> ItemCategories { get; set; }
    }
}
