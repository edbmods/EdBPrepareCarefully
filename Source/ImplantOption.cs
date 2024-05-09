using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class ImplantOption {
        public RecipeDef RecipeDef { get; set; }
        public HediffDef HediffDef { get; set; }
        public HediffDef Dependency { get; set; }
        public ThingDef ThingDef { get; set; }
        public BodyPartRecord BodyPartRecord { get; set; }
        public HashSet<BodyPartDef> BodyPartDefs { get; set; }
        public float MinSeverity { get; set; } = 0;
        public float MaxSeverity { get; set; } = 0;
        public bool SupportsSeverity {
            get {
                return MaxSeverity > 0;
            }
        }
    }
}
