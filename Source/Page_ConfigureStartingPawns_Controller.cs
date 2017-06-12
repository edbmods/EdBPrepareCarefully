using Harmony;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    // EdB: Copy of the RimWorld.Page_ConfigureStartingPawns with changes to add the middle "Prepare Carefully" button.
    // TODO: Alpha 18.  Replace with a new copy and re-do changes every time a new alpha comes out.
    public static class Page_ConfigureStartingPawns_Controller {

        public static void PreOpen() {
            PrepareCarefully.ClearOriginalScenario();
        }

        public static Action PrepareCarefullyAction(Page instance) {
            return new Action(() => {
                PrepareCarefully.Instance.Initialize();
                Page_PrepareCarefully page = new Page_PrepareCarefully();
                PrepareCarefully.Instance.State.Page = page;
                Find.WindowStack.Add(page);
            });
        }
    }
}
