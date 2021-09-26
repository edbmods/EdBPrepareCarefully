using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
                if (patchedMethods.Count != 3
                    || !patchedMethods.Contains((typeof(Page_ConfigureStartingPawns), "PreOpen"))
                    || !patchedMethods.Contains((typeof(Page_ConfigureStartingPawns), "DoWindowContents"))
                    || !patchedMethods.Contains((typeof(Verse.Game), "InitNewGame"))
                ) {
                    String methodsMessage = String.Join(", ", harmony.GetPatchedMethods().Select(i => i.DeclaringType + "." + i.Name));
                    Logger.Warning("Did not patch all of the expected methods.  The following patched methods were found: "
                        + (!methodsMessage.NullOrEmpty() ? methodsMessage : "none"));
                }
            }
        }

        [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
        [HarmonyPatch("PreOpen")]
        [HarmonyPatch(new Type[0])]
        class ClearOriginalScenarioPatch {
            [HarmonyPostfix]
            static void Postfix() {
                PrepareCarefully.ClearVanillaFriendlyScenario();
            }
        }

        [HarmonyPatch(typeof(Verse.Game))]
        [HarmonyPatch("InitNewGame")]
        [HarmonyPatch(new Type[0])]
        class ReplaceScenarioPatch {
            [HarmonyPostfix]
            static void Postfix() {
                // After we've initialized the new game, swap in the vanilla-friendly version of the scenario so that the game save
                // doesn't include any Prepare Carefully-specific scene parts.
                if (PrepareCarefully.VanillaFriendlyScenario != null) {
                    Current.Game.Scenario = PrepareCarefully.VanillaFriendlyScenario;
                    PrepareCarefully.ClearVanillaFriendlyScenario();
                }
            }
        }

        [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
        [HarmonyPatch("DoWindowContents")]
        [HarmonyPatch(new[] { typeof(Rect) })]
        class PrepareCarefullyButtonPatch {
            static void Postfix(Page_ConfigureStartingPawns __instance, ref Rect rect) {
                Vector2 BottomButSize = new Vector2(150f, 38f);
                float num = rect.height + 45f;
                Rect rect4 = new Rect(rect.x + rect.width / 2f - BottomButSize.x / 2f, num, BottomButSize.x, BottomButSize.y);
                if (Widgets.ButtonText(rect4, "EdB.PC.Page.Button.PrepareCarefully".Translate(), true, false, true)) {
                    // Version check
                    if (VersionControl.CurrentVersion < PrepareCarefully.MinimumGameVersion) {
                        Find.WindowStack.Add(new DialogInitializationError(null));
                        SoundDefOf.ClickReject.PlayOneShot(null);
                        Logger.Warning("Prepare Carefully failed to initialize because it requires at least version " + PrepareCarefully.MinimumGameVersion
                            + " of RimWorld.  You are running " + VersionControl.CurrentVersionString);
                        return;
                    }

                    try {
                        ReflectionCache.Instance.Initialize();

                        PrepareCarefully prepareCarefully = PrepareCarefully.Instance;
                        if (prepareCarefully == null) {
                            Logger.Error("Could not open Prepare Carefully screen, because we failed to get the Prepare Carefully singleton.");
                            return;
                        }
                        prepareCarefully.Initialize();
                        prepareCarefully.OriginalPage = __instance;
                        Page_PrepareCarefully page = new Page_PrepareCarefully();

                        State state = prepareCarefully.State;
                        if (state == null) {
                            Logger.Error("Could not open Prepare Carefully screen, because the Prepare Carefully state was null.");
                            return;
                        }
                        state.Page = page;
                        Find.WindowStack.Add(page);
                    }
                    catch (Exception e) {
                        Find.WindowStack.Add(new DialogInitializationError(e));
                        SoundDefOf.ClickReject.PlayOneShot(null);
                        throw new InitializationException("Prepare Carefully failed to initialize", e);
                    }
                }
            }
        }
    }
}
