using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EdB.PrepareCarefully {
    public static class ExtensionsEnumerable {
        public static string CommaDelimitedList(this IEnumerable<object> e) {
            if (e == null) {
                return "null";
            }
            return string.Join(", ", e);
        }
    }
}
