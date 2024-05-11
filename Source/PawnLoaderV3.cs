using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLoaderV3 {
        public ProviderHealthOptions ProviderHealthOptions { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }

        // Maintain lists of definitions that were replaced in newer versions of the game.
        public Dictionary<string, string> thingDefReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> traitReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> recipeReplacements = new Dictionary<string, string>();
        public Dictionary<string, ReplacementBodyPart> bodyPartReplacements = new Dictionary<string, ReplacementBodyPart>();
        Dictionary<string, string> skillDefReplacementLookup = new Dictionary<string, string>();
        private System.Random Random = new System.Random();

        public Dictionary<string, Ideo> IdeoMap { get; set; } = new Dictionary<string, Ideo>();

        public class ReplacementBodyPart {
            public BodyPartDef def;
            public int index = 0;
            public ReplacementBodyPart(BodyPartDef def, int index = 0) {
                this.def = def;
                this.index = index;
            }
        }

        public PawnLoaderV3() {
            thingDefReplacements.Add("Gun_SurvivalRifle", "Gun_BoltActionRifle");
            thingDefReplacements.Add("Gun_Pistol", "Gun_Revolver");
            thingDefReplacements.Add("Medicine", "MedicineIndustrial");
            thingDefReplacements.Add("Component", "ComponentIndustrial");
            thingDefReplacements.Add("WolfTimber", "Wolf_Timber");

            traitReplacements.Add("Prosthophobe", "BodyPurist");
            traitReplacements.Add("Prosthophile", "Transhumanist");
            traitReplacements.Add("SuperImmune", "Immunity");

            InitializeRecipeReplacements();
            InitializeBodyPartReplacements();
            InitializeSkillDefReplacements();
        }

        protected void InitializeRecipeReplacements() {
            recipeReplacements.Add("InstallSyntheticHeart", "InstallSimpleProstheticHeart");
            recipeReplacements.Add("InstallAdvancedBionicArm", "InstallArchtechBionicArm");
            recipeReplacements.Add("InstallAdvancedBionicLeg", "InstallArchtechBionicLeg");
            recipeReplacements.Add("InstallAdvancedBionicEye", "InstallArchtechBionicEye");
        }

        protected void InitializeBodyPartReplacements() {
            AddBodyPartReplacement("LeftFoot", "Foot", 0);
            AddBodyPartReplacement("LeftLeg", "Leg", 0);
            AddBodyPartReplacement("LeftEye", "Eye", 0);
            AddBodyPartReplacement("LeftEar", "Ear", 0);
            AddBodyPartReplacement("LeftLung", "Lung", 0);
            AddBodyPartReplacement("LeftArm", "Arm", 0);
            AddBodyPartReplacement("LeftShoulder", "Shoulder", 0);
            AddBodyPartReplacement("LeftKidney", "Kidney", 0);
            AddBodyPartReplacement("RightFoot", "Foot", 1);
            AddBodyPartReplacement("RightLeg", "Leg", 1);
            AddBodyPartReplacement("RightEye", "Eye", 1);
            AddBodyPartReplacement("RightEar", "Ear", 1);
            AddBodyPartReplacement("RightLung", "Lung", 1);
            AddBodyPartReplacement("RightArm", "Arm", 1);
            AddBodyPartReplacement("RightShoulder", "Shoulder", 1);
            AddBodyPartReplacement("RightKidney", "Kidney", 1);
        }

        protected void InitializeSkillDefReplacements() {
            AddSkillDefReplacement("Growing", "Plants");
            AddSkillDefReplacement("Research", "Intellectual");
        }

        public void AddBodyPartReplacement(string name, string newPart, int index) {
            BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(newPart);
            if (def == null) {
                Logger.Warning("Could not find body part definition \"" + newPart + "\" to replace body part \"" + name + "\"");
                return;
            }
            bodyPartReplacements.Add(name, new ReplacementBodyPart(def, index));
        }

        public List<string> ConvertModStringToModList(string modString) {
            if (string.IsNullOrEmpty(modString)) {
                return null;
            }
            var lastIndex = modString.LastIndexOf(" and ");
            if (lastIndex != -1) {
                modString = modString.Substring(0, lastIndex) + ", " + modString.Substring(lastIndex + 5);
            }
            return new List<string>(modString.Split(new string[] { ", " }, StringSplitOptions.None));
        }

        public PawnLoaderResult Load(string file) {
            SaveRecordPawnV3 pawnRecord = new SaveRecordPawnV3();
            string modString = "";
            string version = "";
            List<string> problems = new List<string>();
            PawnLoaderResult result = new PawnLoaderResult();
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(file));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
                Scribe_Values.Look<string>(ref modString, "mods", "", false);
                result.Mods = ConvertModStringToModList(modString);
                try {
                    Scribe_Deep.Look<SaveRecordPawnV3>(ref pawnRecord, "colonist", null);
                }
                catch (Exception e) {
                    Messages.Message(modString, MessageTypeDefOf.SilentInput);
                    problems.Add("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate());
                    problems.Add(e.ToString());
                    Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                    Logger.Warning(e.ToString());
                    Logger.Warning("Colonist was created with the following mods: " + modString);
                    return result;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to load character file");
                throw e;
            }
            finally {
                UtilitySaveLoad.ClearSaveablesAndCrossRefs();
            }

            if (pawnRecord == null) {
                Messages.Message(modString, MessageTypeDefOf.SilentInput);
                problems.Add("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate());
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                Logger.Warning("Colonist was created with the following mods: " + modString);
                return result;
            }

            PawnLoaderResult loaderResult = ConvertSaveRecordToCustomizedPawn(pawnRecord);
            loaderResult.Mods = result.Mods;

            return loaderResult;
        }

        public PawnLoaderResult ConvertSaveRecordToCustomizedPawn(SaveRecordPawnV3 record) {
            PawnLoaderResult result = new PawnLoaderResult();
            CustomizationsPawn customizations = new CustomizationsPawn();
            CustomizedPawn customizedPawn = new CustomizedPawn() {
                Customizations = customizations,
            };
            customizedPawn.Type = CustomizedPawnType.Colony;
            if (!record.id.NullOrEmpty()) {
                customizedPawn.Id = record.id;
            }
            else {
                customizedPawn.Id = System.Guid.NewGuid().ToString();
            }

            PawnKindDef pawnKindDef = null;
            if (record.pawnKindDef != null) {
                pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.pawnKindDef);
                if (pawnKindDef == null) {
                    result.AddWarning("Could not find the pawn kind definition for the saved character: \"" + record.pawnKindDef + "\"");
                }
            }
            if (pawnKindDef == null) {
                pawnKindDef = Faction.OfPlayer.def.basicMemberKind;
            }
            customizations.PawnKind = pawnKindDef;

            //ThingDef pawnThingDef = ThingDefOf.Human;
            //if (record.thingDef != null) {
            //    ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.thingDef);
            //    if (thingDef != null) {
            //        pawnThingDef = thingDef;
            //    }
            //}



            //Faction playerFaction = Faction.OfPlayerSilentFail;
            //Ideo playerFactionIdeology = playerFaction?.ideos?.PrimaryIdeo;


            customizations.Gender = record.gender;

            if (record.age > 0) {
                int days = Random.Next((int)UtilityAge.DaysPerYear);
                customizations.BiologicalAgeInTicks = UtilityAge.TicksFromYearsAndDays(record.age, days);
                customizations.ChronologicalAgeInTicks = UtilityAge.TicksFromYearsAndDays(record.age, days);
            }
            if (record.chronologicalAge > 0) {
                int days = Random.Next((int)UtilityAge.DaysPerYear);
                customizations.ChronologicalAgeInTicks = UtilityAge.TicksFromYearsAndDays(record.chronologicalAge, days);
            }
            if (record.biologicalAge > 0) {
                int days = Random.Next((int)UtilityAge.DaysPerYear);
                customizations.BiologicalAgeInTicks = UtilityAge.TicksFromYearsAndDays(record.biologicalAge, days);
            }
            if (customizations.BiologicalAgeInTicks > customizations.ChronologicalAgeInTicks) {
                customizations.ChronologicalAgeInTicks = customizations.BiologicalAgeInTicks;
            }

            customizations.FirstName = record.firstName;
            customizations.NickName = record.nickName;
            customizations.LastName = record.lastName;
            customizations.Beard = BeardDefOf.NoBeard;
            customizations.BodyTattoo = TattooDefOf.NoTattoo_Body;
            customizations.FaceTattoo = TattooDefOf.NoTattoo_Face;

            if (!record.hairDef.NullOrEmpty()) {

            }
            HairDef h = DefDatabase<HairDef>.GetNamedSilentFail(record.hairDef);
            if (h != null) {
                customizations.Hair = h;
            }
            else {
                result.AddWarning("Could not load hair definition \"" + record.hairDef + "\"");
            }

            if (!String.IsNullOrEmpty(record.headGraphicPath)) {
                string[] pathParts = record.headGraphicPath.Split(new char[] { '/' }, 20);
                string lastPath = pathParts[pathParts.Length - 1];
                string[] parts = lastPath.Split('_');
                string headTypeDefName = parts[0] + "_";
                for (int i = 1; i < parts.Count(); i++) {
                    headTypeDefName += parts[i];
                }
                var headType = DefDatabase<HeadTypeDef>.GetNamedSilentFail(headTypeDefName);
                if (headType != null) {
                    customizations.HeadType = headType;
                    Logger.Message("Successfully converted headGraphicPath {" + record.headGraphicPath + "} to head type definition {" + headType.defName + "}");
                }
                else {
                    Logger.Warning("Couldn't find head type {" + headTypeDefName + "} for converted headGraphicPath {" + record.headGraphicPath + "}");
                }
            }
            customizations.HairColor = record.hairColor;
            customizations.SkinColor = record.skinColor;

            // TODO
            //if (record.melanin >= 0.0f) {
            //    customizations.Melanin = record.melanin;
            //}

            BackstoryDef backstory = FindBackstory(record.childhood);
            if (backstory != null) {
                customizations.ChildhoodBackstory = backstory;
            }
            else {
                result.AddWarning("Could not load childhood backstory definition \"" + record.childhood + "\"");
            }
            if (record.adulthood != null) {
                backstory = FindBackstory(record.adulthood);
                if (backstory != null) {
                    customizations.AdulthoodBackstory = backstory;
                }
                else {
                    result.AddWarning("Could not load adulthood backstory definition \"" + record.adulthood + "\"");
                }
            }

            BodyTypeDef bodyType = null;
            try {
                bodyType = DefDatabase<BodyTypeDef>.GetNamedSilentFail(record.bodyType);
            }
            catch (Exception) {
            }
            if (bodyType == null) {
                if (customizations.AdulthoodBackstory != null) {
                    bodyType = customizations.AdulthoodBackstory.BodyTypeFor(customizations.Gender);
                }
                else {
                    bodyType = customizations.ChildhoodBackstory.BodyTypeFor(customizations.Gender);
                }
            }
            if (bodyType != null) {
                customizations.BodyType = bodyType;
            }

            for (int i = 0; i < record.traitNames.Count; i++) {
                string traitName = record.traitNames[i];
                TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(traitName);
                if (traitDef != null) {
                    customizations.Traits.Add(new CustomizationsTrait() {
                        TraitDef = traitDef,
                        Degree = record.traitDegrees[i]
                    });
                }
                else {
                    result.AddWarning("Could not load trait definition \"" + traitName + "\"");
                }
            }

            for (int i = 0; i < record.skillNames.Count; i++) {
                string name = record.skillNames[i];
                SkillDef def = FindSkillDef(name);
                if (def == null) {
                    result.AddWarning("Could not load skill definition \"" + name + "\"");
                    continue;
                }
                customizations.Skills.Add(new CustomizationsSkill() {
                    SkillDef = def,
                    Level = record.skillValues[i],
                    Passion = record.passions[i],
                    OriginalLevel = record.skillValues[i],
                    OriginalPassion = record.passions[i]
                });
            }

            for (int i = 0; i < record.apparel.Count; i++) {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(record.apparel[i]);
                if (def == null) {
                    result.AddWarning("Could not load thing definition for apparel \"" + record.apparel[i] + "\"");
                    continue;
                }
                ThingDef stuffDef = null;
                if (!string.IsNullOrEmpty(record.apparelStuff[i])) {
                    stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.apparelStuff[i]);
                    if (stuffDef == null) {
                        result.AddWarning("Could not load stuff definition \"" + record.apparelStuff[i] + "\" for apparel \"" + record.apparel[i] + "\"");
                    }
                }
                customizations.Apparel.Add(new CustomizationsApparel() {
                    ThingDef = def,
                    StuffDef = stuffDef,
                    Color = record.apparelColors[i]
                });
            }

            OptionsHealth healthOptions = ProviderHealthOptions.GetOptions(customizations.PawnKind.race);
            for (int i = 0; i < record.implants.Count; i++) {
                SaveRecordImplantV3 implantRecord = record.implants[i];
                UniqueBodyPart uniqueBodyPart = healthOptions.FindBodyPartByName(implantRecord.bodyPart, implantRecord.bodyPartIndex != null ? implantRecord.bodyPartIndex.Value : 0);
                if (uniqueBodyPart == null) {
                    uniqueBodyPart = FindReplacementBodyPart(healthOptions, implantRecord.bodyPart);
                }
                if (uniqueBodyPart == null) {
                    result.AddWarning("Could not add the implant because it could not find the needed body part \"" + implantRecord.bodyPart + "\""
                        + (implantRecord.bodyPartIndex != null ? " with index " + implantRecord.bodyPartIndex : ""));
                    continue;
                }
                BodyPartRecord bodyPart = uniqueBodyPart.Record;
                if (implantRecord.recipe != null) {
                    RecipeDef recipeDef = FindRecipeDef(implantRecord.recipe);
                    if (recipeDef == null) {
                        result.AddWarning("Could not add the implant because it could not find the recipe definition \"" + implantRecord.recipe + "\"");
                        continue;
                    }
                    ImplantOption implantOption = healthOptions.FindImplantOptionThatAddsRecipeDefToBodyPart(recipeDef, bodyPart?.def);
                    if (implantOption != null) {
                        Implant implant = new Implant() {
                            Option = implantOption,
                            Recipe = recipeDef,
                            BodyPartRecord = bodyPart,
                            HediffDef = recipeDef.addsHediff,
                        };
                        customizations.Implants.Add(implant);
                    }
                    else {
                        result.AddWarning("Could not add implant to pawn because no matching option was found for specified RecipeDef {" + implantRecord.recipe + "} and BodyPartDef {" + bodyPart?.def?.defName + "}");
                    }
                }
            }

            foreach (var injuryRecord in record.injuries) {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(injuryRecord.hediffDef);
                if (def == null) {
                    result.AddWarning("Could not add the injury because it could not find the hediff definition \"" + injuryRecord.hediffDef + "\"");
                    continue;
                }
                InjuryOption option = healthOptions.FindInjuryOptionByHediffDef(def);
                if (option == null) {
                    result.AddWarning("Could not add the injury because it could not find a matching injury option for the saved hediff \"" + injuryRecord.hediffDef + "\"");
                    continue;
                }
                BodyPartRecord bodyPart = null;
                if (injuryRecord.bodyPart != null) {
                    UniqueBodyPart uniquePart = healthOptions.FindBodyPartByName(injuryRecord.bodyPart,
                        injuryRecord.bodyPartIndex != null ? injuryRecord.bodyPartIndex.Value : 0);
                    if (uniquePart == null) {
                        uniquePart = FindReplacementBodyPart(healthOptions, injuryRecord.bodyPart);
                    }
                    if (uniquePart == null) {
                        result.AddWarning("Could not add the injury because it could not find the needed body part \"" + injuryRecord.bodyPart + "\""
                            + (injuryRecord.bodyPartIndex != null ? " with index " + injuryRecord.bodyPartIndex : ""));
                        continue;
                    }
                    bodyPart = uniquePart.Record;
                }
                Injury injury = new Injury();
                injury.Option = option;
                injury.BodyPartRecord = bodyPart;
                if (injuryRecord.severity != null) {
                    injury.Severity = injuryRecord.Severity;
                }
                if (injuryRecord.painFactor != null) {
                    injury.PainFactor = injuryRecord.PainFactor;
                }
                customizations.Injuries.Add(injury);
            }

            result.Pawn = customizedPawn;
            return result;
        }

        protected RecipeDef FindRecipeDef(string name) {
            RecipeDef result = DefDatabase<RecipeDef>.GetNamedSilentFail(name);
            if (result == null) {
                return FindReplacementRecipe(name);
            }
            else {
                return result;
            }
        }

        protected RecipeDef FindReplacementRecipe(string name) {
            String replacementName = null;
            if (recipeReplacements.TryGetValue(name, out replacementName)) {
                return DefDatabase<RecipeDef>.GetNamedSilentFail(replacementName);
            }
            return null;
        }

        protected UniqueBodyPart FindReplacementBodyPart(OptionsHealth healthOptions, string name) {
            ReplacementBodyPart replacement = null;
            if (bodyPartReplacements.TryGetValue(name, out replacement)) {
                return healthOptions.FindBodyPart(replacement.def, replacement.index);
            }
            return null;
        }

        public BackstoryDef FindBackstory(string name) {
            // Assume the name is a definition name.  Look it up based on that and return it if we find it.
            BackstoryDef matchingBackstory = DefDatabase<BackstoryDef>.GetNamedSilentFail(name);
            if (matchingBackstory != null) {
                return matchingBackstory;
            }
            // If we didn't find it, the name is probably a backstory identifier.  Try to find it by matching the id.
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs.Where((BackstoryDef b) => { return b.identifier != null && b.identifier.Equals(name); }).FirstOrDefault();
            if (matchingBackstory != null) {
                return matchingBackstory;
            }
            // If we didn't find it, the identifier probably changed.  Try to find another backstory with the same identifier prefix but with a different numeric suffix at the end
            Regex expression = new Regex("\\d+$");
            string backstoryMinusVersioning = expression.Replace(name, "");
            if (backstoryMinusVersioning == name) {
                return null;
            }
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs
                .Where(b => { return b.identifier != null && b.identifier.StartsWith(backstoryMinusVersioning); })
                .FirstOrDefault();
            if (matchingBackstory != null) {
                Logger.Message("Found replacement backstory.  Using " + matchingBackstory.identifier + " in place of " + name);
                return matchingBackstory;
            }
            // If we still didn't find it, look for a def name that starts with the identifier but with the numeric suffix removed
            matchingBackstory = DefDatabase<BackstoryDef>.AllDefs
                .Where(b => b.defName.StartsWith(backstoryMinusVersioning))
                .FirstOrDefault();
            if (matchingBackstory != null) {
                Logger.Message("Found replacement backstory.  Using " + matchingBackstory.defName + " in place of " + name);
                return matchingBackstory;
            }
            return null;
        }

        protected void AddSkillDefReplacement(String oldValue, String replacement) {
            skillDefReplacementLookup[oldValue] = replacement;
        }

        protected SkillDef FindSkillDef(string name) {
            string skillName = name;
            if (skillDefReplacementLookup.ContainsKey(name)) {
                skillName = skillDefReplacementLookup[name];
            }
            return DefDatabase<SkillDef>.GetNamedSilentFail(skillName);
        }

    }
}
