using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public static class ExtensionsHediffDef {
        public static bool DoesStageDefinitelyKillPawn(this HediffDef def, HediffStage stage) {
            if (def.lethalSeverity > -1.0f && stage.minSeverity >= def.lethalSeverity) {
                return true;
            }
            if (stage.capMods != null) {
                foreach (var c in stage.capMods) {
                    if (c.capacity == PawnCapacityDefOf.Consciousness) {
                        if (c.setMax == 0f) {
                            return true;
                        }
                        else if (c.offset == -1f) {
                            return true;
                        }
                    }
                }
            }
            if (stage.partEfficiencyOffset == -1) {
                return true;
            }
            return false;
        }
    }
}
