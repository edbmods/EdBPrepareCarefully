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
            FieldInfo alienRaceField = def.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
            if (alienRaceField == null) {
                return InitializeHumanlikeBodyTypes();
            }
            else {
                object alienRaceObject = alienRaceField.GetValue(def);
                if (alienRaceObject == null) {
                    Log.Warning("Prepare Carefully could not initialize body types for alien race, " + def.defName + ", because it could not find alien race properies.  Defaulting to humanlike body types.");
                    return InitializeHumanlikeBodyTypes();
                }
                var result = InitializeAlienRaceBodyTypes(def, alienRaceObject);
                if (result == null) {
                    Log.Warning("Prepare Carefully could not initialize body types for alien race, " + def.defName + ". Defaulting to humanlike body types.");
                    return InitializeHumanlikeBodyTypes();
                }
                else if (result.MaleBodyTypes.Count == 0 || result.FemaleBodyTypes.Count == 0) {
                    return InitializeHumanlikeBodyTypes();
                }
                else {
                    return result;
                }
            }
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
        protected RaceBodyTypes InitializeAlienRaceBodyTypes(ThingDef def, object alienRaceObject) {
            Log.Message("InitializeAlienRaceBodyTypes: " + def.defName);
            RaceBodyTypes result = new RaceBodyTypes();
            if (alienRaceObject == null) {
                return null;
            }

            FieldInfo generalSettingsField = alienRaceObject.GetType().GetField("generalSettings", BindingFlags.Public | BindingFlags.Instance);
            if (generalSettingsField == null) {
                Log.Warning("Prepare Carefully could find alien general settings when trying to initialize body types for " + def.defName + ".");
                return null;
            }
            object generalSettingsObject = generalSettingsField.GetValue(alienRaceObject);
            if (generalSettingsObject == null) {
                Log.Warning("Prepare Carefully could find alien general settings when trying to initialize body types for " + def.defName + ".");
                return null;
            }
            FieldInfo alienPartGeneratorField = generalSettingsObject.GetType().GetField("alienPartGenerator", BindingFlags.Public | BindingFlags.Instance);
            if (alienPartGeneratorField == null) {
                Log.Warning("Prepare Carefully could find alien part generator information when trying to initialize body types for " + def.defName + ".");
                return null;
            }
            object alienPartGeneratorObject = alienPartGeneratorField.GetValue(generalSettingsObject);
            if (alienPartGeneratorObject == null) {
                Log.Warning("Prepare Carefully could find alien part generator information when trying to initialize body types for " + def.defName + ".");
                return null;
            }
            FieldInfo alienBodyTypesField = alienPartGeneratorObject.GetType().GetField("alienbodytypes", BindingFlags.Public | BindingFlags.Instance);
            if (alienBodyTypesField == null) {
                Log.Warning("Prepare Carefully could find alien body types information when trying to initialize body types for " + def.defName + ".");
                return null;
            }
            object alienBodyTypesObject = alienBodyTypesField.GetValue(alienPartGeneratorObject);
            if (alienBodyTypesObject == null) {
                Log.Warning("Prepare Carefully could find alien body types information when trying to initialize body types for " + def.defName + ".");
                return null;
            }

            System.Collections.ICollection alienBodyTypesList = alienBodyTypesObject as System.Collections.ICollection;
            if (alienBodyTypesList == null) {
                Log.Warning("Prepare Carefully could find alien body types list when trying to initialize body types for " + def.defName + ".");
                return null;
            }

            if (alienBodyTypesList.Count > 0) {
                foreach (object o in alienBodyTypesList) {
                    if (o.GetType() == typeof(BodyType)) {
                        BodyType type = (BodyType)o;
                        if (type != BodyType.Male) {
                            result.FemaleBodyTypes.Add(type);
                        }
                        if (type != BodyType.Female) {
                            result.MaleBodyTypes.Add(type);
                        }
                        result.NoGenderBodyTypes.Add(type);
                    }
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