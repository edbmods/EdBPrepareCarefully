using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordApparelV4 : IExposable {
        public string layer = null;
        public string apparel = null;
        public string stuff = null;
        public Color color;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.layer, "layer", "", false);
            Scribe_Values.Look<string>(ref this.apparel, "apparel", "", false);
            Scribe_Values.Look<string>(ref this.stuff, "stuff", "", false);
            Scribe_Values.Look<Color>(ref this.color, "color", Color.white, false);
        }
    }
}
