using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully
{
	public class Page_ConfigureStartingPawns : Page
	{
		//
		// Static Fields
		//
		private const float TabAreaHeight = 30;

		private const float RectAreaWidth = 100;

		private const float RightRectLeftPadding = 5;

		private static readonly Vector2 PawnPortraitSize = new Vector2(100, 140);

		//
		// Fields
		//
		private Pawn curPawn;

		//
		// Properties
		//
		// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns.PageTitle
		// Updated for Alpha 14.
		public override string PageTitle {
			get {
				return "CreateCharacters".Translate();
			}
		}

		//
		// Methods
		//
		public override void DoWindowContents(Rect rect)
		{
			base.DrawPageTitle(rect);
			Rect mainRect = base.GetMainRect(rect, 30, false);
			Widgets.DrawMenuSection(mainRect, true);
			TabDrawer.DrawTabs(mainRect, from c in Find.GameInitData.startingPawns
				select new TabRecord(c.LabelCap, delegate {
					this.SelectPawn(c);
				}, c == this.curPawn));
			Rect rect2 = mainRect.ContractedBy(17);
			Rect rect3 = rect2;
			rect3.width = 100;
			GUI.DrawTexture(new Rect(rect3.xMin + (rect3.width - Page_ConfigureStartingPawns.PawnPortraitSize.x) / 2 - 10, rect3.yMin + 20, Page_ConfigureStartingPawns.PawnPortraitSize.x, Page_ConfigureStartingPawns.PawnPortraitSize.y), PortraitsCache.Get(this.curPawn, Page_ConfigureStartingPawns.PawnPortraitSize, default(Vector3), 1));
			Rect rect4 = rect2;
			rect4.xMin = rect3.xMax;
			Rect rect5 = rect4;
			rect5.width = 475;
			CharacterCardUtility.DrawCharacterCard(rect5, this.curPawn, new Action(this.RandomizeCurPawn));
			Rect rect6 = new Rect(rect5.xMax + 5, rect4.y + 100, rect4.width - rect5.width - 5, 200);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect6, "Health".Translate());
			Text.Font = GameFont.Small;
			rect6.yMin += 35;
			HealthCardUtility.DrawHediffListing(rect6, this.curPawn, true);
			Rect rect7 = new Rect(rect6.x, rect6.yMax, rect6.width, 200);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect7, "Relations".Translate());
			Text.Font = GameFont.Small;
			rect7.yMin += 35;
			SocialCardUtility.DrawRelationsAndOpinions(rect7, this.curPawn);
			// EdB: Add a middle "Prepare Carefully" button.
			Action prepareCarefullyAction = () => {
				PrepareCarefully.Instance.Initialize();
				Find.WindowStack.Add(new Page_ConfigureStartingPawnsCarefully());
				if (!PrepareCarefully.Instance.FindScenPart()) {
					Find.WindowStack.Add(new Dialog_Confirm("EdB.PrepareCarefully.ModConfigProblem.Description".Translate(),
						delegate {}, true, "EdB.PrepareCarefully.ModConfigProblem.Title".Translate(), false));
				}
			};
			base.DoBottomButtons(rect, "Start".Translate(), "EdB.PrepareCarefully".Translate(), prepareCarefullyAction, true);
		}

		// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns.PreOpen()
		// Updated for Alpha 14.
		public override void PreOpen()
		{
			base.PreOpen();
			if (Find.GameInitData.startingPawns.Count > 0) {
				this.curPawn = Find.GameInitData.startingPawns[0];
			}
		}

		// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns.RandomizeCurPawn()
		// Updated for Alpha 14.
		private void RandomizeCurPawn()
		{
			do {
				this.curPawn = StartingPawnUtility.RandomizeInPlace(this.curPawn);
			}
			while (!StartingPawnUtility.AnyoneCanDoRequiredWorkTypes());
		}

		// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns.SelectPawn()
		// Updated for Alpha 14.
		public void SelectPawn(Pawn c)
		{
			if (c != this.curPawn) {
				this.curPawn = c;
			}
		}

		// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns.TryNext()
		// Updated for Alpha 14.
		protected override bool TryNext()
		{
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				if (!current.Name.IsValid) {
					Messages.Message("EveryoneNeedsValidName".Translate(), MessageSound.RejectInput);
					return false;
				}
			}
			PortraitsCache.Clear();
			return true;
		}
	}
}
