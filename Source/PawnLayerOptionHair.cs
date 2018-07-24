using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionHair : PawnLayerOption {
        public override string Label {
            get {
                return HairDef.LabelCap;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public HairDef HairDef {
            get;
            set;
        }
    }
}
