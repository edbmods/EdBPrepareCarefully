using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerBeard : PawnLayer {
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


        public override bool IsOptionSelected(CustomizedPawn pawn, PawnLayerOption option) {
            var aOption = option as PawnLayerOptionBeard;
            if (aOption == null) {
                return false;
            }
            return pawn.Pawn.style.beardDef == aOption.BeardDef;
        }

        public override int? GetSelectedIndex(CustomizedPawn pawn) {
            int selectedIndex = options.FirstIndexOf((option) => {
                if (!(option is PawnLayerOptionBeard beardOption)) {
                    return false;
                }
                else {
                    return beardOption.BeardDef == pawn.Pawn.style.beardDef;
                }
            });
            if (selectedIndex > -1) {
                return selectedIndex;
            }
            else {
                return null;
            }
        }

        public override PawnLayerOption GetSelectedOption(CustomizedPawn pawn) {
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

        public override void SelectOption(CustomizedPawn pawn, PawnLayerOption option) {
            if (option is PawnLayerOptionBeard beardOption) {
                pawn.Pawn.style.beardDef = beardOption.BeardDef;
                pawn.Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public override Color GetSelectedColor(CustomizedPawn pawn) {
            return pawn.Pawn.story.HairColor;
        }

        public override void SelectColor(CustomizedPawn pawn, Color color) {
            pawn.Pawn.story.HairColor = color;
            pawn.Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
