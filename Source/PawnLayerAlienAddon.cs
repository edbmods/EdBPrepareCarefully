using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerAlienAddon : PawnLayer {
        private List<PawnLayerOption> options = new List<PawnLayerOption>();
        private List<Color> swatches;
        private ColorSelectorType colorSelectorType = ColorSelectorType.None;

        public override List<PawnLayerOption> Options {
            get {
                return options;
            }
            set {
                options = value;
            }
        }
        public bool Hair {
            get;
            set;
        }
        public bool Skin {
            get;
            set;
        }
        public override ColorSelectorType ColorSelectorType {
            get {
                return colorSelectorType;
            }
            set {
                colorSelectorType = value;
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
        public AlienRaceBodyAddon AlienAddon {
            get;
            set;
        }

        public override bool IsOptionSelected(CustomPawn pawn, PawnLayerOption option) {
            PawnLayerOptionAlienAddon addonOption = option as PawnLayerOptionAlienAddon;
            if (addonOption == null) {
                return false;
            }
            if (pawn.AlienRace != null) {
                ThingComp alienComp = pawn.Pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                    return (comp.GetType().Name == "AlienComp");
                });
                if (alienComp == null) {
                    return false;
                }
                FieldInfo variantsField = ReflectionUtil.GetPublicField(alienComp, "addonVariants");
                if (variantsField == null) {
                    return false;
                }
                List<int> variants = null;
                try {
                    variants = (List<int>)variantsField.GetValue(alienComp);
                }
                catch (Exception) {
                    return false;
                }
                int selectedIndex = variants[AlienAddon.VariantIndex];
                return selectedIndex == addonOption.Index;
            }
            return false;
        }

        private int? GetSelectedVariant(CustomPawn pawn, int variantIndex) {
            if (pawn.AlienRace == null) {
                return null;
            }
            ThingComp alienComp = pawn.Pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                return (comp.GetType().Name == "AlienComp");
            });
            if (alienComp == null) {
                return null;
            }
            FieldInfo variantsField = ReflectionUtil.GetPublicField(alienComp, "addonVariants");
            if (variantsField == null) {
                return null;
            }
            List<int> variants = null;
            try {
                variants = (List<int>)variantsField.GetValue(alienComp);
            }
            catch (Exception) {
                return null;
            }
            return variants[variantIndex];
        }

        public override int? GetSelectedIndex(CustomPawn pawn) {
            if (pawn.AlienRace == null) {
                return null;
            }
            ThingComp alienComp = pawn.Pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                return (comp.GetType().Name == "AlienComp");
            });
            if (alienComp == null) {
                return null;
            }
            FieldInfo variantsField = ReflectionUtil.GetPublicField(alienComp, "addonVariants");
            if (variantsField == null) {
                return null;
            }
            List<int> variants = null;
            try {
                variants = (List<int>)variantsField.GetValue(alienComp);
            }
            catch (Exception) {
                return null;
            }
            return variants[AlienAddon.VariantIndex];
        }

        public override PawnLayerOption GetSelectedOption(CustomPawn pawn) {
            int? selectedIndex = GetSelectedIndex(pawn);
            if (selectedIndex == null) {
                return null;
            }
            else {
                return options[selectedIndex.Value];
            }
        }

        public override void SelectOption(CustomPawn pawn, PawnLayerOption option) {
            PawnLayerOptionAlienAddon addonOption = option as PawnLayerOptionAlienAddon;
            if (addonOption == null) {
                return;
            }
            if (pawn.AlienRace != null) {
                ThingComp alienComp = pawn.Pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                    return (comp.GetType().Name == "AlienComp");
                });
                if (alienComp == null) {
                    return;
                }
                FieldInfo variantsField = ReflectionUtil.GetPublicField(alienComp, "addonVariants");
                if (variantsField == null) {
                    return;
                }
                List<int> variants = null;
                try {
                    variants = (List<int>)variantsField.GetValue(alienComp);
                }
                catch (Exception) {
                    return;
                }
                variants[AlienAddon.VariantIndex] = addonOption.Index;
                pawn.MarkPortraitAsDirty();
            }
        }
        
        public override Color GetSelectedColor(CustomPawn pawn) {
            if (Hair) {
                return pawn.HairColor;
            }
            else if (Skin) {
                return pawn.SkinColor;
            }
            else {
                return Color.white;
            }
        }

        public override void SelectColor(CustomPawn pawn, Color color) {
            if (Hair) {
                pawn.HairColor = color;
            }
            else if (Skin) {
                pawn.SkinColor = color;
            }
        }

    }
}
