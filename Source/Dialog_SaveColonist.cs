using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Dialog_SaveColonist : Dialog_Colonist
	{
		protected const float NewColonistNameButtonSpace = 20;
		protected const float NewColonistHeight = 35;
		protected const float NewColonistNameWidth = 400;
		protected static string Filename = "";

		private bool focusedColonistNameArea;

		public Dialog_SaveColonist(CustomPawn pawn)
		{
			this.interactButLabel = "OverwriteButton".Translate();
			this.bottomAreaHeight = 85;
			Filename = pawn.Pawn.LabelShort;
		}

		protected override void DoMapEntryInteraction(string colonistName)
		{
			Filename = colonistName;
			ColonistSaver.SaveToFile(PrepareCarefully.Instance, CharMakerPage, Filename);
			Messages.Message("SavedAs".Translate(new object[] {
				Filename
			}), MessageSound.Standard);
			Close(true);
		}

		protected override void DoSpecialSaveLoadGUI(Rect inRect)
		{
			GUI.BeginGroup(inRect);
			bool flag = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
			float top = inRect.height - 52;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.SetNextControlName("ColonistNameField");
			Rect rect = new Rect(5, top, 400, 35);
			string text = Widgets.TextField(rect, Filename);
			if (GenText.IsValidFilename(text)) {
				Filename = text;
			}
			if (!this.focusedColonistNameArea) {
				GUI.FocusControl("ColonistNameField");
				this.focusedColonistNameArea = true;
			}
			Rect butRect = new Rect(420, top, inRect.width - 400 - 20, 35);
			if (Widgets.ButtonText(butRect, "EdB.DialogSaveColonistButton".Translate(), true, false, true) || flag) {
				if (Filename.Length == 0) {
					Messages.Message("NeedAName".Translate(), MessageSound.RejectInput);
				}
				else {
					ColonistSaver.SaveToFile(PrepareCarefully.Instance, CharMakerPage, Filename);
					Messages.Message("SavedAs".Translate(new object[] {
						Filename
					}), MessageSound.Standard);
					Close(true);
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.EndGroup();
		}
	}
}

