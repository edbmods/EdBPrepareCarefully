using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public static class PresetSaver {
        //
        // Static Methods
        //
        public static void SaveToFile(PrepareCarefully data, string presetName) {
            bool problem = false;
            try {
                // Verify that all pawns have non-null identifiers.
                foreach (CustomPawn customPawn in data.Pawns) {
                    if (customPawn.Id == null) {
                        customPawn.GenerateId();
                    }
                }
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Pawn != null && parent.Pawn.Id == null) {
                            parent.Pawn.GenerateId();
                        }
                        foreach (var child in g.Children) {
                            if (child.Pawn != null && child.Pawn.Id == null) {
                                child.Pawn.GenerateId();
                            }
                        }
                    }
                }

                // Start saving.
                Scribe.saver.InitSaving(PresetFiles.FilePathForSavedPreset(presetName), "preset");
                string versionStringFull = "3";
                Scribe_Values.Look<string>(ref versionStringFull, "version", null, false);
                bool usePoints = data.Config.pointsEnabled;
                int startingPoints = PrepareCarefully.Instance.StartingPoints;
                Scribe_Values.Look<bool>(ref usePoints, "usePoints", false, true);
                Scribe_Values.Look<int>(ref startingPoints, "startingPoints", 0, true);
                string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                Scribe_Values.Look<string>(ref modString, "mods", null, false);
                // Save pawns.
                Scribe.EnterNode("colonists");
                foreach (CustomPawn customPawn in data.Pawns) {
                    try {
                        SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
                        Scribe_Deep.Look<SaveRecordPawnV3>(ref pawn, "colonist");
                    }
                    catch (Exception e) {
                        problem = true;
                        Log.Warning("Prepare Carefully failed to save a pawn into the preset: " + presetName);
                        Log.Warning("  Exception: " + e.Message);
                    }
                }
                Scribe.ExitNode();

                // Save hidden pawns.
                List<CustomPawn> hiddenPawns = new List<CustomPawn>();
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Hidden) {
                            if (parent.Pawn != null) {
                                hiddenPawns.Add(parent.Pawn);
                            }
                            else {
                                Log.Warning("Prepare Carefully found an empty pawn in a parent child relationship while saving the preset.  Skipping that pawn.");
                            }
                        }
                        foreach (var child in g.Children) {
                            if (child.Hidden) {
                                if (child.Pawn != null) {
                                    hiddenPawns.Add(child.Pawn);
                                }
                                else {
                                    Log.Warning("Prepare Carefully found an empty pawn in a parent child relationship while saving the preset.  Skipping that pawn.");
                                }
                            }
                        }
                    }
                }
                Scribe.EnterNode("hiddenPawns");
                foreach (CustomPawn customPawn in hiddenPawns) {
                    try {
                        SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
                        Scribe_Deep.Look<SaveRecordPawnV3>(ref pawn, "hiddenPawn");
                    }
                    catch (Exception e) {
                        problem = true;
                        Log.Warning("Prepare Carefully failed to save a hidden pawn into the preset: " + presetName);
                        Log.Warning("  Exception: " + e.Message);
                    }
                }
                Scribe.ExitNode();

                Scribe.EnterNode("relationships");
                foreach (var r in data.RelationshipManager.Relationships) {
                    if (r.source != null && r.target != null && r.def != null && r.source.Id != null && r.target.Id != null) {
                        SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3(r);
                        Scribe_Deep.Look<SaveRecordRelationshipV3>(ref s, "relationship");
                    }
                    else {
                        problem = true;
                        Log.Warning("Prepare Carefully found an invalid custom relationship when saving a preset: " + presetName);
                        if (r.target != null && r.source != null) {
                            Log.Warning("  Relationship = { source = " + r.source.Id + ", target = " + r.target.Id + ", relationship = " + r.def + "}");
                        }
                        else {
                            Log.Warning("  Relationship = { source = " + r.source + ", target = " + r.target + ", relationship = " + r.def + "}");
                        }
                    }
                }
                Scribe.ExitNode();

                Scribe.EnterNode("parentChildGroups");
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    if (g.Children.Count == 0 || (g.Parents.Count == 0 && g.Children.Count == 1)) {
                        continue;
                    }
                    SaveRecordParentChildGroupV3 group = new SaveRecordParentChildGroupV3();
                    group.parents = new List<string>();
                    group.children = new List<string>();
                    foreach (var p in g.Parents) {
                        if (p.Pawn == null) {
                            problem = true;
                            Log.Warning("Prepare Carefully found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.parents.Add(p.Pawn.Id);
                        }
                    }
                    foreach (var p in g.Children) {
                        if (p.Pawn == null) {
                            problem = true;
                            Log.Warning("Prepare Carefully found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.children.Add(p.Pawn.Id);
                        }
                    }
                    try {
                        Scribe_Deep.Look<SaveRecordParentChildGroupV3>(ref group, "group");
                    }
                    catch (Exception) {
                        problem = true;
                        Log.Warning("Prepare Carefully failed to save a parent child group when saving the preset: " + presetName);
                    }
                }
                Scribe.ExitNode();

                Scribe.EnterNode("equipment");
                foreach (var e in data.Equipment) {
                    try {
                        EquipmentSaveRecord record = new EquipmentSaveRecord(e);
                        Scribe_Deep.Look<EquipmentSaveRecord>(ref record, "equipment");
                    }
                    catch {
                        problem = true;
                        Log.Warning("Failed to save equipment to preset: " + e.ThingDef.defName);
                    }
                }
                Scribe.ExitNode();
            }
            catch (Exception e) {
                PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.SaveFailed".Translate());
                Log.Error("Failed to save preset file");
                throw e;
            }
            finally {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
                if (problem) {
                    PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.PartialSaveFailure".Translate());
                }
            }
        }
    }
}
