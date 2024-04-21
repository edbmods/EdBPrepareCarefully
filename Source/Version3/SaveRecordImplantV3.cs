using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordImplantV3 : IExposable {
        public string bodyPart = null;
        public int? bodyPartIndex = null;
        public string recipe = null;

        public SaveRecordImplantV3() {
        }

        public SaveRecordImplantV3(Implant option) {
            this.bodyPart = option.BodyPartRecord.def.defName;
            this.recipe = option.Recipe != null ? option.Recipe.defName : null;
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.bodyPart, "bodyPart", null, false);
            Scribe_Values.Look<int?>(ref this.bodyPartIndex, "bodyPartIndex", null, false);
            Scribe_Values.Look<string>(ref recipe, "recipe", null, false);
        }
    }
}

