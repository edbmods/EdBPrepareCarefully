using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Implant : CustomBodyPart {
        protected BodyPartRecord bodyPartRecord;

        public string label = "";
        public RecipeDef recipe = null;
        protected Hediff hediff = null;
        protected HediffDef hediffDef = null;

        protected string tooltip;

        public Implant() {
        }

        public override BodyPartRecord BodyPartRecord {
            get {
                return bodyPartRecord;
            }
            set {
                bodyPartRecord = value;
                tooltip = null;
            }
        }

        public Hediff Hediff {
            get => hediff;
            set => hediff = value;
        }

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

        public override bool Equals(System.Object obj) {
            if (obj == null) {
                return false;
            }

            Implant option = obj as Implant;
            if ((System.Object)option == null) {
                return false;
            }

            return (BodyPartRecord == option.BodyPartRecord) && (recipe == option.recipe) && (hediffDef == option.hediffDef);
        }

        public bool Equals(Implant option) {
            if ((object)option == null) {
                return false;
            }

            return (BodyPartRecord == option.BodyPartRecord) && (recipe == option.recipe) && (hediffDef == option.hediffDef);
        }

        public override void AddToPawn(CustomPawn customPawn, Pawn pawn) {
            Logger.Debug("Adding implant to pawn, recipe = " + this.recipe?.defName + ", hediff = " + this.hediffDef ?.defName);
            if (BodyPartRecord == null) {
                Logger.Warning("Could not add implant to pawn because no BodyPartRecord is defined");
            }
            if (recipe != null) {
                this.hediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn, BodyPartRecord);
                pawn.health.AddHediff(hediff, BodyPartRecord, new DamageInfo?());
            }
            else if (hediffDef != null) {
                this.hediff = HediffMaker.MakeHediff(hediffDef, pawn, BodyPartRecord);
                pawn.health.AddHediff(hediff, BodyPartRecord, new DamageInfo?());
            }
            else {
                Logger.Warning("Could not add implant to pawn because no RecipeDef or HediffDef is defined");
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

        public override int GetHashCode() {
            var hashCode = -775691452;
            hashCode = hashCode * -1521134295 + EqualityComparer<BodyPartRecord>.Default.GetHashCode(BodyPartRecord);
            hashCode = hashCode * -1521134295 + EqualityComparer<RecipeDef>.Default.GetHashCode(recipe);
            hashCode = hashCode * -1521134295 + EqualityComparer<HediffDef>.Default.GetHashCode(hediffDef);
            return hashCode;
        }
    }
}

