using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    public static class EnumHelper<T>
    where T : struct {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Enum[] Values;
        // ReSharper restore StaticFieldInGenericType
        private static readonly T DefaultValue;

        static EnumHelper() {
            var type = typeof(T);
            if (type.IsSubclassOf(typeof(Enum)) == false) {
                throw new ArgumentException();
            }
            Values = Enum.GetValues(type).Cast<Enum>().ToArray();
            DefaultValue = default(T);
        }

        public static T[] MaskToList(Enum mask, bool ignoreDefault = true) {
            var q = Values.Where(mask.HasFlag);
            if (ignoreDefault) {
                q = q.Where(v => !v.Equals(DefaultValue));
            }
            return q.Cast<T>().ToArray();
        }
    }
}
