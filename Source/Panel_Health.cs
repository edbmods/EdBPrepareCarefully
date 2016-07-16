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
	public class Panel_Health
	{
		public static Color BackgroundBoxColor = new Color(43f / 255f, 44f / 255f, 45f / 255f);

		protected Rect RectHeader;
		protected Rect RectBorder;
		protected Rect RectContent;
		protected Rect RectScroll;
		protected Vector2 SizeElement;
		protected Vector2 ContentPadding;
		protected ScrollView scrollView = new ScrollView();

		public IEnumerable<RecipeDef> implantRecipes;
		public List<CustomBodyPart> partRemovalList = new List<CustomBodyPart>();
		protected HashSet<BodyPartRecord> disabledBodyParts = new HashSet<BodyPartRecord>();

		protected List<InjurySeverity> severityOptions = new List<InjurySeverity>();
		protected List<InjurySeverity> oldInjurySeverities = new List<InjurySeverity>();

		protected class InjurySeverity
		{
			public InjurySeverity(float value) {
				this.value = value;
			}
			public InjurySeverity(float value, HediffStage stage) {
				this.value = value;
				this.stage = stage;
			}
			protected float value = 0;
			protected HediffStage stage = null;
			protected int? variant = null;
			public float Value {
				get {
					return this.value;
				}
			}
			public HediffStage Stage {
				get {
					return this.stage;
				}
			}
			public int? Variant {
				get {
					return this.variant;
				}
				set {
					this.variant = value;
				}
			}
			public string Label {
				get {
					if (stage != null) {
						if (variant == null) {
							return stage.label.CapitalizeFirst();
						}
						else {
							return "EdB.PrepareCarefully.Stage.OptionLabel".Translate(new object[] {
								stage.label.CapitalizeFirst(), variant
							});
						}
					}
					else {
						return ("EdB.PrepareCarefully.Severity.OptionLabel." + value).Translate();
					}
				}
			}
		}

		public Panel_Health(Rect rect)
		{
			RectHeader = new Rect(5, 5, rect.width - 10, 30);
			RectBorder = new Rect(0, 33, rect.width, rect.height - 33).ContractedBy(5);
			RectBorder.height -= 50;
			RectContent = RectBorder.ContractedBy(1);
			RectScroll = new Rect(0, 0, RectContent.width, RectContent.height);
			ContentPadding = new Vector2(4, 4);
			SizeElement = new Vector2(RectScroll.width - (ContentPadding.x * 2), 70);

			implantRecipes = PrepareCarefully.Instance.HealthManager.ImplantManager.Recipes;

			oldInjurySeverities.Add(new InjurySeverity(2));
			oldInjurySeverities.Add(new InjurySeverity(3));
			oldInjurySeverities.Add(new InjurySeverity(4));
			oldInjurySeverities.Add(new InjurySeverity(5));
			oldInjurySeverities.Add(new InjurySeverity(6));
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

			cursor = DrawCustomBodyParts(cursor);

			scrollView.End(cursor);
			GUI.EndGroup();

			DrawRandomOption();

			CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
			if (partRemovalList.Count > 0) {
				foreach (var x in partRemovalList) {
					customPawn.RemoveCustomBodyParts(x);
				}
				partRemovalList.Clear();
			}
		}

		public void DrawHeader()
		{
			GUI.BeginGroup(RectHeader);
			GUI.color = Color.white;

			// Injury button.
			if (Widgets.ButtonText(new Rect(0, 0, 120, 28), "EdB.PrepareCarefully.AddInjury".Translate(), true, true, true)) {
				CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
				InjuryOption selectedInjury = null;
				BodyPartRecord selectedBodyPart = null;
				bool bodyPartSelectionRequired = true;
				InjurySeverity selectedSeverity = null;

				Dialog_Options<InjurySeverity> severityDialog;
				Dialog_Options<BodyPartRecord> bodyPartDialog;

				Action addInjuryAction = () => {
					if (bodyPartSelectionRequired) {
						Injury injury = new Injury();
						injury.BodyPartRecord = selectedBodyPart;
						injury.Option = selectedInjury;
						if (selectedSeverity != null) {
							injury.Severity = selectedSeverity.Value;
						}
						customPawn.AddInjury(injury);
					}
					else {
						foreach (var p in selectedInjury.ValidParts) {
							BodyPartRecord record = PrepareCarefully.Instance.HealthManager.FirstBodyPartRecord(p);
							if (record != null) {
								Injury injury= new Injury();
								injury.BodyPartRecord = record;
								injury.Option = selectedInjury;
								if (selectedSeverity != null) {
									injury.Severity = selectedSeverity.Value;
								}
								customPawn.AddInjury(injury);
							}
							else {
								Log.Warning("Could not find body part record for definition: " + p.defName);
							}
						}
					}
				};

				severityDialog = new Dialog_Options<InjurySeverity>(severityOptions) {
					ConfirmButtonLabel = "EdB.PrepareCarefully.Add",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectSeverity",
					NameFunc = (InjurySeverity option) => {
						return option.Label;
					},
					SelectedFunc = (InjurySeverity option) => {
						return option == selectedSeverity;
					},
					SelectAction = (InjurySeverity option) => {
						selectedSeverity = option;
					},
					ConfirmValidation = () => {
						if (selectedSeverity == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectSeverity";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						addInjuryAction();
					}
				};

				bodyPartDialog = new Dialog_Options<BodyPartRecord>(null)
				{
					ConfirmButtonLabel = "EdB.PrepareCarefully.Add",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectBodyPart",
					NameFunc = (BodyPartRecord option) => {
						return option.def.LabelCap;
					},
					SelectedFunc = (BodyPartRecord option) => {
						return option == selectedBodyPart;
					},
					SelectAction = (BodyPartRecord option) => {
						selectedBodyPart = option;
					},
					EnabledFunc = (BodyPartRecord option) => {
						return !disabledBodyParts.Contains(option);
					},
					ConfirmValidation = () => {
						if (selectedBodyPart == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectBodyPart";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						if (this.severityOptions.Count > 1) {
							Find.WindowStack.Add(severityDialog);
						}
						else {
							addInjuryAction();
						}
					}
				};


				Dialog_Options<InjuryOption> injuryOptionDialog
					= new Dialog_Options<InjuryOption>(PrepareCarefully.Instance.HealthManager.InjuryManager.Options)
				{
					ConfirmButtonLabel = "EdB.PrepareCarefully.Next",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectInjury",
					NameFunc = (InjuryOption option) => {
						return option.Label;
					},
					SelectedFunc = (InjuryOption option) => {
						return selectedInjury == option;
					},
					SelectAction = (InjuryOption option) => {
						selectedInjury = option;
						if (option.ValidParts == null || option.ValidParts.Count == 0) {
							bodyPartSelectionRequired = true;
						}
						else {
							bodyPartSelectionRequired = false;
						}
					},
					ConfirmValidation = () => {
						if (selectedInjury == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectInjury";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						ResetSeverityOptions(selectedInjury);
						selectedSeverity = this.severityOptions[0];
						if (bodyPartSelectionRequired) {
							bodyPartDialog.Options = PrepareCarefully.Instance.HealthManager.AllSkinCoveredBodyParts;
							ResetBodyPartEnabledState(bodyPartDialog.Options, customPawn);
							Find.WindowStack.Add(bodyPartDialog);
						}
						else if (severityOptions.Count > 1) {
							Find.WindowStack.Add(severityDialog);
						}
						else {
							addInjuryAction();
						}
					}
				};
				Find.WindowStack.Add(injuryOptionDialog);
			}

			// Implant button.
			if (Widgets.ButtonText(new Rect(RectHeader.width - 120, 0, 120, 28),
				"EdB.PrepareCarefully.AddImplant".Translate(), true, true, true))
			{
				CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
				RecipeDef selectedRecipe = null;
				BodyPartRecord selectedBodyPart = null;
				bool bodyPartSelectionRequired = true;

				Dialog_Options<BodyPartRecord> bodyPartDialog =
					new Dialog_Options<BodyPartRecord>(null)
				{
					ConfirmButtonLabel = "EdB.PrepareCarefully.Add",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectBodyPart",
					NameFunc = (BodyPartRecord record) => {
						return record.def.LabelCap;
					},
					SelectedFunc = (BodyPartRecord record) => {
						return record == selectedBodyPart;
					},
					SelectAction = (BodyPartRecord record) => {
						selectedBodyPart = record;
					},
					EnabledFunc = (BodyPartRecord record) => {
						return !disabledBodyParts.Contains(record);
					},
					ConfirmValidation = () => {
						if (selectedBodyPart == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectBodyPart";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						customPawn.AddImplant(new Implant(selectedBodyPart, selectedRecipe));
					}
				};


				Dialog_Options<RecipeDef> implantRecipeDialog = new Dialog_Options<RecipeDef>(implantRecipes) {
					ConfirmButtonLabel = "EdB.PrepareCarefully.Next",
					CancelButtonLabel = "EdB.PrepareCarefully.Cancel",
					HeaderLabel = "EdB.PrepareCarefully.SelectImplant",
					NameFunc = (RecipeDef recipe) => {
						return recipe.LabelCap;
					},
					SelectedFunc = (RecipeDef recipe) => {
						return selectedRecipe == recipe;
					},
					SelectAction = (RecipeDef recipe) => {
						selectedRecipe = recipe;
						IEnumerable<BodyPartRecord> bodyParts = PrepareCarefully.Instance.HealthManager.ImplantManager.PartsForRecipe(recipe);
						int bodyPartCount = bodyParts.Count();
						if (bodyParts != null && bodyPartCount > 0) {
							if (bodyPartCount > 1) {
								selectedBodyPart = null;
								bodyPartDialog.Options = bodyParts;
								bodyPartSelectionRequired = true;
								ResetBodyPartEnabledState(bodyParts, customPawn);
							}
							else {
								selectedBodyPart = bodyParts.First();
								bodyPartSelectionRequired = false;
							}
						}
						else {
							selectedBodyPart = null;
							bodyPartSelectionRequired = false;
						}
					},
					ConfirmValidation = () => {
						if (selectedRecipe == null) {
							return "EdB.PrepareCarefully.ErrorMustSelectImplant";
						}
						else {
							return null;
						}
					},
					CloseAction = () => {
						if (bodyPartSelectionRequired) {
							Find.WindowStack.Add(bodyPartDialog);
						}
						else {
							customPawn.AddImplant(new Implant(selectedBodyPart, selectedRecipe));
						}
					}
				};
				Find.WindowStack.Add(implantRecipeDialog);
			}

			GUI.EndGroup();
		}

		protected void ResetBodyPartEnabledState(IEnumerable<BodyPartRecord> parts, CustomPawn pawn)
		{
			disabledBodyParts.Clear();
			ImplantManager implantManager = PrepareCarefully.Instance.HealthManager.ImplantManager;
			foreach (var part in parts) {
				if (pawn.IsImplantedPart(part) || implantManager.AncestorIsImplant(part, pawn)) {
					disabledBodyParts.Add(part);
				}
			}
		}

		protected void ResetSeverityOptions(InjuryOption injuryOption)
		{
			severityOptions.Clear();
			if (injuryOption.HediffDef.stages == null || injuryOption.HediffDef.stages.Count == 0) {
				severityOptions.AddRange(oldInjurySeverities);
				return;
			}

			int variant = 1;
			InjurySeverity previous = null;
			foreach (var stage in injuryOption.HediffDef.stages) {
				InjurySeverity value = null;
				if (stage.minSeverity == 0) {
					value = new InjurySeverity(0.001f, stage);
				}
				else {
					value = new InjurySeverity(stage.minSeverity, stage);
				}
				if (previous == null) {
					previous = value;
					variant = 1;
				}
				else {
					if (previous.Stage.label == stage.label) {
						previous.Variant = variant;
						variant++;
						value.Variant = variant;
					}
					else {
						previous = value;
						variant = 1;
					}
				}
				severityOptions.Add(value);
			}
		}

		public float DrawCustomBodyParts(float cursor)
		{
			CustomPawn customPawn = PrepareCarefully.Instance.State.CurrentPawn;
			foreach (var i in customPawn.BodyParts) {
				cursor = DrawCustomBodyPart(cursor, i);
			}
			return cursor;
		}

		public float DrawCustomBodyPart(float cursor, CustomBodyPart customPart)
		{
			cursor += ContentPadding.y;
			Vector2 boxSize = SizeElement;
			if (scrollView.ScrollbarsVisible) {
				boxSize.x -= ScrollView.ScrollbarSize;
			}
			Rect elementBox = new Rect(ContentPadding.x, cursor, boxSize.x, boxSize.y);

			// Draw background box.
			GUI.color = BackgroundBoxColor;
			GUI.DrawTexture(elementBox, BaseContent.WhiteTex);

			GUI.BeginGroup(elementBox);

			// Draw body part name.
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;
			Widgets.Label(new Rect(8, 6, boxSize.x - 16, 24), customPart.PartName);

			// Draw field.
			Rect fieldRect = new Rect(16, 30, boxSize.x - 32, 28);
			GUI.color = Color.white;
			Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;

			// Draw the hediff label.
			GUI.color = customPart.LabelColor;
			Rect labelRect = new Rect(fieldRect.x, fieldRect.y + 1, fieldRect.width, fieldRect.height);
			Widgets.Label(labelRect, customPart.ChangeName);
			if (customPart.HasTooltip) {
				TooltipHandler.TipRegion(labelRect, customPart.Tooltip);
			}
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;

			// Delete the option.
			Rect buttonRect = new Rect(boxSize.x - 21, 3, 18, 18);
			GUI.color = buttonRect.Contains(Event.current.mousePosition) ? Page_ConfigureStartingPawnsCarefully.ButtonHighlightColor : Page_ConfigureStartingPawnsCarefully.ButtonColor;
			GUI.DrawTexture(buttonRect, Textures.TextureButtonDelete);
			if (Widgets.ButtonInvisible(buttonRect, false)) {
				SoundDefOf.TickTiny.PlayOneShotOnCamera();
				partRemovalList.Add(customPart);
			}
			GUI.color = Color.white;

			GUI.EndGroup();

			return cursor + boxSize.y;
		}

		public void DrawRandomOption()
		{
			GUI.color = Page_ConfigureStartingPawnsCarefully.TextColor;
			string optionLabel = "EdB.PrepareCarefully.RandomizeInjuries".Translate();
			Vector2 size = Text.CalcSize(optionLabel);
			float widthWithCheckbox = size.x + 24 + 10;
			Rect rect = new Rect(RectBorder.x + RectBorder.width * 0.5f - widthWithCheckbox * 0.5f, RectBorder.y + RectBorder.height + 10, size.x + 10, 32);
			Widgets.Label(rect, optionLabel);
			GUI.color = Color.white;
			TooltipHandler.TipRegion(rect, "EdB.PrepareCarefully.RandomizeInjuries.Tooltip".Translate());
			bool selected = PrepareCarefully.Instance.State.CurrentPawn.RandomInjuries;
			bool enabled = !PrepareCarefully.Instance.State.CurrentPawn.HasCustomBodyParts;
			Widgets.Checkbox(new Vector2(rect.x + rect.width, rect.y + 3), ref selected, 24, !enabled);
			PrepareCarefully.Instance.State.CurrentPawn.RandomInjuries = selected;
			if (enabled == false && PrepareCarefully.Instance.State.CurrentPawn.RandomInjuries != false) {
				PrepareCarefully.Instance.State.CurrentPawn.RandomInjuries = false;
			}
		}

	}
}

