using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {

    public class ProviderAgeLimits {
        private Dictionary<ThingDef, int> minAgeLookup = new Dictionary<ThingDef, int>();
        private Dictionary<ThingDef, int> maxAgeLookup = new Dictionary<ThingDef, int>();

        public int MinAgeForPawn(Pawn pawn) {
            if (!minAgeLookup.TryGetValue(pawn.def, out int age)) {
                CurvePoint point = pawn.def.race.ageGenerationCurve.First();
                age = (int)point.x;
                minAgeLookup.Add(pawn.def, age);
            }
            return age;
        }

        public int MaxAgeForPawn(Pawn pawn) {
            if (!maxAgeLookup.TryGetValue(pawn.def, out int age)) {
                CurvePoint point = pawn.def.race.ageGenerationCurve.Last();
                age = (int)(point.x * 1.2f);
                maxAgeLookup.Add(pawn.def, age);
            }
            return age;
        }
    }
}
