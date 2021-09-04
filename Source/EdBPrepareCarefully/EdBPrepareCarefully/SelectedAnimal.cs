using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SelectedAnimal {
        public AnimalRecord Record;
        public int Count;
        public AnimalRecordKey Key {
            get {
                return Record.Key;
            }
        }
    }
}
