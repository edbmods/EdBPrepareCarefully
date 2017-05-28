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
    public class ProviderAlienRaces {
        protected Dictionary<ThingDef, AlienRace> lookup = new Dictionary<ThingDef, AlienRace>();
        public ProviderAlienRaces() {

        }
        public AlienRace GetAlienRace(ThingDef def) {
            AlienRace result;
            if (lookup.TryGetValue(def, out result)) {
                return result;
            }
            else {
                if (IsAlienRace(def)) {
                    result = InitializeAlienRace(def);
                    if (result != null) {
                        lookup.Add(def, result);
                    }
                    return result;
                }
                else {
                    return null;
                }
            }
        }
        public static bool IsAlienRace(ThingDef raceDef) {
            FieldInfo alienRaceField = raceDef.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
            return (alienRaceField != null);
        }
        protected AlienRace InitializeAlienRace(ThingDef raceDef) {
            FieldInfo alienRaceField = raceDef.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
            if (alienRaceField == null) {
                Log.Warning("Prepare carefully could not find alien race field for " + raceDef.defName);
                return null;
            }
            object alienRaceObject = alienRaceField.GetValue(raceDef);
            if (alienRaceObject == null) {
                Log.Warning("Prepare carefully could not find alien race field value for " + raceDef.defName);
                return null;
            }
            FieldInfo generalSettingsField = alienRaceObject.GetType().GetField("generalSettings", BindingFlags.Public | BindingFlags.Instance);
            if (generalSettingsField == null) {
                Log.Warning("Prepare carefully could not find alien general settings field for " + raceDef.defName);
                return null;
            }
            object generalSettingsObject = generalSettingsField.GetValue(alienRaceObject);
            if (generalSettingsObject == null) {
                Log.Warning("Prepare carefully could not find alien general settings field value for " + raceDef.defName);
                return null;
            }
            FieldInfo alienPartGeneratorField = generalSettingsObject.GetType().GetField("alienPartGenerator", BindingFlags.Public | BindingFlags.Instance);
            if (alienPartGeneratorField == null) {
                Log.Warning("Prepare carefully could not find alien part generator field for " + raceDef.defName);
                return null;
            }
            object alienPartGeneratorObject = alienPartGeneratorField.GetValue(generalSettingsObject);
            if (alienPartGeneratorObject == null) {
                Log.Warning("Prepare carefully could not find alien part generator field value for " + raceDef.defName);
                return null;
            }

            AlienRace result = new AlienRace();
            FieldInfo alienBodyTypesField = alienPartGeneratorObject.GetType().GetField("alienbodytypes", BindingFlags.Public | BindingFlags.Instance);
            if (alienBodyTypesField == null) {
                Log.Warning("Prepare carefully could not find alien body types field for " + raceDef.defName);
                return null;
            }
            object alienBodyTypesObject = alienBodyTypesField.GetValue(alienPartGeneratorObject);
            if (alienBodyTypesObject == null) {
                Log.Warning("Prepare carefully could not find alien body types field value for " + raceDef.defName);
                return null;
            }
            System.Collections.ICollection alienBodyTypesList = alienBodyTypesObject as System.Collections.ICollection;
            if (alienBodyTypesList == null) {
                Log.Warning("Prepare carefully could not convert alien body types field value into a collection for " + raceDef.defName + ".");
                return null;
            }
            List<BodyType> bodyTypes = new List<BodyType>();
            if (alienBodyTypesList.Count > 0) {
                foreach (object o in alienBodyTypesList) {
                    if (o.GetType() == typeof(BodyType)) {
                        bodyTypes.Add((BodyType)o);
                    }
                }
            }
            result.BodyTypes = bodyTypes;

            return result;
        }
    }
}
