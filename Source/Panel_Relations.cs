using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace EdB.PrepareCarefully
{
	public class Panel_Relations
	{
		public static Color BackgroundBoxColor = new Color(43f / 255f, 44f / 255f, 45f / 255f);

		protected Rect RectHeader;
		protected Rect RectBorder;
		protected Rect RectContent;
		protected Rect RectScroll;
		protected Rect RectAddButton;
		protected Vector2 SizeRelation;
		protected Vector2 ContentPadding;
		protected Vector2 SizeAddButton = new Vector2(180, 28);
		protected ScrollView scrollView = new ScrollView();

		public List<PawnRelationDef> relationDefs = new List<PawnRelationDef>();
		protected HashSet<PawnRelationDef> disabledRelationships = new HashSet<PawnRelationDef>();

		public Panel_Relations(Rect rect)
		{
			RectHeader = new Rect(5, 5, rect.width - 10, 30);
			RectBorder = new Rect(0, 33, rect.width, rect.height - 33).ContractedBy(5);
			RectBorder.height -= 50;
			RectContent = RectBorder.ContractedBy(1);
			RectScroll = new Rect(0, 0, RectContent.width, RectContent.height);
			RectAddButton = new Rect(RectHeader.width / 2 - SizeAddButton.x / 2, 0, SizeAddButton.x, SizeAddButton.y);
			ContentPadding = new Vector2(4, 4);
			SizeRelation = new Vector2(RectScroll.width - (ContentPadding.x * 2), 70);

			relationDefs.AddRange(DefDatabase<PawnRelationDef>.AllDefs.ToList().FindAll((PawnRelationDef def) => {
				MethodInfo info = def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				if (info == null) {
					return false;
				}
				else {
					return true;
				}
			}));
		}

		public void Draw()
		{
			float cursor = 0;
			DrawHeader();
			GUI.color = Page_ConfigureStartingPawnsCarefully.PortraitTabInactiveColor;
			Widgets.DrawBox(RectBorder, 1);
			GUI.color = Color.white;
			GUI.BeginGroup(RectContent);
			scrollView.Begin(RectScroll);

			cursor = DrawRelationships(cursor);
			cursor = DrawFooterControls(cursor);

			scrollView.End(cursor);
			GUI.EndGroup();

			DrawRandomOption();
		}

		public void DrawHeader()
		{
			GUI.BeginGroup(RectHeader);
			GUI.color = Color.white;

			// TODO: Disable button if there's only one pawn?  What about other pawns, i.e. faction leaders, etc.
			if (Widgets.ButtonText(RectAddButton, "EdB.PrepareCarefully.AddRelationship".Translate(), true, true, true)) {
				CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
				PawnRelationDef selectedRelationship = null;
				CustomPawn selectedPawn = null;
				List<CustomPawn> otherPawns = PrepareCarefully.Instance.Pawns.FindAll((CustomPawn p) => {
					return p != customPawn;
				});

				Dialog_Options<PawnRelationDef> relationshipDialog =
					new Dialog_Options<PawnRelationDef>(null)
				{
					ConfirmButtonLabel = "EdB.PrepareCarefully.Add",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectRelationship",
					NameFunc = (PawnRelationDef def) => {
						return def.GetGenderSpecificLabelCap(selectedPawn.Pawn);
					},
					SelectedFunc = (PawnRelationDef def) => {
						return def == selectedRelationship;
					},
					SelectAction = (PawnRelationDef def) => {
						selectedRelationship = def;
					},
					EnabledFunc = (PawnRelationDef d) => {
						return !disabledRelationships.Contains(d);
					},
					ConfirmValidation = () => {
						if (selectedRelationship == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectRelationship";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						PrepareCarefully.Instance.RelationshipManager.AddRelationship(selectedRelationship, customPawn, selectedPawn);
					}
				};

				Dialog_Options<CustomPawn> colonistDialog = new Dialog_Options<CustomPawn>(otherPawns) {
					ConfirmButtonLabel = "EdB.PrepareCarefully.Next",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectColonist",
					NameFunc = (CustomPawn pawn) => {
						return pawn.Name.ToStringFull;
					},
					SelectedFunc = (CustomPawn pawn) => {
						return pawn == selectedPawn;
					},
					SelectAction = (CustomPawn pawn) => {
						selectedPawn = pawn;
					},
					ConfirmValidation = () => {
						if (selectedPawn == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectColonist";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						SetDisabledRelationships(selectedPawn);
						List<PawnRelationDef> relationDefs = new List<PawnRelationDef>(PrepareCarefully.Instance.RelationshipManager.AllowedRelationships);
						relationDefs.Sort((PawnRelationDef a, PawnRelationDef b) => {
							return a.GetGenderSpecificLabelCap(selectedPawn.Pawn).CompareTo(b.GetGenderSpecificLabelCap(selectedPawn.Pawn));
						});
						relationshipDialog.Options = relationDefs;
						Find.WindowStack.Add(relationshipDialog);
					}
				};
				Find.WindowStack.Add(colonistDialog);
			}

			GUI.EndGroup();
		}

		public float DrawRelationships(float cursor)
		{
			CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
			foreach (var r in PrepareCarefully.Instance.RelationshipManager.AllRelationships) {
				if (r.source == customPawn) {
					cursor = DrawRelationship(cursor, r.def, r.source, r.target, r.removeable);
				}
				else if (r.target == customPawn) {
					cursor = DrawRelationship(cursor, r.inverseDef, r.target, r.source, r.removeable);
				}
			}
			return cursor;
		}

		public float DrawRelationship(float cursor, PawnRelationDef def, CustomPawn source, CustomPawn target, bool removeable)
		{
			cursor += ContentPadding.y;
			Vector2 relationshipBoxSize = SizeRelation;
			if (scrollView.ScrollbarsVisible) {
				relationshipBoxSize.x -= ScrollView.ScrollbarSize;
			}
			Rect relationshipBox = new Rect(ContentPadding.x, cursor, relationshipBoxSize.x, relationshipBoxSize.y);

			// Draw background box.
			GUI.color = BackgroundBoxColor;
			GUI.DrawTexture(relationshipBox, BaseContent.WhiteTex);

			GUI.BeginGroup(relationshipBox);

			// Draw pawn name.
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;
			Widgets.Label(new Rect(8, 6, relationshipBoxSize.x - 16, 24), target.Pawn.NameStringShort);

			// Draw relationship field.
			Rect fieldRect = new Rect(16, 30, relationshipBoxSize.x - 32, 28);
			GUI.color = Color.white;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);
			// TODO: Move this color to another file like we did with the textures.
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;
			if (def != null) {
				Widgets.Label(new Rect(fieldRect.x, fieldRect.y + 1, fieldRect.width, fieldRect.height), def.GetGenderSpecificLabelCap(target.Pawn));
			}

			// Delete relation
			Rect buttonRect = new Rect(relationshipBoxSize.x - 21, 3, 18, 18);
			GUI.color = buttonRect.Contains(Event.current.mousePosition) ? Page_ConfigureStartingPawnsCarefully.ButtonHighlightColor : Page_ConfigureStartingPawnsCarefully.ButtonColor;
			if (removeable) {
				GUI.DrawTexture(buttonRect, Textures.TextureButtonDelete);
				if (Widgets.ButtonInvisible(buttonRect, false)) {
					SoundDefOf.TickTiny.PlayOneShotOnCamera();
					PrepareCarefully.Instance.RelationshipManager.DeleteRelationship(def, source, target);
				}
			}
			else {
				GUI.DrawTexture(buttonRect, Textures.TextureDerivedRelationship);
				TooltipHandler.TipRegion(buttonRect, "EdB.PrepareCarefully.CannotDeleteRelationship".Translate());
			}
			GUI.color = Color.white;

			GUI.EndGroup();

			return cursor + relationshipBoxSize.y;
		}

		public void SetDisabledRelationships(CustomPawn target) {
			disabledRelationships.Clear();
			CustomPawn source = PrepareCarefully.Instance.State.CurrentPawn;
			bool bloodRelation = false;
			foreach (CustomRelationship r in PrepareCarefully.Instance.RelationshipManager.ExplicitRelationships) {
				if (r.def.familyByBloodRelation) {
					if ((r.source == source && r.target == target) || (r.source == target && r.target == source)) {
						bloodRelation = true;
						break;
					}
				}
			}
			if (bloodRelation) {
				foreach (PawnRelationDef r in PrepareCarefully.Instance.RelationshipManager.AllowedRelationships) {
					if (r.familyByBloodRelation) {
						disabledRelationships.Add(r);
					}
				}
			}
			foreach (CustomRelationship r in PrepareCarefully.Instance.RelationshipManager.ExplicitRelationships) {
				if ((r.source == source && r.target == target) || (r.source == target && r.target == source)) {
					disabledRelationships.Add(r.def);
					CarefullyPawnRelationDef extendedDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(r.def.defName);
					if (extendedDef != null) {
						if (extendedDef.conflicts != null) {
							foreach (string conflictName in extendedDef.conflicts) {
								PawnRelationDef conflict = DefDatabase<PawnRelationDef>.GetNamedSilentFail(conflictName);
								if (conflict != null) {
									disabledRelationships.Add(conflict);
								}
							}
						}
					}
					if (r.inverseDef != null) {
						disabledRelationships.Add(r.inverseDef);
						extendedDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(r.inverseDef.defName);
						if (extendedDef != null) {
							if (extendedDef.conflicts != null) {
								foreach (string conflictName in extendedDef.conflicts) {
									PawnRelationDef conflict = DefDatabase<PawnRelationDef>.GetNamedSilentFail(conflictName);
									if (conflict != null) {
										disabledRelationships.Add(conflict);
									}
								}
							}
						}

					}
				}
			}
		}

		public float DrawFooterControls(float cursor)
		{
			return cursor;
		}

		public void DrawRandomOption()
		{
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;
			string optionLabel = "EdB.PrepareCarefully.RandomizeRelationships".Translate();
			Vector2 size = Text.CalcSize(optionLabel);
			float widthWithCheckbox = size.x + 24 + 10;
			Rect rect = new Rect(RectBorder.x + RectBorder.width * 0.5f - widthWithCheckbox * 0.5f, RectBorder.y + RectBorder.height + 10, size.x + 10, 32);
			Widgets.Label(rect, optionLabel);
			GUI.color = Color.white;
			TooltipHandler.TipRegion(rect, "EdB.PrepareCarefully.RandomizeRelationships.Tooltip".Translate());
			bool selected = PrepareCarefully.Instance.State.CurrentPawn.randomRelations;
			bool enabled = !PrepareCarefully.Instance.State.CurrentPawn.HasRelationships;
			Widgets.Checkbox(new Vector2(rect.x + rect.width, rect.y + 3), ref selected, 24, !enabled);
			PrepareCarefully.Instance.State.CurrentPawn.randomRelations = selected;
			if (enabled == false && PrepareCarefully.Instance.State.CurrentPawn.randomRelations != false) {
				PrepareCarefully.Instance.State.CurrentPawn.randomRelations = false;
			}
		}

	}
}

