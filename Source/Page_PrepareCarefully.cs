using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public abstract class Page_PrepareCarefully : Window
	{
		private bool broken = false;
		protected bool hidden = false;

		protected static Color ColorText = new Color(0.80f, 0.80f, 0.80f);

		public Page_PrepareCarefully()
		{

		}

		protected bool IsBroken {
			get {
				return broken;
			}
		}

		public Configuration Config {
			get {
				return PrepareCarefully.Instance.Config;
			}
		}

		public State State {
			get {
				return PrepareCarefully.Instance.State;
			}
		}
			
		protected void FatalError(string message, Exception e) {
			Log.Error("An unrecoverable error has occurred in the Prepare Carefully mod");
			Log.Error(message);
			Log.Error(e.ToString());
			Log.Error(e.StackTrace);
			this.broken = true;
		}

		protected void DrawCost(Rect parentRect)
		{
			Rect rect = new Rect(parentRect.width - 446, parentRect.height - 104, 418, 32);
			Text.Anchor = TextAnchor.LowerRight;
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			CostDetails cost = PrepareCarefully.Instance.Cost;
			string label;
			if (Config.pointsEnabled) {
				int points = PrepareCarefully.Instance.PointsRemaining;
				if (points < 0) {
					GUI.color = Color.yellow;
				}
				else {
					GUI.color = ColorText;
				}
				label = "EdB.PointsRemaining".Translate(new string[] { "" + points });
			}
			else {
				double points = cost.total;
				GUI.color = ColorText;
				label = "EdB.PointsSpent".Translate(new string[] { "" + points });
			}
			Widgets.Label(rect, label);

			string tooltipText = "";
			foreach (var c in cost.colonistDetails) {
				tooltipText += "EdB.CostSummaryColonist".Translate(new object[] { c.name, (c.total - c.apparel - c.bionics)}) + "\n";
			}
			tooltipText += "\n" + "EdB.CostSummaryApparel".Translate(new object[] { cost.colonistApparel }) + "\n"
				+ "EdB.PrepareCarefully.CostSummary.Implants".Translate(new object[] { cost.colonistBionics }) + "\n" 
				+ "EdB.CostSummaryEquipment".Translate(new object[] { cost.equipment }) + "\n\n" 
				+ "EdB.CostSummaryTotal".Translate(new object[] { cost.total });
			TipSignal tip = new TipSignal(() => tooltipText, tooltipText.GetHashCode());
			TooltipHandler.TipRegion(rect, tip);

			GUI.color = ColorText;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
		
			// Removed points.
			/*
			string optionLabel;
			float optionTop = parentRect.height - 97;
			optionLabel = "EdB.PrepareCarefully.UsePoints".Translate();
			Vector2 size = Text.CalcSize(optionLabel);
			Rect optionRect = new Rect(24, optionTop, size.x + 10, 32);
			Widgets.Label(optionRect, optionLabel);
			GUI.color = Color.white;
			TooltipHandler.TipRegion(optionRect, "EdB.PrepareCarefully.UsePoints.Tip".Translate());
			Widgets.Checkbox(new Vector2(optionRect.x + optionRect.width, optionRect.y - 3), ref Config.pointsEnabled, 24, false);

			GUI.color = ColorText;
			optionLabel = "EdB.PrepareCarefully.FixedPoints".Translate();
			Vector2 fixedPointsSize = Text.CalcSize(optionLabel);
			Rect fixedPointsRect = new Rect(optionRect.x + optionRect.width + 40, optionTop, fixedPointsSize.x + 10, 32);
			Widgets.Label(fixedPointsRect, optionLabel);
			GUI.color = Color.white;
			TooltipHandler.TipRegion(fixedPointsRect, "EdB.PrepareCarefully.FixedPoints.Tip".Translate(new object[] { Config.points }));
			Widgets.Checkbox(new Vector2(fixedPointsRect.x + fixedPointsRect.width, fixedPointsRect.y - 3), ref Config.fixedPointsEnabled, 24, !Config.pointsEnabled);
			*/
		}

		protected void DrawPresetButtons()
		{
			float middle = 982f / 2f;
			float buttonWidth = 150;
			float buttonSpacing = 24;
			if (Widgets.ButtonText(new Rect(middle - buttonWidth - buttonSpacing / 2, 692, buttonWidth, 38), "EdB.LoadPresetButton".Translate(), true, false, true)) {
				Hide();
				Find.WindowStack.Add(new Dialog_LoadPreset());
			}
			if (Widgets.ButtonText(new Rect(middle + buttonSpacing / 2, 692, buttonWidth, 38), "EdB.SavePresetButton".Translate(), true, false, true)) {
				Hide();
				Find.WindowStack.Add(new Dialog_SavePreset());
			}
			GUI.color = Color.white;
		}
			
		public void Hide() {
			hidden = true;
		}

		public void Show() {
			hidden = false;
		}
			
		protected void OnBackButton(Action defaultAction, Action scenariosAction)
		{
			defaultAction();
		}

		protected void OnNextButton(Action defaultAction, Action scenariosAction)
		{
			defaultAction();
		}

		//
		// Static Fields
		//
		public const float BottomAreaHeight = 38;
		private readonly Vector2 BottomButSize = new Vector2(150, 38);

		//
		// Static Methods
		//
		public bool DoMiddleButton(Rect innerRect, string label)
		{
			float top = innerRect.height - 38;
			Rect rect = new Rect(innerRect.width / 2 - BottomButSize.x / 2, top, BottomButSize.x, BottomButSize.y);
			return Widgets.ButtonText(rect, label, true, false, true);
		}

		public void DoNextBackButtons(Rect innerRect, string nextLabel, Action nextAct, Action backAct)
		{
			float top = innerRect.height - 38;
			Text.Font = GameFont.Small;
			if (backAct != null) {
				Rect rect = new Rect(0, top, BottomButSize.x, BottomButSize.y);
				if (Widgets.ButtonText(rect, "Back".Translate(), true, false, true)) {
					backAct();
				}
			}
			if (nextAct != null) {
				Rect rect2 = new Rect(innerRect.width - BottomButSize.x, top, BottomButSize.x, BottomButSize.y);
				if (Widgets.ButtonText(rect2, nextLabel, true, false, true)) {
					nextAct();
				}
			}
		}

	}
}

