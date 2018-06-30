using Harmony;
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

    [StaticConstructorOnStartup]
    internal class HarmonyPatches {
        static HarmonyPatches() {
            HarmonyInstance harmony = HarmonyInstance.Create("EdB.PrepareCarefully");
            harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("PreOpen"),
                new HarmonyMethod(null),
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("PreOpenPostfix")));
            harmony.Patch(typeof(Page_ConfigureStartingPawns).GetMethod("DoWindowContents"),
                new HarmonyMethod(null),
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("DoWindowContentsPostfix")));
            harmony.Patch(typeof(Game).GetMethod("InitNewGame"),
                new HarmonyMethod(null),
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("InitNewGamePostfix")));
        }

        // Clear the original scenario when opening the Configure Starting Pawns page.  This makes
        // sure that the workaround static variable gets cleared if you quit to the main menu from
        // gameplay and then start a new game.
        public static void PreOpenPostfix() {
            PrepareCarefully.ClearOriginalScenario();
        }

        // Removes the customized scenario (with PrepareCarefully-specific scenario parts) and replaces
        // it with a vanilla-friendly version that was prepared earlier.  This is a workaround to avoid
        // creating a dependency between a saved game and the mod.  See Controller.PrepareGame() for 
        // more details.
        public static void InitNewGamePostfix() {
            if (PrepareCarefully.OriginalScenario != null) {
                Current.Game.Scenario = PrepareCarefully.OriginalScenario;
                PrepareCarefully.ClearOriginalScenario();
            }
        }

        // Draw the "Prepare Carefully" button at the bottom of the Configure Starting Pawns page.
        public static void DoWindowContentsPostfix(Rect rect, Page_ConfigureStartingPawns __instance) {
            Vector2 BottomButSize = new Vector2(150f, 38f);
            float num = rect.height + 45f;
            Rect rect4 = new Rect(rect.x + rect.width / 2f - BottomButSize.x / 2f, num, BottomButSize.x, BottomButSize.y);
            if (Widgets.ButtonText(rect4, "EdB.PC.Page.Button.PrepareCarefully".Translate(), true, false, true)) {
                PrepareCarefully.Instance.Initialize();
                PrepareCarefully.Instance.OriginalPage = __instance;
                Page_PrepareCarefully page = new Page_PrepareCarefully();
                PrepareCarefully.Instance.State.Page = page;
                Find.WindowStack.Add(page);
            }
        }
    }
}
