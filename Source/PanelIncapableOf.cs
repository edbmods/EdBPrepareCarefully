using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class PanelIncapableOf : PanelBase {
        protected Rect RectText;
        protected Vector2 SizeAlert = new Vector2(20, 20);
        protected List<string> cachedMissingWorkTypes = null;
        protected string missingWorkTypeString = null;
        public PanelIncapableOf() {
        }
        public override string PanelHeader {
            get {
                return "IncapableOf".Translate();
            }
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);
            Vector2 panelPadding = new Vector2(11, 16);
            float textWidth = PanelRect.width - panelPadding.x * 2;
            float textHeight = PanelRect.height - BodyRect.y - panelPadding.y;
            RectText = new Rect(panelPadding.x, BodyRect.y, textWidth, textHeight);
        }
        protected override void DrawPanelContent(State state) {
            if (state.MissingWorkTypes != null) {
                if (cachedMissingWorkTypes != state.MissingWorkTypes) {
                    cachedMissingWorkTypes = state.MissingWorkTypes;
                    missingWorkTypeString = "";
                    foreach (var type in state.MissingWorkTypes) {
                        missingWorkTypeString += "EdB.PC.Panel.Incapable.WarningItem".Translate(type ) + "\n";
                    }
                    missingWorkTypeString = missingWorkTypeString.TrimEndNewlines();
                }
                Warning = "EdB.PC.Panel.Incapable.Warning".Translate(missingWorkTypeString);
            }
            else {
                Warning = null;
            }

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
