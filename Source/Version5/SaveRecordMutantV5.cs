using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordMutantV5 : IExposable {
        public string def;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
        }
    }
}
