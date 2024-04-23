using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class PresetLoader {

        public PresetLoaderV3 PresetLoaderV3 { get; set; }
        public PresetLoaderV5 PresetLoaderV5 { get; set; }

        public PresetLoaderResult LoadFromFile(string presetName) {
            string version = "";
            try {
                Scribe.loader.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if ("1".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.PreAlpha13NotSupported".Translate(), MessageTypeDefOf.ThreatBig);
                return null;
            }
            else if ("2".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.PreAlpha13NotSupported".Translate(), MessageTypeDefOf.ThreatBig);
                return null;
            }
            else if ("3".Equals(version)) {
                return PresetLoaderV3.Load(presetName);
            }
            else if ("4".Equals(version) || "5".Equals(version)) {
                return PresetLoaderV5.Load(presetName);
            }
            else {
                throw new Exception("Invalid preset version");
            }
        }

    }

}

