using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderVersion3 {
        public bool Failed = false;
        public string ModString = "";
        public Dictionary<string, string> thingDefReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> traitReplacements = new Dictionary<string, string>();
        public Dictionary<string, string> recipeReplacements = new Dictionary<string, string>();
        public Dictionary<string, ReplacementBodyPart> bodyPartReplacements = new Dictionary<string, ReplacementBodyPart>();

        public class ReplacementBodyPart {
            public BodyPartDef def;
            public int index = 0;
            public ReplacementBodyPart(BodyPartDef def, int index = 0) {
                this.def = def;
                this.index = index;
            }
        }

        public PresetLoaderVersion3() {
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

        public void AddBodyPartReplacement(string name, string newPart, int index) {
            BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(newPart);
            if (def == null) {
                Log.Warning("Could not find body part definition \"" + newPart + "\" to replace body part \"" + name + "\"");
                return;
            }
            bodyPartReplacements.Add(name, new ReplacementBodyPart(def, index));
        }

        public bool Load(PrepareCarefully loadout, string presetName) {
            List<SaveRecordPawnV3> pawns = new List<SaveRecordPawnV3>();
            List<SaveRecordPawnV3> hiddenPawns = new List<SaveRecordPawnV3>();
            List<SaveRecordRelationshipV3> savedRelationships = new List<SaveRecordRelationshipV3>();
            List<SaveRecordParentChildGroupV3> parentChildGroups = new List<SaveRecordParentChildGroupV3>();
            Failed = false;
            int startingPoints = 0;
            bool usePoints = false;
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));

                Scribe_Values.Look<bool>(ref usePoints, "usePoints", true, false);
                Scribe_Values.Look<int>(ref startingPoints, "startingPoints", 0, false);
                Scribe_Values.Look<string>(ref ModString, "mods", "", false);

                try {
                    Scribe_Collections.Look<SaveRecordPawnV3>(ref pawns, "colonists", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                try {
                    Scribe_Collections.Look<SaveRecordPawnV3>(ref hiddenPawns, "hiddenPawns", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                try {
                    Scribe_Collections.Look<SaveRecordRelationshipV3>(ref savedRelationships, "relationships", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                try {
                    Scribe_Collections.Look<SaveRecordParentChildGroupV3>(ref parentChildGroups, "parentChildGroups", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                List<SaveRecordEquipmentV3> tempEquipment = new List<SaveRecordEquipmentV3>();
                Scribe_Collections.Look<SaveRecordEquipmentV3>(ref tempEquipment, "equipment", LookMode.Deep, null);
                loadout.Equipment.Clear();
                if (tempEquipment != null) {
                    List<EquipmentSelection> equipment = new List<EquipmentSelection>(tempEquipment.Count);
                    foreach (var e in tempEquipment) {
                        ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(e.def);
                        if (thingDef == null) {
                            string replacementDefName;
                            if (thingDefReplacements.TryGetValue(e.def, out replacementDefName)) {
                                thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(replacementDefName);
                            }
                        }
                        ThingDef stuffDef = null;
                        Gender gender = Gender.None;
                        if (!string.IsNullOrEmpty(e.stuffDef)) {
                            stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(e.stuffDef);
                        }
                        if (!string.IsNullOrEmpty(e.gender)) {
                            try {
                                gender = (Gender)Enum.Parse(typeof(Gender), e.gender);
                            }
                            catch (Exception) {
                                Log.Warning("Failed to load gender value for animal.");
                                Failed = true;
                                continue;
                            }
                        }
                        if (thingDef != null) {
                            if (string.IsNullOrEmpty(e.stuffDef)) {
                                EquipmentKey key = new EquipmentKey(thingDef, null, gender);
                                EquipmentRecord record = PrepareCarefully.Instance.EquipmentDatabase.LookupEquipmentRecord(key);
                                if (record != null) {
                                    equipment.Add(new EquipmentSelection(record, e.count));
                                }
                                else {
                                    Log.Warning("Could not find equipment in equipment database: " + key);
                                    Failed = true;
                                    continue;
                                }
                            }
                            else {
                                if (stuffDef != null) {
                                    EquipmentKey key = new EquipmentKey(thingDef, stuffDef, gender);
                                    EquipmentRecord record = PrepareCarefully.Instance.EquipmentDatabase.LookupEquipmentRecord(key);
                                    if (record == null) {
                                        string thing = thingDef != null ? thingDef.defName : "null";
                                        string stuff = stuffDef != null ? stuffDef.defName : "null";
                                        Log.Warning(string.Format("Could not load equipment/resource from the preset.  This may be caused by an invalid thing/stuff combination: " + key));
                                        Failed = true;
                                        continue;
                                    }
                                    else {
                                        equipment.Add(new EquipmentSelection(record, e.count));
                                    }
                                }
                                else {
                                    Log.Warning("Could not load stuff definition \"" + e.stuffDef + "\" for item \"" + e.def + "\"");
                                    Failed = true;
                                }
                            }
                        }
                        else {
                            Log.Warning("Could not load thing definition \"" + e.def + "\"");
                            Failed = true;
                        }
                    }
                    loadout.Equipment.Clear();
                    foreach (var e in equipment) {
                        loadout.Equipment.Add(e);
                    }
                }
                else {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.EquipmentFailed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning("Failed to load equipment from preset");
                    Failed = true;
                }

                //PrepareCarefully.Instance.Config.pointsEnabled = usePoints;
            }
            catch (Exception e) {
                Log.Error("Failed to load preset file");
                throw e;
            }
            finally {
                PresetLoader.ClearSaveablesAndCrossRefs();
            }

            List<CustomPawn> allPawns = new List<CustomPawn>();
            List<CustomPawn> colonistCustomPawns = new List<CustomPawn>();
            try {
                foreach (SaveRecordPawnV3 p in pawns) {
                    CustomPawn pawn = LoadPawn(p);
                    if (pawn != null) {
                        allPawns.Add(pawn);
                        colonistCustomPawns.Add(pawn);
                    }
                    else {
                        Messages.Message("EdB.PC.Dialog.Preset.Error.NoCharacter".Translate(), MessageTypeDefOf.ThreatBig);
                        Log.Warning("Preset was created with the following mods: " + ModString);
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            List<CustomPawn> hiddenCustomPawns = new List<CustomPawn>();
            try {
                if (hiddenPawns != null) {
                    foreach (SaveRecordPawnV3 p in hiddenPawns) {
                        CustomPawn pawn = LoadPawn(p);
                        if (pawn != null) {
                            allPawns.Add(pawn);
                            hiddenCustomPawns.Add(pawn);
                        }
                        else {
                            Log.Warning("Prepare Carefully failed to load a hidden character from the preset");
                        }
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            loadout.ClearPawns();
            foreach (CustomPawn p in colonistCustomPawns) {
                loadout.AddPawn(p);
            }
            loadout.RelationshipManager.Clear();
            loadout.RelationshipManager.InitializeWithCustomPawns(colonistCustomPawns.AsEnumerable().Concat(hiddenCustomPawns));

            bool atLeastOneRelationshipFailed = false;
            List<CustomRelationship> allRelationships = new List<CustomRelationship>();
            if (savedRelationships != null) {
                try {
                    foreach (SaveRecordRelationshipV3 r in savedRelationships) {
                        if (string.IsNullOrEmpty(r.source) || string.IsNullOrEmpty(r.target) || string.IsNullOrEmpty(r.relation)) {
                            atLeastOneRelationshipFailed = true;
                            Log.Warning("Prepare Carefully failed to load a custom relationship from the preset: " + r);
                            continue;
                        }
                        CustomRelationship relationship = LoadRelationship(r, allPawns);
                        if (relationship == null) {
                            atLeastOneRelationshipFailed = true;
                            Log.Warning("Prepare Carefully failed to load a custom relationship from the preset: " + r);
                        }
                        else {
                            allRelationships.Add(relationship);
                        }
                    }
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }
                if (atLeastOneRelationshipFailed) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                }
            }
            loadout.RelationshipManager.AddRelationships(allRelationships);

            if (parentChildGroups != null) {
                foreach (var groupRecord in parentChildGroups) {
                    ParentChildGroup group = new ParentChildGroup();
                    if (groupRecord.parents != null) {
                        foreach (var id in groupRecord.parents) {
                            CustomPawn parent = FindPawnById(id, colonistCustomPawns, hiddenCustomPawns);
                            if (parent != null) {
                                var pawn = parent;
                                if (pawn != null) {
                                    group.Parents.Add(pawn);
                                }
                                else {
                                    Log.Warning("Prepare Carefully could not load a custom parent relationship because it could not find a matching pawn in the relationship manager.");
                                }
                            }
                            else {
                                Log.Warning("Prepare Carefully could not load a custom parent relationship because it could not find a pawn with the saved identifer.");
                            }
                        }
                    }
                    if (groupRecord.children != null) {
                        foreach (var id in groupRecord.children) {
                            CustomPawn child = FindPawnById(id, colonistCustomPawns, hiddenCustomPawns);
                            if (child != null) {
                                var pawn = child;
                                if (pawn != null) {
                                    group.Children.Add(pawn);
                                }
                                else {
                                    Log.Warning("Prepare Carefully could not load a custom child relationship because it could not find a matching pawn in the relationship manager.");
                                }
                            }
                            else {
                                Log.Warning("Prepare Carefully could not load a custom child relationship because it could not find a pawn with the saved identifer.");
                            }
                        }
                    }
                    loadout.RelationshipManager.ParentChildGroups.Add(group);
                }
            }
            loadout.RelationshipManager.ReassignHiddenPawnIndices();

            if (Failed) {
                Messages.Message(ModString, MessageTypeDefOf.SilentInput);
                Messages.Message("EdB.PC.Dialog.Preset.Error.ThingDefFailed".Translate(), MessageTypeDefOf.ThreatBig);
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            return true;
        }

        protected UniqueBodyPart FindReplacementBodyPart(OptionsHealth healthOptions, string name) {
            ReplacementBodyPart replacement = null;
            if (bodyPartReplacements.TryGetValue(name, out replacement)) {
                return healthOptions.FindBodyPart(replacement.def, replacement.index);
            }
            return null;
        }

        public CustomPawn LoadPawn(SaveRecordPawnV3 record) {
            PawnKindDef pawnKindDef = null;
            if (record.pawnKindDef != null) {
                pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.pawnKindDef);
                if (pawnKindDef == null) {
                    Log.Warning("Prepare Carefully could not find the pawn kind definition for the saved character: \"" + record.pawnKindDef + "\"");
                    return null;
                }
            }
            
            ThingDef pawnThingDef = ThingDefOf.Human;
            if (record.thingDef != null) {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.thingDef);
                if (thingDef != null) {
                    pawnThingDef = thingDef;
                }
            }
            
            Pawn source;
            if (pawnKindDef != null) {
                source = new Randomizer().GenerateKindOfColonist(pawnKindDef);
            }
            else {
                source = new Randomizer().GenerateColonist();
            }
            source.health.Reset();
            
            CustomPawn pawn = new CustomPawn(source);
            if (pawn.Id == null) {
                pawn.GenerateId();
            }
            else {
                pawn.Id = record.id;
            }
            
            pawn.Type = CustomPawnType.Colonist;

            pawn.Gender = record.gender;
            if (record.age > 0) {
                pawn.ChronologicalAge = record.age;
                pawn.BiologicalAge = record.age;
            }
            if (record.chronologicalAge > 0) {
                pawn.ChronologicalAge = record.chronologicalAge;
            }
            if (record.biologicalAge > 0) {
                pawn.BiologicalAge = record.biologicalAge;
            }

            pawn.FirstName = record.firstName;
            pawn.NickName = record.nickName;
            pawn.LastName = record.lastName;

            HairDef h = FindHairDef(record.hairDef);
            if (h != null) {
                pawn.HairDef = h;
            }
            else {
                Log.Warning("Could not load hair definition \"" + record.hairDef + "\"");
                Failed = true;
            }
            
            pawn.HeadGraphicPath = record.headGraphicPath;
            pawn.Pawn.story.hairColor = record.hairColor;
            
            if (record.melanin >= 0.0f) {
                pawn.MelaninLevel = record.melanin;
            }
            else {
                pawn.MelaninLevel = PawnColorUtils.FindMelaninValueFromColor(record.skinColor);
            }
            // Set the skin color (only for Alien Races).
            if (pawn.AlienRace != null) {
                pawn.SkinColor = record.skinColor;
            }
            
            Backstory backstory = FindBackstory(record.childhood);
            if (backstory != null) {
                pawn.Childhood = backstory;
            }
            else {
                Log.Warning("Could not load childhood backstory definition \"" + record.childhood + "\"");
                Failed = true;
            }
            if (record.adulthood != null) {
                backstory = FindBackstory(record.adulthood);
                if (backstory != null) {
                    pawn.Adulthood = backstory;
                }
                else {
                    Log.Warning("Could not load adulthood backstory definition \"" + record.adulthood + "\"");
                    Failed = true;
                }
            }

            // Get the body type from the save record.  If there's no value in the save, then assign the 
            // default body type from the pawn's backstories.
            // TODO: 1.0
            /*
            BodyType? bodyType = null;
            try {
                bodyType = (BodyType)Enum.Parse(typeof(BodyType), record.bodyType);
            }
            catch (Exception) {
            }
            if (!bodyType.HasValue) {
                if (pawn.Adulthood != null) {
                    bodyType = pawn.Adulthood.BodyTypeFor(pawn.Gender);
                }
                else {
                    bodyType = pawn.Childhood.BodyTypeFor(pawn.Gender);
                }
            }
            if (bodyType.HasValue) {
                pawn.BodyType = bodyType.Value;
            }
            */
            BodyTypeDef bodyType = null;
            try {
                bodyType = DefDatabase<BodyTypeDef>.GetNamedSilentFail(record.bodyType);
            }
            catch (Exception) {
            }
            if (bodyType == null) {
                if (pawn.Adulthood != null) {
                    bodyType = pawn.Adulthood.BodyTypeFor(pawn.Gender);
                }
                else {
                    bodyType = pawn.Childhood.BodyTypeFor(pawn.Gender);
                }
            }
            if (bodyType != null) {
                pawn.BodyType = bodyType;
            }

            pawn.ClearTraits();
            for (int i = 0; i < record.traitNames.Count; i++) {
                string traitName = record.traitNames[i];
                Trait trait = FindTrait(traitName, record.traitDegrees[i]);
                if (trait != null) {
                    pawn.AddTrait(trait);
                }
                else {
                    Log.Warning("Could not load trait definition \"" + traitName + "\"");
                    Failed = true;
                }
            }
            
            for (int i = 0; i < record.skillNames.Count; i++) {
                string name = record.skillNames[i];
                SkillDef def = FindSkillDef(pawn.Pawn, name);
                if (def == null) {
                    Log.Warning("Could not load skill definition \"" + name + "\"");
                    Failed = true;
                    continue;
                }
                pawn.currentPassions[def] = record.passions[i];
                pawn.originalPassions[def] = record.passions[i];
                pawn.SetOriginalSkillLevel(def, record.skillValues[i]);
                pawn.SetUnmodifiedSkillLevel(def, record.skillValues[i]);
            }
            if (record.originalPassions != null && record.originalPassions.Count == record.skillNames.Count) {
                for (int i = 0; i < record.skillNames.Count; i++) {
                    string name = record.skillNames[i];
                    SkillDef def = FindSkillDef(pawn.Pawn, name);
                    if (def == null) {
                        Log.Warning("Could not load skill definition \"" + name + "\"");
                        Failed = true;
                        continue;
                    }
                    //pawn.originalPassions[def] = record.originalPassions[i];
                }
            }
            
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn)) {
                if (layer.Apparel) {
                    pawn.SetSelectedApparel(layer, null);
                    pawn.SetSelectedStuff(layer, null);
                }
            }
            for (int i = 0; i < record.apparelLayers.Count; i++) {
                int layerIndex = record.apparelLayers[i];
                PawnLayer layer = PrepareCarefully.Instance.Providers.PawnLayers.FindLayerFromDeprecatedIndex(layerIndex);
                if (layer == null) {
                    Log.Warning("Could not find pawn layer from saved pawn layer index: \"" + layerIndex + "\"");
                    Failed = true;
                    continue;
                }
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(record.apparel[i]);
                if (def == null) {
                    Log.Warning("Could not load thing definition for apparel \"" + record.apparel[i] + "\"");
                    Failed = true;
                    continue;
                }
                ThingDef stuffDef = null;
                if (!string.IsNullOrEmpty(record.apparelStuff[i])) {
                    stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.apparelStuff[i]);
                    if (stuffDef == null) {
                        Log.Warning("Could not load stuff definition \"" + record.apparelStuff[i] + "\" for apparel \"" + record.apparel[i] + "\"");
                        Failed = true;
                        continue;
                    }
                }
                pawn.SetSelectedApparel(layer, def);
                pawn.SetSelectedStuff(layer, stuffDef);
                pawn.SetColor(layer, record.apparelColors[i]);
            }

            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            for (int i = 0; i < record.implants.Count; i++) {
                SaveRecordImplantV3 implantRecord = record.implants[i];
                UniqueBodyPart uniqueBodyPart = healthOptions.FindBodyPartByName(implantRecord.bodyPart, implantRecord.bodyPartIndex != null ? implantRecord.bodyPartIndex.Value : 0);
                if (uniqueBodyPart == null) {
                    uniqueBodyPart = FindReplacementBodyPart(healthOptions, implantRecord.bodyPart);
                }
                if (uniqueBodyPart == null) {
                    Log.Warning("Prepare Carefully could not add the implant because it could not find the needed body part \"" + implantRecord.bodyPart + "\""
                        + (implantRecord.bodyPartIndex != null ? " with index " + implantRecord.bodyPartIndex : ""));
                    Failed = true;
                    continue;
                }
                BodyPartRecord bodyPart = uniqueBodyPart.Record;
                if (implantRecord.recipe != null) {
                    RecipeDef recipeDef = FindRecipeDef(implantRecord.recipe);
                    if (recipeDef == null) {
                        Log.Warning("Prepare Carefully could not add the implant because it could not find the recipe definition \"" + implantRecord.recipe + "\"");
                        Failed = true;
                        continue;
                    }
                    bool found = false;
                    foreach (var p in recipeDef.appliedOnFixedBodyParts) {
                        if (p.defName.Equals(bodyPart.def.defName)) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        Log.Warning("Prepare carefully could not apply the saved implant recipe \"" + implantRecord.recipe + "\" to the body part \"" + bodyPart.def.defName + "\".  Recipe does not support that part.");
                        Failed = true;
                        continue;
                    }
                    Implant implant = new Implant();
                    implant.BodyPartRecord = bodyPart;
                    implant.recipe = recipeDef;
                    implant.label = implant.Label;
                    pawn.AddImplant(implant);
                }
            }
            
            foreach (var injuryRecord in record.injuries) {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(injuryRecord.hediffDef);
                if (def == null) {
                    Log.Warning("Prepare Carefully could not add the injury because it could not find the hediff definition \"" + injuryRecord.hediffDef + "\"");
                    Failed = true;
                    continue;
                }
                InjuryOption option = healthOptions.FindInjuryOptionByHediffDef(def);
                if (option == null) {
                    Log.Warning("Prepare Carefully could not add the injury because it could not find a matching injury option for the saved hediff \"" + injuryRecord.hediffDef + "\"");
                    Failed = true;
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
                        Log.Warning("Prepare Carefully could not add the injury because it could not find the needed body part \"" + injuryRecord.bodyPart + "\""
                            + (injuryRecord.bodyPartIndex != null ? " with index " + injuryRecord.bodyPartIndex : ""));
                        Failed = true;
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
                pawn.AddInjury(injury);
            }
            
            pawn.CopySkillsAndPassionsToPawn();
            pawn.ClearPawnCaches();

            return pawn;
        }

        protected CustomPawn FindPawnById(string id, List<CustomPawn> colonistPawns, List<CustomPawn> hiddenPawns) {
            CustomPawn result = colonistPawns.FirstOrDefault((CustomPawn c) => {
                return id == c.Id;
            });
            if (result == null) {
                result = hiddenPawns.FirstOrDefault((CustomPawn c) => {
                    return id == c.Id;
                });
            }
            return result;
        }

        public CustomRelationship LoadRelationship(SaveRecordRelationshipV3 saved, List<CustomPawn> pawns) {
            CustomRelationship result = new CustomRelationship();

            foreach (var p in pawns) {
                if (p.Id == saved.source || p.Name.ToStringFull == saved.source) {
                    result.source = p;
                }
                if (p.Id == saved.target || p.Name.ToStringFull == saved.target) {
                    result.target = p;
                }
            }

            result.def = DefDatabase<PawnRelationDef>.GetNamedSilentFail(saved.relation);
            if (result.def != null) {
                result.inverseDef = PrepareCarefully.Instance.RelationshipManager.FindInverseRelationship(result.def);
            }
            if (result.def == null) {
                Log.Warning("Couldn't find relationship definition: " + saved.relation);
                return null;
            }
            else if (result.source == null) {
                Log.Warning("Couldn't find relationship source pawn: " + saved.source);
                return null;
            }
            else if (result.target == null) {
                Log.Warning("Couldn't find relationship target pawn: " + saved.source);
                return null;
            }
            else if (result.inverseDef == null) {
                Log.Warning("Couldn't determine inverse relationship: " + saved.relation);
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

        public Backstory FindBackstory(string name) {
            Backstory matchingBackstory = BackstoryDatabase.allBackstories.Values.ToList().Find((Backstory b) => {
                return b.identifier.Equals(name);
            });
            // If we couldn't find a matching backstory, look for one with the same identifier, but a different version number at the end.
            if (matchingBackstory == null) {
                Regex expression = new Regex("\\d+$");
                string backstoryMinusVersioning = expression.Replace(name, "");
                matchingBackstory = BackstoryDatabase.allBackstories.Values.ToList().Find((Backstory b) => {
                    return b.identifier.StartsWith(backstoryMinusVersioning);
                });
                if (matchingBackstory != null) {
                    Log.Message("Found replacement backstory.  Using " + matchingBackstory.identifier + " in place of " + name);
                }
            }
            return matchingBackstory;
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

