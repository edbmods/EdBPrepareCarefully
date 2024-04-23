using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelWorldPawnList : PanelPawnList {
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.WorldPawnList.Title".Translate();
            }
        }

        protected override IEnumerable<CustomizedPawn> GetPawns() {
            return State.Customizations.WorldPawns;
        }

        protected override bool IsTopPanel() {
            return false;
        }
        
        protected override bool StartingPawns {
            get {
                return false;
            }
        }

        protected override bool CanDeleteLastPawn {
            get {
                return true;
            }
        }
    }
}
