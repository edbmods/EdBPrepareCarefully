using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetSaver {
        public PawnSaver PawnSaver { get; set; }

        private string GenerateId() {
            return Guid.NewGuid().ToString();
        }

        public void SaveToFile(ModState state, string presetName) {
            bool problem = false;
            try {
                // Verify that all pawns have non-null identifiers.
                foreach (CustomizedPawn customizedPawn in state.Customizations.AllPawns) {
                    if (customizedPawn.Id == null) {
                        customizedPawn.Id = GenerateId();
                    }
                }
                foreach (var g in state.Customizations.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent != null && parent.Id == null) {
                            parent.Id = GenerateId();
                        }
                        foreach (var child in g.Children) {
                            if (child != null && child.Id == null) {
                                child.Id = GenerateId();
                            }
                        }
                    }
                }

                SaveRecordPresetV5 preset = new SaveRecordPresetV5();
                preset.mods = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                foreach (var pawn in state.Customizations.TemporaryPawns) {
                    preset.temporaryPawns.Add(new SaveRecordTemporaryPawnV5() {
                        id = pawn.Id,
                        gender = pawn.Pawn?.gender.ToString()
                    });
                }
                foreach (CustomizedPawn customizedPawn in state.Customizations.AllPawns) {
                    if (customizedPawn.Type != CustomizedPawnType.Hidden) {
                        SaveRecordPawnV5 pawn = PawnSaver.ConvertCustomizedPawnToSaveRecord(customizedPawn);
                        preset.pawns.Add(pawn);
                    }
                }
                foreach (var g in state.Customizations.ParentChildGroups) {
                    HashSet<string> idSet = new HashSet<string>(preset.temporaryPawns.Select(p => p.id));
                    foreach (var parent in g.Parents) {
                        if (parent.Type == CustomizedPawnType.Hidden || parent.Type == CustomizedPawnType.Temporary) {
                            if (!idSet.Contains(parent.Id)) {
                                preset.temporaryPawns.Add(new SaveRecordTemporaryPawnV5() {
                                    id = parent.Id,
                                    gender = parent.Pawn?.gender.ToString()
                                });
                                idSet.Add(parent.Id);
                            }
                        }
                    }
                    foreach (var child in g.Children) {
                        if (child.Type == CustomizedPawnType.Hidden || child.Type == CustomizedPawnType.Temporary) {
                            if (!idSet.Contains(child.Id)) {
                                preset.temporaryPawns.Add(new SaveRecordTemporaryPawnV5() {
                                    id = child.Id,
                                    gender = child.Pawn?.gender.ToString()
                                });
                                idSet.Add(child.Id);
                            }
                        }
                    }
                }
                foreach (var r in state.Customizations.Relationships) {
                    if (r.Source != null && r.Target != null && r.Def != null && r.Source.Id != null && r.Target.Id != null) {
                        SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3(r);
                        preset.relationships.Add(s);
                    }
                    else {
                        problem = true;
                        Logger.Warning("Found an invalid custom relationship when saving a preset: " + presetName);
                        if (r.Target != null && r.Source != null) {
                            Logger.Warning("  Relationship = { source = " + r.Source.Id + ", target = " + r.Target.Id + ", relationship = " + r.Def + "}");
                        }
                        else {
                            Logger.Warning("  Relationship = { source = " + r.Source + ", target = " + r.Target + ", relationship = " + r.Def + "}");
                        }
                    }
                }
                foreach (var g in state.Customizations.ParentChildGroups) {
                    if (g.Children.Count == 0 || (g.Parents.Count == 0 && g.Children.Count == 1)) {
                        continue;
                    }
                    SaveRecordParentChildGroupV3 group = new SaveRecordParentChildGroupV3();
                    group.parents = new List<string>();
                    group.children = new List<string>();
                    foreach (var p in g.Parents) {
                        if (p.Pawn == null) {
                            problem = true;
                            Logger.Warning("Found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.parents.Add(p.Id);
                        }
                    }
                    foreach (var p in g.Children) {
                        if (p.Pawn == null) {
                            problem = true;
                            Logger.Warning("Found an invalid parent/child relationship when saving the preset: " + presetName);
                            continue;
                        }
                        else {
                            group.children.Add(p.Id);
                        }
                    }
                    preset.parentChildGroups.Add(group);
                }
                foreach (var e in state.Customizations.Equipment) {
                    SaveRecordEquipmentV3 record = new SaveRecordEquipmentV3(e);
                    preset.equipment.Add(record);
                }

                // Start saving.
                Scribe.saver.InitSaving(PresetFiles.FilePathForSavedPreset(presetName), "preset");
                preset.ExposeData();
            }
            catch (Exception e) {
                // TODO
                //PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.SaveFailed".Translate());
                Logger.Error("Failed to save preset file");
                throw e;
            }
            finally {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
                if (problem) {
                    // TODO
                    //PrepareCarefully.Instance.State.AddError("EdB.PC.Dialog.Preset.Error.PartialSaveFailure".Translate());
                }
            }
        }
    }
}
