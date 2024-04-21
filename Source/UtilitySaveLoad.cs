using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilitySaveLoad {
        public static void ClearSaveablesAndCrossRefs() {
            // I don't fully understand how these cross-references and saveables are resolved, but
            // if we don't clear them out, we get null pointer exceptions.
            Reflection.ReflectorPostLoadIniter.ClearSaveablesToPostLoad(Scribe.loader.initer);
            if (Scribe.loader.crossRefs.crossReferencingExposables != null) {
                Scribe.loader.crossRefs.crossReferencingExposables.Clear();
            }
            Scribe.loader.FinalizeLoading();
        }
    }
}
