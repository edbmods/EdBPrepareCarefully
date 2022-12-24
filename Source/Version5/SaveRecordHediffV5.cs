using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordHediffV5 : IExposable {
        public Pawn Pawn { get; set; }
        public Hediff Hediff { get; set; }
        public void ExposeData() {
            if (Scribe.mode == LoadSaveMode.Saving && Hediff != null) {
                if (Hediff != null) {
                    Hediff.ExposeData();
                }
            }
        }
    }
}
