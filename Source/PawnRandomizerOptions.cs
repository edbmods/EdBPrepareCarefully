using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnRandomizerOptions {
        public XenotypeDef Xenotype { get; set; }
        public CustomXenotype CustomXenotype { get; set; }
        public bool AnyNonArchite { get; set; }
        public DevelopmentalStage DevelopmentalStage { get; set; }
    }
}
