using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public class ColonistLoader
	{
		public static bool LoadFromFile(PrepareCarefully loadout, Page_ConfigureStartingPawnsCarefully charMakerPage, string colonistName)
		{
			string version = "";
			bool result = false;
			try {
				Scribe.InitLoading(ColonistFiles.FilePathForSavedColonist(colonistName));
				Scribe_Values.LookValue<string>(ref version, "version", "unknown", false);
			}
			catch (Exception e) {
				Log.Error("Failed to load preset file");
				throw e;
			}
			finally {
				Scribe.mode = LoadSaveMode.Inactive;
			}

			if ("2".Equals(version)) {
				Messages.Message("EdB.PrepareCarefully.SavedColonistVersionNotSupported".Translate(), MessageSound.SeriousAlert);
				return false;
			}
			else if ("3".Equals(version)) {
				result = new ColonistLoaderVersion3().Load(loadout, charMakerPage, colonistName);
			}
			else {
				throw new Exception("Invalid preset version");
			}

			return result;
		}
	}
}

