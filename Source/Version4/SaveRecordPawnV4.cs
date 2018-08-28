using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordPawnV4 : IExposable {
        public string id;
        public string type;
        public SaveRecordFactionV4 faction;
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
        public List<SaveRecordSkillV4> skills = new List<SaveRecordSkillV4>();
        public List<SaveRecordApparelV4> apparel = new List<SaveRecordApparelV4>();
        public List<int> apparelLayers = new List<int>();
        public List<string> apparelStuff = new List<string>();
        public List<Color> apparelColors = new List<Color>();
        public bool randomInjuries = true;
        public bool randomRelations = false;
        public List<SaveRecordImplantV3> implants = new List<SaveRecordImplantV3>();
        public List<SaveRecordInjuryV3> injuries = new List<SaveRecordInjuryV3>();

        public SaveRecordPawnV4() {

        }

        public SaveRecordPawnV4(CustomPawn pawn) {
            this.id = pawn.Id;
            this.thingDef = pawn.Pawn.def.defName;
            this.type = pawn.Type.ToString();
            if (pawn.Type == CustomPawnType.World && pawn.Faction != null) {
                this.faction = new SaveRecordFactionV4();
                this.faction.def = pawn.Faction.Def != null ? pawn.Faction.Def.defName : null;
                this.faction.index = pawn.Faction.Index;
                this.faction.leader = pawn.Faction.Leader;
            }
            this.pawnKindDef = pawn.Pawn.kindDef.defName;
            this.gender = pawn.Gender;
            if (pawn.Adulthood != null) {
                this.adulthood = pawn.Adulthood.identifier;
            }
            else {
                this.adulthood = pawn.LastSelectedAdulthoodBackstory.identifier;
            }
            this.childhood = pawn.Childhood.identifier;
            this.skinColor = pawn.Pawn.story.SkinColor;
            this.melanin = pawn.Pawn.story.melanin;
            this.hairDef = pawn.HairDef.defName;
            this.hairColor = pawn.Pawn.story.hairColor;
            this.headGraphicPath = pawn.HeadGraphicPath;
            this.bodyType = pawn.BodyType.defName;
            this.firstName = pawn.FirstName;
            this.nickName = pawn.NickName;
            this.lastName = pawn.LastName;
            this.age = 0;
            this.biologicalAge = pawn.BiologicalAge;
            this.chronologicalAge = pawn.ChronologicalAge;
            foreach (var trait in pawn.Traits) {
                if (trait != null) {
                    this.traitNames.Add(trait.def.defName);
                    this.traitDegrees.Add(trait.Degree);
                }
            }
            foreach (var skill in pawn.Pawn.skills.skills) {
                SaveRecordSkillV4 skillRecord = new SaveRecordSkillV4();
                skillRecord.name = skill.def.defName;
                skillRecord.value = pawn.GetUnmodifiedSkillLevel(skill.def);
                skillRecord.passion = pawn.currentPassions[skill.def];
                this.skills.Add(skillRecord);
            }
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn)) {
                if (layer.Apparel) {
                    ThingDef apparelThingDef = pawn.GetAcceptedApparel(layer);
                    Color color = pawn.GetColor(layer);
                    if (apparelThingDef != null) {
                        ThingDef apparelStuffDef = pawn.GetSelectedStuff(layer);
                        SaveRecordApparelV4 apparelRecord = new SaveRecordApparelV4();
                        apparelRecord.layer = layer.Name;
                        apparelRecord.apparel = apparelThingDef.defName;
                        apparelRecord.stuff = apparelStuffDef != null ? apparelStuffDef.defName : "";
                        apparelRecord.color = color;
                        this.apparel.Add(apparelRecord);
                    }
                }
            }
            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            foreach (Implant implant in pawn.Implants) {
                var saveRecord = new SaveRecordImplantV3(implant);
                if (implant.BodyPartRecord != null) {
                    UniqueBodyPart part = healthOptions.FindBodyPartsForRecord(implant.BodyPartRecord);
                    if (part != null && part.Index > 0) {
                        saveRecord.bodyPartIndex = part.Index;
                    }
                }
                this.implants.Add(saveRecord);
            }
            foreach (Injury injury in pawn.Injuries) {
                var saveRecord = new SaveRecordInjuryV3(injury);
                if (injury.BodyPartRecord != null) {
                    UniqueBodyPart part = healthOptions.FindBodyPartsForRecord(injury.BodyPartRecord);
                    if (part != null && part.Index > 0) {
                        saveRecord.bodyPartIndex = part.Index;
                    }
                }
                this.injuries.Add(saveRecord);
            }
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.id, "id", null, false);
            Scribe_Values.Look<string>(ref this.type, "type", null, false);
            Scribe_Deep.Look<SaveRecordFactionV4>(ref this.faction, "faction");
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
            Scribe_Collections.Look<SaveRecordSkillV4>(ref this.skills, "skills", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordApparelV4>(ref this.apparel, "apparel", LookMode.Deep, null);

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

