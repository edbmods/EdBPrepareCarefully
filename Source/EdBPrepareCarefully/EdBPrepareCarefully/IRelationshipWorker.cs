using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public interface IRelationshipWorker {
        void CreateRelationship(Pawn source, Pawn target);
        PawnRelationDef Def {
            get;
            set;
        }
    }
}
