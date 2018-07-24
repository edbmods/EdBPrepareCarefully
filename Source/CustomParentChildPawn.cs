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
        public CustomPawn Pawn {
            get {
                return pawn;
            }
            set {
                pawn = value;
            }
        }
        public CustomParentChildPawn(CustomPawn pawn) {
            this.pawn = pawn;
        }
    }
}
