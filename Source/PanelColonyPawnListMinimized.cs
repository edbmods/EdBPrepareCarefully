using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelColonyPawnListMinimized : PanelPawnListMinimized {
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.ColonyPawnList.Title".Translate();
            }
        }

        protected override IEnumerable<CustomizedPawn> GetPawns() {
            return State.Customizations.ColonyPawns;
        }

        protected override bool IsTopPanel() {
            return true;
        }
    }
}
