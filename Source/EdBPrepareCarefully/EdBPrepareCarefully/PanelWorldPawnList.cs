using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelWorldPawnList : PanelPawnList {
        private List<CustomPawn> pawns = new List<CustomPawn>();
        public override string PanelHeader {
            get {
                return "EdB.PC.Panel.WorldPawnList.Title".Translate();
            }
        }

        protected override bool IsMaximized(State state) {
            return state.PawnListMode != PawnListMode.ColonyPawnsMaximized;
        }

        protected override bool IsMinimized(State state) {
            return state.PawnListMode == PawnListMode.ColonyPawnsMaximized;
        }

        protected override List<CustomPawn> GetPawnListFromState(State state) {
            // Re-use the same list instead of instantiating a new one every frame.
            pawns.Clear();
            pawns.AddRange(state.WorldPawns);
            return pawns;
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
