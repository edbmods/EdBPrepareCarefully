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
        public ManagerPawns PawnManager { get; set; }

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
        public bool Hair { get; set; }
        public bool Skin { get; set; }
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
        public List<PawnLayerOption> LinkedLayers { get; set; }

        public AlienRaceBodyAddon AlienAddon { get; set; }

        public override bool IsOptionSelected(CustomizedPawn pawn, PawnLayerOption option) {
            PawnLayerOptionAlienAddon addonOption = option as PawnLayerOptionAlienAddon;
            if (addonOption == null) {
                return false;
            }
            if (pawn.Customizations.AlienRace != null) {
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


        public override int? GetSelectedIndex(CustomizedPawn pawn) {
            if (pawn.Customizations.AlienRace == null) {
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

        public override PawnLayerOption GetSelectedOption(CustomizedPawn pawn) {
            int? selectedIndex = GetSelectedIndex(pawn);
            if (selectedIndex == null) {
                return null;
            }
            else {
                return options[selectedIndex.Value];
            }
        }


        public override Color GetSelectedColor(CustomizedPawn pawn) {
            if (Hair) {
                return pawn.Pawn.story.HairColor;
            }
            else if (Skin) {
                return pawn.Pawn.story.SkinColor;
            }
            else {
                return Color.white;
            }
        }

    }
}
