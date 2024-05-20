using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public static class ExtensionsPawnRelationDef {

        public static string GetGenderSpecificLabelCap(this PawnRelationDef relationDef, CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn?.Pawn;
            if (pawn != null) {
                return relationDef.GetGenderSpecificLabelCap(pawn);
            }
            else if (customizedPawn.TemporaryPawn != null && customizedPawn.TemporaryPawn.Gender == Gender.Female && !relationDef.labelFemale.NullOrEmpty()) {
                return relationDef.labelFemale.CapitalizeFirst();
            }
            else {
                return relationDef.label.CapitalizeFirst();
            }
        }
    }
}
