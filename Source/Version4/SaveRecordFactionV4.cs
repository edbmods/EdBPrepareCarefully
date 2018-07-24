using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordFactionV4 : IExposable {
        public string def = null;
        public int? index = null;
        public bool leader;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
            Scribe_Values.Look<int?>(ref this.index, "index", null, false);
            Scribe_Values.Look<bool>(ref this.leader, "leader", false, false);
        }
    }
}
