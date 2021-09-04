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
    public class PanelModuleAge : PanelModule {
        public static readonly Vector2 FieldPadding = new Vector2(6, 6);
        public delegate void UpdateAgeHandler(int age);

        public event UpdateAgeHandler BiologicalAgeUpdated;
        public event UpdateAgeHandler ChronologicalAgeUpdated;

        private ProviderAgeLimits providerAgeLimits = PrepareCarefully.Instance.Providers.AgeLimits;

        protected static Rect RectBiologicalAgeLabel;
        protected static Rect RectBiologicalAgeField;
        protected static Rect RectChronologicalAgeLabel;
        protected static Rect RectChronologicalAgeField;

        private WidgetNumberField biologicalField;
        private WidgetNumberField chronologicalField;

        public PanelModuleAge() {
            biologicalField = new WidgetNumberField() {
                DragSlider = new DragSlider(0.4f, 20, 100),
                MinValue = 14,
                MaxValue = 99,
                UpdateAction = (int value) => {
                    UpdateBiologicalAge(value);
                }
            };
            chronologicalField = new WidgetNumberField() {
                DragSlider = new DragSlider(0.4f, 15, 100),
                MinValue = 14,
                MaxValue = Constraints.AgeChronologicalMax,
                UpdateAction = (int value) => {
                    UpdateChronologicalAge(value);
                }
            };
        }

        protected void UpdateBiologicalAge(int value) {
            BiologicalAgeUpdated(value);
        }

        protected void UpdateChronologicalAge(int value) {
            ChronologicalAgeUpdated(value);
        }

        public override void Resize(float width) {
            base.Resize(width);

            float arrowPadding = 1;
            float arrowWidth = Textures.TextureButtonNext.width;
            float bioWidth = 32;
            float chronoWidth = 48;

            float extendedArrowSize = arrowPadding + arrowWidth;
            float extendedFieldSize = extendedArrowSize * 2;

            float usedSpace = (extendedFieldSize * 2) + bioWidth + chronoWidth;

            float availableSpace = width - usedSpace;
            float spacing = availableSpace / 3;

            float idealSpace = 15;
            float extraFieldWidth = 0;
            if (spacing > idealSpace) {
                float extra = (spacing - idealSpace) * 3;
                extraFieldWidth += Mathf.Floor(extra / 2);
                spacing = idealSpace;
            }
            else {
                spacing = Mathf.Floor(spacing);
            }
            float fieldHeight = 28;

            GameFont saveFont = Text.Font;
            Text.Font = GameFont.Tiny;
            Vector2 bioLabelSize = Text.CalcSize("EdB.PC.Panel.Age.Biological".Translate());
            Vector2 chronoLabelSize = Text.CalcSize("EdB.PC.Panel.Age.Chronological".Translate());
            Text.Font = saveFont;

            float labelHeight = Mathf.Max(bioLabelSize.y, chronoLabelSize.y);
            float top = 0;
            float fieldTop = top + labelHeight;

            RectBiologicalAgeField = new Rect(spacing + extendedArrowSize, fieldTop, bioWidth + extraFieldWidth, fieldHeight);
            RectChronologicalAgeField = new Rect(RectBiologicalAgeField.xMax + extendedArrowSize +
                spacing + extendedArrowSize, fieldTop, chronoWidth + extraFieldWidth, fieldHeight);

            RectBiologicalAgeLabel = new Rect(RectBiologicalAgeField.MiddleX() - bioLabelSize.x / 2,
                RectBiologicalAgeField.y - bioLabelSize.y, bioLabelSize.x, bioLabelSize.y);
            RectChronologicalAgeLabel = new Rect(RectChronologicalAgeField.MiddleX() - chronoLabelSize.x / 2,
                RectChronologicalAgeField.y - chronoLabelSize.y, chronoLabelSize.x, chronoLabelSize.y);
        }

        public float Measure() {
            return 0;
        }

        public override bool IsVisible(State state) {
            return base.IsVisible(state);
        }

        public override float Draw(State state, float y) {
            float top = y;
            y += Margin.y;

            // Update field values.
            CustomPawn pawn = state.CurrentPawn;
            int maxAge = providerAgeLimits.MaxAgeForPawn(pawn.Pawn);
            int minAge = providerAgeLimits.MinAgeForPawn(pawn.Pawn);
            chronologicalField.MinValue = pawn.BiologicalAge;
            biologicalField.MinValue = minAge;
            biologicalField.MaxValue = pawn.ChronologicalAge < maxAge ? pawn.ChronologicalAge : maxAge;

            // Age labels.
            Text.Font = GameFont.Tiny;
            GUI.color = Style.ColorText;
            Widgets.Label(RectBiologicalAgeLabel.OffsetBy(0, y), "EdB.PC.Panel.Age.Biological".Translate());
            Widgets.Label(RectChronologicalAgeLabel.OffsetBy(0, y), "EdB.PC.Panel.Age.Chronological".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Biological age field.
            Rect fieldRect = RectBiologicalAgeField;
            biologicalField.Draw(fieldRect.OffsetBy(0, y), pawn.BiologicalAge);

            // Chronological age field.
            fieldRect = RectChronologicalAgeField;
            chronologicalField.Draw(fieldRect.OffsetBy(0, y), pawn.ChronologicalAge);

            y += RectBiologicalAgeLabel.height;
            y += fieldRect.height;

            y += Margin.y;
            return y - top;
        }
    }
}
