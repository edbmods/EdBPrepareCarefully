using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace EdB.PrepareCarefully {
    public class CarefullyPawnRelationDef : Def {
        public string inverse = null;

        public bool animal = false;

        public List<String> conflicts = null;

        public Type workerClass = null;

        public bool needsCompatibility = false;

        [Unsaved]
        private PawnRelationWorker worker;

        public PawnRelationWorker Worker {
            get {
                if (this.workerClass != null && this.worker == null) {
                    PawnRelationDef pawnRelationDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail(this.defName);
                    if (pawnRelationDef != null) {
                        this.worker = (PawnRelationWorker)Activator.CreateInstance(this.workerClass);
                        this.worker.def = pawnRelationDef;
                    }
                }
                return this.worker;
            }
        }
    }
}
