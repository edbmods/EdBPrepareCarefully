using Harmony;
using RimWorld;

namespace EdB.PrepareCarefully.Patches.ConfigureStartingPawns_Patches {

    [HarmonyPatch]
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
    [HarmonyPatch("PreOpen")]
    public static class PreOpen_Patch {

        /// <summary>
        /// This is a prefix for Page_ConfigureStartingPawns PreOpen method
        /// It checks that we have the correct page (because were really patching all base
        /// classes of type Page) and then call our own method.
        /// </summary>
        public static void Prefix(this Page __instance) {
            if (__instance.GetType() == typeof(Page_ConfigureStartingPawns)) {
                Page_ConfigureStartingPawns_Controller.PreOpen();
            }
        }
    }
}