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
                PawnKindDef kindDef = pawn.Pawn.kindDef;
                HashSet<string> pawnKindBackstoryCategories = new HashSet<string>(kindDef.backstoryCategories);
                if (kindDef.backstoryCategories == null || kindDef.backstoryCategories.Count == 0) {
                    return true;
                }
                foreach (var c in backstory.spawnCategories) {
                    if (pawnKindBackstoryCategories.Contains(c)) {
                        return true;
                    }
                }
                return false;
            };
        }
    }
}
