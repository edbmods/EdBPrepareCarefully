using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderV5 {
        public bool Failed = false;
        public PawnLoaderV5 PawnLoaderV5 { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }
        public ManagerRelationships ManagerRelationships { get; set; }

        public PresetLoaderResult Load(string presetName) {
            PresetLoaderResult result = new PresetLoaderResult();
            Customizations customizations = new Customizations();
            SaveRecordPresetV5 preset = new SaveRecordPresetV5();
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
                preset.ExposeData();

                //if (ModsConfig.IdeologyActive) {
                //    pawnLoader.IdeoMap = ResolveIdeoMap(preset);
                //}
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file", e);
                throw e;
            }
            finally {
                UtilitySaveLoad.ClearSaveablesAndCrossRefs();
            }

            LoadEquipment(preset, result, customizations);
            LoadPawns(preset, result, customizations);
            LoadTemporaryPawns(preset, result, customizations);
            LoadRelationships(preset, result, customizations);
            LoadParentChildGroups(preset, result, customizations);

            result.Customizations = customizations;

            return result;
        }

        protected void LoadEquipment(SaveRecordPresetV5 preset, PresetLoaderResult result, Customizations customizations) {
            if (preset.equipment == null) {
                return;
            }
            List<CustomizedEquipment> customizedEquipment = new List<CustomizedEquipment>(preset.equipment.Count);
            foreach (var e in preset.equipment) {
                if (e.count < 1) {
                    continue;
                }
                if (e.def == null && e.spawnType == "Animal" && e.gender == null) {
                    customizedEquipment.Add(new CustomizedEquipment() {
                        EquipmentOption = EquipmentDatabase.RandomAnimalEquipmentOption,
                        Count = e.count,
                    });
                    continue;
                }
                ThingDef thingDef = e?.def != null ? DefDatabase<ThingDef>.GetNamedSilentFail(e.def) : null;
                if (thingDef == null) {
                    result.AddWarning(string.Format("Could not load thing definition for equipment \"{0}\"", e.def));
                    continue;
                }
                EquipmentOption equipmentOption = EquipmentDatabase.FindOptionForThingDef(thingDef);
                if (equipmentOption == null) {
                    result.AddWarning(string.Format("No equipment option found for equipment \"{0}\"", e.def));
                    continue;
                }
                ThingDef stuffDef = e.stuffDef != null ? DefDatabase<ThingDef>.GetNamedSilentFail(e.stuffDef) : null;
                Gender gender = Gender.Female;;
                try {
                    gender = (Gender)Enum.Parse(typeof(Gender), e.gender);
                }
                catch {}
                QualityCategory? quality = null;
                try {
                    quality = Enum.Parse(typeof(QualityCategory), e.quality) as QualityCategory?;
                }
                catch {
                }
                EquipmentSpawnType? spawnType = null;
                try {
                    spawnType = Enum.Parse(typeof(EquipmentSpawnType), e.quality) as EquipmentSpawnType?;
                }
                catch {
                    spawnType = EquipmentDatabase.DefaultSpawnTypeForThingDef(thingDef);
                }
                customizedEquipment.Add(new CustomizedEquipment() {
                    EquipmentOption = equipmentOption,
                    StuffDef = stuffDef,
                    Quality = quality,
                    SpawnType = spawnType,
                    Count = e.count
                });
            }
            customizations.Equipment = customizedEquipment;
        }

        protected void LoadPawns(SaveRecordPresetV5 preset, PresetLoaderResult result, Customizations customizations) {
            try {
                if (preset.pawns == null) {
                    return;
                }
                customizations.ColonyPawns = new List<CustomizedPawn>();
                customizations.WorldPawns = new List<CustomizedPawn>();
                foreach (SaveRecordPawnV5 p in preset.pawns) {
                    PawnLoaderResult pawnLoaderResult = PawnLoaderV5.ConvertSaveRecordToCustomizedPawn(p);
                    if (pawnLoaderResult.Pawn != null) {
                        if (pawnLoaderResult.Pawn.Type == CustomizedPawnType.Colony) {
                            customizations.ColonyPawns.Add(pawnLoaderResult.Pawn);
                        }
                        else if (pawnLoaderResult.Pawn.Type == CustomizedPawnType.World) {
                            customizations.WorldPawns.Add(pawnLoaderResult.Pawn);
                        }
                    }
                    result.Problems.AddRange(pawnLoaderResult.Problems.ConvertAll(problem => new PresetLoaderResult.Problem() { Severity = problem.Severity, Message = problem.Message }));
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Error while loading preset", e);
                Logger.Warning("Preset was created with the following mods: " + preset.mods);
            }
        }

        protected void LoadRelationships(SaveRecordPresetV5 preset, PresetLoaderResult result, Customizations customizations) {

            bool atLeastOneRelationshipFailed = false;
            RelationshipList relationships = new RelationshipList();
            if (preset.relationships == null) {
                return;
            }
            try {
                foreach (SaveRecordRelationshipV3 r in preset.relationships) {
                    if (string.IsNullOrEmpty(r.source) || string.IsNullOrEmpty(r.target) || string.IsNullOrEmpty(r.relation)) {
                        atLeastOneRelationshipFailed = true;
                        Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                        continue;
                    }
                    CustomizedRelationship relationship = LoadRelationship(r, customizations.AllPawns);
                    if (relationship == null) {
                        atLeastOneRelationshipFailed = true;
                        Logger.Warning("Failed to load a custom relationship from the preset: " + r);
                    }
                    else {
                        relationships.Add(relationship);
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Error while loading preset", e);
                Logger.Warning("Preset was created with the following mods: " + preset.mods);
                //return false;
            }
            if (atLeastOneRelationshipFailed) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
            }
            customizations.Relationships = relationships;
        }

        protected void LoadTemporaryPawns(SaveRecordPresetV5 preset, PresetLoaderResult result, Customizations customizations) {
            if (preset.temporaryPawns != null) {
                foreach (var temporaryPawn in preset.temporaryPawns) {
                    Gender gender = Gender.None;
                    try {
                        gender = (Gender) Enum.Parse(typeof(Gender), temporaryPawn.gender);
                    }
                    catch { }
                    customizations.TemporaryPawns.Add(new CustomizedPawn() {
                        Id = temporaryPawn.id,
                        Type = CustomizedPawnType.Temporary,
                        TemporaryPawn = new TemporaryPawn() {
                            Gender = gender
                        }
                    });
                }
            }
        }

        protected void LoadParentChildGroups(SaveRecordPresetV5 preset, PresetLoaderResult result, Customizations customizations) {
            if (preset.parentChildGroups == null) {
                return;
            }
            Dictionary<string, CustomizedPawn> temporaryPawns = new Dictionary<string, CustomizedPawn>();
            foreach (var temporaryPawn in customizations.TemporaryPawns) {
                temporaryPawns.Add(temporaryPawn.Id, temporaryPawn);
            }
            foreach (var groupRecord in preset.parentChildGroups) {
                ParentChildGroup group = new ParentChildGroup();
                if (groupRecord.parents != null) {
                    foreach (var id in groupRecord.parents) {
                        CustomizedPawn parent = FindPawnById(id, customizations.AllPawns);
                        if (parent != null) {
                            var pawn = parent;
                            if (pawn != null) {
                                group.Parents.Add(pawn);
                            }
                            else {
                                Logger.Warning("Could not load a custom parent relationship because it could not find a matching pawn in the relationship manager.");
                            }
                        }
                        else if (temporaryPawns.ContainsKey(id)) {
                            group.Parents.Add(temporaryPawns[id]);
                        }
                        else {
                            Logger.Warning("Could not load a custom parent relationship because it could not find a pawn with the saved identifer.");
                        }
                    }
                }
                if (groupRecord.children != null) {
                    foreach (var id in groupRecord.children) {
                        CustomizedPawn child = FindPawnById(id, customizations.AllPawns);
                        if (child != null) {
                            var pawn = child;
                            if (pawn != null) {
                                group.Children.Add(pawn);
                            }
                            else {
                                Logger.Warning("Could not load a custom child relationship because it could not find a matching pawn in the relationship manager.");
                            }
                        }
                        else if (temporaryPawns.ContainsKey(id)) {
                            group.Parents.Add(temporaryPawns[id]);
                        }
                        else {
                            Logger.Warning("Could not load a custom child relationship because it could not find a pawn with the saved identifer.");
                        }
                    }
                }
                if (group.Parents.Count > 0 && group.Children.Count > 0) {
                    customizations.ParentChildGroups.Add(group);
                    Logger.Debug("Loaded parent child group");
                }
            }
            ManagerRelationships.ReassignHiddenPawnIndices();
            foreach (var p in temporaryPawns.Values) {
                var customizedPawn = ManagerRelationships.CreateNewTemporaryPawn(p.TemporaryPawn.Gender);
                p.Pawn = customizedPawn.Pawn;
                p.TemporaryPawn = customizedPawn.TemporaryPawn;
            }
        }

        protected Dictionary<string, Ideo> ResolveIdeoMap(SaveRecordPresetV5 preset) {
            Dictionary<string, Ideo> ideoMap = new Dictionary<string, Ideo>();
            Dictionary<string, SaveRecordIdeoV5> uniqueSaveRecordsToResolve = new Dictionary<string, SaveRecordIdeoV5>();
            // Go through the pawns and look at their ideo record.  If their saved ideo was not the same as the colony ideo, we'll need to
            // try to find a matching ideo in the world.  Create a collection of all of the ideo save records that we need to match.
            foreach (var p in preset.pawns) {
                if (p.ideo != null && p.ideo.name != null && !p.ideo.sameAsColony) {
                    if (!uniqueSaveRecordsToResolve.ContainsKey(p.ideo.name)) {
                        uniqueSaveRecordsToResolve.Add(p.ideo.name, p.ideo);
                    }
                }
            }

            // If there are any save records that we need to match, do the matching
            if (uniqueSaveRecordsToResolve.Count > 0) {
                // Create a set of all of the ideos in the world.  Every time we match against one of them, we remove it from the set.
                HashSet<Ideo> ideosToMatchAgainst = new HashSet<Ideo>(Find.World.ideoManager.IdeosInViewOrder);
                // We remove the colony's ideo from those that we're matching against.
                Ideo primaryIdeo = Find.World.factionManager.OfPlayer?.ideos?.PrimaryIdeo;
                if (primaryIdeo != null) {
                    ideosToMatchAgainst.Remove(primaryIdeo);
                }
                
                // Find the best matching ideo for the save record.  As we find matches, we'll remove the save record from the unique save records
                // to keep track of which ones we failed to match.
                foreach (var r in uniqueSaveRecordsToResolve.Values.ToList()) {
                    // Validate the culture and memes in the save record so that we're only matching against values that are actually in the game.
                    if (r.culture != null && DefDatabase<CultureDef>.GetNamedSilentFail(r.culture) == null) {
                        r.culture = null;
                    }
                    List<string> validatedMemes;
                    if (r.memes != null) {
                        validatedMemes = r.memes.Where(m => m != null && DefDatabase<MemeDef>.GetNamedSilentFail(m) != null).ToList();
                    }
                    else {
                        validatedMemes = new List<string>();
                    }
                    r.memes = validatedMemes;

                    Ideo ideo = FindBestMatch(r, ideosToMatchAgainst);
                    if (ideo != null) {
                        ideoMap.Add(r.name, ideo);
                        ideosToMatchAgainst.Remove(ideo);
                        uniqueSaveRecordsToResolve.Remove(r.name);
                        //Logger.Debug(string.Format("Found match for ideo \"{0}\" with memes ({1}) => ideo \"{2}\" with memes ({3})", r.name, string.Join(", ", r.memes),
                        //    ideo.name, string.Join(", ", ideo.memes.Select(m => m.defName))));
                    }
                    else {
                        //Logger.Debug(string.Format("Found no match for ideo \"{0}\" with memes ({1})", r.name, string.Join(", ", r.memes)));
                    }
                }

                // For any save record that we failed to match, pick a random ideo from the remaining available ideos in the world.
                if (uniqueSaveRecordsToResolve.Count > 0) {
                    foreach (var r in uniqueSaveRecordsToResolve.Values) {
                        if (ideosToMatchAgainst.Count < 1) {
                            //Logger.Debug(string.Format("No ideos left available for matching.  Could not match ideo \"{0}\" with memes ({1})", r.name, string.Join(", ", r.memes)));
                            break;
                        }
                        Ideo ideo = ideosToMatchAgainst.RandomElement();
                        ideoMap.Add(r.name, ideo);
                        ideosToMatchAgainst.Remove(ideo);
                        //Logger.Debug(string.Format("Picked random ideo to match ideo \"{0}\" with memes ({1}) => ideo \"{2}\" with memes ({3})", r.name, string.Join(", ", r.memes),
                        //    ideo.name, string.Join(", ", ideo.memes.Select(m => m.defName))));
                    }
                }

            }
            return ideoMap;
        }

        protected Ideo FindBestMatch(SaveRecordIdeoV5 record, HashSet<Ideo> ideosToMatchAgainst) {
            float bestScore = 0;
            Ideo bestMatch = null;
            // To find the best match we try to find the ideo with a matching culture and the most matching memes.
            foreach (var ideo in ideosToMatchAgainst) {
                float score = 0;
                // We don't think that the name matters too much in the matching.  Faction ideos will be different for every world generation, so unless the same faction ideos
                // get restored thanks to a mod, they should be different every time.  Even so, we add a little score for a matching name so that it wins any ties.
                if (ideo.name == record.name) {
                    score += 0.1f;
                }
                if (record.culture != null && record.culture == ideo.culture.defName) {
                    score += 1.0f;
                }
                foreach (var memeName in record.memes) {
                    if (ideo.memes.Select(m => m.defName).Contains(memeName)) {
                        score += 1.0f;
                    }
                }
                // An ideo with the same culture should win ties.
                if (score > bestScore || (score == bestScore && record.culture == ideo.culture.defName)) {
                    bestScore = score;
                    bestMatch = ideo;
                }
            }
            return bestMatch;
        }
        
        protected CustomizedPawn FindPawnById(string id, IEnumerable<CustomizedPawn> colonistPawns) {
            CustomizedPawn result = colonistPawns.FirstOrDefault((CustomizedPawn c) => {
                return id == c.Id;
            });
            return result;
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

            result.Def = saved?.relation != null ? DefDatabase<PawnRelationDef>.GetNamedSilentFail(saved.relation) : null;
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
                Logger.Warning("Couldn't find relationship target pawn: " + saved.source);
                return null;
            }
            else if (result.InverseDef == null) {
                Logger.Warning("Couldn't determine inverse relationship: " + saved.relation);
                return null;
            }
            return result;
        }
    }
}
