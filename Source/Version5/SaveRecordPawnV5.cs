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
        public string headType;
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
        public long? biologicalAgeInTicks;
        public long? chronologicalAgeInTicks;
        public string developmentalStage;
        public List<SaveRecordSkillV4> skills = new List<SaveRecordSkillV4>();
        public List<SaveRecordApparelV5> apparel = new List<SaveRecordApparelV5>();
        public List<int> apparelLayers = new List<int>();
        public List<string> apparelStuff = new List<string>();
        public List<Color> apparelColors = new List<Color>();
        public bool randomInjuries = true;
        public bool randomRelations = false;
        public List<SaveRecordImplantV5> implants = new List<SaveRecordImplantV5>();
        public List<SaveRecordInjuryV5> injuries = new List<SaveRecordInjuryV5>();
        public SaveRecordIdeoV5 ideo;
        public List<string> abilities = new List<string>();
        public string compsXml = null;
        public List<string> savedComps = new List<string>();
        public List<string> hediffXmls = null;
        public SaveRecordGenesV5 genes;
        public List<SaveRecordHediffV5> hediffs = null;
        public List<SaveRecordPossessionV5> possessions = null;
        public List<SaveRecordTitleV5> titles = null;

        // Deprecated.  Here for backwards compatibility with V4
        public List<string> traitNames = new List<string>();
        public List<int> traitDegrees = new List<int>();

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
            Scribe_Values.Look<string>(ref this.headType, "headType", null, false);
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
            Scribe_Values.Look<long?>(ref this.biologicalAgeInTicks, "biologicalAgeInTicks", null, false);
            Scribe_Values.Look<long?>(ref this.chronologicalAgeInTicks, "chronologicalAgeInTicks", null, false);
            Scribe_Collections.Look<SaveRecordSkillV4>(ref this.skills, "skills", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordApparelV5>(ref this.apparel, "apparel", LookMode.Deep, null);
            Scribe_Deep.Look<SaveRecordIdeoV5>(ref this.ideo, "ideo");
            Scribe_Deep.Look<SaveRecordGenesV5>(ref this.genes, "genes");
            Scribe_Collections.Look<string>(ref this.abilities, "abilities", LookMode.Value, null);
            Scribe_Collections.Look<SaveRecordPossessionV5>(ref this.possessions, "possessions", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordTitleV5>(ref this.titles, "titles", LookMode.Deep, null);

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look<SaveRecordImplantV5>(ref this.implants, "implants", LookMode.Deep, null);
            }
            else {
                if (Scribe.loader.curXmlParent["implants"] != null) {
                    Scribe_Collections.Look<SaveRecordImplantV5>(ref this.implants, "implants", LookMode.Deep, null);
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look<SaveRecordInjuryV5>(ref this.injuries, "injuries", LookMode.Deep, null);
            }
            else {
                if (Scribe.loader.curXmlParent["injuries"] != null) {
                    Scribe_Collections.Look<SaveRecordInjuryV5>(ref this.injuries, "injuries", LookMode.Deep, null);
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look<SaveRecordHediffV5>(ref this.hediffs, "hediffs", LookMode.Deep, null);
            }

        }
    }
}
