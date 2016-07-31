using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class PresetFiles
	{
		public static string SavedPresetsFolderPath {
			get {
				try {
					return (string) typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "PrepareCarefully" });
				}
				catch (Exception e) {
					Log.Error("Failed to get preset save directory");
					throw e;
				}
			}
		}

		public static string FilePathForSavedPreset(string presetName)
		{
			return Path.Combine(SavedPresetsFolderPath, presetName + ".pcp");
		}

		//
		// Static Properties
		//
		public static IEnumerable<FileInfo> AllFiles {
			get {
				DirectoryInfo directoryInfo = new DirectoryInfo(SavedPresetsFolderPath);
				if (!directoryInfo.Exists) {
					directoryInfo.Create();
				}
				return from f in directoryInfo.GetFiles()
						where f.Extension == ".pcp"
					orderby f.LastWriteTime descending
					select f;
			}
		}

		//
		// Static Methods
		//
		public static bool HavePresetNamed(string presetName)
		{
			foreach (string current in from f in AllFiles
				select Path.GetFileNameWithoutExtension(f.Name)) {
				if (current == presetName) {
					return true;
				}
			}
			return false;
		}

		public static string UnusedDefaultName()
		{
			string text = string.Empty;
			int num = 1;
			do {
				text = "Preset" + num.ToString();
				num++;
			}
			while (HavePresetNamed(text));
			return text;
		}
	}
}

