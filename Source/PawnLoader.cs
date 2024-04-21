using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PawnLoader {
        public PawnLoaderV3 PawnLoaderV3 { get; set; }
        public PawnLoaderV5 PawnLoaderV5 { get; set; }

        public PawnLoaderResult Load(string file) {
            string version = "";
            try {
                Scribe.loader.InitLoading(ColonistFiles.FilePathForSavedColonist(file));
                Scribe_Values.Look<string>(ref version, "version", "unknown", false);
            }
            catch (Exception e) {
                Logger.Error("Failed to load preset file");
                throw e;
            }
            finally {
                Scribe.mode = LoadSaveMode.Inactive;
            }

            if ("2".Equals(version)) {
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.PreAlpha13NotSupported".Translate(), MessageTypeDefOf.ThreatBig);
                return null;
            }
            else if ("3".Equals(version)) {
                return PawnLoaderV3.Load(file);
            }
            else if ("4".Equals(version)) {
                return PawnLoaderV5.Load(file);
            }
            else if ("5".Equals(version)) {
                return PawnLoaderV5.Load(file);
            }
            else {
                throw new Exception("Invalid preset version");
            }
        }
    }
}

