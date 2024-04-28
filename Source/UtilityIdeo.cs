using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityIdeo {
        public static bool IdeoEnabledForPawn(CustomizedPawn pawn) {
            return ModsConfig.IdeologyActive
                && !Find.IdeoManager.classicMode
                && !(pawn?.Pawn?.DevelopmentalStage.Baby() ?? false)
                && (pawn?.Pawn?.ShouldHaveIdeo ?? false);
        }
    }
}
