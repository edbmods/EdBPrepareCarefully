using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class ViewState {
        public CustomizedPawn CurrentPawn { get; set; }
        public PawnListMode PawnListMode { get; set; } = PawnListMode.ColonyPawnsMaximized;
        public FactionDef LastSelectedFactionDef { get; set; }
        public PawnKindOption LastSelectedPawnKindDef { get; set; }
        public Rot4 PawnViewRotation { get; set; } = Rot4.South;
        public bool PointsEnabled { get; set; } = false;
        public bool CostCalculationDirtyFlag { get; set; } = true;

        public Dictionary<CustomizedPawn, PawnRandomizerOptions> PawnRandomizerOptions = new Dictionary<CustomizedPawn, PawnRandomizerOptions>();

        public string Filename { get; set; } = "";
    }
}
