using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionTattoo : PawnLayerOption {
        public override string Label {
            get {
                return TattooDef.LabelCap;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public TattooDef TattooDef {
            get;
            set;
        }
    }
}
