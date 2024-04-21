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
            //Logger.Debug("BuildFromCustomizations() ForcedXenotype = " + customizations.XenotypeDef?.LabelCap.ToString());

            PawnGenerationRequestWrapper wrapper = new PawnGenerationRequestWrapper() {
                KindDef = customizations.PawnKind,
                AllowDowned = true,
                FixedGender = customizations.Gender,
                FixedBiologicalAge = biologicalAge,
                FixedChronologicalAge = chronologicalAge,
                ForceBodyType = customizations.BodyType,
                ForcedXenotype = customizations.XenotypeDef
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
