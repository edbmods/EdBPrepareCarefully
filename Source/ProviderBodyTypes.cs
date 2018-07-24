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
        protected Dictionary<ThingDef, OptionsBodyType> raceBodyTypeLookup = new Dictionary<ThingDef, OptionsBodyType>();
        protected Dictionary<string, string> labels = new Dictionary<string, string>();
        public ProviderBodyTypes() {
            labels.Add("Female", "EdB.PC.Pawn.BodyType.Average".Translate());
            labels.Add("Male", "EdB.PC.Pawn.BodyType.Average".Translate());
            labels.Add("Hulk", "EdB.PC.Pawn.BodyType.Hulking".Translate());
            labels.Add("Thin", "EdB.PC.Pawn.BodyType.Thin".Translate());
            labels.Add("Fat", "EdB.PC.Pawn.BodyType.Heavyset".Translate());
        }
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public List<BodyTypeDef> GetBodyTypesForPawn(CustomPawn pawn) {
            return GetBodyTypesForPawn(pawn.Pawn);
        }
        public List<BodyTypeDef> GetBodyTypesForPawn(Pawn pawn) {
            return GetBodyTypesForPawn(pawn.def, pawn.gender);
        }
        public List<BodyTypeDef> GetBodyTypesForPawn(ThingDef race, Gender gender) {
            OptionsBodyType bodyTypes;
            if (!raceBodyTypeLookup.TryGetValue(race, out bodyTypes)) {
                bodyTypes = InitializeBodyTypes(race);
                raceBodyTypeLookup.Add(race, bodyTypes);
            }
            return bodyTypes.GetBodyTypes(gender);
        }
        public string GetBodyTypeLabel(BodyTypeDef bodyType) {
            if (bodyType.label.NullOrEmpty()) {
                string label = null;
                if (labels.TryGetValue(bodyType.defName, out label)) {
                    return label;
                }
                else {
                    return "EdB.PC.Pawn.BodyType.Unnamed".Translate();
                }
            }
            else {
                return bodyType.LabelCap;
            }
        }
        protected OptionsBodyType InitializeBodyTypes(ThingDef def) {
            if (!ProviderAlienRaces.IsAlienRace(def)) {
                return InitializeHumanlikeBodyTypes();
            }
            else {
                OptionsBodyType result = InitializeAlienRaceBodyTypes(def);
                if (result == null) {
                    Log.Warning("Prepare Carefully could not initialize body types for alien race, " + def.defName + ". Defaulting to humanlike body types.");
                    return InitializeHumanlikeBodyTypes();
                }
                if (result.MaleBodyTypes.Count == 0 || result.FemaleBodyTypes.Count == 0) {
                    return InitializeHumanlikeBodyTypes();
                }
                else {
                    return result;
                }
            }
        }
        protected OptionsBodyType InitializeHumanlikeBodyTypes() {
            OptionsBodyType result = new OptionsBodyType();
            foreach (BodyTypeDef d in DefDatabase<BodyTypeDef>.AllDefs) {
                if (d != BodyTypeDefOf.Female) {
                    result.MaleBodyTypes.Add(d);
                }
                if (d != BodyTypeDefOf.Male) {
                    result.FemaleBodyTypes.Add(d);
                }
                result.NoGenderBodyTypes.Add(d);
            }
            return result;
        }
        protected OptionsBodyType InitializeAlienRaceBodyTypes(ThingDef def) {
            OptionsBodyType result = new OptionsBodyType();
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(def);
            if (alienRace == null) {
                return null;
            }
            if (alienRace.BodyTypes.Count > 0) {
                bool containsMale = alienRace.BodyTypes.Contains(BodyTypeDefOf.Male);
                bool containsFemale = alienRace.BodyTypes.Contains(BodyTypeDefOf.Female);
                bool containsBothMaleAndFemale = containsMale && containsFemale;
                foreach (BodyTypeDef type in alienRace.BodyTypes) {
                    if (type != BodyTypeDefOf.Male || !containsBothMaleAndFemale) {
                        result.FemaleBodyTypes.Add(type);
                    }
                    if (type != BodyTypeDefOf.Female || !containsBothMaleAndFemale) {
                        result.MaleBodyTypes.Add(type);
                    }
                    result.NoGenderBodyTypes.Add(type);
                }
            }

            if (result.MaleBodyTypes.Count == 0 && result.FemaleBodyTypes.Count > 0) {
                result.MaleBodyTypes = result.FemaleBodyTypes;
            }
            else if (result.FemaleBodyTypes.Count == 0 && result.MaleBodyTypes.Count > 0) {
                result.FemaleBodyTypes = result.MaleBodyTypes;
            }

            return result;
        }
    }
}