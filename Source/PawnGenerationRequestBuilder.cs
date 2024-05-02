using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnGenerationRequestBuilder {
        public PawnGenerationRequest BuildFromCustomizations(CustomizationsPawn customizations) {
            int biologicalAge = (int)(customizations.BiologicalAgeInTicks / 3600000);
            int chronologicalAge = (int)(customizations.ChronologicalAgeInTicks / 3600000);

            PawnGenerationRequestWrapper wrapper = new PawnGenerationRequestWrapper() {
                KindDef = customizations.PawnKind,
                ForcedMutant = customizations.Mutant?.MutantDef,
                AllowDowned = true,
                FixedGender = customizations.Gender,
                FixedBiologicalAge = biologicalAge,
                FixedChronologicalAge = chronologicalAge,
                ForceBodyType = customizations.BodyType,
                ForcedXenotype = customizations.XenotypeDef,
                ForcedEndogenes = customizations.Genes?.Endogenes?.Select(g => g.GeneDef).ToList(),
                ForcedXenogenes = customizations.Genes?.Xenogenes?.Select(g => g.GeneDef).ToList()
            };
            if (customizations.PawnKind is CreepJoinerFormKindDef) {
                wrapper.IsCreepJoiner = true;
            }
            return wrapper.Request;
        }
        public PawnGenerationRequest BuildFromCustomizationsWithoutAppearance(CustomizationsPawn customizations) {
            PawnGenerationRequest request = BuildFromCustomizations(customizations);
            request.ForceBodyType = null;
            return request;
        }
    }
}
