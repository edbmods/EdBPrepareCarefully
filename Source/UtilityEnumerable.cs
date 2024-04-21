using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityEnumerable {
        public static bool Equals<T>(IEnumerable<T> a, IEnumerable<T> b) {
            if (ReferenceEquals(a, b)) {
                return true;
            }
            if (a == null || b == null) {
                return false;
            }
            return Enumerable.SequenceEqual(a, b);
        }
    }
}
