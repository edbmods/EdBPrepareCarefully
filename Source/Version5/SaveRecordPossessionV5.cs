using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordPossessionV5 : IExposable {
        public string thingDef;
        public int count;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.thingDef, "thingDef", null, false);
            Scribe_Values.Look<int>(ref this.count, "count", 0, false);
        }
    }
}
