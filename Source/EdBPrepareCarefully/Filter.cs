using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdB.PrepareCarefully {
    public class Filter<T> {
        public string LabelShort {
            get; set;
        }
        public string LabelFull {
            get; set;
        }
        public Func<T, bool> FilterFunction {
            get; set;
        }
        public virtual bool ConflictsWith(Filter<T> filter) {
            return false;
        }
    }
}
