using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully
{
	// EdB: Copy of the RimWorld.Page_ConfigureStartingPawns with changes to add the middle "Prepare Carefully" button.
	// TODO: Alpha 17.  Replace with a new copy and re-do changes every time a new alpha comes out.
	public class Page_ConfigureStartingPawns : Page
	{
		//
		// Static Fields
		//
		private const float TabAreaHeight = 30f;

		private const float RectAreaWidth = 100f;

		private const float RightRectLeftPadding = 5f;

		private static readonly Vector2 PawnPortraitSize = new Vector2(100f, 140f);

		//
		// Fields
		//
		private Pawn curPawn;

		//
		// Properties
		//
		public override string PageTitle {
			get {
				return "CreateCharacters".Translate();
			}
		}

		//
		// Methods
		//
		protected override bool CanDoNext()
		{
			if (!base.CanDoNext()) {
				return false;
			}
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				if (!current.Name.IsValid) {
					Messages.Message("EveryoneNeedsValidName".Translate(), MessageSound.RejectInput);
					return false;
				}
			}
			PortraitsCache.Clear();
			return true;
		}

		public override void DoWindowContents(Rect rect)
		{
			base.DrawPageTitle(rect);
			Rect mainRect = base.GetMainRect(rect, 30f, false);
			Widgets.DrawMenuSection(mainRect, true);
			TabDrawer.DrawTabs(mainRect, from c in Find.GameInitData.startingPawns
										 select new TabRecord(c.LabelCap, delegate {
											 this.SelectPawn(c);
										 }, c == this.curPawn));
			Rect rect2 = mainRect.ContractedBy(17f);
			Rect rect3 = rect2;
			rect3.width = 100f;
			GUI.DrawTexture(new Rect(rect3.xMin + (rect3.width - Page_ConfigureStartingPawns.PawnPortraitSize.x) / 2f - 10f, rect3.yMin + 20f, Page_ConfigureStartingPawns.PawnPortraitSize.x, Page_ConfigureStartingPawns.PawnPortraitSize.y), PortraitsCache.Get(this.curPawn, Page_ConfigureStartingPawns.PawnPortraitSize, default(Vector3), 1f));
			Rect rect4 = rect2;
			rect4.xMin = rect3.xMax;
			Rect rect5 = rect4;
			rect5.width = 475f;
			CharacterCardUtility.DrawCharacterCard(rect5, this.curPawn, new Action(this.RandomizeCurPawn));
			Rect rect6 = new Rect(rect5.xMax + 5f, rect4.y + 100f, rect4.width - rect5.width - 5f, 200f);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect6, "Health".Translate());
			Text.Font = GameFont.Small;
			rect6.yMin += 35f;
			HealthCardUtility.DrawHediffListing(rect6, this.curPawn, true);
			Rect rect7 = new Rect(rect6.x, rect6.yMax, rect6.width, 200f);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect7, "Relations".Translate());
			Text.Font = GameFont.Small;
			rect7.yMin += 35f;
			SocialCardUtility.DrawRelationsAndOpinions(rect7, this.curPawn);

			// EdB: Add a middle "Prepare Carefully" button.
			// base.DoBottomButtons(rect, "Start".Translate(), null, null, true);
			Action prepareCarefullyAction = () => {
				PrepareCarefully.Instance.Initialize();
				PrepareCarefully.Instance.OriginalPage = this;
				Find.WindowStack.Add(new Page_ConfigureStartingPawnsCarefully());
				if (!PrepareCarefully.Instance.FindScenPart()) {
					Find.WindowStack.Add(new Dialog_Confirm("EdB.PrepareCarefully.ModConfigProblem.Description".Translate(),
						delegate { }, true, "EdB.PrepareCarefully.ModConfigProblem.Title".Translate(), false));
				}
			};
			base.DoBottomButtons(rect, "Start".Translate(), "EdB.PrepareCarefully".Translate(), prepareCarefullyAction, true);
		}

		public override void PostOpen()
		{
			base.PostOpen();
			TutorSystem.Notify_Event("PageStart-ConfigureStartingPawns");
		}

		public override void PreOpen()
		{
			base.PreOpen();
			if (Find.GameInitData.startingPawns.Count > 0) {
				this.curPawn = Find.GameInitData.startingPawns[0];
			}
		}

		private void RandomizeCurPawn()
		{
			if (!TutorSystem.AllowAction("RandomizePawn")) {
				return;
			}
			int num = 0;
			while (true) {
				this.curPawn = StartingPawnUtility.RandomizeInPlace(this.curPawn);
				num++;
				if (num > 15) {
					break;
				}
				if (StartingPawnUtility.WorkTypeRequirementsSatisfied()) {
					goto Block_3;
				}
			}
			return;
		Block_3:
			TutorSystem.Notify_Event("RandomizePawn");
		}

		public void SelectPawn(Pawn c)
		{
			if (c != this.curPawn) {
				this.curPawn = c;
			}
		}
	}

	/*
	public class Page_ConfigureStartingPawns : Page
	{
		//
		// Static Fields
		//
		private const float TabAreaHeight = 30f;

		private const float RectAreaWidth = 100f;

		private const float RightRectLeftPadding = 5f;

		private static readonly Vector2 PawnPortraitSize = new Vector2(100f, 140f);

		//
		// Fields
		//
		private Pawn curPawn;

		//
		// Properties
		//
		public override string PageTitle {
			get {
				return "CreateCharacters".Translate();
			}
		}

		//
		// Methods
		//
		protected override bool CanDoNext()
		{
			if (!base.CanDoNext()) {
				return false;
			}
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				if (!current.Name.IsValid) {
					Messages.Message("EveryoneNeedsValidName".Translate(), MessageSound.RejectInput);
					return false;
				}
			}
			PortraitsCache.Clear();
			return true;
		}

		public override void DoWindowContents(Rect rect)
		{
			base.DrawPageTitle(rect);
			Rect mainRect = base.GetMainRect(rect, 30f, false);
			Widgets.DrawMenuSection(mainRect, true);
			TabDrawer.DrawTabs(mainRect, from c in Find.GameInitData.startingPawns
										 select new TabRecord(c.LabelCap, delegate {
											 this.SelectPawn(c);
										 }, c == this.curPawn));
			Rect rect2 = mainRect.ContractedBy(17f);
			Rect rect3 = rect2;
			rect3.width = 100f;
			GUI.DrawTexture(new Rect(rect3.xMin + (rect3.width - Page_ConfigureStartingPawns.PawnPortraitSize.x) / 2f - 10f, rect3.yMin + 20f, Page_ConfigureStartingPawns.PawnPortraitSize.x, Page_ConfigureStartingPawns.PawnPortraitSize.y), PortraitsCache.Get(this.curPawn, Page_ConfigureStartingPawns.PawnPortraitSize, default(Vector3), 1f));
			Rect rect4 = rect2;
			rect4.xMin = rect3.xMax;
			Rect rect5 = rect4;
			rect5.width = 475f;
			CharacterCardUtility.DrawCharacterCard(rect5, this.curPawn, new Action(this.RandomizeCurPawn));
			Rect rect6 = new Rect(rect5.xMax + 5f, rect4.y + 100f, rect4.width - rect5.width - 5f, 200f);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect6, "Health".Translate());
			Text.Font = GameFont.Small;
			rect6.yMin += 35f;
			HealthCardUtility.DrawHediffListing(rect6, this.curPawn, true);
			Rect rect7 = new Rect(rect6.x, rect6.yMax, rect6.width, 200f);
			Text.Font = GameFont.Medium;
			Widgets.Label(rect7, "Relations".Translate());
			Text.Font = GameFont.Small;
			rect7.yMin += 35f;
			SocialCardUtility.DrawRelationsAndOpinions(rect7, this.curPawn);

			// EdB: Add a middle "Prepare Carefully" button.
			// base.DoBottomButtons(rect, "Start".Translate(), null, null, true);
			Action prepareCarefullyAction = () => {
				PrepareCarefully.Instance.Initialize();
				PrepareCarefully.Instance.OriginalPage = this;
				Find.WindowStack.Add(new Page_ConfigureStartingPawnsCarefully());
				if (!PrepareCarefully.Instance.FindScenPart()) {
					Find.WindowStack.Add(new Dialog_Confirm("EdB.PrepareCarefully.ModConfigProblem.Description".Translate(),
						delegate { }, true, "EdB.PrepareCarefully.ModConfigProblem.Title".Translate(), false));
				}
			};
			base.DoBottomButtons(rect, "Start".Translate(), "EdB.PrepareCarefully".Translate(), prepareCarefullyAction, true);
		}

		public override void PostOpen()
		{
			base.PostOpen();
			TutorSystem.Notify_Event("PageStart-ConfigureStartingPawns");
		}

		public override void PreOpen()
		{
			base.PreOpen();
			if (Find.GameInitData.startingPawns.Count > 0) {
				this.curPawn = Find.GameInitData.startingPawns[0];
			}
		}

		private void RandomizeCurPawn()
		{
			if (!TutorSystem.AllowAction("RandomizePawn")) {
				return;
			}
			do {
				this.curPawn = StartingPawnUtility.RandomizeInPlace(this.curPawn);
			}
			while (!StartingPawnUtility.WorkTypeRequirementsSatisfied());
			TutorSystem.Notify_Event("RandomizePawn");
		}

		public void SelectPawn(Pawn c)
		{
			if (c != this.curPawn) {
				this.curPawn = c;
			}
		}
	}
	*/

}
