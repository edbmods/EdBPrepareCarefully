using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstoryNoDisabledWorkTypes : Filter<Backstory> {
        public FilterBackstoryNoDisabledWorkTypes() {
            this.Label = "EdB.PC.Dialog.Backstory.Filter.NoDisabledWorkTypes".Translate();
            this.FilterFunction = (Backstory backstory) => {
                return (backstory.DisabledWorkTypes.FirstOrDefault() == null);
            };
        }
    }
}
