using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdB.PrepareCarefully {
    public class Filter<T> {
        public string Label {
            get; set;
        }
        public Func<T, bool> FilterFunction {
            get; set;
        }
    }
}
