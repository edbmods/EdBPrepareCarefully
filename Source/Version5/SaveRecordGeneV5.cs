using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordGeneV5 : IExposable {
        public string def;
        public string overriddenByEndogene;
        public string overriddenByXenogene;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
            Scribe_Values.Look<string>(ref this.overriddenByEndogene, "overriddenByEndogene", null, false);
            Scribe_Values.Look<string>(ref this.overriddenByXenogene, "overriddenByXenogene", null, false);
        }
    }
}
