﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerBody : PawnLayer {
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
                return ColorSelectorType.Skin;
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
            PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
            if (bodyOption == null) {
                return false;
            }
            return pawn.BodyType == bodyOption.BodyTypeDef;
        }

        public override int? GetSelectedIndex(CustomPawn pawn) {
            int selectedIndex = options.FirstIndexOf((option) => {
                PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
                if (bodyOption == null) {
                    return false;
                }
                else {
                    return bodyOption.BodyTypeDef == pawn.BodyType;
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
            PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
            if (bodyOption != null) {
                pawn.BodyType = bodyOption.BodyTypeDef;
            }
        }

        public override Color GetSelectedColor(CustomPawn pawn) {
            return pawn.SkinColor;
        }

        public override void SelectColor(CustomPawn pawn, Color color) {
            pawn.SkinColor = color;
        }
    }
}
