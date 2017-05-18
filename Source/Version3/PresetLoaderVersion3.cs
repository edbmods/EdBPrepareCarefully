using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderVersion3 {
        public bool Failed = false;
        public string ModString = "";

        public PresetLoaderVersion3() {
        }

        public bool Load(PrepareCarefully loadout, string presetName) {
            List<SaveRecordPawnV3> pawns = new List<SaveRecordPawnV3>();
            List<SaveRecordPawnV3> hiddenPawns = new List<SaveRecordPawnV3>();
            List<SaveRecordRelationshipV3> savedRelationships = new List<SaveRecordRelationshipV3>();
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
                    Messages.Message(ModString, MessageSound.Silent);
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageSound.SeriousAlert);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                try {
                    Scribe_Collections.Look<SaveRecordPawnV3>(ref hiddenPawns, "hiddenPawns", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message(ModString, MessageSound.Silent);
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageSound.SeriousAlert);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                try {
                    Scribe_Collections.Look<SaveRecordRelationshipV3>(ref savedRelationships, "relationships", LookMode.Deep, null);
                }
                catch (Exception e) {
                    Messages.Message(ModString, MessageSound.Silent);
                    Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageSound.SeriousAlert);
                    Log.Warning(e.ToString());
                    Log.Warning("Preset was created with the following mods: " + ModString);
                    return false;
                }

                List<EquipmentSaveRecord> tempEquipment = new List<EquipmentSaveRecord>();
                Scribe_Collections.Look<EquipmentSaveRecord>(ref tempEquipment, "equipment", LookMode.Deep, null);

                List<EquipmentSelection> equipment = new List<EquipmentSelection>(tempEquipment.Count);
                foreach (var e in tempEquipment) {
                    ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(e.def);
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
                            EquipmentRecord record = PrepareCarefully.Instance.EquipmentDatabase[key];
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
                                EquipmentRecord record = PrepareCarefully.Instance.EquipmentDatabase[key];
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

                // After loading items using the Scribe methods, the saveables that were loaded get
                // put into this saveablesToPostLoad map.  This post-load initialization is only
                // applicable for when we load a save game.  We need to clear our saveables out of
                // there so that they don't cause errors later.
                HashSet<IExposable> saveables = (HashSet<IExposable>)(typeof(PostLoadIniter).GetField("saveablesToPostLoad", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.initer));
                saveables.Clear();

                //PrepareCarefully.Instance.Config.pointsEnabled = usePoints;
            }
            catch (Exception e) {
                Log.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            List<CustomPawn> allPawns = new List<CustomPawn>();
            List<CustomPawn> colonistCustomPawns = new List<CustomPawn>();
            try {
                foreach (SaveRecordPawnV3 p in pawns) {
                    CustomPawn pawn = LoadPawn(p);
                    allPawns.Add(pawn);
                    colonistCustomPawns.Add(pawn);
                }
            }
            catch (Exception e) {
                Messages.Message(ModString, MessageSound.Silent);
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageSound.SeriousAlert);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }


            List<CustomPawn> hiddenCustomPawns = new List<CustomPawn>();
            try {
                foreach (SaveRecordPawnV3 p in hiddenPawns) {
                    CustomPawn pawn = LoadPawn(p);
                    allPawns.Add(pawn);
                    hiddenCustomPawns.Add(pawn);
                }
            }
            catch (Exception e) {
                Messages.Message(ModString, MessageSound.Silent);
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageSound.SeriousAlert);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            List<CustomRelationship> parentChildRelationships = new List<CustomRelationship>();
            List<CustomRelationship> otherRelationships = new List<CustomRelationship>();
            try {
                foreach (SaveRecordRelationshipV3 r in savedRelationships) {
                    CustomRelationship relationship = LoadRelationship(r, allPawns);
                    if (relationship == null) {
                        Messages.Message(ModString, MessageSound.Silent);
                        Messages.Message("EdB.PresetRelationshipLoadFailed".Translate(), MessageSound.SeriousAlert);
                        Log.Warning("Failed to load relationship: " + r.relation);
                        Log.Warning("Preset was created with the following mods: " + ModString);
                    }
                    else {
                        if (r.relation == "Parent" || r.relation == "Child") {
                            parentChildRelationships.Add(relationship);
                        }
                        else {
                            otherRelationships.Add(relationship);
                        }
                    }
                }
            }
            catch (Exception e) {
                Messages.Message(ModString, MessageSound.Silent);
                Messages.Message("EdB.PresetRelationshipLoadFailed".Translate(), MessageSound.SeriousAlert);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            loadout.ClearPawns();
            foreach (CustomPawn p in colonistCustomPawns) {
                loadout.AddPawn(p);
            }

            loadout.RelationshipManager.Clear();
            foreach (CustomRelationship r in otherRelationships) {
                loadout.RelationshipManager.AddRelationship(r.def, r.source, r.target);
            }
            loadout.RelationshipManager.InitializeParentChildRelationshipsForLoadedPawns(parentChildRelationships, colonistCustomPawns, hiddenCustomPawns);
            
            if (Failed) {
                Messages.Message(ModString, MessageSound.Silent);
                Messages.Message("EdB.PC.Dialog.Preset.Error.ThingDefFailed".Translate(), MessageSound.SeriousAlert);
                Log.Warning("Preset was created with the following mods: " + ModString);
                return false;
            }

            return true;
        }

        public CustomPawn LoadPawn(SaveRecordPawnV3 record) {
            PawnKindDef pawnKindDef = null;
            if (record.pawnKindDef != null) {
                pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(record.pawnKindDef);
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

            CustomPawn pawn = new CustomPawn(source);
            pawn.Id = record.id;

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
            pawn.SetColor(PawnLayers.Hair, record.hairColor);

            if (record.melanin >= 0.0f) {
                pawn.MelaninLevel = record.melanin;
            }
            else {
                pawn.MelaninLevel = PawnColorUtils.FindMelaninValueFromColor(record.skinColor);
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
            BodyType? bodyType = null;
            try {
                bodyType = (BodyType)Enum.Parse(typeof(BodyType), record.bodyType);
            }
            catch (Exception) {
                Log.Warning("Invalid body type value \"" + record.bodyType + "\"");
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
                if (name == "Research") {
                    name = "Intellectual";
                }
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

            for (int i = 0; i < PawnLayers.Count; i++) {
                if (PawnLayers.IsApparelLayer(i)) {
                    pawn.SetSelectedApparel(i, null);
                    pawn.SetSelectedStuff(i, null);
                }
            }
            for (int i = 0; i < record.apparelLayers.Count; i++) {
                int layer = record.apparelLayers[i];
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

            for (int i = 0; i < record.implants.Count; i++) {
                SaveRecordImplantV3 implantRecord = record.implants[i];
                BodyPartRecord bodyPart = PrepareCarefully.Instance.HealthManager.ImplantManager.FindReplaceableBodyPartByName(pawn.Pawn, implantRecord.bodyPart);
                if (bodyPart == null) {
                    Log.Warning("Could not find replaceable body part definition \"" + implantRecord.bodyPart + "\"");
                    Failed = true;
                    continue;
                }
                if (implantRecord.recipe != null) {
                    RecipeDef recipeDef = FindRecipeDef(implantRecord.recipe);
                    if (recipeDef == null) {
                        Log.Warning("Could not find recipe definition \"" + implantRecord.recipe + "\"");
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
                        Log.Warning("Body part \"" + bodyPart.def.defName + "\" does not match recipe used to replace it");
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
                    Log.Warning("Could not find hediff definition \"" + injuryRecord.hediffDef + "\"");
                    Failed = true;
                    continue;
                }
                InjuryOption option = PrepareCarefully.Instance.HealthManager.InjuryManager.FindOptionByHediffDef(def);
                if (option == null) {
                    Log.Warning("Could not find injury option for \"" + injuryRecord.hediffDef + "\"");
                    Failed = true;
                    continue;
                }
                BodyPartRecord bodyPart = null;
                if (injuryRecord.bodyPart != null) {
                    bodyPart = PrepareCarefully.Instance.HealthManager.FirstBodyPartRecord(pawn, injuryRecord.bodyPart);
                    if (bodyPart == null) {
                        Log.Warning("Could not find body part \"" + injuryRecord.bodyPart + "\"");
                        Failed = true;
                        continue;
                    }
                }
                Injury injury = new Injury();
                injury.Option = option;
                injury.BodyPartRecord = bodyPart;
                if (injuryRecord.severity != null) {
                    injury.Severity = injuryRecord.Severity;
                }
                pawn.AddInjury(injury);
            }

            pawn.RandomInjuries = record.randomInjuries;
            pawn.CopySkillLevelsToPawn();
            pawn.ClearPawnCaches();

            return pawn;
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
            return DefDatabase<RecipeDef>.GetNamedSilentFail(name);
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

