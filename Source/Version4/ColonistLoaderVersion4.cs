using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace EdB.PrepareCarefully {
    public class ColonistLoaderVersion4 {
        public CustomPawn Load(PrepareCarefully loadout, string name) {
            SaveRecordPawnV4 pawnRecord = new SaveRecordPawnV4();
            string modString = "";
            string version = "";
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(name));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
                Scribe_Values.Look<string>(ref modString, "mods", "", false);

                try {
                    Scribe_Deep.Look<SaveRecordPawnV4>(ref pawnRecord, "pawn", null);
                }
                catch (Exception e) {
                    Messages.Message(modString, MessageTypeDefOf.SilentInput);
                    Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                    Logger.Warning(e.ToString());
                    Logger.Warning("Colonist was created with the following mods: " + modString);
                    return null;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file");
                throw e;
            }
            finally {
                PresetLoader.ClearSaveablesAndCrossRefs();
            }

            if (pawnRecord == null) {
                Messages.Message(modString, MessageTypeDefOf.SilentInput);
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                Logger.Warning("Colonist was created with the following mods: " + modString);
                return null;
            }

            PresetLoaderVersion4 loader = new PresetLoaderVersion4();
            CustomPawn loadedPawn = loader.LoadPawn(pawnRecord);
            if (loadedPawn != null) {
                CustomPawn idConflictPawn = PrepareCarefully.Instance.Pawns.FirstOrDefault((CustomPawn p) => {
                    return p.Id == loadedPawn.Id;
                });
                if (idConflictPawn != null) {
                    loadedPawn.GenerateId();
                }
                return loadedPawn;
            }
            else {
                loadout.State.AddError(modString);
                loadout.State.AddError("EdB.PC.Dialog.Preset.Error.NoCharacter".Translate());
                Logger.Warning("Preset was created with the following mods: " + modString);
                return null;
            }
        }
    }
}

