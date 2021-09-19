using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PanelModule {
        public static readonly Vector2 Margin = new Vector2(10, 3);
        public static readonly float HeaderHeight = 36;

        public virtual float Width {
            get;
            protected set;
        }

        public virtual bool IsVisible(State state) {
            return true;
        }

        public virtual void Resize(float width) {
            this.Width = width;
            return;
        }

        // Leaving here for compatibility with any patches that used the old method for drawing, but don't use it.
        [Obsolete("DrawPanelContent is deprecated.  Patch the Draw() method instead")]
        protected virtual void DrawPanelContent(State state) {
        }

        public virtual float Draw(State state, float y) {
            return 0;
        }

        public virtual float DrawHeader(float y, float width, string text) {
            Rect headerLabelRect = new Rect(Margin.x, y, width - (Margin.x * 2), HeaderHeight);
            var fontValue = Text.Font;
            var anchorValue = Text.Anchor;
            var colorValue = GUI.color;
            GUI.color = Style.ColorTextPanelHeader;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(headerLabelRect, text);
            Text.Font = fontValue;
            Text.Anchor = anchorValue;
            GUI.color = colorValue;
            return HeaderHeight;
        }
    }
}
