using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryMatchesFaction : Filter<BackstoryDef> {
        public ViewState ViewState { get; set; }
        public ProviderBackstories ProviderBackstories { get; set; }

        public FilterBackstoryMatchesFaction() {
            this.LabelShort = this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.MatchesFaction".Translate();
            this.FilterFunction = (BackstoryDef backstory) => {
                CustomizedPawn pawn = ViewState.CurrentPawn;
                PawnKindDef kindDef = pawn?.Pawn.kindDef;
                if (kindDef == null) {
                    kindDef = PawnKindDefOf.Colonist;
                }
                var set = ProviderBackstories.BackstoriesForPawnKindDef(kindDef);
                if (set == null) {
                    return false;
                }
                return set.Contains(backstory);
            };
        }
    }
}
