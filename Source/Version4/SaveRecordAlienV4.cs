using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordAlienV4 : IExposable {
        public string crownType;
        public Color skinColor;
        public Color skinColorSecond;
        public Color hairColorSecond;

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.crownType, "crownType", "", false);
            Scribe_Values.Look<Color>(ref this.skinColor, "skinColor", Color.white, false);
            Scribe_Values.Look<Color>(ref this.skinColorSecond, "skinColorSecond", Color.white, false);
            Scribe_Values.Look<Color>(ref this.hairColorSecond, "hairColorSecond", Color.white, false);
        }
    }
}
