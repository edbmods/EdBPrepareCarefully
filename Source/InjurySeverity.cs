using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class InjurySeverity {
        public static readonly List<InjurySeverity> PermanentInjurySeverities = new List<InjurySeverity>() {
            new InjurySeverity(2), new InjurySeverity(3), new InjurySeverity(4), new InjurySeverity(5), new InjurySeverity(6)
        };
        public InjurySeverity(float value) {
            this.value = value;
        }
        public InjurySeverity(float value, HediffStage stage) {
            this.value = value;
            this.stage = stage;
        }
        protected float value = 0;
        protected HediffStage stage = null;
        protected int? variant = null;
        public float Value {
            get {
                return this.value;
            }
        }
        public HediffStage Stage {
            get {
                return this.stage;
            }
        }
        public int? Variant {
            get {
                return this.variant;
            }
            set {
                this.variant = value;
            }
        }
        public bool SeverityRepresentsLevel {
            get; set;
        }
        public string Label {
            get {
                if (stage != null) {
                    if (SeverityRepresentsLevel) {
                        return "Level".Translate().CapitalizeFirst() + " " + (int)stage.minSeverity;
                    }
                    else if (variant == null) {
                        return stage.label.CapitalizeFirst();
                    }
                    else {
                        return "EdB.PC.Dialog.Severity.Stage.Label.".Translate(stage.label.CapitalizeFirst(), variant.Value);
                    }
                }
                else {
                    return ("EdB.PC.Dialog.Severity.OldInjury.Label." + value).Translate();
                }
            }
        }
    }
}
