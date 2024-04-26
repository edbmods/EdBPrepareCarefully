using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordValueV5 : IExposable {
        public string name;
        public int? intValue;
        public float? floatValue;
        public string stringValue;
        public bool? boolValue;

        public void ExposeData() {
            Scribe_Values.Look(ref this.name, "name", null, true);
            Scribe_Values.Look(ref this.intValue, "intValue", null, false);
            Scribe_Values.Look(ref this.floatValue, "floatValue", null, false);
            Scribe_Values.Look(ref this.stringValue, "stringValue", null, false);
            Scribe_Values.Look(ref this.boolValue, "boolValue", null, false);
        }
    }
}
