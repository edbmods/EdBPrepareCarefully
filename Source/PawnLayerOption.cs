using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdB.PrepareCarefully {
    public abstract class PawnLayerOption {
        public abstract string Label {
            get;
            set;
        }
        public virtual bool Selectable { get; set; } = true;
    }
}
