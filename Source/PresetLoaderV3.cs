using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderV3 {
        public string ModString = "";
        public Dictionary<string, string> thingDefReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> traitReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> recipeReplacements = new Dictionary<string, string>();
        public Dictionary<string, ReplacementBodyPart> bodyPartReplacements = new Dictionary<string, ReplacementBodyPart>();
        public Dictionary<string, CustomizedPawn> pawnsByName = new Dictionary<string, CustomizedPawn>();

        public EquipmentDatabase EquipmentDatabase { get; set; }
        public PawnLoaderV3 PawnLoaderV3 { get; set; }
        public ManagerRelationships ManagerRelationships { get; set; }

        public class ReplacementBodyPart {
            public BodyPartDef def;
            public int index = 0;
            public ReplacementBodyPart(BodyPartDef def, int index = 0) {
                this.def = def;
                this.index = index;
            }
        }

        public PresetLoaderV3() {
            thingDefReplacements.Add("Gun_SurvivalRifle", "Gun_BoltActionRifle");
            thingDefReplacements.Add("Gun_Pistol", "Gun_Revolver");
            thingDefReplacements.Add("Medicine", "MedicineIndustrial");
            thingDefReplacements.Add("Component", "ComponentIndustrial");
            thingDefReplacements.Add("WolfTimber", "Wolf_Timber");
            thingDefReplacements.Add("Capybara_Leather", "LightLeather");

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

        public void AddBodyPartReplacement(string name, string newPart, int index) {
            BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(newPart);
            if (def == null) {
                Logger.Warning("Could not find body part definition \"" + newPart + "\" to replace body part \"" + name + "\"");
                return;
            }
            bodyPartReplacements.Add(name, new ReplacementBodyPart(def, index));
        }

        public PresetLoaderResult Load(string presetName) {
            Logger.Debug("Loading preset (" + presetName + ")");
            PresetLoaderResult result = new PresetLoaderResult();
            Customizations customizations = new Customizations();
            pawnsByName.Clear();

            List<SaveRecordPawnV3> pawns = new List<SaveRecordPawnV3>();
            List<SaveRecordPawnV3> hiddenPawns = new List<SaveRecordPawnV3>();
            List<SaveRecordRelationshipV3> savedRelationships = new List<SaveRecordRelationshipV3>();
            List<SaveRecordParentChildGroupV3> parentChildGroups = new List<SaveRecordParentChildGroupV3>();
            int startingPoints = 0;
            bool usePoints = false;
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));

                Scribe_Values.Look<bool>(ref usePoints, "usePoints", true, false);
                Scribe_Values.Look<int>(ref startingPoints, "startingPoints", 0, false);
                Scribe_Values.Look<string>(ref ModString, "mods", "", false);

                try {
                    Scribe_Collections.Look<SaveRecordPawnV3>(ref hiddenPawns, "hiddenPawns", LookMode.Deep, null);
                }
                catch (Exception e) {
                    //Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    //Logger.Warning("Error while loading preset. Failed to load hidden pawns", e);
                    //Logger.Warning("Preset was created with the following mods: " + ModString);
                }


                try {
                    Scribe_Collections.Look<SaveRecordPawnV3>(ref pawns, "colonists", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Logger.Warning("Error while loading preset.  Failed to load colonists", e);
                    Logger.Warning("Preset was created with the following mods: " + ModString);
                    return result;
                }

                try {
                    Scribe_Collections.Look<SaveRecordRelationshipV3>(ref savedRelationships, "relationships", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Logger.Warning("Error while loading preset. Failed to load relationships", e);
                    Logger.Warning("Preset was created with the following mods: " + ModString);
                }

                try {
                    Scribe_Collections.Look<SaveRecordParentChildGroupV3>(ref parentChildGroups, "parentChildGroups", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Logger.Warning("Error while loading preset", e);
                    Logger.Warning("Preset was created with the following mods: " + ModString);
                }

                List<SaveRecordEquipmentV3> tempEquipment = new List<SaveRecordEquipmentV3>();
                Scribe_Collections.Look<SaveRecordEquipmentV3>(ref tempEquipment, "equipment", LookMode.Deep, null);
                if (tempEquipment != null) {
                    List<CustomizedEquipment> equipment = new List<CustomizedEquipment>(tempEquipment.Count);
                    foreach (var e in tempEquipment) {
                        ThingDef thingDef = FindThingDef(e.def);
                        if (thingDef == null) {
                            string replacementDefName;
                            if (thingDefReplacements.TryGetValue(e.def, out replacementDefName)) {
                                thingDef = FindThingDef(replacementDefName);
                            }
                        }
                        ThingDef stuffDef = null;
                        Gender? gender = null;
                        if (!string.IsNullOrEmpty(e.stuffDef)) {
                            stuffDef = FindThingDef(e.stuffDef);
                        }
                        if (!string.IsNullOrEmpty(e.gender)) {
                            try {
                                gender = (Gender)Enum.Parse(typeof(Gender), e.gender);
                            }
                            catch (Exception) {
                                result.AddWarning("Failed to load gender value for animal.");
                                continue;
                            }
                        }
                        if (thingDef != null) {
                            if (string.IsNullOrEmpty(e.stuffDef)) {
                                EquipmentOption option = EquipmentDatabase.FindOptionForThingDef(thingDef);
                                if (option != null) {
                                    equipment.Add(new CustomizedEquipment() {
                                        EquipmentOption = option,
                                        Count = e.count,
                                        StuffDef = stuffDef,
                                        Gender = gender
                                    });
                                }
                                else {
                                    result.AddWarning("Could not find equipment in equipment database: " + thingDef?.defName);
                                    continue;
                                }
                            }
                        }
                        else {
                            Logger.Warning("Could not load thing definition \"" + e.def + "\"");
                        }
                    }
                    customizations.Equipment = equipment;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file");
                throw e;
            }
            finally {
                UtilitySaveLoad.ClearSaveablesAndCrossRefs();
            }

            try {
                foreach (SaveRecordPawnV3 p in pawns) {
                    PawnLoaderResult pawnLoaderResult = PawnLoaderV3.ConvertSaveRecordToCustomizedPawn(p);
                    if (pawnLoaderResult.Pawn != null) {
                        customizations.ColonyPawns.Add(pawnLoaderResult.Pawn);
                        AddPawnToByNameLookup(pawnLoaderResult.Pawn);
                        result.Problems.AddRange(pawnLoaderResult.Problems.ConvertAll(problem => new PresetLoaderResult.Problem() { Severity = problem.Severity, Message = problem.Message }));
                    }
                    else {
                        result.AddError("Failed to load pawn");
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Error while loading preset while loading pawns", e);
                Logger.Warning("Preset was created with the following mods: " + ModString);
                return result;
            }

            try {
                if (hiddenPawns != null) {
                    foreach (SaveRecordPawnV3 p in hiddenPawns) {
                        customizations.TemporaryPawns.Add(new CustomizedPawn() {
                            Id = p.id,
                            Type = CustomizedPawnType.Temporary,
                            TemporaryPawn = new TemporaryPawn() {
                                Gender = p.gender
                            }
                        });
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Error while loading preset while loading hidden pawns", e);
                Logger.Warning("Preset was created with the following mods: " + ModString);
                return result;
            }


            RelationshipList allRelationships = new RelationshipList();
            if (savedRelationships != null) {
                try {
                    foreach (SaveRecordRelationshipV3 r in savedRelationships) {
                        if (string.IsNullOrEmpty(r.source) || string.IsNullOrEmpty(r.target) || string.IsNullOrEmpty(r.relation)) {
                            Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                            continue;
                        }
                        // TODO: Should create parent-child groups from these relationships
                        //if (r.relation == "Parent") {
                        //    continue;
                        //}
                        CustomizedRelationship relationship = LoadRelationship(r, customizations.AllPawns.Concat(customizations.TemporaryPawns));
                        if (relationship == null) {
                            Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                        }
                        else {
                            allRelationships.Add(relationship);
                        }
                    }
                    customizations.Relationships = allRelationships;
                }
                catch (Exception) {
                    //Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                    //Logger.Warning("Error while loading preset", e);
                    //Logger.Warning("Preset was created with the following mods: " + ModString);
                }
                //if (atLeastOneRelationshipFailed) {
                //    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                //}
            }

            if (parentChildGroups != null) {
                foreach (var groupRecord in parentChildGroups) {
                    ParentChildGroup group = new ParentChildGroup();
                    if (groupRecord.parents != null) {
                        foreach (var id in groupRecord.parents) {
                            CustomizedPawn parent = FindPawnById(id, customizations.AllPawns.Concat(customizations.TemporaryPawns));
                            if (parent != null) {
                                group.Parents.Add(parent);
                            }
                            else {
                                Logger.Warning("Could not load a custom parent relationship because it could not find a pawn with the saved identifer.");
                            }
                        }
                    }
                    if (groupRecord.children != null) {
                        foreach (var id in groupRecord.children) {
                            CustomizedPawn child = FindPawnById(id, customizations.AllPawns.Concat(customizations.TemporaryPawns));
                            if (child != null) {
                                group.Children.Add(child);
                            }
                            else {
                                Logger.Warning("Could not load a custom child relationship because it could not find a pawn with the saved identifer.");
                            }
                        }
                    }
                    if (!group.Parents.NullOrEmpty() || !group.Children.NullOrEmpty()) {
                        customizations.ParentChildGroups.Add(group);
                    }
                }
            }
            ManagerRelationships.ReassignHiddenPawnIndices();

            //if (Failed) {
            //    Messages.Message(ModString, MessageTypeDefOf.SilentInput);
            //    Messages.Message("EdB.PC.Dialog.Preset.Error.ThingDefFailed".Translate(), MessageTypeDefOf.ThreatBig);
            //    Logger.Warning("Preset was created with the following mods: " + ModString);
            //    return result;
            //}

            result.Customizations = customizations;
            return result;
        }

        protected void AddPawnToByNameLookup(CustomizedPawn pawn) {
            NameTriple nameTriple = new NameTriple(pawn.Customizations.FirstName, pawn.Customizations.NickName, pawn.Customizations.LastName);
            string fullname = nameTriple.ToStringFull;
            if (!pawnsByName.ContainsKey(fullname)) {
                pawnsByName.Add(nameTriple.ToStringFull, pawn);
            }
        }

        protected UniqueBodyPart FindReplacementBodyPart(OptionsHealth healthOptions, string name) {
            ReplacementBodyPart replacement = null;
            if (bodyPartReplacements.TryGetValue(name, out replacement)) {
                return healthOptions.FindBodyPart(replacement.def, replacement.index);
            }
            return null;
        }

        protected CustomizedPawn FindPawnById(string id, IEnumerable<CustomizedPawn> colonistPawns) {
            CustomizedPawn result = colonistPawns.FirstOrDefault((CustomizedPawn c) => {
                return id == c.Id;
            });
            if (result != null) {
                return result;
            }
            if (pawnsByName.ContainsKey(id)) {
                return pawnsByName[id];
            }
            return null;
        }

        public CustomizedRelationship LoadRelationship(SaveRecordRelationshipV3 saved, IEnumerable<CustomizedPawn> pawns) {
            CustomizedRelationship result = new CustomizedRelationship();

            foreach (var p in pawns) {
                if (p.Id == saved.source) {
                    result.Source = p;
                }
                if (p.Id == saved.target) {
                    result.Target = p;
                }
            }
            if (pawnsByName.ContainsKey(saved.source)) {
                result.Source = pawnsByName[saved.source];
            }
            if (pawnsByName.ContainsKey(saved.target)) {
                result.Target = pawnsByName[saved.target];
            }

            result.Def = DefDatabase<PawnRelationDef>.GetNamedSilentFail(saved.relation);
            if (result.Def != null) {
                result.InverseDef = ManagerRelationships.FindInverseRelationship(result.Def);
            }
            if (result.Def == null) {
                Logger.Warning("Couldn't find relationship definition: " + saved.relation);
                return null;
            }
            else if (result.Source == null) {
                Logger.Warning("Couldn't find relationship source pawn: " + saved.source);
                return null;
            }
            else if (result.Target == null) {
                Logger.Warning("Couldn't find relationship target pawn: " + saved.target);
                return null;
            }
            else if (result.InverseDef == null) {
                Logger.Warning("Couldn't determine inverse relationship: " + saved.relation);
                return null;
            }
            return result;
        }

        public RecipeDef FindRecipeDef(string name) {
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

        public HairDef FindHairDef(string name) {
            return DefDatabase<HairDef>.GetNamedSilentFail(name);
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

        public ThingDef FindThingDef(string defName) {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def != null) {
                return def;
            }
            if (thingDefReplacements.ContainsKey(defName)) {
                defName = thingDefReplacements[defName];
                return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            }
            else {
                return null;
            }
        }

        public Trait FindTrait(string name, int degree) {
            Trait trait = LookupTrait(name, degree);
            if (trait != null) {
                return trait;
            }
            else {
                if (traitReplacements.ContainsKey(name)) {
                    return LookupTrait(traitReplacements[name], degree);
                }
            }
            return null;
        }

        protected Trait LookupTrait(string name, int degree) {
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

        // Maintains a list of skill definitions that were replaced in newer versions of the game.
        Dictionary<string, List<string>> skillDefReplacementLookup = new Dictionary<string, List<string>>();

        protected void InitializeSkillDefReplacements() {
            AddSkillDefReplacement("Growing", "Plants");
            AddSkillDefReplacement("Research", "Intellectual");
        }

        protected void AddSkillDefReplacement(String skill, String replacement) {
            List<string> replacements = null;
            if (!skillDefReplacementLookup.TryGetValue(skill, out replacements)) {
                replacements = new List<string>();
                skillDefReplacementLookup.Add(skill, replacements);
            }
            replacements.Add(replacement);
        }

        public SkillDef FindSkillDef(Pawn pawn, string name) {
            List<string> replacements = null;
            if (skillDefReplacementLookup.ContainsKey(name)) {
                replacements = skillDefReplacementLookup[name];
            }
            foreach (var skill in pawn.skills.skills) {
                if (skill.def.defName.Equals(name)) {
                    return skill.def;
                }
                if (replacements != null) {
                    foreach (var r in replacements) {
                        if (skill.def.defName.Equals(r)) {
                            return skill.def;
                        }
                    }
                }
            }
            return null;
        }
    }
}

