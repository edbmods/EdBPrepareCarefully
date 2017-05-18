using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class RelatedPawn {
        public CustomPawn Pawn = null;
        private Gender gender = Gender.None;
        public Gender Gender {
            get {
                return Pawn != null ? Pawn.Gender : gender;
            }
            set {
                gender = value;
            }
        }
    }
}
