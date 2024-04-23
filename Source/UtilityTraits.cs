using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityTraits {
        public static bool TraitsMatch(Trait a, Trait b) {
            return a.def == b.def && a.Degree == b.Degree;
        }
        public static bool TraitsMatch(BackstoryTrait a, Trait b) {
            return a.def == b.def && a.degree == b.Degree;
        }

    }
}
