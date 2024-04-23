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
        public string stuff = null;
        public string quality = null;
        public float? hitPoints = null;
        public Color? color;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.apparel, "apparel", "", false);
            Scribe_Values.Look<string>(ref this.stuff, "stuff", "", false);
            Scribe_Values.Look<string>(ref quality, "quality", "", false);
            Scribe_Values.Look<float?>(ref hitPoints, "hitPoints", null, false);
            Scribe_Values.Look<Color?>(ref this.color, "color", null, false);
        }
    }
}
