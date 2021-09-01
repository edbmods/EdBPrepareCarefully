using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PanelModuleIdeo : PanelModule {
        public static readonly Vector2 FieldPadding = new Vector2(6, 6);

        public Rect FieldRect;
        public Rect CertaintyLabelRect;
        public Rect CertaintySliderRect;
        protected Field FieldFaction = new Field();
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();

        public override void Resize(float width) {
            base.Resize(width);
            float fieldPadding = 8;
            FieldRect = new Rect(FieldPadding.x, 0, width - FieldPadding.x * 2, 36);

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 sizeCertainty = Text.CalcSize("Certainty".Translate().CapitalizeFirst());
            Text.Font = savedFont;
            CertaintyLabelRect = new Rect(Margin.x, 0, sizeCertainty.x, sizeCertainty.y);

            float sliderHeight = 8f;

            CertaintySliderRect = new Rect(CertaintyLabelRect.xMax + fieldPadding, CertaintyLabelRect.yMin + CertaintyLabelRect.height * 0.5f - sliderHeight * 0.5f,
                width - sizeCertainty.x - fieldPadding - Margin.x * 2f, sliderHeight);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible(State state) {
            return ModsConfig.IdeologyActive;
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Ideo".Translate().CapitalizeFirst().Resolve());

            CustomPawn pawn = state.CurrentPawn;
            Pawn_IdeoTracker ideoTracker = pawn.Pawn.ideo;
            Ideo ideo = ideoTracker?.Ideo;

            FieldFaction.Rect = FieldRect.OffsetBy(0, y);
            labelTrimmer.Rect = FieldFaction.Rect.InsetBy(8, 0);

            if (ideo != null) {
                FieldFaction.Label = labelTrimmer.TrimLabelIfNeeded(ideo.name);
                FieldFaction.Tip = ideo.description;
            }
            FieldFaction.Enabled = true;
            FieldFaction.ClickAction = () => {
                var dialog = new DialogIdeos() {
                    Pawn = pawn
                };
                Find.WindowStack.Add(dialog);
            };

            FieldFaction.Draw();

            Rect iconRect = FieldRect.OffsetBy(8, y).InsetBy(2);
            iconRect = new Rect(iconRect.x, iconRect.y, 32, 32);
            ideo.DrawIcon(iconRect);

            y += FieldRect.height;
            y += FieldPadding.y;

            // Draw the certainty slider
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect labelRect = CertaintyLabelRect.OffsetBy(0, y);
            Widgets.Label(labelRect, "Certainty".Translate().CapitalizeFirst());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            float certainty = GUI.HorizontalSlider(CertaintySliderRect.OffsetBy(0, y), ideoTracker.Certainty, 0, 1);
            y += CertaintyLabelRect.height;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            y += Margin.y;

            if (ideoTracker.Certainty != certainty) {
                float diff = ideoTracker.Certainty - certainty;
                ideoTracker.Debug_ReduceCertainty(diff);
            }

            return y - top;
        }

    }
}
