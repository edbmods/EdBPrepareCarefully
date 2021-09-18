using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class InjuryOption {
        protected HediffDef hediffDef;
        protected HediffGiver hediffGiver;
        protected bool oldInjury = false;
        protected bool removesPart = false;
        protected bool wholeBody = false;
        protected string label = "?";

        public bool UsesSeverityPercentile {
            get {
                return hediffDef?.stages != null && hediffDef.stages.Count > 0;
            }
        }

        protected List<BodyPartDef> validParts = null;

        public InjuryOption() {
        }

        public bool Warning { get; set; } = false;

        public HediffDef HediffDef {
            get { return hediffDef; }
            set { hediffDef = value; }
        }

        public HediffGiver Giver {
            get { return hediffGiver; }
            set { hediffGiver = value; }
        }

        public bool IsOldInjury {
            get { return oldInjury; }
            set { oldInjury = value; }
        }

        public bool IsAddiction {
            get {
                if (hediffDef.hediffClass != null && typeof(Hediff_Addiction).IsAssignableFrom(hediffDef.hediffClass)) {
                    return true;
                }
                return false;
            }
        }

        public bool RemovesPart {
            get { return removesPart; }
            set { removesPart = value; }
        }
        
        public string Label {
            get { return label; }
            set { label = value; }
        }

        public bool WholeBody {
            get { return wholeBody; }
            set { wholeBody = value; }
        }

        public List<BodyPartDef> ValidParts {
            get { return validParts; }
            set { validParts = value; }
        }

        public List<UniqueBodyPart> UniqueParts { get; set; }

        public bool HasStageLabel {
            get {
                if (hediffDef.stages == null || hediffDef.stages.Count <= 1) {
                    return false;
                }
                if (IsAddiction) {
                    return false;
                }
                return true;
            }
        }

        protected List<InjurySeverity> severityStages = null;

        protected void InitializeLevels() {
            severityStages = new List<InjurySeverity>();
            foreach (var stage in HediffDef.stages) {
                severityStages.Add(new InjurySeverity(stage.minSeverity, stage) {
                    SeverityRepresentsLevel = true
                });
            }
        }

        protected void InitializeSeverityStages() {
            if (typeof(Hediff_Level).IsAssignableFrom(this.hediffDef.hediffClass)) {
                InitializeLevels();
                return;
            }

            severityStages = new List<InjurySeverity>();

            int variant = 1;
            InjurySeverity previous = null;

            foreach (var stage in HediffDef.stages) {
                // Filter out a stage if it will definitely kill the pawn.
                if (HediffDef.DoesStageDefinitelyKillPawn(stage)) {
                    continue;
                }
                // Filter out hidden stages.
                if (!stage.becomeVisible) {
                    continue;
                }
                InjurySeverity value = null;
                if (stage.minSeverity == 0) {
                    float severity = HediffDef.initialSeverity > 0 ? HediffDef.initialSeverity : 0.001f;
                    value = new InjurySeverity(severity, stage);
                }
                else {
                    value = new InjurySeverity(stage.minSeverity, stage);
                }
                if (previous == null) {
                    previous = value;
                    variant = 1;
                }
                else {
                    if (previous.Stage.label == stage.label) {
                        previous.Variant = variant;
                        variant++;
                        value.Variant = variant;
                    }
                    else {
                        previous = value;
                        variant = 1;
                    }
                }
                severityStages.Add(value);
            }
        }

        public IEnumerable<InjurySeverity> SeverityOptions() {
            if (HediffDef.hediffClass != null && typeof(Hediff_Addiction).IsAssignableFrom(HediffDef.hediffClass)) {
                yield break;
            }
            if (HediffDef.hediffClass == typeof(Hediff_MissingPart)) {
                yield break;
            }
            if (HediffDef.stages == null || HediffDef.stages.Count == 0) {
                foreach (var severity in InjurySeverity.PermanentInjurySeverities) {
                    yield return severity;
                }
                yield break;
            }

            if (severityStages == null) {
                InitializeSeverityStages();
            }
            if (severityStages.Count > 0) {
                foreach (var stage in severityStages) {
                    yield return stage;
                }
                yield break;
            }
        }
    }
}

