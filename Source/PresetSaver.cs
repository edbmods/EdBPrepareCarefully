using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class PresetSaver
	{
		//
		// Static Methods
		//
		public static void SaveToFile(PrepareCarefully data, string presetName)
		{
			try {
				Scribe.InitWriting(PresetFiles.FilePathForSavedPreset(presetName), "preset");
				string versionStringFull = "3";
				Scribe_Values.LookValue<string>(ref versionStringFull, "version", null, false);
				bool usePoints = data.Config.pointsEnabled;
				int startingPoints = PrepareCarefully.Instance.StartingPoints;
				Scribe_Values.LookValue<bool>(ref usePoints, "usePoints", false, true);
				Scribe_Values.LookValue<int>(ref startingPoints, "startingPoints", 0, true);
				string modString = GenText.ToCommaList(Enumerable.Select<ModContentPack, string>(LoadedModManager.RunningMods, (Func<ModContentPack, string>) (mod => mod.Name)), true);
				Scribe_Values.LookValue<string>(ref modString, "mods", null, false);
				Scribe.EnterNode("colonists");
				foreach (CustomPawn customPawn in data.Pawns) {
					SaveRecordPawnV3 pawn = new SaveRecordPawnV3(customPawn);
					Scribe_Deep.LookDeep<SaveRecordPawnV3>(ref pawn, "colonist");
				}
				Scribe.ExitNode();

				Scribe.EnterNode("relationships");
				foreach (var r in data.RelationshipManager.ExplicitRelationships) {
					SaveRecordRelationshipV3 s = new SaveRecordRelationshipV3(r);
					Scribe_Deep.LookDeep<SaveRecordRelationshipV3>(ref s, "relationship");
				}
				Scribe.ExitNode();

				Scribe.EnterNode("equipment");
				foreach (var e in data.Equipment) {
					SelectedEquipment customPawn = e;
					Scribe_Deep.LookDeep<SelectedEquipment>(ref customPawn, "equipment");
				}
				Scribe.ExitNode();
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
