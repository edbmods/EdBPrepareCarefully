using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class ColonistFiles
	{
		public static string SavedColonistsFolderPath {
			get {
				try {
					return (string) typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "PrepareCarefully" });
				}
				catch (Exception e) {
					Log.Error("Failed to get colonist save directory");
					throw e;
				}
			}
		}

		public static string FilePathForSavedColonist(string colonistName)
		{
			return Path.Combine(SavedColonistsFolderPath, colonistName + ".pcc");
		}

		//
		// Static Properties
		//
		public static IEnumerable<FileInfo> AllFiles {
			get {
				DirectoryInfo directoryInfo = new DirectoryInfo(SavedColonistsFolderPath);
				if (!directoryInfo.Exists) {
					directoryInfo.Create();
				}
				return from f in directoryInfo.GetFiles()
						where f.Extension == ".pcc"
					orderby f.LastWriteTime descending
					select f;
			}
		}

		//
		// Static Methods
		//
		public static bool HaveColonistNamed(string colonistName)
		{
			foreach (string current in from f in AllFiles
				select Path.GetFileNameWithoutExtension(f.Name)) {
				if (current == colonistName) {
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
				text = "Colonist" + num.ToString();
				num++;
			}
			while (HaveColonistNamed(text));
			return text;
		}
	}
}

