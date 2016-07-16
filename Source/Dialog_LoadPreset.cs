using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Dialog_LoadPreset : Dialog_Preset
	{
		public Dialog_LoadPreset()
		{
			this.interactButLabel = "EdB.LoadPresetButton".Translate();
		}

		protected override void DoMapEntryInteraction(string presetName)
		{
			bool result = PresetLoader.LoadFromFile(PrepareCarefully.Instance, presetName);
			if (result) {
				Messages.Message("EdB.LoadedPreset".Translate(new object[] {
					presetName
				}), MessageSound.Standard);
			}
			RemovePageFromStack();
			Close(true);
		}

		protected void RemovePageFromStack() {
			Window page = Find.WindowStack.WindowOfType<Page_ConfigureStartingPawnsCarefully>();
			if (page == null) {
				page = Find.WindowStack.WindowOfType<Page_Equipment>();
			}
			if (page != null) {
				Find.WindowStack.TryRemove(page, true);
			}
		}
	}
}

