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
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
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
        protected override void DrawPanelContent() {
            base.DrawPanelContent();

            GUI.color = Style.ColorText;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;

            // TODO: In the previous version, we cached the result of this logic in the customized pawn.  We don't
            // want to cache it there, but evaluate if we want to cache somewhere in the ViewState
            string incapable = null;
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn.Pawn;
            List<string> incapableList = new List<string>();
            WorkTags combinedDisabledWorkTags = pawn.story.DisabledWorkTagsBackstoryAndTraits;
            if (combinedDisabledWorkTags != WorkTags.None) {
                IEnumerable<WorkTags> list = Reflection.ReflectorCharacterCardUtility.WorkTagsFrom(combinedDisabledWorkTags);
                foreach (var tag in list) {
                    incapableList.Add(WorkTypeDefsUtility.LabelTranslated(tag).CapitalizeFirst());
                }
                if (incapableList.Count > 0) {
                    incapable = string.Join(", ", incapableList.ToArray());
                }
            }

            if (incapable == null) {
                incapable = "EdB.PC.Panel.Incapable.None".Translate();
            }
            Text.WordWrap = true;
            Text.Font = GameFont.Small;
            Widgets.Label(RectText, incapable);
        }
    }
}
