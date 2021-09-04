using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
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
                return new Vector2(400f, 280f);
            }
        }

        protected void Resize() {
            HeaderHeight = 32;
            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(400f, 280f);
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

        public override void DoWindowContents(Rect inRect) {
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            Widgets.Label(HeaderRect, "EdB.PC.Dialog.FavoriteColor.Header".Translate());

            GUI.color = Color.white;
            GUI.DrawTexture(ColorSwatchRect, BaseContent.WhiteTex);
            GUI.color = currentColor;
            GUI.DrawTexture(ColorSwatchRect.ContractedBy(1), BaseContent.WhiteTex);

            GUI.color = Color.red;
            float originalR = currentColor.r;
            float originalG = currentColor.g;
            float originalB = currentColor.b;
            float r = GUI.HorizontalSlider(RedSliderRect, currentColor.r, 0, 1);
            GUI.color = Color.green;
            float g = GUI.HorizontalSlider(GreenSliderRect, currentColor.g, 0, 1);
            GUI.color = Color.blue;
            float b = GUI.HorizontalSlider(BlueSliderRect, currentColor.b, 0, 1);
            if (!CloseEnough(r, originalR) || !CloseEnough(g, originalG) || !CloseEnough(b, originalB)) {
                currentColor = new Color(r, g, b);
            }

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

