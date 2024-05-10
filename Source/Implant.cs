using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class Implant : CustomizedHediff {

        public string label = "";

        public Implant() {
        }

        public Implant(BodyPartRecord bodyPartRecord, RecipeDef recipe) {
            this.BodyPartRecord = bodyPartRecord;
            this.Recipe = recipe;
        }

        public ImplantOption Option { get; set; }
        public RecipeDef Recipe { get; set; }
        public HediffDef HediffDef { get; set; }
        public Hediff Hediff { get; set; }
        public float Severity { get; set; } = 0f;

        public string Label {
            get {
                if (Recipe != null) {
                    return Recipe?.addsHediff?.LabelCap ?? "";
                }
                else {
                    return "EdB.PC.Dialog.Implant.InstallImplantLabel".Translate(Option?.HediffDef?.label);
                }
            }
        }

        public bool ReplacesPart {
            get {
                if (Recipe != null && Recipe.addsHediff != null
                        && (typeof(Hediff_AddedPart).IsAssignableFrom(Recipe.addsHediff.hediffClass)
                            || typeof(Hediff_MissingPart).IsAssignableFrom(Recipe.addsHediff.hediffClass))) {
                    return true;
                }
                else if (Option?.HediffDef?.organicAddedBodypart ?? false) {
                    return true;
                }
                return false;
            }
        }

        public override bool Equals(object obj) {
            return obj is Implant implant &&
                   ReferenceEquals(BodyPartRecord, implant.BodyPartRecord) &&
                   ReferenceEquals(HediffDef, implant.HediffDef) &&
                   ReferenceEquals(Recipe, implant.Recipe)
                   ;
        }

        public override int GetHashCode() {
            var hashCode = 223280684;
            hashCode = hashCode * -1521134295 + EqualityComparer<BodyPartRecord>.Default.GetHashCode(BodyPartRecord);
            hashCode = hashCode * -1521134295 + EqualityComparer<HediffDef>.Default.GetHashCode(HediffDef);
            hashCode = hashCode * -1521134295 + EqualityComparer<RecipeDef>.Default.GetHashCode(Recipe);
            return hashCode;
        }
    }
}

