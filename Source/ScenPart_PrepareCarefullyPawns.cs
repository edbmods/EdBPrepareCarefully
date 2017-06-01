using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ScenPart_PrepareCarefullyStartingPawns : ScenPart_ConfigPage {
        public int pawnCount = 3;

        public ScenPart_PrepareCarefullyStartingPawns(int count) {
            this.pawnCount = count;
            // Set the def to match the standard pawn configuration part that we'll be replacing with this one.
            // Doing so makes sure that this part gets sorted as expected when building the scenario description.
            this.def = ScenPartDefOf.ConfigPage_ConfigureStartingPawns;
        }

        public override string Summary(Scenario scen) {
            if (pawnCount == 1) {
                return "EdB.PC.Page.StartingPawnScenarioPartMessage.Singular".Translate();
            }
            else {
                return "EdB.PC.Page.StartingPawnScenarioPartMessage.Plural".Translate(new object[] {
                    this.pawnCount });
            }
        }
    }
}
