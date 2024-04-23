using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipList : List<CustomizedRelationship> {
        public bool Contains(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            return Find(def, source, target) != null;
        }

        public CustomizedRelationship Find(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            foreach (var r in this) {
                if (r.Def == def && r.Source == source && r.Target == target) {
                    return r;
                }
                else if (r.InverseDef == def && r.Source == target && r.Target == source) {
                    return r;
                }
            }
            return null;
        }
    }
}

