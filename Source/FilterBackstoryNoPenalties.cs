using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryNoPenalties : Filter<Backstory> {
        public FilterBackstoryNoPenalties() {
            this.Label = "EdB.PC.Dialog.Backstory.Filter.NoSkillPenalties".Translate();
            this.FilterFunction = (backstory) => {
                if (backstory.skillGainsResolved.Count == 0) {
                    return true;
                }
                foreach (var gain in backstory.skillGainsResolved.Values) {
                    if (gain < 0) {
                        return false;
                    }
                }
                return true;
            };
        }
    }
}
