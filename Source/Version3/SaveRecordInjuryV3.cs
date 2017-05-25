using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordInjuryV3 : IExposable {
        public string bodyPart = null;
        public string hediffDef = null;
        public string severity = null;
        public string painFactor = null;

        public SaveRecordInjuryV3() {
        }

        public SaveRecordInjuryV3(Injury injury) {
            this.bodyPart = injury.BodyPartRecord != null ? injury.BodyPartRecord.def.defName : null;
            this.hediffDef = injury.Option.HediffDef != null ? injury.Option.HediffDef.defName : null;
            if (injury.Severity != 0) {
                this.severity = injury.Severity.ToString();
            }
            if (injury.PainFactor != null) {
                this.painFactor = injury.PainFactor.Value.ToString();
            }
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.hediffDef, "hediffDef", null, false);
            Scribe_Values.Look<string>(ref this.bodyPart, "bodyPart", null, false);
            Scribe_Values.Look<string>(ref this.severity, "severity", null, false);
            Scribe_Values.Look<string>(ref this.painFactor, "painFactor", null, false);
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

