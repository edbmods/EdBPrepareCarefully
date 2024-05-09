using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Injury : CustomizedHediff {

        public InjuryOption Option { get; set; }
        public Hediff Hediff { get; set; }
        public float Severity { get; set; } = 0;
        public float? PainFactor { get; set; }
        public ChemicalDef Chemical { get; set; }

        public Injury() {
        }

        protected HediffStage CurStage {
            get {
                return (!this.Option.HediffDef.stages.NullOrEmpty<HediffStage>()) ? this.Option.HediffDef.stages[this.CurStageIndex] : null;
            }
        }

        protected int CurStageIndex {
            get {
                if (this.Option.HediffDef.stages == null) {
                    return 0;
                }
                List<HediffStage> stages = this.Option.HediffDef.stages;
                for (int i = stages.Count - 1; i >= 0; i--) {
                    if (this.Severity >= stages[i].minSeverity) {
                        return i;
                    }
                }
                return 0;
            }
        }

        public override bool Equals(object obj) {
            return obj is Injury injury &&
                   EqualityComparer<InjuryOption>.Default.Equals(Option, injury.Option) &&
                   PainFactor == injury.PainFactor &&
                   EqualityComparer<ChemicalDef>.Default.Equals(Chemical, injury.Chemical) &&
                   Severity == injury.Severity &&
                   EqualityComparer<BodyPartRecord>.Default.Equals(BodyPartRecord, injury.BodyPartRecord) &&
                   EqualityComparer<HediffStage>.Default.Equals(CurStage, injury.CurStage);
        }

        public override int GetHashCode() {
            var hashCode = 662792533;
            hashCode = hashCode * -1521134295 + EqualityComparer<InjuryOption>.Default.GetHashCode(Option);
            hashCode = hashCode * -1521134295 + PainFactor.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ChemicalDef>.Default.GetHashCode(Chemical);
            hashCode = hashCode * -1521134295 + Severity.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<BodyPartRecord>.Default.GetHashCode(BodyPartRecord);
            hashCode = hashCode * -1521134295 + EqualityComparer<HediffStage>.Default.GetHashCode(CurStage);
            return hashCode;
        }
    }
}

