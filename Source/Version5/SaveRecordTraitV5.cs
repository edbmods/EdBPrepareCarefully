using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordTraitV5 : IExposable {
        public string def;
        public int degree;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
            Scribe_Values.Look<int>(ref this.degree, "degree", 0, false);
        }
    }
}
