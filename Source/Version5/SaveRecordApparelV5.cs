using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordApparelV5 : IExposable {
        public string apparel = null;
        public string style = null;
        public string stuff = null;
        public string quality = null;
        public float? hitPoints = null;
        public Color? color;

        public void ExposeData() {
            Scribe_Values.Look(ref this.apparel, "apparel", null, true);
            Scribe_Values.Look(ref this.style, "style", null, false);
            Scribe_Values.Look(ref this.stuff, "stuff", null, false);
            Scribe_Values.Look(ref quality, "quality", null, false);
            Scribe_Values.Look(ref hitPoints, "hitPoints", null, false);
            Scribe_Values.Look(ref this.color, "color", null, false);
        }
    }
}
