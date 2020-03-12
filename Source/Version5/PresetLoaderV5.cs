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
                        Log.Warning("Preset was created with the following mods: " + preset.mods);
                    }
                }
            }
            catch (Exception e) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
                Log.Warning(e.ToString());
                Log.Warning("Preset was created with the following mods: " + preset.mods);
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
                    Log.Warning("Preset was created with the following mods: " + preset.mods);
                    return false;
                }
                if (atLeastOneRelationshipFailed) {
                    Messages.Message("EdB.PC.Dialog.Preset.Error.RelationshipFailed".Translate(), MessageTypeDefOf.ThreatBig);
                }
            }
            loadout.RelationshipManager.AddRelationships(allRelationships);

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
                Messages.Message(preset.mods, MessageTypeDefOf.SilentInput);
                Messages.Message("EdB.PC.Dialog.Preset.Error.ThingDefFailed".Translate(), MessageTypeDefOf.ThreatBig);
                Log.Warning("Preset was created with the following mods: " + preset.mods);
                return false;
            }

            return true;
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
    }
}
