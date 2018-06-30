using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class Field {
        private Rect rect;
        private string label;
        private string tip;
        private Action nextAction;
        private Action previousAction;
        private Action clickAction;
        private Color color = Style.ColorText;
        private bool enabled = true;
        public Field() {
        }
        public Rect Rect {
            get {
                return rect;
            }
            set {
                rect = value;
            }
        }
        public Nullable<Rect> ClickRect {
            get;
            set;
        }
        public string Label {
            get {
                return label;
            }
            set {
                label = value;
            }
        }
        public string Tip {
            get {
                return tip;
            }
            set {
                tip = value;
            }
        }
        public Action ClickAction {
            get {
                return clickAction;
            }
            set {
                clickAction = value;
            }
        }
        public Action PreviousAction {
            get {
                return previousAction;
            }
            set {
                previousAction = value;
            }
        }
        public Action NextAction {
            get {
                return nextAction;
            }
            set {
                nextAction = value;
            }
        }
        public Color Color {
            get {
                return color;
            }
            set {
                color = value;
            }
        }
        public bool Enabled {
            get {
                return enabled;
            }
            set {
                enabled = value;
            }
        }
        public void Draw() {
            TextAnchor saveAnchor = Text.Anchor;
            Color saveColor = GUI.color;
            GameFont saveFont = Text.Font;
            try {
                // Adjust the width of the rectangle if the field has next and previous buttons.
                Rect fieldRect = rect;
                if (previousAction != null) {
                    fieldRect.x += 12;
                    fieldRect.width -= 12;
                }
                if (nextAction != null) {
                    fieldRect.width -= 12;
                }

                // If the field is not enabled, draw the disabled background and then return.
                if (!enabled) {
                    GUI.color = Style.ColorControlDisabled;
                    Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);
                    return;
                }

                // Draw the field background.
                GUI.color = Color.white;
                Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                // Draw the label.
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect textRect = new Rect(rect.x, rect.y + 1, rect.width, rect.height);
                if (clickAction != null && fieldRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Color.white;
                }
                else {
                    GUI.color = this.Color;
                }
                if (label != null) {
                    Widgets.Label(textRect, label);
                }
                GUI.color = Color.white;

                // Handle the tooltip.
                if (tip != null) {
                    TooltipHandler.TipRegion(fieldRect, tip);
                }

                // Draw the previous button and handle any click events on it.
                if (previousAction != null) {
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
                        previousAction();
                    }
                }

                // Draw the next button and handle any click events on it.
                if (nextAction != null) {
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
                        nextAction();
                    }
                }

                // Handle any click event on the field.
                if (clickAction != null) {
                    if (ClickRect == null) {
                        if (Widgets.ButtonInvisible(fieldRect, false)) {
                            clickAction();
                        }
                    }
                    else {
                        if (Widgets.ButtonInvisible(ClickRect.Value, false)) {
                            clickAction();
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
