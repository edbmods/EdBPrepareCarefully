using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class PanelBase : IPanel {
        public Rect PanelRect {
            get;
            protected set;
        }
        public Rect BodyRect {
            get;
            protected set;
        }
        public virtual Color ColorPanelBackground {
            get {
                return Style.ColorPanelBackground;
            }
        }
        public virtual string PanelHeader {
            get {
                return null;
            }
        }
        public PanelBase() {
        }

        public virtual void Resize(Rect rect) {
            PanelRect = rect;
            BodyRect = new Rect(0, 0, rect.width, rect.height);
            if (PanelHeader != null) {
                BodyRect = new Rect(0, 36, rect.width, rect.height - 36);
            }
        }
        public virtual void Draw(State state) {
            DrawPanelBackground();
            DrawPanelHeader();
            GUI.BeginGroup(PanelRect);
            try {
                DrawPanelContent(state);
            }
            finally {
                GUI.EndGroup();
            }
            GUI.color = Color.white;
        }
        protected virtual void DrawPanelBackground() {
            GUI.color = ColorPanelBackground;
            GUI.DrawTexture(PanelRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }
        protected virtual void DrawPanelHeader() {
            if (PanelHeader == null) {
                return;
            }
            var fontValue = Text.Font;
            var anchorValue = Text.Anchor;
            var colorValue = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(15 + PanelRect.xMin, 5 + PanelRect.yMin, PanelRect.width - 30, 40), PanelHeader);
            Text.Font = fontValue;
            Text.Anchor = anchorValue;
            GUI.color = colorValue;
        }
        protected virtual void DrawPanelContent(State state) {
            GUI.color = Style.ColorTextPanelHeader;

            GUI.color = Color.white;
        }
    }
}