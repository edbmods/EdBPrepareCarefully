using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipList : List<CustomRelationship> {
        public bool Contains(PawnRelationDef def, CustomPawn source, CustomPawn target) {
            return Find(def, source, target) != null;
        }

        public CustomRelationship Find(PawnRelationDef def, CustomPawn source, CustomPawn target) {
            foreach (var r in this) {
                if (r.def == def && r.source == source && r.target == target) {
                    return r;
                }
                else if (r.inverseDef == def && r.source == target && r.target == source) {
                    return r;
                }
            }
            return null;
        }
    }
}

