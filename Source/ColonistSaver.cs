using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class ColonistSaver
	{
		//
		// Static Methods
		//
		public static void SaveToFile(PrepareCarefully loadout, Page_ConfigureStartingPawnsCarefully page, string colonistName)
		{
			try {
				Scribe.InitWriting(ColonistFiles.FilePathForSavedColonist(colonistName), "colonist");
				string versionStringFull = "3";
				Scribe_Values.LookValue<string>(ref versionStringFull, "version", null, false);
				string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>) (mod => mod.Name)), true);
				Scribe_Values.LookValue<string>(ref modString, "mods", null, false);

				SaveRecordPawnV3 pawn = new SaveRecordPawnV3(page.SelectedPawn);
				Scribe_Deep.LookDeep<SaveRecordPawnV3>(ref pawn, "colonist");
			}
			catch (Exception e) {
				Log.Error("Failed to save preset file");
				throw e;
			}
			finally {
				Scribe.FinalizeWriting();
				Scribe.mode = LoadSaveMode.Inactive;
			}
		}
	}
}
