using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Implant : CustomizedHediff {

        public string label = "";

        protected string tooltip;

        public Implant() {
        }

        protected BodyPartRecord bodyPartRecord;
        public override BodyPartRecord BodyPartRecord {
            get {
                return bodyPartRecord;
            }
            set {
                bodyPartRecord = value;
                tooltip = null;
            }
        }

        protected Hediff hediff = null;
        public Hediff Hediff {
            get => hediff;
            set => hediff = value;
        }

        protected HediffDef hediffDef = null;
        public HediffDef HediffDef {
            get => hediffDef;
            set => hediffDef = value;
        }


        override public string ChangeName {
            get {
                return Label;
            }
        }

        override public Color LabelColor {
            get {
                if (recipe.addsHediff != null) {
                    return recipe.addsHediff.defaultLabelColor;
                }
                else {
                    return Style.ColorText;
                }
            }
        }

        public Implant(BodyPartRecord bodyPartRecord, RecipeDef recipe) {
            this.BodyPartRecord = bodyPartRecord;
            this.recipe = recipe;
        }

        protected RecipeDef recipe = null;
        public RecipeDef Recipe {
            get {
                return recipe;
            }
            set {
                recipe = value;
                tooltip = null;
            }
        }

        public string Label {
            get {
                if (recipe == null) {
                    return "";
                }
                return recipe.addsHediff.LabelCap;
            }
        }

        public bool ReplacesPart {
            get {
                if (Recipe != null && Recipe.addsHediff != null
                        && (typeof(Hediff_AddedPart).IsAssignableFrom(Recipe.addsHediff.hediffClass)
                            || typeof(Hediff_MissingPart).IsAssignableFrom(Recipe.addsHediff.hediffClass))) {
                    return true;
                }
                return false;
            }
        }

        public override bool HasTooltip {
            get {
                return hediff != null;
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
            Hediff_Injury hediff_Injury = hediff as Hediff_Injury;
            string damageLabel = hediff.SeverityLabel;
            if (!hediff.Label.NullOrEmpty() || !damageLabel.NullOrEmpty() || !hediff.CapMods.NullOrEmpty<PawnCapacityModifier>()) {
                stringBuilder.Append(hediff.LabelCap);
                if (!damageLabel.NullOrEmpty()) {
                    stringBuilder.Append(": " + damageLabel);
                }
                stringBuilder.AppendLine();
                string tipStringExtra = hediff.TipStringExtra;
                if (!tipStringExtra.NullOrEmpty()) {
                    stringBuilder.AppendLine(tipStringExtra.TrimEndNewlines().Indented());
                }
            }
            tooltip = stringBuilder.ToString();
        }

        public override bool Equals(object obj) {
            return obj is Implant implant &&
                   EqualityComparer<BodyPartRecord>.Default.Equals(bodyPartRecord, implant.bodyPartRecord) &&
                   EqualityComparer<HediffDef>.Default.Equals(hediffDef, implant.hediffDef) &&
                   EqualityComparer<RecipeDef>.Default.Equals(recipe, implant.recipe);
        }

        public override int GetHashCode() {
            var hashCode = 223280684;
            hashCode = hashCode * -1521134295 + EqualityComparer<BodyPartRecord>.Default.GetHashCode(bodyPartRecord);
            hashCode = hashCode * -1521134295 + EqualityComparer<HediffDef>.Default.GetHashCode(hediffDef);
            hashCode = hashCode * -1521134295 + EqualityComparer<RecipeDef>.Default.GetHashCode(recipe);
            return hashCode;
        }
    }
}

