using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SelectedPet {
        public AnimalRecord Record;
        public string Id;
        public string Name;
        public Pawn Pawn;
        public CustomPawn BondedPawn;
        public AnimalRecordKey Key {
            get {
                return Record.Key;
            }
        }
    }
}
