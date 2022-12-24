using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderVersion4 {
        public bool Failed = false;
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

        public PresetLoaderVersion4() {
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
                Logger.Warning("Could not find body part definition \"" + newPart + "\" to replace body part \"" + name + "\"");
                return;
            }
            bodyPartReplacements.Add(name, new ReplacementBodyPart(def, index));
        }

        public bool Load(PrepareCarefully loadout, string presetName) {
            SaveRecordPresetV4 preset = new SaveRecordPresetV4();
            Failed = false;
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
                preset.ExposeData();

                if (preset.equipment != null) {
                    List<EquipmentSelection> equipment = new List<EquipmentSelection>(preset.equipment.Count);
                    foreach (var e in preset.equipment) {
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
                                Logger.Warning("Failed to load gender value for animal.");
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
                                    Logger.Warning("Could not find equipment in equipment database: " + key);
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
                                        Logger.Warning(string.Format("Could not load equipment/resource from the preset.  This may be caused by an invalid thing/stuff combination: " + key));
                                        Failed = true;
                                        continue;
                                    }
                                    else {
                                        equipment.Add(new EquipmentSelection(record, e.count));
                                    }
                                }
                                else {
                                    Logger.Warning("Could not load stuff definition \"" + e.stuffDef + "\" for item \"" + e.def + "\"");
                                    Failed = true;
                                }
                            }
                        }
                        else {
                            Logger.Warning("Could not load thing definition \"" + e.def + "\"");
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
                    Logger.Warning("Failed to load equipment from preset");
                    Failed = true;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file");
                throw e;
            }
            finally {
                PresetLoader.ClearSaveablesAndCrossRefs();
            }

            List<CustomPawn> allPawns = new List<CustomPawn>();
            List<CustomPawn> colonistCustomPawns = new List<CustomPawn>();
            List<CustomPawn> hiddenCustomPawns = new List<CustomPawn>();
            try {
                foreach (SaveRecordPawnV4 p in preset.pawns) {
                    CustomPawn pawn = LoadPawn(p);
                    if (pawn != null) {
                        allPawns.Add(pawn);
                        if (!pawn.Hidden) {
                            colonistCustomPawns.Add(pawn);
                        }
                        else {
                            hiddenCustomPawns.Add(pawn);
                        }
                    }
                    else {
                        Messages.Message("EdB.PC.Dialog.Preset.Error.NoCharacter".Translate(), MessageTypeDefOf.ThreatBig);
                        Logger.Warning("Preset was created with the following mods: " + preset.mods);
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Error while loading preset", e);
                Logger.Warning("Preset was created with the following mods: " + preset.mods);
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
            if (preset.relationships != null) {
                try {
                    foreach (SaveRecordRelationshipV3 r in preset.relationships) {
                        if (string.IsNullOrEmpty(r.source) || string.IsNullOrEmpty(r.target) || string.IsNullOrEmpty(r.relation)) {
                            atLeastOneRelationshipFailed = true;
                            Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                            continue;
                        }
                        CustomRelationship relationship = LoadRelationship(r, allPawns);
                        if (relationship == null) {
                            atLeastOneRelationshipFailed = true;
                            Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                        }
                        else {
                            allRelationships.Add(relationship);
                        }
                    }
                }
                catch (Exception e) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                    Logger.Warning("Error while loading preset", e);
                    Logger.Warning("Preset was created with the following mods: " + preset.mods);
                    return false;
                }
                if (atLeastOneRelationshipFailed) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                }
            }
            loadout.RelationshipManager.InitializeWithRelationships(allRelationships);

            if (preset.parentChildGroups != null) {
                foreach (var groupRecord in preset.parentChildGroups) {
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
                                    Logger.Warning("Could not load a custom parent relationship because it could not find a matching pawn in the relationship manager.");
                                }
                            }
                            else {
                                Logger.Warning("Could not load a custom parent relationship because it could not find a pawn with the saved identifer.");
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
                                    Logger.Warning("Could not load a custom child relationship because it could not find a matching pawn in the relationship manager.");
                                }
                            }
                            else {
                                Logger.Warning("Could not load a custom child relationship because it could not find a pawn with the saved identifer.");
                            }
                        }
                    }
                    loadout.RelationshipManager.ParentChildGroups.Add(group);
                }
            }
            loadout.RelationshipManager.ReassignHiddenPawnIndices();

            if (Failed) {
                Messages.Message(preset.mods, MessageTypeDefOf.SilentInput);
                Messages.Message("EdB.PC.Dialog.Preset.Error.ThingDefFailed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Preset was created with the following mods: " + preset.mods);
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

        public CustomPawn LoadPawn(SaveRecordPawnV4 record) {
            PawnKindDef pawnKindDef = null;
            if (record.pawnKindDef != null) {
                pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.pawnKindDef);
                if (pawnKindDef == null) {
                    Logger.Warning("Could not find the pawn kind definition for the saved character: \"" + record.pawnKindDef + "\"");
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

            PawnGenerationRequestWrapper generationRequest = new PawnGenerationRequestWrapper() {
                FixedBiologicalAge = record.biologicalAge,
                FixedChronologicalAge = record.chronologicalAge,
                FixedGender = record.gender,
                Context = PawnGenerationContext.NonPlayer,
                WorldPawnFactionDoesntMatter = true
            };
            Faction playerFaction = Find.FactionManager.OfPlayer;
            Ideo playerFactionIdeology = playerFaction?.ideos?.PrimaryIdeo;
            if (playerFactionIdeology != null) {
                generationRequest.FixedIdeology = playerFactionIdeology;
            }

            if (pawnKindDef != null) {
                generationRequest.KindDef = pawnKindDef;
            }
            Pawn source = PawnGenerator.GeneratePawn(generationRequest.Request);
            if (source.health != null) {
                source.health.Reset();
            }
            
            CustomPawn pawn = new CustomPawn(source);
            if (record.id == null) {
                pawn.GenerateId();
            }
            else {
                pawn.Id = record.id;
            }

            if (record.type != null) {
                try {
                    pawn.Type = (CustomPawnType)Enum.Parse(typeof(CustomPawnType), record.type);
                }
                catch (Exception) {
                    pawn.Type = CustomPawnType.Colonist;
                }
            }
            else {
                pawn.Type = CustomPawnType.Colonist;
            }

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
            if (pawn.Pawn.style != null) {
                pawn.Pawn.style.beardDef = BeardDefOf.NoBeard;
                pawn.Pawn.style.BodyTattoo = TattooDefOf.NoTattoo_Body;
                pawn.Pawn.style.FaceTattoo = TattooDefOf.NoTattoo_Face;
            }

            if (record.originalFactionDef != null) {
                pawn.OriginalFactionDef = DefDatabase<FactionDef>.GetNamedSilentFail(record.originalFactionDef);
            }
            pawn.OriginalKindDef = pawnKindDef;

            if (pawn.Type == CustomPawnType.World) {
                if (record.faction != null) {
                    if (record.faction.def != null) {
                        FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(record.faction.def);
                        if (factionDef != null) {
                            bool randomFaction = false;
                            if (record.faction.index != null) {
                                CustomFaction customFaction = null;
                                if (!record.faction.leader) {
                                    customFaction = PrepareCarefully.Instance.Providers.Factions.FindCustomFactionByIndex(factionDef, record.faction.index.Value);
                                }
                                else {
                                    customFaction = PrepareCarefully.Instance.Providers.Factions.FindCustomFactionWithLeaderOptionByIndex(factionDef, record.faction.index.Value);
                                }
                                if (customFaction != null) {
                                    pawn.Faction = customFaction;
                                }
                                else {
                                    Logger.Warning("Could not place at least one preset character into a saved faction because there were not enough available factions of that type in the world");
                                    randomFaction = true;
                                }
                            }
                            else {
                                randomFaction = true;
                            }
                            if (randomFaction) {
                                CustomFaction customFaction = PrepareCarefully.Instance.Providers.Factions.FindRandomCustomFactionByDef(factionDef);
                                if (customFaction != null) {
                                    pawn.Faction = customFaction;
                                }
                            }
                        }
                        else {
                            Logger.Warning("Could not place at least one preset character into a saved faction because that faction is not available in the world");
                        }
                    }
                }
            }

            HairDef h = FindHairDef(record.hairDef);
            if (h != null) {
                pawn.HairDef = h;
            }
            else {
                Logger.Warning("Could not load hair definition \"" + record.hairDef + "\"");
                Failed = true;
            }
            
            if (pawn.Pawn.story != null) {
                pawn.Pawn.story.HairColor = record.hairColor;
            }
            
            pawn.SkinColor = record.skinColor;

            // Set the skin color for Alien Races and alien comp values.
            if (pawn.AlienRace != null) {
                pawn.SkinColor = record.skinColor;
                if (record.alien != null) {
                    ThingComp alienComp = ProviderAlienRaces.FindAlienCompForPawn(pawn.Pawn);
                    if (alienComp != null) {
                        ProviderAlienRaces.SetCrownTypeOnComp(alienComp, record.alien.crownType);
                        ProviderAlienRaces.SetSkinColorOnComp(alienComp, record.alien.skinColor);
                        ProviderAlienRaces.SetSkinColorSecondOnComp(alienComp, record.alien.skinColorSecond);
                        ProviderAlienRaces.SetHairColorSecondOnComp(alienComp, record.alien.hairColorSecond);
                    }
                }
            }
            
            BackstoryDef backstory = FindBackstory(record.childhood);
            if (backstory != null) {
                pawn.Childhood = backstory;
            }
            else {
                Logger.Warning("Could not load childhood backstory definition \"" + record.childhood + "\"");
                Failed = true;
            }
            if (record.adulthood != null) {
                backstory = FindBackstory(record.adulthood);
                if (backstory != null) {
                    pawn.Adulthood = backstory;
                }
                else {
                    Logger.Warning("Could not load adulthood backstory definition \"" + record.adulthood + "\"");
                    Failed = true;
                }
            }

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
                    Logger.Warning("Could not load trait definition \"" + traitName + "\"");
                    Failed = true;
                }
            }
            
            foreach (var skill in record.skills) {
                SkillDef def = FindSkillDef(pawn.Pawn, skill.name);
                if (def == null) {
                    Logger.Warning("Could not load skill definition \"" + skill.name + "\" from saved preset");
                    Failed = true;
                    continue;
                }
                pawn.currentPassions[def] = skill.passion;
                pawn.originalPassions[def] = skill.passion;
                pawn.SetOriginalSkillLevel(def, skill.value);
                pawn.SetUnmodifiedSkillLevel(def, skill.value);
            }

            pawn.ClearApparel();
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn)) {
                if (layer.Apparel) {
                    pawn.SetSelectedApparel(layer, null);
                    pawn.SetSelectedStuff(layer, null);
                }
            }
            List<PawnLayer> apparelLayers = PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn).FindAll((layer) => { return layer.Apparel; });
            foreach (var apparelRecord in record.apparel) {
                // Find the pawn layer for the saved apparel record.
                PawnLayer layer = apparelLayers.FirstOrDefault((apparelLayer) => { return apparelLayer.Name == apparelRecord.layer; });
                if (layer == null) {
                    Logger.Warning("Could not find a matching pawn layer for the saved apparel \"" + apparelRecord.layer + "\"");
                    Failed = true;
                    continue;
                }
                if (apparelRecord.apparel.NullOrEmpty()) {
                    Logger.Warning("Saved apparel entry for layer \"" + apparelRecord.layer + "\" had an empty apparel def");
                    Failed = true;
                    continue;
                }
                // Set the defaults.
                pawn.SetSelectedApparel(layer, null);
                pawn.SetSelectedStuff(layer, null);
                pawn.SetColor(layer, Color.white);

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(apparelRecord.apparel);
                if (def == null) {
                    Logger.Warning("Could not load thing definition for apparel \"" + apparelRecord.apparel + "\"");
                    Failed = true;
                    continue;
                }
                ThingDef stuffDef = null;
                if (!string.IsNullOrEmpty(apparelRecord.stuff)) {
                    stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelRecord.stuff);
                    if (stuffDef == null) {
                        Logger.Warning("Could not load stuff definition \"" + apparelRecord.stuff + "\" for apparel \"" + apparelRecord.apparel + "\"");
                        Failed = true;
                        continue;
                    }
                }
                pawn.SetSelectedApparel(layer, def);
                pawn.SetSelectedStuff(layer, stuffDef);
                pawn.SetColor(layer, apparelRecord.color);
            }

            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            for (int i = 0; i < record.implants.Count; i++) {
                SaveRecordImplantV3 implantRecord = record.implants[i];
                UniqueBodyPart uniqueBodyPart = healthOptions.FindBodyPartByName(implantRecord.bodyPart, implantRecord.bodyPartIndex != null ? implantRecord.bodyPartIndex.Value : 0);
                if (uniqueBodyPart == null) {
                    uniqueBodyPart = FindReplacementBodyPart(healthOptions, implantRecord.bodyPart);
                }
                if (uniqueBodyPart == null) {
                    Logger.Warning("Could not add the implant because it could not find the needed body part \"" + implantRecord.bodyPart + "\""
                        + (implantRecord.bodyPartIndex != null ? " with index " + implantRecord.bodyPartIndex : ""));
                    Failed = true;
                    continue;
                }
                BodyPartRecord bodyPart = uniqueBodyPart.Record;
                if (implantRecord.recipe != null) {
                    RecipeDef recipeDef = FindRecipeDef(implantRecord.recipe);
                    if (recipeDef == null) {
                        Logger.Warning("Could not add the implant because it could not find the recipe definition \"" + implantRecord.recipe + "\"");
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
                        Logger.Warning("Could not apply the saved implant recipe \"" + implantRecord.recipe + "\" to the body part \"" + bodyPart.def.defName + "\".  Recipe does not support that part.");
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
                    Logger.Warning("Could not add the injury because it could not find the hediff definition \"" + injuryRecord.hediffDef + "\"");
                    Failed = true;
                    continue;
                }
                InjuryOption option = healthOptions.FindInjuryOptionByHediffDef(def);
                if (option == null) {
                    Logger.Warning("Could not add the injury because it could not find a matching injury option for the saved hediff \"" + injuryRecord.hediffDef + "\"");
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
                        Logger.Warning("Could not add the injury because it could not find the needed body part \"" + injuryRecord.bodyPart + "\""
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
                Logger.Warning("Couldn't find relationship definition: " + saved.relation);
                return null;
            }
            else if (result.source == null) {
                Logger.Warning("Couldn't find relationship source pawn: " + saved.source);
                return null;
            }
            else if (result.target == null) {
                Logger.Warning("Couldn't find relationship target pawn: " + saved.source);
                return null;
            }
            else if (result.inverseDef == null) {
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

