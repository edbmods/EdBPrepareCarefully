using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerBodyTattoo : PawnLayer {
        private List<PawnLayerOption> options = new List<PawnLayerOption>();

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
                return ColorSelectorType.None;
            }
        }

        public override bool IsOptionSelected(CustomizedPawn pawn, PawnLayerOption option) {
            var aOption = option as PawnLayerOptionTattoo;
            if (aOption == null) {
                return false;
            }
            return pawn.Pawn.style.BodyTattoo == aOption.TattooDef;
        }

        public override int? GetSelectedIndex(CustomizedPawn pawn) {
            int selectedIndex = options.FirstIndexOf((option) => {
                if (!(option is PawnLayerOptionTattoo layerOption)) {
                    return false;
                }
                else {
                    return layerOption.TattooDef == pawn.Pawn.style.BodyTattoo;
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
            if (option is PawnLayerOptionTattoo layerOption) {
                pawn.Pawn.style.BodyTattoo = layerOption.TattooDef;
                pawn.Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

    }
}
