using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityQuality {
        public static bool ThingDefSupportsQuality(ThingDef thingDef) {
            if (thingDef == null) {
                return false;
            }
            return thingDef.HasComp(typeof(CompQuality));
        }
    }
}
