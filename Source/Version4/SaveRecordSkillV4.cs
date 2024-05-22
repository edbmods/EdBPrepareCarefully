using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordSkillV4 : IExposable {
        public string name;
        public int value;
        public string passion;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.name, "name", null, true);
            Scribe_Values.Look<int>(ref this.value, "value", 0, true);
            Scribe_Values.Look<string>(ref this.passion, "passion", "None", true);
        }
    }
}
