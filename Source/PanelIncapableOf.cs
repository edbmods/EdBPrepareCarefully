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

            if (state.MissingWorkTypes != null) {
                GUI.color = Color.white;
                Rect alertRect = new Rect(PanelRect.width - SizeAlert.x - 12, 10, SizeAlert.x, SizeAlert.y);
                GUI.DrawTexture(alertRect, Textures.TextureAlertSmall);
                if (cachedMissingWorkTypes != state.MissingWorkTypes) {
                    cachedMissingWorkTypes = state.MissingWorkTypes;
                    missingWorkTypeString = "";
                    foreach (var type in state.MissingWorkTypes) {
                        missingWorkTypeString += "EdB.PC.Panel.Incapable.WarningItem".Translate(new object[] { type }) + "\n";
                    }
                    missingWorkTypeString = missingWorkTypeString.TrimEndNewlines();
                }
                TooltipHandler.TipRegion(alertRect, "EdB.PC.Panel.Incapable.Warning".Translate(new object[] { missingWorkTypeString }));
            }
        }
    }
}
