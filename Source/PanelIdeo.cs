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
    public class PanelIdeo : PanelModule {
        public delegate void CertaintyUpdatedHandler(float value);
        public delegate void IdeoRandomizedHandler();
        public delegate void IdeoUpdatedHandler(Ideo ideo);

        public event CertaintyUpdatedHandler CertaintyUpdated;
        public event IdeoRandomizedHandler IdeoRandomized;
        public event IdeoUpdatedHandler IdeoUpdated;

        public static readonly Vector2 FieldPadding = new Vector2(6, 6);

        public Rect FieldRect;
        public Rect CertaintyLabelRect;
        public Rect CertaintySliderRect;
        public Rect PercentLabelRect;
        protected WidgetField FieldIdeo = new WidgetField();
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }

        public override void Resize(float width) {
            base.Resize(width);
            float fieldPadding = 8;
            FieldRect = new Rect(FieldPadding.x, 0, width - FieldPadding.x * 2, 36);

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 sizeCertainty = Text.CalcSize("Certainty".Translate().CapitalizeFirst());
            Text.Font = GameFont.Tiny;
            Vector2 sizePercent = Text.CalcSize("100%");
            Text.Font = savedFont;
            float labelHeight = Math.Max(sizeCertainty.y, sizePercent.y);
            CertaintyLabelRect = new Rect(Margin.x, 0, sizeCertainty.x, labelHeight);
            PercentLabelRect = new Rect(width - Margin.x - sizePercent.x, 1, sizePercent.x, labelHeight);

            float sliderHeight = 8f;
            float sliderWidth = PercentLabelRect.xMin - CertaintyLabelRect.xMax - fieldPadding * 2f;
            CertaintySliderRect = new Rect(CertaintyLabelRect.xMax + fieldPadding, CertaintyLabelRect.yMin + CertaintyLabelRect.height * 0.5f - sliderHeight * 0.5f - 1,
                sliderWidth, sliderHeight);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible() {
            return ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode && !ViewState.CurrentPawn.Pawn.DevelopmentalStage.Baby();
        }

        public override float Draw(float y) {
            float top = y;
            y += Margin.y;

            y += DrawHeader(y, Width, "Ideo".Translate().CapitalizeFirst().Resolve());

            CustomizedPawn pawn = ViewState.CurrentPawn;
            Pawn_IdeoTracker ideoTracker = pawn.Pawn.ideo;
            Ideo ideo = ideoTracker?.Ideo;

            FieldIdeo.Rect = FieldRect.OffsetBy(0, y);
            labelTrimmer.Rect = FieldIdeo.Rect.InsetBy(8, 0);

            if (ideo != null) {
                FieldIdeo.Label = labelTrimmer.TrimLabelIfNeeded(ideo.name);
                FieldIdeo.Tip = ideo.description;
            }
            FieldIdeo.Enabled = true;
            FieldIdeo.ClickAction = () => {
                var dialog = new DialogIdeos() {
                    Pawn = pawn
                };
                dialog.IdeoUpdated += (i) => { IdeoUpdated?.Invoke(i); };
                Find.WindowStack.Add(dialog);
            };
            FieldIdeo.DrawIconFunc = (Rect rect) => {
                if (ideo != null) {
                    ideo.DrawIcon(rect);
                }
            };
            FieldIdeo.IconSizeFunc = () => new Vector2(32, 32);

            FieldIdeo.Draw();

            y += FieldRect.height;
            y += FieldPadding.y;

            // Draw the certainty slider
            float currentCertainty = pawn.Pawn.ideo.Certainty;
            Text.Font = GameFont.Small;
            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(CertaintyLabelRect.OffsetBy(0, y), "Certainty".Translate().CapitalizeFirst());
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            Widgets.Label(PercentLabelRect.OffsetBy(0, y), Math.Round(ideoTracker.Certainty * 100f, 0).ToString() + "%");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            float certainty = GUI.HorizontalSlider(CertaintySliderRect.OffsetBy(0, y), ideoTracker.Certainty, 0f, 1f);
            y += CertaintyLabelRect.height;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            y += Margin.y;

            // Update the certainty based on the slider value
            if (certainty != currentCertainty) {
                CertaintyUpdated?.Invoke(certainty);
            }

            // Randomize button
            Rect randomizeRect = new Rect(Width - 32, top + 9, 22, 22);
            if (randomizeRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
            if (Widgets.ButtonInvisible(randomizeRect, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                IdeoRandomized?.Invoke();
            }

            return y - top;
        }

        //protected void SelectRandomIdeo(State state) {
        //    CustomizedPawn pawn = state.CurrentPawn;
        //    Pawn_IdeoTracker ideo = pawn.Pawn?.ideo;
        //    if (ideo != null) {
        //        Ideo currentIdeo = ideo.Ideo;
        //        float certainty = ideo.Certainty;
        //        Ideo newIdeo = Find.IdeoManager.IdeosInViewOrder.Where(i => i != currentIdeo).RandomElement();
        //        ideo.SetIdeo(newIdeo);
        //        pawn.Certainty = certainty;
        //    }
        //}
    }
}
