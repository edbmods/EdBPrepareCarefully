using RimWorld;
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




        public override bool IsOptionSelected(CustomizedPawn pawn, PawnLayerOption option) {
            PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
            if (bodyOption == null) {
                return false;
            }
            return pawn.Pawn.story.bodyType == bodyOption.BodyTypeDef;
        }
        public override int? GetSelectedIndex(CustomizedPawn pawn) {
            int selectedIndex = options.FirstIndexOf((option) => {
                PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
                if (bodyOption == null) {
                    return false;
                }
                else {
                    return bodyOption.BodyTypeDef == pawn.Pawn.story.bodyType;
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
            PawnLayerOptionBody bodyOption = option as PawnLayerOptionBody;
            if (bodyOption != null) {
                pawn.Pawn.story.bodyType = bodyOption.BodyTypeDef;
                pawn.Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }
        public override Color GetSelectedColor(CustomizedPawn pawn) {
            return pawn.Pawn.story.SkinColor;
        }

        public override void SelectColor(CustomizedPawn pawn, Color color) {
            if (color == pawn.Pawn.story.SkinColor) {
                return;
            }
            bool removeOverride = false;
            var melaninGeneDef = pawn.Pawn.genes.GetMelaninGene();
            Gene activeSkinColorGene = null;
            if (pawn.Pawn?.genes?.GenesListForReading != null) {
                activeSkinColorGene = pawn.Pawn.genes.GenesListForReading.Where(g => g.Active && g.def.skinColorOverride.HasValue && g.overriddenByGene == null).FirstOrDefault();
            }
            if (activeSkinColorGene == null && melaninGeneDef?.skinColorBase != null && melaninGeneDef.skinColorBase == color) {
                removeOverride = true;
            }
            if (removeOverride) {
                pawn.Pawn.story.skinColorOverride = null;
            }
            else {
                pawn.Pawn.story.skinColorOverride = color;
            }
            pawn.Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
