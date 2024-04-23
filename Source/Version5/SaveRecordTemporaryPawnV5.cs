using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordTemporaryPawnV5 : IExposable {
        public string id;
        public string gender;
        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.id, "id", null, false);
            Scribe_Values.Look<string>(ref this.gender, "gender", null, false);
        }
    }
}
