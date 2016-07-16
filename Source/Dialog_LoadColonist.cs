using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Dialog_LoadColonist : Dialog_Colonist
	{
		public Dialog_LoadColonist()
		{
			this.interactButLabel = "EdB.DialogLoadColonistButton".Translate();
		}

		protected override void DoMapEntryInteraction(string colonistName)
		{
			bool result = ColonistLoader.LoadFromFile(PrepareCarefully.Instance, CharMakerPage, colonistName);
			if (result) {
				Messages.Message("EdB.LoadedColonist".Translate(new object[] {
					colonistName
				}), MessageSound.Standard);
			}
			Close(true);
		}
	}
}

