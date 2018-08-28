using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoader {
        public static bool LoadFromFile(PrepareCarefully loadout, string presetName) {
            string version = "";
            bool result = false;
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
            }
            catch (Exception e) {
                Log.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if ("1".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.PreAlpha13NotSupported".Translate(), MessageTypeDefOf.ThreatBig);
                return false;
            }
            else if ("2".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.PreAlpha13NotSupported".Translate(), MessageTypeDefOf.ThreatBig);
                return false;
            }
            else if ("3".Equals(version)) {
                result = new PresetLoaderVersion3().Load(loadout, presetName);
            }
            else if ("4".Equals(version)) {
                result = new PresetLoaderVersion4().Load(loadout, presetName);
            }
            else {
                throw new Exception("Invalid preset version");
            }

            return result;
        }
    }

}

