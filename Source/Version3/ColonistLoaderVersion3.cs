using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace EdB.PrepareCarefully
{
	public class ColonistLoaderVersion3
	{
		public bool Load(PrepareCarefully loadout, Page_ConfigureStartingPawnsCarefully charMakerPage, string colonistName)
		{
			SaveRecordPawnV3 pawnRecord = new SaveRecordPawnV3();
			string modString = "";
			string version = "";
			try {
				Scribe.InitLoading(ColonistFiles.FilePathForSavedColonist(colonistName));
				Scribe_Values.LookValue<string>(ref version, "version", "unknown", false);
				Scribe_Values.LookValue<string>(ref modString, "mods", "", false);

				try {
					Scribe_Deep.LookDeep<SaveRecordPawnV3>(ref pawnRecord, "colonist", null);
				}
				catch (Exception e) {
					Messages.Message(modString, MessageSound.Silent);
					Messages.Message("EdB.ColonistLoadFailed".Translate(), MessageSound.RejectInput);
					Log.Warning(e.ToString());
					Log.Warning("Colonist was created with the following mods: " + modString);
					return false;
				}
			}
			catch (Exception e) {
				Log.Error("Failed to load preset file");
				throw e;
			}
			finally {
				Scribe.mode = LoadSaveMode.Inactive;
			}

			PresetLoaderVersion3 loader = new PresetLoaderVersion3();
			charMakerPage.AddColonist(loader.LoadPawn(pawnRecord));
			if (loader.Failed) {
				Messages.Message(loader.ModString, MessageSound.Silent);
				Messages.Message("EdB.ColonistThingDefFailed".Translate(), MessageSound.SeriousAlert);
				Log.Warning("Preset was created with the following mods: " + modString);
				return false;
			}

			return true;
		}
	}
}

