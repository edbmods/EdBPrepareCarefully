using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryMatchesFaction : Filter<Backstory> {
        public FilterBackstoryMatchesFaction() {
            this.LabelShort = this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.MatchesFaction".Translate();
            this.FilterFunction = (Backstory backstory) => {
                CustomPawn pawn = PrepareCarefully.Instance.State.CurrentPawn;
                PawnKindDef kindDef = pawn.OriginalKindDef;
                if (kindDef == null) {
                    kindDef = PawnKindDefOf.Colonist;
                }
                var set = PrepareCarefully.Instance.Providers.Backstories.BackstoriesForPawnKindDef(kindDef);
                if (set == null) {
                    return false;
                }
                return set.Contains(backstory);
            };
        }
    }
}
