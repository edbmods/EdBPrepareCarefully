using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class CustomParentChildPawn {
        private CustomPawn pawn = null;
        private Gender? gender = null;
        private bool hidden = false;
        private int index = 0;
        private string name = null;
        public CustomPawn Pawn {
            get {
                return pawn;
            }
            set {
                pawn = value;
            }
        }
        public Gender? Gender {
            get {
                if (Pawn != null) {
                    return Pawn.Gender;
                }
                else {
                    return null;
                }
            }
            set {
                gender = value;
            }
        }
        public bool Hidden {
            get {
                return hidden;
            }
            set {
                if (value != hidden) {
                    hidden = value;
                }
            }
        }
        public int Index {
            get {
                return index;
            }
            set {
                index = value;
            }
        }
        public string Name {
            get {
                if (hidden) {
                    if (name != null) {
                        return name;
                    }
                    else {
                        return "EdB.PC.Pawn.HiddenPawnName".Translate(new object[] { index });
                    }
                }
                else {
                    return pawn.Pawn.LabelShort;
                }
            }
            set {
                name = value;
            }
        }
        public CustomParentChildPawn() {
            this.pawn = null;
            this.hidden = false;
        }
        public CustomParentChildPawn(CustomPawn pawn) {
            this.pawn = pawn;
            this.hidden = false;
        }
        public CustomParentChildPawn(CustomPawn pawn, bool hidden) {
            this.pawn = pawn;
            this.hidden = hidden;
        }
        public override string ToString() {
            if (Pawn == null) {
                return "null";
            }
            else {
                return (hidden == true ? "HIDDEN " : "") + Pawn.Pawn.LabelShort;
            }
        }
    }
}
