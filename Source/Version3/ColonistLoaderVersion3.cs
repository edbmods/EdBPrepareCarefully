using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace EdB.PrepareCarefully {
    public class ColonistLoaderVersion3 {
        public CustomPawn Load(PrepareCarefully loadout, string name) {
            SaveRecordPawnV3 pawnRecord = new SaveRecordPawnV3();
            string modString = "";
            string version = "";
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(name));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
                Scribe_Values.Look<string>(ref modString, "mods", "", false);

                try {
                    Scribe_Deep.Look<SaveRecordPawnV3>(ref pawnRecord, "colonist", null);
                }
                catch (Exception e) {
                    Messages.Message(modString, MessageSound.Silent);
                    Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageSound.RejectInput);
                    Log.Warning(e.ToString());
                    Log.Warning("Colonist was created with the following mods: " + modString);
                    return null;
                }
            }
            catch (Exception e) {
                Log.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if (pawnRecord == null) {
                Messages.Message(modString, MessageSound.Silent);
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageSound.RejectInput);
                Log.Warning("Colonist was created with the following mods: " + modString);
                return null;
            }

            PresetLoaderVersion3 loader = new PresetLoaderVersion3();
            CustomPawn loadedPawn = loader.LoadPawn(pawnRecord);
            if (loadedPawn != null) {
                return loadedPawn;
            }
            if (loader.Failed) {
                loadout.State.AddError(loader.ModString);
                loadout.State.AddError("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate());
                Log.Warning("Preset was created with the following mods: " + modString);
                return null;
            }

            return null;
        }
    }
}

