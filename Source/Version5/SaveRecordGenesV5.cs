using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordGenesV5 : IExposable {
        public string xenotypeDef;
        public string customXenotypeName;
        public List<string> endogenes;
        public List<string> xenogenes;

        public SaveRecordGenesV5() {
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.xenotypeDef, "xenotypeDef", null, false);
            Scribe_Values.Look<string>(ref this.customXenotypeName, "customXenotypeName", null, false);
            Scribe_Collections.Look<string>(ref this.endogenes, "endogenes");
            Scribe_Collections.Look<string>(ref this.xenogenes, "xenogenes");
        }
    }
}
