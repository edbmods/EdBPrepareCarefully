using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class WidgetField {
        public Rect Rect { get; set; }
        public Nullable<Rect> ClickRect { get; set; }
        public Nullable<Rect> LabelRect { get; set; }
        public string Label { get; set; }
        public string Tip { get; set; }
        public Action ClickAction { get; set; }
        public Action PreviousAction { get; set; }
        public Action NextAction { get; set; }
        public Action<Rect> TipAction { get; set; }
        public Color Color { get; set; } = Style.ColorText;
        public bool Enabled { get; set; } = true;
        public bool NextPreviousButtonsHidden { get; set; } = false;
        public Action<Rect> DrawIconFunc { get; set; }
        public Func<Vector2> IconSizeFunc { get; set; }
        public TextAnchor Alignment { get; set; } = TextAnchor.MiddleCenter;

        public void Draw() {
            TextAnchor saveAnchor = Text.Anchor;
            Color saveColor = GUI.color;
            GameFont saveFont = Text.Font;
            try {
                // Adjust the width of the rectangle if the field has next and previous buttons.
                Rect fieldRect = Rect;
                if (PreviousAction != null || NextPreviousButtonsHidden) {
                    fieldRect.x += 12;
                    fieldRect.width -= 12;
                }
                if (NextAction != null || NextPreviousButtonsHidden) {
                    fieldRect.width -= 12;
                }
                
                // Draw the field background.
                if (Enabled) {
                    GUI.color = Color.white;
                }
                else {
                    GUI.color = Style.ColorControlDisabled;
                }
                Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                // Draw the label.
                Text.Anchor = Alignment;
                Rect iconRect = new Rect();
                Rect fullRect = new Rect(Rect.x, Rect.y + 1, Rect.width, Rect.height);
                Rect textRect = LabelRect.HasValue ? new Rect(fullRect.x + LabelRect.Value.x, fullRect.y, LabelRect.Value.width, fullRect.height) : fullRect;
                bool drawIcon = DrawIconFunc != null && IconSizeFunc != null;
                if (drawIcon) {
                    Vector2 iconSize = IconSizeFunc();
                    textRect = textRect.InsetBy(iconSize.x * 2f, 0).OffsetBy(4, 0);
                    Vector2 textSize = Text.CalcSize(Label);
                    iconRect = new Rect(fullRect.x + fullRect.width / 2f - textSize.x / 2f - iconSize.x - 4, fullRect.y + fullRect.height / 2 - iconSize.y / 2, iconSize.x, iconSize.y);
                }
                if (!Enabled) {
                    GUI.color = Style.ColorControlDisabled;
                }
                else if (ClickAction != null && textRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Color.white;
                }
                else {
                    GUI.color = this.Color;
                }
                if (drawIcon) {
                    DrawIconFunc(iconRect);
                }
                if (Label != null) {
                    Widgets.Label(textRect, Label);
                }
                GUI.color = Color.white;

                if (!Enabled) {
                    return;
                }

                // Handle the tooltip.
                if (Tip != null) {
                    TooltipHandler.TipRegion(ClickRect ?? textRect, Tip);
                }
                if (TipAction != null) {
                    TipAction(ClickRect ?? textRect);
                }

                // Draw the previous button and handle any click events on it.
                if (PreviousAction != null) {
                    Rect buttonRect = new Rect(fieldRect.x - 17, fieldRect.MiddleY() - 8, 16, 16);
                    if (buttonRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
                    if (Widgets.ButtonInvisible(buttonRect, false)) {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        PreviousAction();
                    }
                }

                // Draw the next button and handle any click events on it.
                if (NextAction != null) {
                    Rect buttonRect = new Rect(fieldRect.xMax + 1, fieldRect.MiddleY() - 8, 16, 16);
                    if (buttonRect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
                    if (Widgets.ButtonInvisible(buttonRect, false)) {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        NextAction();
                    }
                }

                // Handle any click event on the field.
                if (ClickAction != null) {
                    if (ClickRect == null) {
                        if (Widgets.ButtonInvisible(textRect, false)) {
                            ClickAction();
                        }
                    }
                    else {
                        if (Widgets.ButtonInvisible(ClickRect.Value, false)) {
                            ClickAction();
                        }
                    }
                }
            }
            finally {
                Text.Anchor = saveAnchor;
                GUI.color = saveColor;
                Text.Font = saveFont;
            }
        }
    }
}
