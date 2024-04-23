using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionBody : PawnLayerOption {
        public override string Label { get; set; }
        public BodyTypeDef BodyTypeDef { get; set; }
    }
}
