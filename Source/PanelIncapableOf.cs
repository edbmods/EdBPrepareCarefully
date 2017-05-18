using RimWorld;
using System;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class PanelIncapableOf : PanelBase {
        protected Rect RectText;
        public PanelIncapableOf() {
        }
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.Incapable.Title".Translate();
            }
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);
            Vector2 panelPadding = new Vector2(16, 16);
            float textWidth = PanelRect.width - panelPadding.x * 2;
            float textHeight = PanelRect.height - BodyRect.y - panelPadding.y;
            RectText = new Rect(panelPadding.x, BodyRect.y, textWidth, textHeight);
        }
        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            GUI.color = Style.ColorText;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;

            string incapable = state.CurrentPawn.IncapableOf;
            if (incapable == null) {
                incapable = "EdB.PC.Panel.Incapable.None".Translate();
            }
            Text.WordWrap = true;
            Text.Font = GameFont.Small;
            Widgets.Label(RectText, incapable);
        }
    }
}
