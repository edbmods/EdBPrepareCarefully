using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordIdeoV5 : IExposable {
        public string name;
        public float certainty;
        public bool sameAsColony;
        public List<string> memes;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.name, "name", null, false);
            Scribe_Values.Look<float>(ref this.certainty, "certainty", 0.85f, false);
            Scribe_Values.Look<bool>(ref this.sameAsColony, "sameAsColony", true, false);
            Scribe_Collections.Look<string>(ref this.memes, "memes");
        }
    }
}
