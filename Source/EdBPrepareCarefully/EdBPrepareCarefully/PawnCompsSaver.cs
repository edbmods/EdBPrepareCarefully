using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnCompsSaver : IExposable {
        private List<ThingComp> comps = new List<ThingComp>();
        public ThingWithComps SourcePawn { get; set; }
        public PawnCompRules Rules { get; set; }
        public List<string> savedComps = new List<string>();

        // The constructor needs to take the target pawn as an argument--the pawn to which the comps will be copied.
        public PawnCompsSaver(Pawn source, PawnCompRules rules) {
            this.comps = source.AllComps ?? new List<ThingComp>();
            this.Rules = rules ?? new PawnCompExclusionRules();
        }

        public PawnCompsSaver(IEnumerable<ThingComp> comps, PawnCompRules rules) {
            this.comps = new List<ThingComp>(comps);
            this.Rules = rules;
        }

        public void ExposeData() {
            if (Scribe.mode == LoadSaveMode.Saving && this.comps != null) {
                for (int i = 0; i < comps.Count; i++) {
                    ThingComp comp = comps[i];
                    if (Rules == null || Rules.IsCompIncluded(comp)) {
                        comp.PostExposeData();
                        savedComps.Add(comp.GetType().FullName);
                    }
                    else {
                        //Logger.Debug("Excluded comp: " + comp.GetType().FullName);
                    }
                }
            }
        }
    }
}
