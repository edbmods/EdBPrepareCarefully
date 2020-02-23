using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordPawnV3 : IExposable {
        public string id;
        public string pawnKindDef;
        public string thingDef;
        public Gender gender;
        public string adulthood;
        public string childhood;
        public List<string> traitNames = new List<string>();
        public List<int> traitDegrees = new List<int>();
        public Color skinColor;
        public float melanin;
        public string hairDef;
        public Color hairColor;
        public string headGraphicPath;
        public string bodyType;
        public string firstName;
        public string lastName;
        public string nickName;
        public int age;
        public int biologicalAge;
        public int chronologicalAge;
        public List<string> skillNames = new List<string>();
        public List<int> skillValues = new List<int>();
        public List<Passion> passions = new List<Passion>();
        public List<Passion> originalPassions = new List<Passion>();
        public List<string> apparel = new List<string>();
        public List<int> apparelLayers = new List<int>();
        public List<string> apparelStuff = new List<string>();
        public List<Color> apparelColors = new List<Color>();
        public bool randomInjuries = true;
        public bool randomRelations = false;
        public List<SaveRecordImplantV3> implants = new List<SaveRecordImplantV3>();
        public List<SaveRecordInjuryV3> injuries = new List<SaveRecordInjuryV3>();

        public SaveRecordPawnV3() {
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.id, "id", null, false);
            Scribe_Values.Look<string>(ref this.pawnKindDef, "pawnKindDef", null, false);
            Scribe_Values.Look<string>(ref this.thingDef, "thingDef", ThingDefOf.Human.defName, false);
            Scribe_Values.Look<Gender>(ref this.gender, "gender", Gender.Male, false);
            Scribe_Values.Look<string>(ref this.childhood, "childhood", null, false);
            Scribe_Values.Look<string>(ref this.adulthood, "adulthood", null, false);
            Scribe_Collections.Look<string>(ref this.traitNames, "traitNames", LookMode.Value, null);
            Scribe_Collections.Look<int>(ref this.traitDegrees, "traitDegrees", LookMode.Value, null);
            Scribe_Values.Look<Color>(ref this.skinColor, "skinColor", Color.white, false);
            Scribe_Values.Look<float>(ref this.melanin, "melanin", -1.0f, false);
            Scribe_Values.Look<string>(ref this.bodyType, "bodyType", null, false);
            Scribe_Values.Look<string>(ref this.hairDef, "hairDef", null, false);
            Scribe_Values.Look<Color>(ref this.hairColor, "hairColor", Color.white, false);
            Scribe_Values.Look<string>(ref this.headGraphicPath, "headGraphicPath", null, false);
            Scribe_Values.Look<string>(ref this.firstName, "firstName", null, false);
            Scribe_Values.Look<string>(ref this.nickName, "nickName", null, false);
            Scribe_Values.Look<string>(ref this.lastName, "lastName", null, false);
            if (Scribe.mode == LoadSaveMode.LoadingVars) {
                Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            }
            Scribe_Values.Look<int>(ref this.biologicalAge, "biologicalAge", 0, false);
            Scribe_Values.Look<int>(ref this.chronologicalAge, "chronologicalAge", 0, false);
            Scribe_Collections.Look<string>(ref this.skillNames, "skillNames", LookMode.Value, null);
            Scribe_Collections.Look<int>(ref this.skillValues, "skillValues", LookMode.Value, null);
            Scribe_Collections.Look<Passion>(ref this.passions, "passions", LookMode.Value, null);
            Scribe_Collections.Look<string>(ref this.apparel, "apparel", LookMode.Value, null);
            Scribe_Collections.Look<int>(ref this.apparelLayers, "apparelLayers", LookMode.Value, null);
            Scribe_Collections.Look<string>(ref this.apparelStuff, "apparelStuff", LookMode.Value, null);
            Scribe_Collections.Look<Color>(ref this.apparelColors, "apparelColors", LookMode.Value, null);

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look<SaveRecordImplantV3>(ref this.implants, "implants", LookMode.Deep, null);
            }
            else {
                if (Scribe.loader.curXmlParent["implants"] != null) {
                    Scribe_Collections.Look<SaveRecordImplantV3>(ref this.implants, "implants", LookMode.Deep, null);
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look<SaveRecordInjuryV3>(ref this.injuries, "injuries", LookMode.Deep, null);
            }
            else {
                if (Scribe.loader.curXmlParent["injuries"] != null) {
                    Scribe_Collections.Look<SaveRecordInjuryV3>(ref this.injuries, "injuries", LookMode.Deep, null);
                }
            }
        }

        public HairDef FindHairDef(string name) {
            return DefDatabase<HairDef>.GetNamedSilentFail(name);
        }

        public Backstory FindBackstory(string name) {
            return BackstoryDatabase.allBackstories.Values.ToList().Find((Backstory b) => {
                return b.identifier.Equals(name);
            });
        }

        public Trait FindTrait(string name, int degree) {
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
                if (!def.defName.Equals(name)) {
                    continue;
                }
                List<TraitDegreeData> degreeData = def.degreeDatas;
                int count = degreeData.Count;
                if (count > 0) {
                    for (int i = 0; i < count; i++) {
                        if (degree == degreeData[i].degree) {
                            Trait trait = new Trait(def, degreeData[i].degree, true);
                            return trait;
                        }
                    }
                }
                else {
                    return new Trait(def, 0, true);
                }
            }
            return null;
        }

        public SkillDef FindSkillDef(Pawn pawn, string name) {
            foreach (var skill in pawn.skills.skills) {
                if (skill.def.defName.Equals(name)) {
                    return skill.def;
                }
            }
            return null;
        }
    }
}

