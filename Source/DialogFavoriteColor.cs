using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class DialogFavoriteColor : Window {
        public float HeaderHeight { get; protected set; }
        public Rect FooterRect;
        public Rect HeaderRect;
        public Rect CancelButtonRect;
        public Rect ConfirmButtonRect;
        public CustomizedPawn CurrentPawn { get; set; }
        public float WindowPadding { get; protected set; }
        public Vector2 ContentMargin { get; protected set; }
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ButtonSize { get; protected set; }
        public Vector2 ContentSize { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public float FooterHeight { get; protected set; }
        public Action<Color> ConfirmAction { get; set; }
        public string CancelButtonLabel { get; protected set; }
        public string ConfirmButtonLabel { get; protected set; }
        public Rect ColorSwatchRect { get; protected set; }
        public Rect RedSliderRect { get; protected set; }
        public Rect GreenSliderRect { get; protected set; }
        public Rect BlueSliderRect { get; protected set; }
        protected Color currentColor;
        protected List<Color> StandardColors = new List<Color>();

        public DialogFavoriteColor(Color startingColor) {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            this.currentColor = startingColor;
            CancelButtonLabel = "EdB.PC.Common.Cancel".Translate();
            ConfirmButtonLabel = "EdB.PC.Common.Select".Translate();
            Resize();
        }

        public override Vector2 InitialSize {
            get {
                return new Vector2(400f, 300f);
            }
        }

        protected void Resize() {
            HeaderHeight = 32;
            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(400f, 300f);
            ButtonSize = new Vector2(140f, 40f);

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - HeaderHeight);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + HeaderHeight, ContentSize.x, ContentSize.y);

            HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);
            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20, ContentSize.x, FooterHeight);

            Vector2 colorSwatchMargin = new Vector2(10f, 20f);
            ColorSwatchRect = new Rect(HeaderRect.xMin, HeaderRect.yMax + colorSwatchMargin.y, 100, 100);

            float sliderWidth = ContentSize.x - ColorSwatchRect.width - colorSwatchMargin.x;
            float sliderX = ColorSwatchRect.xMax + colorSwatchMargin.x;
            float sliderHeight = 16;
            float middleSliderY = ColorSwatchRect.y + (ColorSwatchRect.height / 2f) - (sliderHeight / 2f);
            RedSliderRect = new Rect(sliderX, ColorSwatchRect.y, sliderWidth, sliderHeight);
            GreenSliderRect = new Rect(sliderX, middleSliderY, sliderWidth, sliderHeight);
            BlueSliderRect = new Rect(sliderX, ColorSwatchRect.yMax - sliderHeight, sliderWidth, sliderHeight);

            CancelButtonRect = new Rect(0,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
            ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
        }

        public override void PreOpen() {
            base.PreOpen();

            foreach (ColorDef colDef in DefDatabase<ColorDef>.AllDefs.Where((ColorDef x) => x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc)) {
                if (!StandardColors.Any((Color x) => x.IndistinguishableFrom(colDef.color))) {
                    StandardColors.Add(colDef.color);
                }
            }
            if (UtilityIdeo.IdeoEnabledForPawn(CurrentPawn) && CurrentPawn.Pawn?.ideo?.Ideo != null) {
                Color ideoColor = CurrentPawn.Pawn.ideo.Ideo.ApparelColor;
                if (!StandardColors.Any((Color x) => x.IndistinguishableFrom(ideoColor))) {
                    StandardColors.Add(ideoColor);
                }
            }
            StandardColors.SortByColor((Color x) => x);
        }

        public override void DoWindowContents(Rect inRect) {
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            Widgets.Label(HeaderRect, "EdB.PC.Dialog.FavoriteColor.Header".Translate());

            float topPadding = 4;
            float swatchesTop = ContentRect.y + topPadding;
            float y = WidgetColorSelector.DrawSwatches(ContentRect.x, swatchesTop, ContentRect.width, 20, currentColor, StandardColors, (Color color) => { currentColor = color; }, CurrentPawn);
            y += 12;
            //Widgets.DrawBox(ContentRect, 1);
            Rect selectorRect = new Rect(ContentRect.x, swatchesTop + y, ContentRect.width, ContentRect.height - topPadding - y);
            //Widgets.DrawBox(selectorRect, 1);
            WidgetColorSelector.DrawSelector(selectorRect, currentColor, (Color color) => { currentColor = color; });

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            GUI.BeginGroup(FooterRect);
            if (CancelButtonLabel != null) {
                if (Widgets.ButtonText(CancelButtonRect, CancelButtonLabel, true, true, true)) {
                    this.Close(true);
                }
            }
            if (Widgets.ButtonText(ConfirmButtonRect, ConfirmButtonLabel, true, true, true)) {
                this.Confirm();
                this.Close(true);
            }
            GUI.EndGroup();
        }

        protected void Confirm() {
            ConfirmAction(currentColor);
        }

        public override void PostClose() {
            GUI.FocusControl(null);
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

