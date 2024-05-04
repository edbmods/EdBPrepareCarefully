using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelAppearance : PanelModule {
        public delegate void RandomizeAppearanceHandler();
        public delegate void UpdateGenderHandler(Gender gender);
        public delegate void UpdateSkinColorHandler(Color color);

        public event RandomizeAppearanceHandler RandomizeAppearance;
        public event UpdateGenderHandler GenderUpdated;
        public event UpdateSkinColorHandler SkinColorUpdated;

        public static Color ColorPortraitBorder = new Color(0.3843f, 0.3843f, 0.3843f);

        public Rect RectGenderFemale;
        public Rect RectGenderMale;

        protected CustomizedPawn CachedCustomizedPawn = null;
        protected Pawn CachedPawn = null;
        protected List<PawnLayer> currentPawnLayers = null;
        protected PawnLayer selectedPawnLayer = null;
        protected List<Action> pawnLayerActions = new List<Action>();
        protected string pawnLayerLabel = null;
        protected PawnLayer pawnLayerLabelLayer = null;
        protected CustomizedPawn pawnLayerLabelModel = null;
        protected List<Color> skinColors = new List<Color>();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ControllerTabViewPawns PawnController { get; set; }
        public ProviderPawnLayers ProviderPawnLayers { get; set; }
        public ProviderAlienRaces ProviderAlienRaces { get; set; }

        public PanelAppearance() {

        }

        private Rect RectLayerSelector;
        private Rect RectPortrait;
        private Rect RectButtonRandomize;
        private Rect RectButtonRotateView;
        private Rect RectPawn;

        protected static float SwatchLimit = 210;
        protected static Vector2 SwatchSize = new Vector2(15, 15);
        protected static Vector2 SwatchPosition = new Vector2(18, 320);
        protected static Vector2 SwatchSpacing = new Vector2(21, 21);
        protected static Color ColorSwatchBorder = new Color(0.77255f, 0.77255f, 0.77255f);
        protected static Color ColorSwatchSelection = new Color(0.9098f, 0.9098f, 0.9098f);

        public override void Resize(float width) {

            Width = width;
            Vector2 panelPadding = new Vector2(16, 12);
            float panelContentWidth = width - panelPadding.x - panelPadding.x;
            Vector2 randomizeButtonSize = new Vector2(22, 22);
            float buttonHeight = 28;

            Vector2 portraitSize = new Vector2(panelContentWidth, 164);
            RectPortrait = new Rect(panelPadding.x, panelPadding.y, portraitSize.x, portraitSize.y);
            RectPawn = RectPortrait.OutsetBy(0, 12).OffsetBy(0, 8);
            RectLayerSelector = new Rect(panelPadding.x, RectPortrait.yMax + 8, panelContentWidth, buttonHeight);
            RectButtonRandomize = new Rect(RectPortrait.xMax - randomizeButtonSize.x - 12, RectPortrait.y + 12,
                randomizeButtonSize.x, randomizeButtonSize.y);
            RectGenderFemale = new Rect(RectPortrait.x + 8, RectPortrait.y + 6, 18, 24);
            RectGenderMale = new Rect(RectGenderFemale.xMax + 2, RectPortrait.y + 6, 20, 24);

            Vector2 rotateViewButtonSize = new Vector2(24, 12);
            RectButtonRotateView = new Rect(RectPortrait.x + 10, RectPortrait.yMax - rotateViewButtonSize.y - 10, rotateViewButtonSize.x, rotateViewButtonSize.y);
        }

        public override float Draw(float y) {
            float top = y;

            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn.Pawn;
            if (pawn == null) {
                return 0;
            }
            if (currentPawnLayers == null || CachedCustomizedPawn != customizedPawn || CachedPawn != pawn) {
                CachedCustomizedPawn = customizedPawn;
                CachedPawn = pawn;
                UpdatePawnLayers();
            }

            string label = this.selectedPawnLayer?.Label;
            if (WidgetDropdown.Button(RectLayerSelector, label, true, false, true)) {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                int i = 0;
                foreach (var layer in currentPawnLayers) {
                    label = layer.Label;
                    list.Add(new FloatMenuOption(label, this.pawnLayerActions[i], MenuOptionPriority.Default, null, null, 0, null, null));
                    i++;
                }
                Find.WindowStack.Add(new FloatMenu(list, null, false));
            }

            Rect rectPortrait = RectPortrait.OffsetBy(0, y);
            Rect rectGenderFemale = RectGenderFemale.OffsetBy(0, y);
            Rect rectGenderMale = RectGenderMale.OffsetBy(0, y);
            GUI.DrawTexture(rectPortrait, Textures.TexturePortraitBackground);
            DrawPawn(customizedPawn, RectPawn.OffsetBy(0, y));
            GUI.color = ColorPortraitBorder;
            Widgets.DrawBox(rectPortrait, 1);
            GUI.color = Color.white;
            // Gender buttons.
            if (pawn.RaceProps != null && pawn.RaceProps.hasGenders) {
                bool genderFemaleSelected = pawn.gender == Gender.Female;
                Style.SetGUIColorForButton(rectGenderFemale, genderFemaleSelected);
                GUI.DrawTexture(rectGenderFemale, Textures.TextureButtonGenderFemale);
                if (!genderFemaleSelected && Widgets.ButtonInvisible(rectGenderFemale, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    GenderUpdated?.Invoke(Gender.Female);
                    UpdatePawnLayers();
                }
                bool genderMaleSelected = pawn.gender == Gender.Male;
                Style.SetGUIColorForButton(rectGenderMale, genderMaleSelected);
                GUI.DrawTexture(rectGenderMale, Textures.TextureButtonGenderMale);
                if (!genderMaleSelected && Widgets.ButtonInvisible(rectGenderMale, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    GenderUpdated?.Invoke(Gender.Male);
                    UpdatePawnLayers();
                }
            }


            // Draw selector field.
            Rect rectLayerSelector = RectLayerSelector.OffsetBy(0, y);
            Rect fieldRect = new Rect(rectPortrait.x, rectLayerSelector.yMax + 5, rectPortrait.width, 40);
            Action previousSelectionAction = null;
            Action nextSelectionAction = null;
            Action clickAction = null;

            if (this.selectedPawnLayer.Options != null) {
                if (this.selectedPawnLayer.Options.Count > 1) {
                    previousSelectionAction = () => {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        SelectNextPawnLayerOption(customizedPawn, -1);
                    };
                    nextSelectionAction = () => {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        SelectNextPawnLayerOption(customizedPawn, 1);
                    };
                }
                if (this.selectedPawnLayer.Options.Count > 0) {
                    clickAction = () => {
                        ShowPawnLayerOptionsDialog(customizedPawn);
                    };
                }
            }

            string selectorLabel = PawnLayerLabel.CapitalizeFirst();
            DrawFieldSelector(fieldRect, selectorLabel, previousSelectionAction, nextSelectionAction, clickAction);

            y = fieldRect.yMax + 6;


            // Draw Color Selector
            if (selectedPawnLayer.ColorSelectorType == ColorSelectorType.RGB) {
                y += DrawColorSelectorForPawnLayer(customizedPawn, y, selectedPawnLayer.ColorSwatches, true);
            }
            else if (selectedPawnLayer.ColorSelectorType == ColorSelectorType.Skin) {
                AlienRace alienRace = ProviderAlienRaces.GetAlienRaceForPawn(customizedPawn);
                if (alienRace == null || alienRace.UseMelaninLevels || alienRace.ThingDef?.defName == "Human") {
                    y += DrawSkinColorSelector(customizedPawn, y, skinColors, true);
                }
                else if (alienRace.ChangeableColor) {
                    y += DrawSkinColorSelector(customizedPawn, y, alienRace.PrimaryColors, true);
                }
            }

            // Random button
            if (RectButtonRandomize.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonRandomize, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(RectButtonRandomize, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                RandomizeAppearance?.Invoke();
            }

            // Rotate view button
            if (RectButtonRotateView.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonRotateView, Textures.TextureButtonRotateView);
            if (Widgets.ButtonInvisible(RectButtonRotateView, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                if (ViewState.PawnViewRotation == Rot4.South) {
                    ViewState.PawnViewRotation = Rot4.East;
                }
                else if (ViewState.PawnViewRotation == Rot4.East) {
                    ViewState.PawnViewRotation = Rot4.North;
                }
                else if (ViewState.PawnViewRotation == Rot4.North) {
                    ViewState.PawnViewRotation = Rot4.West;
                }
                else if (ViewState.PawnViewRotation == Rot4.West) {
                    ViewState.PawnViewRotation = Rot4.South;
                }
            }

            y += 8;
            GUI.color = Color.white;
            return y - top;
        }

        protected void DrawPawn(CustomizedPawn customizedPawn, Rect rect) {
            Rect pawnRect = rect.OffsetBy(-rect.x, -rect.y);
            RenderTexture pawnTexture = PortraitsCache.Get(customizedPawn.Pawn, rect.size, ViewState.PawnViewRotation);
            try {
                GUI.BeginClip(rect);
                GUI.DrawTexture(pawnRect, (Texture)pawnTexture);
            }
            catch (Exception e) {
                Logger.Error("Failed to draw pawn", e);
            }
            finally {
                GUI.EndClip();
                GUI.color = Color.white;
            }
        }

        public void UpdatePawnLayers() {
            CustomizedPawn pawn = ViewState?.CurrentPawn;
            currentPawnLayers = ProviderPawnLayers.GetLayersForPawn(pawn);

            // Make sure the selected layer is still valid for the new pawn.
            List<PawnLayer> layers = ProviderPawnLayers.GetLayersForPawn(pawn);
            if (selectedPawnLayer != null) {
                selectedPawnLayer = layers.FirstOrDefault((layer) => { return layer.Name == selectedPawnLayer.Name; });
            }
            if (selectedPawnLayer == null) {
                selectedPawnLayer = layers.FirstOrDefault();
                selectedPawnLayer = layers.FirstOrDefault();
            }

            pawnLayerActions.Clear();
            foreach (var layer in currentPawnLayers) {
                pawnLayerActions.Add(delegate { this.ChangePawnLayer(layer); });
            }
            skinColors = SkinColorsForPawn(pawn?.Pawn);
        }
        protected void ChangePawnLayer(PawnLayer layer) {
            this.selectedPawnLayer = layer;
        }
        public List<Color> SkinColorsForPawn(Verse.Pawn pawn) {
            List<Color> result = new List<Color>();
            result.AddRange(RimWorld.PawnSkinColors.SkinColorGenesInOrder.ConvertAll(gene => RimWorld.PawnSkinColors.GetSkinColor(gene.minMelanin)).Distinct());
            if (pawn == null) {
                return result;
            }
            result.AddRange(pawn.genes.GenesListForReading.Where(g => g.def.skinColorOverride.HasValue).Select(g => g.def.skinColorOverride.Value));
            return result.Distinct().ToList();
        }

        protected void SelectNextPawnLayerOption(CustomizedPawn customizedPawn, int direction) {
            int optionCount = selectedPawnLayer.Options.Count;
            int? optionalIndex = selectedPawnLayer.GetSelectedIndex(customizedPawn);
            if (optionalIndex == null) {
                return;
            }
            int index = optionalIndex.Value;
            index += direction;
            if (index < 0) {
                index = optionCount - 1;
            }
            else if (index >= optionCount) {
                index = 0;
            }
            PawnLayerOption option = selectedPawnLayer.Options[index];
            PawnController?.UpdatePawnLayerOption(selectedPawnLayer, option);
            this.pawnLayerLabel = option.Label;
        }

        protected void ShowPawnLayerOptionsDialog(CustomizedPawn customizedPawn) {
            List<PawnLayerOption> options = selectedPawnLayer.Options;
            DialogOptions<PawnLayerOption> dialog = new DialogOptions<PawnLayerOption>(options) {
                NameFunc = (PawnLayerOption option) => {
                    return option.Label;
                },
                SelectedFunc = (PawnLayerOption option) => {
                    return selectedPawnLayer.IsOptionSelected(customizedPawn, option);
                },
                SelectAction = (PawnLayerOption option) => {
                    Logger.Debug("selected pawn layer option");
                    PawnController?.UpdatePawnLayerOption(selectedPawnLayer, option);
                    this.pawnLayerLabel = option.Label;
                },
                CloseAction = () => { },
                InitialPositionX = CalculateDialogPositionX()
            };
            Find.WindowStack.Add(dialog);
        }
        protected float CalculateDialogPositionX() {
            return Find.WindowStack.currentlyDrawnWindow.windowRect.x + 212f + Width + 6f;
        }
        protected string PawnLayerLabel {
            get {
                CustomizedPawn customizedPawn = ViewState.CurrentPawn;
                string label = "EdB.PC.Panel.Appearance.NoneSelected".Translate();
                PawnLayerOption option = selectedPawnLayer.GetSelectedOption(customizedPawn);
                if (option != null) {
                    label = option.Label;
                }
                pawnLayerLabelLayer = selectedPawnLayer;
                pawnLayerLabelModel = customizedPawn;
                pawnLayerLabel = label;
                return label;
            }
        }
        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Action clickAction) {
            DrawFieldSelector(fieldRect, label, previousAction, nextAction, clickAction, Style.ColorText);
        }

        protected void DrawFieldSelector(Rect fieldRect, string label, Action previousAction, Action nextAction, Action clickAction, Color labelColor) {
            GUI.color = Color.white;
            Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

            Text.Anchor = TextAnchor.MiddleCenter;
            fieldRect.y += 2;
            GUI.color = labelColor;
            if (clickAction != null && fieldRect.Contains(Event.current.mousePosition)) {
                GUI.color = Color.white;
            }
            Widgets.Label(fieldRect, label);
            GUI.color = Color.white;

            // Draw previous and next buttons.  Disable the buttons if no action arguments were passed in.
            float buttonHalfHeight = Textures.TextureButtonPrevious.height * 0.5f;
            Rect prevButtonRect = new Rect(fieldRect.x - Textures.TextureButtonPrevious.width - 2, fieldRect.MiddleY() - buttonHalfHeight, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
            Rect nextButtonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.MiddleY() - buttonHalfHeight, Textures.TextureButtonPrevious.width, Textures.TextureButtonPrevious.height);
            if (previousAction != null) {
                Style.SetGUIColorForButton(prevButtonRect);
                GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
                if (previousAction != null && Widgets.ButtonInvisible(prevButtonRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    previousAction();
                }
            }
            else {
                GUI.color = Style.ColorButtonDisabled;
                GUI.DrawTexture(prevButtonRect, Textures.TextureButtonPrevious);
            }
            if (nextAction != null) {
                Style.SetGUIColorForButton(nextButtonRect);
                GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
                if (nextAction != null && Widgets.ButtonInvisible(nextButtonRect, false)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    nextAction();
                }
            }
            else {
                GUI.color = Style.ColorButtonDisabled;
                GUI.DrawTexture(nextButtonRect, Textures.TextureButtonNext);
            }

            if (clickAction != null && Widgets.ButtonInvisible(fieldRect, false)) {
                clickAction();
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }
        protected float DrawColorSelectorForPawnLayer(CustomizedPawn customizedPawn, float cursorY, List<Color> swatches, bool allowAnyColor) {
            float top = cursorY;
            Color currentColor = selectedPawnLayer.GetSelectedColor(customizedPawn);
            Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
            if (swatches != null) {
                foreach (Color color in swatches) {
                    bool selected = (color == currentColor);
                    if (selected) {
                        Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, SwatchSize.x + 4, SwatchSize.y + 4);
                        GUI.color = ColorSwatchSelection;
                        GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
                    }

                    Rect borderRect = new Rect(rect.x - 1, rect.y - 1, SwatchSize.x + 2, SwatchSize.y + 2);
                    GUI.color = ColorSwatchBorder;
                    GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

                    GUI.color = color;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);

                    if (!selected) {
                        if (Widgets.ButtonInvisible(rect, false)) {
                            PawnController?.UpdatePawnLayerColor(selectedPawnLayer, color);
                            // TODO
                            //selectedPawnLayer.SelectColor(customizedPawn, color);
                            currentColor = color;
                        }
                    }

                    rect.x += SwatchSpacing.x;
                    if (rect.x >= SwatchLimit - SwatchSize.x) {
                        rect.y += SwatchSpacing.y;
                        rect.x = SwatchPosition.x;
                    }
                }
            }

            GUI.color = Color.white;
            if (!allowAnyColor) {
                return rect.y - top;
            }

            if (rect.x != SwatchPosition.x) {
                rect.x = SwatchPosition.x;
                rect.y += SwatchSpacing.y;
            }
            rect.y += 4;
            rect.width = 49;
            rect.height = 49;
            GUI.color = ColorSwatchBorder;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = currentColor;
            GUI.DrawTexture(rect.ContractedBy(1), BaseContent.WhiteTex);

            GUI.color = Color.red;
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                PawnController?.UpdatePawnLayerColor(selectedPawnLayer, new Color(r, g, b));
                // TODO
                //selectedPawnLayer.SelectColor(customizedPawn, new Color(r, g, b));
            }

            GUI.color = Color.white;
            return rect.yMax - top;
        }

        protected float DrawSkinColorSelector(CustomizedPawn customizedPawn, float cursorY, List<Color> colors, bool allowAnyColor) {
            float top = cursorY;
            Color currentColor = customizedPawn.Pawn.story.SkinColor;
            Color clickedColor = currentColor;
            Rect rect = new Rect(SwatchPosition.x, cursorY, SwatchSize.x, SwatchSize.y);
            if (colors != null) {
                foreach (Color color in colors) {
                    bool selected = (color == currentColor);
                    if (selected) {
                        Rect selectionRect = new Rect(rect.x - 2, rect.y - 2, SwatchSize.x + 4, SwatchSize.y + 4);
                        GUI.color = ColorSwatchSelection;
                        GUI.DrawTexture(selectionRect, BaseContent.WhiteTex);
                    }

                    Rect borderRect = new Rect(rect.x - 1, rect.y - 1, SwatchSize.x + 2, SwatchSize.y + 2);
                    GUI.color = ColorSwatchBorder;
                    GUI.DrawTexture(borderRect, BaseContent.WhiteTex);

                    GUI.color = color;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);

                    if (!selected) {
                        if (Widgets.ButtonInvisible(rect, false)) {
                            clickedColor = color;
                        }
                    }

                    rect.x += SwatchSpacing.x;
                    if (rect.x >= SwatchLimit - SwatchSize.x) {
                        rect.y += SwatchSpacing.y;
                        rect.x = SwatchPosition.x;
                    }
                }
            }

            GUI.color = Color.white;
            if (!allowAnyColor) {
                return rect.y - top;
            }

            if (rect.x != SwatchPosition.x) {
                rect.x = SwatchPosition.x;
                rect.y += SwatchSpacing.y;
            }
            rect.y += 4;
            rect.width = 49;
            rect.height = 49;
            GUI.color = ColorSwatchBorder;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = currentColor;
            GUI.DrawTexture(rect.ContractedBy(1), BaseContent.WhiteTex);

            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            GUI.color = Color.red;
            float r = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y - 1, 136, 16), currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 19, 136, 16), currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(new Rect(rect.x + 56, rect.y + 39, 136, 16), currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                clickedColor = new Color(r, g, b);
            }

            GUI.color = Color.white;

            if (clickedColor != currentColor) {
                SkinColorUpdated?.Invoke(clickedColor);
            }
            return rect.yMax - top;
        }

        protected bool CloseEnough(float a, float b) {
            if (a > b - 0.0001f && a < b + 0.0001f) {
                return true;
            }
            else {
                return false;
            }
        }
    }
}
