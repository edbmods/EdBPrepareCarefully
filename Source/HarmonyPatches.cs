using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {

    namespace HarmonyPatches {
        [StaticConstructorOnStartup]
        internal static class Main {
            static Main() {
                var harmony = new Harmony("EdB.PrepareCarefully");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                HashSet<ValueTuple<Type, String>> patchedMethods = new HashSet<ValueTuple<Type, string>>();
                foreach (var m in harmony.GetPatchedMethods()) {
                    patchedMethods.Add((m.DeclaringType, m.Name));
                }
                if (patchedMethods.Count != 2
                        || !patchedMethods.Contains((typeof(Page_ConfigureStartingPawns), "DoWindowContents"))
                        || !patchedMethods.Contains((typeof(Verse.Game), "InitNewGame"))) {
                    String methodsMessage = String.Join(", ", harmony.GetPatchedMethods().Select(i => i.DeclaringType + "." + i.Name));
                    Logger.Warning("Did not patch all of the expected methods.  The following patched methods were found: "
                        + (!methodsMessage.NullOrEmpty() ? methodsMessage : "none"));
                }

            }
        }

        [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
        [HarmonyPatch("DoWindowContents")]
        [HarmonyPatch(new[] { typeof(Rect) })]
        class PrepareCarefullyButtonPatch {
            static void Postfix(Page_ConfigureStartingPawns __instance, ref Rect rect) {
                Vector2 BottomButSize = new Vector2(150f, 38f);
                float halfButtonWidth = 75f;
                float num = rect.height + 55f;
                Rect rect4 = new Rect(rect.width / 2 - halfButtonWidth, num, BottomButSize.x, BottomButSize.y);
                if (ModsConfig.BiotechActive) {
                    float w = (rect.width * 0.5f) - 16f - BottomButSize.HalfX();
                    rect4 = new Rect(16f + (w * 0.5f), num, BottomButSize.x, BottomButSize.y);
                }
                if (Widgets.ButtonText(rect4, "EdB.PC.Page.Button.PrepareCarefully".Translate(), true, false, true)) {
                    // Version check
                    if (VersionControl.CurrentVersion < Mod.MinimumGameVersion) {
                        Find.WindowStack.Add(new DialogInitializationError(null));
                        SoundDefOf.ClickReject.PlayOneShot(null);
                        Logger.Warning("Prepare Carefully failed to initialize because it requires at least version " + Mod.MinimumGameVersion
                            + " of RimWorld.  You are running " + VersionControl.CurrentVersionString);
                        return;
                    }

                    try {
                        Mod.Instance.Start(__instance);
                    }
                    catch (Exception e) {
                        Find.WindowStack.Add(new DialogInitializationError(e));
                        SoundDefOf.ClickReject.PlayOneShot(null);
                        throw new InitializationException("Prepare Carefully failed to initialize", e);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Verse.Game))]
        [HarmonyPatch("InitNewGame")]
        [HarmonyPatch(new Type[0])]
        class ReplaceScenarioPatch {
            [HarmonyPostfix]
            static void Postfix() {
                // After we've initialized the new game, swap in the original scenario parts so that the game save
                // doesn't include any Prepare Carefully-specific scene parts.
                Mod.Instance.RestoreScenarioParts();
            }
        }
    }
}
