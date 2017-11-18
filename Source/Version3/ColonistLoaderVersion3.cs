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
                    Messages.Message(modString, MessageTypeDefOf.SilentInput);
                    Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
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
                // I don't fully understand how these cross-references and saveables are resolved, but
                // if we don't clear them out, we get null pointer exceptions.
                HashSet<IExposable> saveables = (HashSet<IExposable>)(typeof(PostLoadIniter).GetField("saveablesToPostLoad", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.initer));
                saveables.Clear();
                List<IExposable> crossReferencingExposables = (List<IExposable>)(typeof(CrossRefHandler).GetField("crossReferencingExposables", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Scribe.loader.crossRefs));
                crossReferencingExposables.Clear();
                Scribe.loader.FinalizeLoading();
            }

            if (pawnRecord == null) {
                Messages.Message(modString, MessageTypeDefOf.SilentInput);
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.RejectInput);
                Log.Warning("Colonist was created with the following mods: " + modString);
                return null;
            }

            PresetLoaderVersion3 loader = new PresetLoaderVersion3();
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
                loadout.State.AddError(loader.ModString);
                loadout.State.AddError("EdB.PC.Dialog.Preset.Error.NoCharacter".Translate());
                Log.Warning("Preset was created with the following mods: " + modString);
                return null;
            }
        }
    }
}

