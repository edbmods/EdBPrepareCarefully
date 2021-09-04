using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionBeard : PawnLayerOption {
        public override string Label {
            get {
                return BeardDef.LabelCap;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public BeardDef BeardDef {
            get;
            set;
        }
    }
}
