using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordGenesV5 : IExposable {
        public string xenotypeDef;
        public string customXenotypeName;
        public bool uniqueXenotype;
        public List<string> endogenes;
        public List<string> xenogenes;
        public List<SaveRecordGeneV5> endogeneRecords;
        public List<SaveRecordGeneV5> xenogeneRecords;

        public SaveRecordGenesV5() {
        }

        public void ExposeData() {
            Scribe_Values.Look(ref this.xenotypeDef, "xenotypeDef", null, false);
            Scribe_Values.Look(ref this.customXenotypeName, "customXenotypeName", null, false);
            Scribe_Values.Look(ref this.uniqueXenotype, "uniqueXenotype", false);
            Scribe_Collections.Look(ref this.endogenes, "endogenes");
            Scribe_Collections.Look(ref this.xenogenes, "xenogenes");
            Scribe_Collections.Look(ref this.endogeneRecords, "endogeneRecords");
            Scribe_Collections.Look(ref this.xenogeneRecords, "xenogeneRecords");
        }
    }
}
