using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordPawnV5 : IExposable {
        public string id;
        public string type;
        public SaveRecordFactionV4 faction;
        public string pawnKindDef;
        public string originalFactionDef;
        public string thingDef;
        public Gender gender;
        public string adulthood;
        public string childhood;
        public List<SaveRecordTraitV5> traits = new List<SaveRecordTraitV5>();
        public Color skinColor;
        public float melanin;
        public string hairDef;
        public Color hairColor;
        public string headGraphicPath;
        public string bodyType;
        public string beard;
        public string faceTattoo;
        public string bodyTattoo;
        public string firstName;
        public string lastName;
        public string nickName;
        public Color? favoriteColor;
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
        public SaveRecordIdeoV5 ideo;
        public List<string> abilities = new List<string>();
        public string compsXml = null;
        public List<string> savedComps = new List<string>();

        // Deprecated.  Here for backwards compatibility with V4
        public List<string> traitNames = new List<string>();
        public List<int> traitDegrees = new List<int>();

        public PawnCompsSaver pawnCompsSaver = null;

        public SaveRecordPawnV5() {
        }

        public SaveRecordPawnV5(CustomPawn pawn) {
            this.id = pawn.Id;
            this.thingDef = pawn.Pawn.def.defName;
            this.type = pawn.Type.ToString();
            if (pawn.Type == CustomPawnType.World && pawn.Faction != null) {
                this.faction = new SaveRecordFactionV4() {
                    def = pawn.Faction?.Def?.defName,
                    index = pawn.Faction.Index,
                    leader = pawn.Faction.Leader
                };
            }
            this.pawnKindDef = pawn.OriginalKindDef?.defName ?? pawn.Pawn.kindDef.defName;
            this.originalFactionDef = pawn.OriginalFactionDef?.defName;
            this.gender = pawn.Gender;
            this.adulthood = pawn.Adulthood?.identifier ?? pawn.LastSelectedAdulthoodBackstory?.identifier;
            this.childhood = pawn.Childhood?.identifier;
            this.skinColor = pawn.Pawn.story.SkinColor;
            this.melanin = pawn.Pawn.story.melanin;
            this.hairDef = pawn.HairDef.defName;
            this.hairColor = pawn.Pawn.story.hairColor;
            this.headGraphicPath = pawn.HeadGraphicPath;
            this.bodyType = pawn.BodyType.defName;
            this.beard = pawn.Beard?.defName;
            this.faceTattoo = pawn.FaceTattoo?.defName;
            this.bodyTattoo = pawn.BodyTattoo?.defName;
            this.firstName = pawn.FirstName;
            this.nickName = pawn.NickName;
            this.lastName = pawn.LastName;
            this.favoriteColor = pawn.Pawn.story.favoriteColor;
            this.age = 0;
            this.biologicalAge = pawn.BiologicalAge;
            this.chronologicalAge = pawn.ChronologicalAge;
            foreach (var trait in pawn.Traits) {
                if (trait != null) {
                    this.traits.Add(new SaveRecordTraitV5() {
                        def = trait.def.defName,
                        degree = trait.Degree
                    });
                }
            }
            foreach (var skill in pawn.Pawn.skills.skills) {
                this.skills.Add(new SaveRecordSkillV4() {
                    name = skill.def.defName,
                    value = pawn.GetUnmodifiedSkillLevel(skill.def),
                    passion = pawn.currentPassions[skill.def]
                });
            }
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn)) {
                if (layer.Apparel) {
                    ThingDef apparelThingDef = pawn.GetAcceptedApparel(layer);
                    Color color = pawn.GetColor(layer);
                    if (apparelThingDef != null) {
                        ThingDef apparelStuffDef = pawn.GetSelectedStuff(layer);
                        this.apparel.Add(new SaveRecordApparelV4() {
                            layer = layer.Name,
                            apparel = apparelThingDef.defName,
                            stuff = apparelStuffDef?.defName ?? "",
                            color = color
                        });
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
            if (pawn.Pawn?.abilities != null) {
                this.abilities.AddRange(pawn.Pawn.abilities.abilities.Select(a => a.def.defName));
            }
            if (ModsConfig.IdeologyActive && pawn.Pawn.ideo != null) {
                Ideo ideo = pawn.Pawn.ideo.Ideo;
                this.ideo = new SaveRecordIdeoV5() {
                    certainty = pawn.Pawn.ideo.Certainty,
                    name = ideo?.name,
                    sameAsColony = ideo == Find.FactionManager.OfPlayer.ideos.PrimaryIdeo,
                    culture = ideo?.culture.defName
                };
                if (ideo != null) {
                    this.ideo.memes = new List<string>(ideo.memes.Select(m => m.defName));
                }
                Logger.Debug(string.Join(", ", pawn.Pawn.ideo.Ideo?.memes.Select(m => m.defName)));
            }

            pawnCompsSaver = new PawnCompsSaver(pawn.Pawn, DefaultPawnCompRules.RulesForSaving);
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.id, "id", null, false);
            Scribe_Values.Look<string>(ref this.type, "type", null, false);
            Scribe_Deep.Look<SaveRecordFactionV4>(ref this.faction, "faction");
            Scribe_Values.Look<string>(ref this.pawnKindDef, "pawnKindDef", null, false);
            Scribe_Values.Look<string>(ref this.originalFactionDef, "originalFactionDef", null, false);
            Scribe_Values.Look<string>(ref this.thingDef, "thingDef", ThingDefOf.Human.defName, false);
            Scribe_Values.Look<Gender>(ref this.gender, "gender", Gender.Male, false);
            Scribe_Values.Look<string>(ref this.childhood, "childhood", null, false);
            Scribe_Values.Look<string>(ref this.adulthood, "adulthood", null, false);
            Scribe_Collections.Look<SaveRecordTraitV5>(ref this.traits, "traits", LookMode.Deep, null);
            Scribe_Collections.Look<string>(ref this.traitNames, "traitNames", LookMode.Value, null);
            Scribe_Collections.Look<int>(ref this.traitDegrees, "traitDegrees", LookMode.Value, null);
            Scribe_Values.Look<Color>(ref this.skinColor, "skinColor", Color.white, false);
            Scribe_Values.Look<float>(ref this.melanin, "melanin", -1.0f, false);
            Scribe_Values.Look<string>(ref this.bodyType, "bodyType", null, false);
            Scribe_Values.Look<string>(ref this.headGraphicPath, "headGraphicPath", null, false);
            Scribe_Values.Look<string>(ref this.hairDef, "hairDef", null, false);
            Scribe_Values.Look<Color>(ref this.hairColor, "hairColor", Color.white, false);
            Scribe_Values.Look<string>(ref this.beard, "beard", null, false);
            Scribe_Values.Look<string>(ref this.faceTattoo, "faceTattoo", null, false);
            Scribe_Values.Look<string>(ref this.bodyTattoo, "bodyTattoo", null, false);
            Scribe_Values.Look<string>(ref this.hairDef, "hairDef", null, false);
            Scribe_Values.Look<string>(ref this.firstName, "firstName", null, false);
            Scribe_Values.Look<string>(ref this.nickName, "nickName", null, false);
            Scribe_Values.Look<string>(ref this.lastName, "lastName", null, false);
            Scribe_Values.Look<Color?>(ref this.favoriteColor, "favoriteColor", null, false);
            Scribe_Values.Look<int>(ref this.biologicalAge, "biologicalAge", 0, false);
            Scribe_Values.Look<int>(ref this.chronologicalAge, "chronologicalAge", 0, false);
            Scribe_Collections.Look<SaveRecordSkillV4>(ref this.skills, "skills", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordApparelV4>(ref this.apparel, "apparel", LookMode.Deep, null);
            Scribe_Deep.Look<SaveRecordIdeoV5>(ref this.ideo, "ideo");
            Scribe_Collections.Look<string>(ref this.abilities, "abilities", LookMode.Value, null);

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

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Deep.Look<PawnCompsSaver>(ref this.pawnCompsSaver, "compFields");
                Scribe_Collections.Look<string>(ref this.pawnCompsSaver.savedComps, "savedComps");
            }
            else {
                if (Scribe.loader.EnterNode("compFields")) {
                    try {
                        compsXml = Scribe.loader.curXmlParent.InnerXml;
                    }
                    finally {
                        Scribe.loader.ExitNode();
                    }
                }
                Scribe_Collections.Look<string>(ref this.savedComps, "savedComps");
            }

        }
    }
}
