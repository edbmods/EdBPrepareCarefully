using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerHair : PawnLayer {
        private List<PawnLayerOption> options = new List<PawnLayerOption>();
        private List<Color> swatches = null;

        public override List<PawnLayerOption> Options {
            get {
                return options;
            }
            set {
                options = value;
            }
        }

        public override ColorSelectorType ColorSelectorType {
            get {
                return ColorSelectorType.RGB;
            }
        }

        public override List<Color> ColorSwatches {
            get {
                return swatches;
            }
            set {
                swatches = value;
            }
        }

        public override bool IsOptionSelected(CustomPawn pawn, PawnLayerOption option) {
            PawnLayerOptionHair hairOption = option as PawnLayerOptionHair;
            if (hairOption == null) {
                return false;
            }
            return pawn.Pawn.story.hairDef == hairOption.HairDef;
        }

        public override int? GetSelectedIndex(CustomPawn pawn) {
            int selectedIndex = options.FirstIndexOf((option) => {
                PawnLayerOptionHair hairOption = option as PawnLayerOptionHair;
                if (hairOption == null) {
                    return false;
                }
                else {
                    return hairOption.HairDef == pawn.Pawn.story.hairDef;
                }
            });
            if (selectedIndex > -1) {
                return selectedIndex;
            }
            else {
                return null;
            }
        }

        public override PawnLayerOption GetSelectedOption(CustomPawn pawn) {
            int? selectedIndex = GetSelectedIndex(pawn);
            if (selectedIndex == null) {
                return null;
            }
            else if (selectedIndex.Value >= 0 && selectedIndex.Value < options.Count) {
                    return options[selectedIndex.Value];
            }
            else {
                return null;
            }
        }

        public override void SelectOption(CustomPawn pawn, PawnLayerOption option) {
            PawnLayerOptionHair hairOption = option as PawnLayerOptionHair;
            if (hairOption != null) {
                pawn.Pawn.story.hairDef = hairOption.HairDef;
                pawn.MarkPortraitAsDirty();
            }
        }

        public override Color GetSelectedColor(CustomPawn pawn) {
            return pawn.HairColor;
        }

        public override void SelectColor(CustomPawn pawn, Color color) {
            pawn.HairColor = color;
        }
    }
}
