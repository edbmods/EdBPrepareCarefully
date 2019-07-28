using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {

    public class ProviderAgeLimits {
        public static readonly int DEFAULT_MIN_AGE = 15;
        public static readonly int DEFAULT_MAX_AGE = 100;
        private Dictionary<ThingDef, int> minAgeLookup = new Dictionary<ThingDef, int>();
        private Dictionary<ThingDef, int> maxAgeLookup = new Dictionary<ThingDef, int>();

        public int MinAgeForPawn(Pawn pawn) {
            if (!minAgeLookup.TryGetValue(pawn.def, out int age)) {
                SimpleCurve simpleCurve = pawn.def.race.ageGenerationCurve;
                if (simpleCurve == null) {
                    Log.Warning("Prepare Carefully :: No age generation curve defined for " + pawn.def.defName + ". Using default age generation curve to determine minimum age.");
                    simpleCurve = DefaultAgeGenerationCurve;
                    if (simpleCurve == null) {
                        Log.Warning("Prepare Carefully :: Failed to get default age generation curve. Using default minimum age of " + DEFAULT_MIN_AGE);
                        age = DEFAULT_MIN_AGE;
                    }
                    else {
                        age = Mathf.CeilToInt(pawn.def.race.lifeExpectancy * simpleCurve.First().x);
                    }
                }
                else {
                    CurvePoint point = simpleCurve.First();
                    age = (int)point.x;
                }
                minAgeLookup.Add(pawn.def, age);
            }
            return age;
        }

        public int MaxAgeForPawn(Pawn pawn) {
            if (!maxAgeLookup.TryGetValue(pawn.def, out int age)) {
                SimpleCurve simpleCurve = pawn.def.race.ageGenerationCurve;
                if (simpleCurve == null) {
                    Log.Warning("Prepare Carefully :: No age generation curve defined for " + pawn.def.defName + ". Using default age generation curve to determine maximum age.");
                    simpleCurve = DefaultAgeGenerationCurve;
                    if (simpleCurve == null) {
                        Log.Warning("Prepare Carefully :: Failed to get default age generation curve. Using default maximum age of " + DEFAULT_MAX_AGE);
                        age = DEFAULT_MAX_AGE;
                    }
                    else {
                        age = Mathf.CeilToInt(pawn.def.race.lifeExpectancy * simpleCurve.Last().x);
                    }
                }
                else {
                    CurvePoint point = simpleCurve.Last();
                    age = (int)(point.x * 1.2f);
                }
                maxAgeLookup.Add(pawn.def, age);
            }
            return age;
        }

        protected SimpleCurve DefaultAgeGenerationCurve {
            get {
                FieldInfo field = ReflectionUtil.GetNonPublicStaticField(typeof(Verse.PawnGenerator), "DefaultAgeGenerationCurve");
                if (field != null) {
                    return field.GetValue(null) as SimpleCurve;
                }
                else {
                    return null;
                }
            }
        }
    }
}
