using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Injury : CustomizedHediff {


        public Hediff Hediff { get; set; }

        public string tooltip;

        public InjuryOption option;
        public InjuryOption Option {
            get {
                return option;
            }
            set {
                option = value;
                tooltip = null;
                stageLabel = ComputeStageLabel();
            }
        }



        protected string stageLabel = null;

        public float? PainFactor { get; set; }

        public ChemicalDef Chemical { get; set; }

        protected float severity = 0;
        public float Severity {
            get {
                return severity;
            }
            set {
                tooltip = null;
                severity = value;
                stageLabel = ComputeStageLabel();
            }
        }

        public Injury() {
        }

        public BodyPartRecord bodyPartRecord;
        override public BodyPartRecord BodyPartRecord {
            get {
                return bodyPartRecord;
            }
            set {
                bodyPartRecord = value;
            }
        }

        override public string ChangeName {
            get {
                if (stageLabel != null) {
                    return stageLabel;
                }
                else if (Option != null) {
                    return Option.Label;
                }
                else {
                    return "?";
                }
            }
        }

        override public Color LabelColor {
            get {
                if (Option != null && Option.HediffDef != null) {
                    return Option.IsOldInjury ? Color.gray : Option.HediffDef.defaultLabelColor;
                }
                else {
                    return Style.ColorText;
                }
            }
        }

        // EVERY RELEASE:
        // Check the PainCategory enum to verify that we still only have 4 values and that their int values match the logic here.
        // This method converts a float value into a PainCategory.  It's here because we don't quite remember where that float
        // value comes from and if it contain a value that won't map to one of the PainCategory enum values.
        // Unchanged for 1.14
        protected PainCategory PainCategoryForFloat(float value) {
            int intValue = Mathf.FloorToInt(value);
            if (intValue == 2) {
                intValue = 1;
            }
            else if (intValue > 3 && intValue < 6) {
                intValue = 3;
            }
            else if (intValue > 6) {
                intValue = 6;
            }
            return (PainCategory)intValue;
        }

        protected string ComputeStageLabel() {
            if (Option.HasStageLabel) {
                return "EdB.PC.Panel.Health.InjuryLabel.Stage".Translate(this.option.Label, CurStage.label);
            }
            else if (Option.IsOldInjury) {
                return "EdB.PC.Panel.Health.InjuryLabel.Severity".Translate(this.option.Label, (int)severity);
            }
            else {
                return null;
            }
        }

        protected HediffStage CurStage {
            get {
                return (!this.option.HediffDef.stages.NullOrEmpty<HediffStage>()) ? this.option.HediffDef.stages[this.CurStageIndex] : null;
            }
        }

        protected int CurStageIndex {
            get {
                if (this.option.HediffDef.stages == null) {
                    return 0;
                }
                List<HediffStage> stages = this.option.HediffDef.stages;
                for (int i = stages.Count - 1; i >= 0; i--) {
                    if (this.Severity >= stages[i].minSeverity) {
                        return i;
                    }
                }
                return 0;
            }
        }

        public override bool HasTooltip {
            get {
                return Hediff != null;
            }
        }

        public override string Tooltip {
            get {
                if (tooltip == null) {
                    InitializeTooltip();
                }
                return tooltip;
            }
        }

        protected void InitializeTooltip() {
            StringBuilder stringBuilder = new StringBuilder();
            if (Hediff.Part != null) {
                stringBuilder.Append(Hediff.Part.def.LabelCap + ": ");
                stringBuilder.Append(" " + Hediff.pawn.health.hediffSet.GetPartHealth(Hediff.Part).ToString() + " / " + Hediff.Part.def.GetMaxHealth(Hediff.pawn).ToString());
            }
            else {
                stringBuilder.Append("WholeBody".Translate());
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("------------------");
            Hediff_Injury hediff_Injury = Hediff as Hediff_Injury;
            string damageLabel = Hediff.SeverityLabel;
            if (!Hediff.Label.NullOrEmpty() || !damageLabel.NullOrEmpty() || !Hediff.CapMods.NullOrEmpty<PawnCapacityModifier>()) {
                stringBuilder.Append(Hediff.LabelCap);
                if (!damageLabel.NullOrEmpty()) {
                    stringBuilder.Append(": " + damageLabel);
                }
                stringBuilder.AppendLine();
                string tipStringExtra = Hediff.TipStringExtra;
                if (!tipStringExtra.NullOrEmpty()) {
                    stringBuilder.AppendLine(tipStringExtra.TrimEndNewlines().Indented());
                }
            }
            tooltip = stringBuilder.ToString();
        }

        public override bool Equals(object obj) {
            return obj is Injury injury &&
                   EqualityComparer<InjuryOption>.Default.Equals(option, injury.option) &&
                   PainFactor == injury.PainFactor &&
                   EqualityComparer<ChemicalDef>.Default.Equals(Chemical, injury.Chemical) &&
                   severity == injury.severity &&
                   EqualityComparer<BodyPartRecord>.Default.Equals(bodyPartRecord, injury.bodyPartRecord) &&
                   EqualityComparer<HediffStage>.Default.Equals(CurStage, injury.CurStage);
        }

        public override int GetHashCode() {
            var hashCode = 662792533;
            hashCode = hashCode * -1521134295 + EqualityComparer<InjuryOption>.Default.GetHashCode(option);
            hashCode = hashCode * -1521134295 + PainFactor.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ChemicalDef>.Default.GetHashCode(Chemical);
            hashCode = hashCode * -1521134295 + severity.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<BodyPartRecord>.Default.GetHashCode(bodyPartRecord);
            hashCode = hashCode * -1521134295 + EqualityComparer<HediffStage>.Default.GetHashCode(CurStage);
            return hashCode;
        }
    }
}

