using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryNoDisabledWorkTypes : Filter<BackstoryDef> {
        public FilterBackstoryNoDisabledWorkTypes() {
            this.LabelShort = this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.NoDisabledWorkTypes".Translate();
            this.FilterFunction = (BackstoryDef backstory) => {
                return (backstory.DisabledWorkTypes.FirstOrDefault() == null);
            };
        }
    }
}
