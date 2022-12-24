using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordInjuryV5 : IExposable {
        public string bodyPart = null;
        public int? bodyPartIndex = null;
        public string hediffDef = null;
        public string severity = null;
        public string painFactor = null;
        public string chemical = null;

        public SaveRecordInjuryV5() {
        }

        public SaveRecordInjuryV5(Injury injury) {
            this.bodyPart = injury?.BodyPartRecord?.def?.defName;
            this.hediffDef = injury?.Option?.HediffDef?.defName;
            if (injury.Severity != 0) {
                this.severity = injury.Severity.ToString();
            }
            if (injury.PainFactor != null) {
                this.painFactor = injury.PainFactor.Value.ToString();
            }
            if (injury.Chemical != null) {
                this.chemical = injury.Chemical.defName;
            }
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.hediffDef, "hediffDef", null, false);
            Scribe_Values.Look<string>(ref this.bodyPart, "bodyPart", null, false);
            Scribe_Values.Look<int?>(ref this.bodyPartIndex, "bodyPartIndex", null, false);
            Scribe_Values.Look<string>(ref this.severity, "severity", null, false);
            Scribe_Values.Look<string>(ref this.painFactor, "painFactor", null, false);
            Scribe_Values.Look<string>(ref this.chemical, "chemical", null, false);
        }

        public float Severity {
            get {
                return float.Parse(severity);
            }
        }
        public float PainFactor {
            get {
                return float.Parse(painFactor);
            }
        }
    }
}

