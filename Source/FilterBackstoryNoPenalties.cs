using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryNoPenalties : Filter<BackstoryDef> {
        public FilterBackstoryNoPenalties() {
            this.LabelShort = this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.NoSkillPenalties".Translate();
            this.FilterFunction = (backstory) => {
                return !backstory.HasSkillPenalties();
            };
        }
    }
}
