using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelAge : PanelBase {
        public delegate void UpdateAgeHandler(int age);

        public event UpdateAgeHandler BiologicalAgeUpdated;
        public event UpdateAgeHandler ChronologicalAgeUpdated;

        protected static Rect RectBiologicalAgeLabel;
        protected static Rect RectBiologicalAgeField;
        protected static Rect RectChronologicalAgeLabel;
        protected static Rect RectChronologicalAgeField;

        private DragSlider chronologicalAgeDragSlider;
        private DragSlider biologicalAgeDragSlider;

        public PanelAge() {
            chronologicalAgeDragSlider = new DragSlider(0.4f, 20, 100);
            chronologicalAgeDragSlider.minValue = Constraints.AgeBiologicalMin;
            chronologicalAgeDragSlider.maxValue = Constraints.AgeChronologicalMax;

            biologicalAgeDragSlider = new DragSlider(0.4f, 15, 100);
            biologicalAgeDragSlider.minValue = Constraints.AgeBiologicalMin;
            biologicalAgeDragSlider.maxValue = Constraints.AgeBiologicalMax;
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 available = PanelRect.size - Style.SizePanelPadding;

            float arrowPadding = 1;
            float arrowWidth = Textures.TextureButtonNext.width;
            float bioWidth = 32;
            float chronoWidth = 48;

            float extendedArrowSize = arrowPadding + arrowWidth;
            float extendedFieldSize = extendedArrowSize * 2;

            float usedSpace = (extendedFieldSize * 2) + bioWidth + chronoWidth;

            float availableSpace = PanelRect.width - usedSpace;
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
            float contentHeight = labelHeight + fieldHeight;
            float top = PanelRect.HalfHeight() - (contentHeight * 0.5f);
            float fieldTop = top + labelHeight;

            RectBiologicalAgeField = new Rect(spacing + extendedArrowSize, fieldTop, bioWidth + extraFieldWidth, fieldHeight);
            RectChronologicalAgeField = new Rect(RectBiologicalAgeField.xMax + extendedArrowSize +
                spacing + extendedArrowSize, fieldTop, chronoWidth + extraFieldWidth, fieldHeight);


            RectBiologicalAgeLabel = new Rect(RectBiologicalAgeField.MiddleX() - bioLabelSize.x / 2,
                                              RectBiologicalAgeField.y - bioLabelSize.y, bioLabelSize.x, bioLabelSize.y);
            RectChronologicalAgeLabel = new Rect(RectChronologicalAgeField.MiddleX() - chronoLabelSize.x / 2,
                                              RectChronologicalAgeField.y - chronoLabelSize.y, chronoLabelSize.x, chronoLabelSize.y);

        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            CustomPawn customPawn = state.CurrentPawn;
            chronologicalAgeDragSlider.minValue = customPawn.BiologicalAge;
            biologicalAgeDragSlider.maxValue = customPawn.ChronologicalAge < Constraints.AgeBiologicalMax ?
                customPawn.ChronologicalAge : Constraints.AgeBiologicalMax;

            // Age labels.
            Text.Font = GameFont.Tiny;
            GUI.color = Style.ColorText;
            Widgets.Label(RectBiologicalAgeLabel, "EdB.PC.Panel.Age.Biological".Translate());
            Widgets.Label(RectChronologicalAgeLabel, "EdB.PC.Panel.Age.Chronological".Translate());
            Text.Font = GameFont.Small;

            // Biological age field.
            GUI.color = Color.white;
            Rect fieldRect = RectBiologicalAgeField;
            Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

            biologicalAgeDragSlider.OnGUI(fieldRect, customPawn.BiologicalAge, (int value) => {
                customPawn.BiologicalAge = value;
            });
            bool dragging = DragSlider.IsDragging();

            Rect buttonRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
            if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int age = customPawn.BiologicalAge - amount;
                BiologicalAgeUpdated(age);
            }

            buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
            if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int age = customPawn.BiologicalAge + amount;
                BiologicalAgeUpdated(age);
            }

            // Chronological age field.
            GUI.color = Color.white;
            fieldRect = RectChronologicalAgeField;
            Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

            chronologicalAgeDragSlider.OnGUI(fieldRect, customPawn.ChronologicalAge, (int value) => {
                customPawn.ChronologicalAge = value;
            });
            dragging = DragSlider.IsDragging();

            buttonRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
            if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonPrevious);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int age = customPawn.ChronologicalAge - amount;
                ChronologicalAgeUpdated(age);
            }

            buttonRect = new Rect(fieldRect.x + fieldRect.width + 1, fieldRect.y + 6, 16, 16);
            if (!dragging && buttonRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(buttonRect, Textures.TextureButtonNext);
            if (Widgets.ButtonInvisible(buttonRect, false)) {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                int amount = Event.current.shift ? 10 : 1;
                int age = customPawn.ChronologicalAge + amount;
                ChronologicalAgeUpdated(age);
            }
        }
    }
}
