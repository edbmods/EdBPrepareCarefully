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
    // Removes the customized scenario (with PrepareCarefully-specific scenario parts) and replaces
    // it with a vanilla-friendly version that was prepared earlier.  This is a workaround to avoid
    // creating a dependency between a saved game and the mod.  See Controller.PrepareGame() for 
    // more details.
    // TODO: Re-evaluate to see if it would be better to use method routing instead of a map generator step.
    public class GenStep_RemovePrepareCarefullyScenario : GenStep {
        public override void Generate(Map map) {
            if (PrepareCarefully.OriginalScenario != null) {
                Current.Game.Scenario = PrepareCarefully.OriginalScenario;
            }
        }
    }
}
