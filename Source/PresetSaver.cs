using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully {
    public static class PresetSaver {
        //
        // Static Methods
        //
        public static void SaveToFile(PrepareCarefully data, string presetName) {
            try {
                Scribe.saver.InitSaving(PresetFiles.FilePathForSavedPreset(presetName), "preset");
                string versionStringFull = "3";
                Scribe_Values.Look<string>(ref versionStringFull, "version", null, false);
                bool usePoints = data.Config.pointsEnabled;
                int startingPoints = PrepareCarefully.Instance.StartingPoints;
                Scribe_Values.Look<bool>(ref usePoints, "usePoints", false, true);
                Scribe_Values.Look<int>(ref startingPoints, "startingPoints", 0, true);
                string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                Scribe_Values.Look<string>(ref modString, "mods", null, false);
                Scribe.EnterNode("colonists");
                foreach (CustomPawn customPawn in data.Pawns) {
                    SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
                    Scribe_Deep.Look<SaveRecordPawnV3>(ref pawn, "colonist");
                }
                Scribe.ExitNode();

                List<CustomPawn> hiddenPawns = new List<CustomPawn>();
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Hidden) {
                            hiddenPawns.Add(parent.Pawn);
                        }
                        foreach (var child in g.Children) {
                            if (child.Hidden) {
                                hiddenPawns.Add(child.Pawn);
                            }
                        }
                    }
                }
                Scribe.EnterNode("hiddenPawns");
                foreach (CustomPawn customPawn in hiddenPawns) {
                    SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
                    Scribe_Deep.Look<SaveRecordPawnV3>(ref pawn, "hiddenPawn");
                }
                Scribe.ExitNode();

                Scribe.EnterNode("relationships");
                foreach (var r in data.RelationshipManager.Relationships) {
                    SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3(r);
                    Scribe_Deep.Look<SaveRecordRelationshipV3>(ref s, "relationship");
                }
                foreach (var g in data.RelationshipManager.ParentChildGroups) {
                    foreach (var parent in g.Parents) {
                        if (parent.Hidden) {
                            hiddenPawns.Add(parent.Pawn);
                        }
                        foreach (var child in g.Children) {
                            SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3();
                            s.source = child.Pawn.Id;
                            s.target = parent.Pawn.Id;
                            s.relation = "Parent";
                            Scribe_Deep.Look<SaveRecordRelationshipV3>(ref s, "relationship");
                        }
                    }
                }
                Scribe.ExitNode();

                Scribe.EnterNode("equipment");
                foreach (var e in data.Equipment) {
                    EquipmentSaveRecord record = new EquipmentSaveRecord(e);
                    Scribe_Deep.Look<EquipmentSaveRecord>(ref record, "equipment");
                }
                Scribe.ExitNode();
            }
            catch (Exception e) {
                Log.Error("Failed to save preset file");
                throw e;
            }
            finally {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }
    }
}
