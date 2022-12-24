using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordImplantV5 : IExposable {
        public string bodyPart = null;
        public int? bodyPartIndex = null;
        public string recipe = null;
        public string hediffDef = null;

        public SaveRecordImplantV5() {
        }

        public SaveRecordImplantV5(Implant option) {
            this.bodyPart = option.BodyPartRecord.def.defName;
            this.recipe = option.recipe != null ? option.recipe.defName : null;
            this.hediffDef = option?.Hediff?.def?.defName;
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.bodyPart, "bodyPart", null, false);
            Scribe_Values.Look<int?>(ref this.bodyPartIndex, "bodyPartIndex", null, false);
            Scribe_Values.Look<string>(ref recipe, "recipe", null, false);
            Scribe_Values.Look<string>(ref hediffDef, "hediff", null, false);
        }
    }
}

