using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class PawnLayer {
        public string Name {
            get;
            set;
        }
        public bool Apparel {
            get;
            set;
        }
        public ApparelLayerDef ApparelLayer {
            get;
            set;
        }
        public string Label {
            get;
            set;
        }
        public virtual ColorSelectorType ColorSelectorType {
            get {
                return ColorSelectorType.None;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public virtual List<Color> ColorSwatches {
            get {
                return null;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public virtual List<PawnLayerOption> Options {
            get {
                return null;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public virtual bool IsOptionSelected(CustomPawn pawn, PawnLayerOption option) {
            return false;
        }
        public virtual PawnLayerOption GetSelectedOption(CustomPawn pawn) {
            return null;
        }
        public virtual void SelectOption(CustomPawn pawn, PawnLayerOption option) {
        }
        public virtual int? GetSelectedIndex(CustomPawn pawn) {
            return null;
        }
        public virtual Color GetSelectedColor(CustomPawn pawn) {
            return Color.white;
        }
        public virtual void SelectColor(CustomPawn pawn, Color color) {
        }
    }
}
