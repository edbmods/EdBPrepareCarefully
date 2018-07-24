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
                Scribe.saver.InitSaving(ColonistFiles.FilePathForSavedColonist(colonistName), "character");
                string versionStringFull = "4";
                Scribe_Values.Look<string>(ref versionStringFull, "version", null, false);
                string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>)(mod => mod.Name)), true);
                Scribe_Values.Look<string>(ref modString, "mods", null, false);

                SaveRecordPawnV4 pawn = new SaveRecordPawnV4(customPawn);
                Scribe_Deep.Look<SaveRecordPawnV4>(ref pawn, "pawn");
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
