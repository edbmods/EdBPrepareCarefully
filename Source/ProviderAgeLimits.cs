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

        private Dictionary<ValueTuple<ThingDef, DevelopmentalStage>, int> minAgeTicksLookup = new Dictionary<ValueTuple<ThingDef, DevelopmentalStage>, int>();
        private Dictionary<ValueTuple<ThingDef, DevelopmentalStage>, int> maxAgeTicksLookup = new Dictionary<ValueTuple<ThingDef, DevelopmentalStage>, int>();

        public int MinAgeForPawn(Pawn pawn) {
            // TODO: This probably doesn't work for alien races
            if (!minAgeTicksLookup.TryGetValue(ValueTuple.Create(pawn.def, pawn.DevelopmentalStage), out int years)) {
                float min = float.MaxValue;
                foreach (var a in pawn.def.race.lifeStageAges) {
                    if (a.def.developmentalStage == pawn.DevelopmentalStage && a.minAge < min) {
                        min = a.minAge;
                    }
                }
                if (min == float.MaxValue) {
                    min = 0;
                }
                int value = Mathf.FloorToInt(min);
                minAgeTicksLookup.Add(ValueTuple.Create(pawn.def, pawn.DevelopmentalStage), value);
                //Logger.Debug("Min age for " + pawn.DevelopmentalStage + " = " + value);
                return value;
            }
            else {
                return years;
            }

            //if (!minAgeLookup.TryGetValue(pawn.def, out int age)) {
            //    SimpleCurve simpleCurve = pawn.def.race.ageGenerationCurve;
            //    if (simpleCurve == null) {
            //        Logger.Warning("No age generation curve defined for " + pawn.def.defName + ". Using default age generation curve to determine minimum age.");
            //        simpleCurve = DefaultAgeGenerationCurve;
            //        if (simpleCurve == null) {
            //            Logger.Warning("Failed to get default age generation curve. Using default minimum age of " + DEFAULT_MIN_AGE);
            //            age = DEFAULT_MIN_AGE;
            //        }
            //        else {
            //            age = Mathf.CeilToInt(pawn.def.race.lifeExpectancy * simpleCurve.First().x);
            //        }
            //    }
            //    else {
            //        CurvePoint point = simpleCurve.First();
            //        age = (int)point.x;
            //    }
            //    minAgeLookup.Add(pawn.def, age);
            //}
            //return age;
        }

        public int MaxAgeForPawn(Pawn pawn) {
            // TODO: This probably doesn't work for alien races
            if (!maxAgeTicksLookup.TryGetValue(ValueTuple.Create(pawn.def, pawn.DevelopmentalStage), out int years)) {
                float max = float.MinValue;
                int count = pawn.def.race.lifeStageAges.Count;
                float previousValue = float.MinValue;
                for(int i=count-1; i>=0; i--) {
                    var a = pawn.def.race.lifeStageAges[i];
                    if (a.def.developmentalStage == pawn.DevelopmentalStage) {
                        max = previousValue;
                        break;
                    }
                    previousValue = a.minAge;
                }
                if (max == float.MinValue) {
                    max = pawn.def.race.lifeExpectancy * 1.5f;
                }
                else {
                    max--;
                }
                int value = Mathf.FloorToInt(max);
                maxAgeTicksLookup.Add(ValueTuple.Create(pawn.def, pawn.DevelopmentalStage), value);
                //Logger.Debug("Max age for " + pawn.DevelopmentalStage + " " + pawn.def.defName + " = " + value);
                return value;
            }
            else {
                return years;
            }
            //if (!maxAgeLookup.TryGetValue(pawn.def, out int age)) {
            //    SimpleCurve simpleCurve = pawn.def.race.ageGenerationCurve;
            //    if (simpleCurve == null) {
            //        Logger.Warning("No age generation curve defined for " + pawn.def.defName + ". Using default age generation curve to determine maximum age.");
            //        simpleCurve = DefaultAgeGenerationCurve;
            //        if (simpleCurve == null) {
            //            Logger.Warning("Failed to get default age generation curve. Using default maximum age of " + DEFAULT_MAX_AGE);
            //            age = DEFAULT_MAX_AGE;
            //        }
            //        else {
            //            age = Mathf.CeilToInt(pawn.def.race.lifeExpectancy * simpleCurve.Last().x);
            //        }
            //    }
            //    else {
            //        CurvePoint point = simpleCurve.Last();
            //        age = (int)(point.x * 1.2f);
            //    }
            //    maxAgeLookup.Add(pawn.def, age);
            //}
            //return age;
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
