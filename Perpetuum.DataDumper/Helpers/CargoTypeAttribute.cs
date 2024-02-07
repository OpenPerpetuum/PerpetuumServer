using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class CargoTypeAttribute : System.Attribute {
        public string Type;

        public CargoTypeAttribute(string type) {
            this.Type = type;
        }
    }
}
