using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class ColonistLoader {
        public static CustomPawn LoadFromFile(PrepareCarefully loadout, string name) {
            string version = "";
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(name));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
            }
            catch (Exception e) {
                Log.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if ("2".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.PreAlpha13NotSupported".Translate(), MessageSound.SeriousAlert);
                return null;
            }
            else if ("3".Equals(version)) {
                return new ColonistLoaderVersion3().Load(loadout, name);
            }
            else {
                throw new Exception("Invalid preset version");
            }
        }
    }
}

