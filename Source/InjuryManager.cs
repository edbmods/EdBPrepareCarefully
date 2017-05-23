using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class InjuryManager {
        private ProviderInjuries providerInjuries = new ProviderInjuries();

        public InjuryManager() {
        }

        public IEnumerable<InjuryOption> Options {
            get {
                return providerInjuries.InjuryOptions;
            }
        }

        public InjuryOption FindOptionByHediffDef(HediffDef def) {
            foreach (InjuryOption o in Options) {
                if (o.HediffDef == def) {
                    return o;
                }
            }
            return null;
        }

        public bool DoesStageKillPawn(HediffDef def, HediffStage stage) {
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
            return false;
        }
    }
}

