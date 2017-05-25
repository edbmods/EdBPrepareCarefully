using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully {
    public static class ColonistSaver {
        //
        // Static Methods
        //
        public static void SaveToFile(CustomPawn customPawn, string colonistName) {
            try {
                Scribe.saver.InitSaving(ColonistFiles.FilePathForSavedColonist(colonistName), "colonist");
                string versionStringFull = "3";
                Scribe_Values.Look<string>(ref versionStringFull, "version", null, false);
                string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                Scribe_Values.Look<string>(ref modString, "mods", null, false);

                SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
                Scribe_Deep.Look<SaveRecordPawnV3>(ref pawn, "colonist");
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
