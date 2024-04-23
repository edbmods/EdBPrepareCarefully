using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class CustomizedPawn {
        public string Id { get; set; }
        public CustomizedPawnType Type { get; set; }
        public Pawn Pawn { get; set; }
        public CustomizationsPawn Customizations { get; set; }
        public TemporaryPawn TemporaryPawn { get; set; }
    }
}
