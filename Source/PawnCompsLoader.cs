using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnCompsLoader : IExposable {

        public List<ThingComp> Comps = new List<ThingComp>();
        public ThingWithComps TargetPawn { get; set; }
        public PawnCompRules Rules { get; set; }
        public PawnCompsLoader(Pawn target, PawnCompRules rules) {
            this.TargetPawn = target;
            this.Rules = rules ?? new PawnCompInclusionRules();
        }

        public void ExposeData() {
            if (Scribe.mode == LoadSaveMode.LoadingVars) {
                if (TargetPawn.AllComps != null) {
                    foreach (var c in TargetPawn.AllComps) {
                        if (Rules.IsCompIncluded(c)) {
                            //Logger.Debug("Deserializing into " + c.GetType().FullName);
                            c.PostExposeData();
                        }
                    }
                }
            }
        }
    }
}
