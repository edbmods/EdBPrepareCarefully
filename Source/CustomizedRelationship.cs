using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class CustomizedRelationship {
        public CustomizedPawn Source { get; set; }
        public CustomizedPawn Target { get; set; }
        public PawnRelationDef Def { get; set; }
        public PawnRelationDef InverseDef { get; set; }

        public CustomizedRelationship() {
        }

        public CustomizedRelationship(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            this.Def = def;
            this.InverseDef = null;
            this.Source = source;
            this.Target = target;
        }

        public CustomizedRelationship(PawnRelationDef def, PawnRelationDef inverseDef, CustomizedPawn source, CustomizedPawn target) {
            this.Def = def;
            this.InverseDef = inverseDef;
            this.Source = source;
            this.Target = target;
        }

        public override string ToString() {
            return Source?.Pawn?.Name?.ToStringShort + " + "
                + Target?.Pawn?.Name?.ToStringShort + " = "
                + Def?.defName;
        }

        public override bool Equals(object obj) {
            return obj is CustomizedRelationship relationship &&
                   ReferenceEquals(Source, relationship.Source) &&
                   ReferenceEquals(Target, relationship.Target) &&
                   ReferenceEquals(Def, relationship.Def);
        }

        public override int GetHashCode() {
            var hashCode = 648056908;
            hashCode = hashCode * -1521134295 + EqualityComparer<CustomizedPawn>.Default.GetHashCode(Source);
            hashCode = hashCode * -1521134295 + EqualityComparer<CustomizedPawn>.Default.GetHashCode(Target);
            hashCode = hashCode * -1521134295 + EqualityComparer<PawnRelationDef>.Default.GetHashCode(Def);
            return hashCode;
        }
    }
}
