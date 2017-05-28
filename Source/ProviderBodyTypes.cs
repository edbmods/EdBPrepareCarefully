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
    public class ProviderBodyTypes {
        protected Dictionary<ThingDef, RaceBodyTypes> raceBodyTypeLookup = new Dictionary<ThingDef, RaceBodyTypes>();
        protected Dictionary<BodyType, string> bodyTypeLabels = new Dictionary<BodyType, string>();
        public ProviderBodyTypes() {
            bodyTypeLabels.Add(BodyType.Fat, "EdB.PC.Pawn.BodyType.Fat".Translate());
            bodyTypeLabels.Add(BodyType.Hulk, "EdB.PC.Pawn.BodyType.Hulk".Translate());
            bodyTypeLabels.Add(BodyType.Thin, "EdB.PC.Pawn.BodyType.Thin".Translate());
            bodyTypeLabels.Add(BodyType.Male, "EdB.PC.Pawn.BodyType.Average".Translate());
            bodyTypeLabels.Add(BodyType.Female, "EdB.PC.Pawn.BodyType.Average".Translate());
        }
        public List<BodyType> GetBodyTypesForPawn(CustomPawn pawn) {
            return GetBodyTypesForPawn(pawn.Pawn);
        }
        public List<BodyType> GetBodyTypesForPawn(Pawn pawn) {
            RaceBodyTypes bodyTypes;
            if (!raceBodyTypeLookup.TryGetValue(pawn.def, out bodyTypes)) {
                bodyTypes = InitializeBodyTypes(pawn.def);
                raceBodyTypeLookup.Add(pawn.def, bodyTypes);
            }
            return bodyTypes.GetBodyTypes(pawn.gender);
        }
        public string GetBodyTypeLabel(BodyType bodyType) {
            return bodyTypeLabels[bodyType];
        }
        protected RaceBodyTypes InitializeBodyTypes(ThingDef def) {
            return InitializeHumanlikeBodyTypes();
        }
        protected RaceBodyTypes InitializeHumanlikeBodyTypes() {
            RaceBodyTypes result = new RaceBodyTypes();
            result.MaleBodyTypes.Add(BodyType.Male);
            result.MaleBodyTypes.Add(BodyType.Thin);
            result.MaleBodyTypes.Add(BodyType.Fat);
            result.MaleBodyTypes.Add(BodyType.Hulk);
            result.FemaleBodyTypes.Add(BodyType.Female);
            result.FemaleBodyTypes.Add(BodyType.Thin);
            result.FemaleBodyTypes.Add(BodyType.Fat);
            result.FemaleBodyTypes.Add(BodyType.Hulk);
            result.NoGenderBodyTypes.Add(BodyType.Male);
            result.NoGenderBodyTypes.Add(BodyType.Thin);
            result.NoGenderBodyTypes.Add(BodyType.Fat);
            result.NoGenderBodyTypes.Add(BodyType.Hulk);
            return result;
        }
    }
}
