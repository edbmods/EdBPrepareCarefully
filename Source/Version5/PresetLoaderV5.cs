using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoaderV5 {
        public bool Failed = false;
        protected PawnLoaderV5 pawnLoader = new PawnLoaderV5();

        public bool Load(PrepareCarefully loadout, string presetName) {
            SaveRecordPresetV5 preset = new SaveRecordPresetV5();
            Failed = false;
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
                preset.ExposeData();

                if (ModsConfig.IdeologyActive) {
                    pawnLoader.IdeoMap = ResolveIdeoMap(preset);
                }

                if (preset.equipment != null) {
                    List<EquipmentSelection> equipment = new List<EquipmentSelection>(preset.equipment.Count);
                    foreach (var e in preset.equipment) {
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
                Logger.Error("Failed to load preset file", e);
                throw e;
            }
            finally {
                PresetLoader.ClearSaveablesAndCrossRefs();
            }

            List<CustomPawn> allPawns = new List<CustomPawn>();
            List<CustomPawn> colonistCustomPawns = new List<CustomPawn>();
            List<CustomPawn> hiddenCustomPawns = new List<CustomPawn>();
            try {
                foreach (SaveRecordPawnV5 p in preset.pawns) {
                    CustomPawn pawn = pawnLoader.ConvertSaveRecordToPawn(p);
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
    }
}
