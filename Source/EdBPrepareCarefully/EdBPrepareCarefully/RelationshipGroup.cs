using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class RelationshipGroup {
        public List<RelatedPawn> Parents = new List<RelatedPawn>();
        public List<RelatedPawn> Children = new List<RelatedPawn>();
    }
}
