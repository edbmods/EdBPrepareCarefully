using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public class PresetLoader
	{
		public static bool LoadFromFile(PrepareCarefully loadout, string presetName)
		{
			string version = "";
			bool result = false;
			try {
				Scribe.InitLoading(PresetFiles.FilePathForSavedPreset(presetName));
				Scribe_Values.LookValue<string>(ref version, "version", "unknown", false);
			}
			catch (Exception e) {
				Log.Error("Failed to load preset file");
				throw e;
			}
			finally {
				Scribe.mode = LoadSaveMode.Inactive;
			}
				
			if ("1".Equals(version)) {
				Messages.Message("EdB.PrepareCarefully.PresetVersionNotSupported".Translate(), MessageSound.SeriousAlert);
				return false;
			}
			else if ("2".Equals(version)) {
				Messages.Message("EdB.PrepareCarefully.PresetVersionNotSupported".Translate(), MessageSound.SeriousAlert);
				return false;
			}
			else if ("3".Equals(version)) {
				result = new PresetLoaderVersion3().Load(loadout, presetName);
			}
			else {
				throw new Exception("Invalid preset version");
			}

			return result;
		}
	}

}

