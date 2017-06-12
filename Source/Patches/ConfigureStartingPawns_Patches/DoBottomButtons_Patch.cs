using Harmony;
using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully.Source.Patches.ConfigureStartingPawns_Patches.Patches {

    [HarmonyPatch]
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
    [HarmonyPatch("DoBottomButtons")]
    public static class DoBottomButtons_Patch {

        /// <summary>
        /// This is a prefix for Page_ConfigureStartingPawns DoBottomButtons method
        /// It checks that were drawing on the correct page (because were really patching all base
        /// classes of type Page) and fill the middle button variables before they get passed to
        /// the original method.
        /// </summary>
        public static void Prefix(this Page __instance, ref string midLabel, ref Action midAct) {
            if (__instance.GetType() == typeof(Page_ConfigureStartingPawns)) {
                midLabel = "EdB.PC.Page.Button.PrepareCarefully".Translate();
                midAct = Page_ConfigureStartingPawns_Controller.PrepareCarefullyAction(__instance);
            }
        }
    }
}