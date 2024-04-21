using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordTitleV5 : IExposable {
        public string factionName;
        public string factionDef;
        public string titleDef;
        public int favor;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.factionName, "factionName", null, false);
            Scribe_Values.Look<string>(ref this.factionDef, "factionDef", null, false);
            Scribe_Values.Look<string>(ref this.titleDef, "titleDef", null, false);
            Scribe_Values.Look<int>(ref this.favor, "favor", 0, false);
        }
    }
}
