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
        public static ThingComp FindAlienCompForPawn(Pawn pawn) {
            return pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                return (comp.GetType().Name == "AlienComp");
            });
        }
        public static string GetCrownTypeFromComp(ThingComp alienComp) {
            return ReflectionUtil.GetPublicField(alienComp, "crownType").GetValue(alienComp) as string;
        }
        public static void SetCrownTypeOnComp(ThingComp alienComp, string value) {
            ReflectionUtil.GetPublicField(alienComp, "crownType").SetValue(alienComp, value);
        }
        public static Color GetSkinColorFromComp(ThingComp alienComp) {
            return (Color)ReflectionUtil.GetPublicField(alienComp, "skinColor").GetValue(alienComp);
        }
        public static void SetSkinColorOnComp(ThingComp alienComp, Color value) {
            ReflectionUtil.GetPublicField(alienComp, "skinColor").SetValue(alienComp, value);
        }
        public static Color GetSkinColorSecondFromComp(ThingComp alienComp) {
            return (Color)ReflectionUtil.GetPublicField(alienComp, "skinColorSecond").GetValue(alienComp);
        }
        public static void SetSkinColorSecondOnComp(ThingComp alienComp, Color value) {
            ReflectionUtil.GetPublicField(alienComp, "skinColorSecond").SetValue(alienComp, value);
        }
        public static Color GetHairColorSecondFromComp(ThingComp alienComp) {
            return (Color)ReflectionUtil.GetPublicField(alienComp, "hairColorSecond").GetValue(alienComp);
        }
        public static void SetHairColorSecondOnComp(ThingComp alienComp, Color value) {
            ReflectionUtil.GetPublicField(alienComp, "hairColorSecond").SetValue(alienComp, value);
        }
        public static string GetCrownTypeFromPawn(Pawn pawn) {
            var alienComp = FindAlienCompForPawn(pawn);
            if (alienComp == null) {
                return null;
            }
            return GetCrownTypeFromComp(alienComp);
        }

        public static bool IsAlienRace(ThingDef raceDef) {
            FieldInfo alienRaceField = raceDef.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
            return (alienRaceField != null);
        }
        protected AlienRace InitializeAlienRace(ThingDef raceDef) {
            object alienRaceObject = GetFieldValue(raceDef, raceDef, "alienRace");
            if (alienRaceObject == null) {
                return null;
            }
            object generalSettingsObject = GetFieldValue(raceDef, alienRaceObject, "generalSettings");
            if (generalSettingsObject == null) {
                return null;
            }
            object alienPartGeneratorObject = GetFieldValue(raceDef, generalSettingsObject, "alienPartGenerator");
            if (alienPartGeneratorObject == null) {
                return null;
            }
            System.Collections.ICollection graphicPathsCollection = GetFieldValueAsCollection(raceDef, alienRaceObject, "graphicPaths");
            if (graphicPathsCollection == null) {
                return null;
            }

            /*
            Log.Message("GraphicsPaths for " + raceDef.defName + ":");
            if (graphicPathsCollection.Count > 0) {
                foreach (object o in graphicPathsCollection) {
                    Log.Message("  GraphicsPath");
                    Log.Message("    .body = " + GetFieldValueAsString(raceDef, o, "body"));
                    Log.Message("    .head = " + GetFieldValueAsString(raceDef, o, "head"));
                    System.Collections.ICollection lifeStagesCollections = GetFieldValueAsCollection(raceDef, o, "lifeStageDefs");
                }
            }
            */

            // We have enough to start putting together the result object, so we instantiate it now.
            AlienRace result = new AlienRace();
            result.ThingDef = raceDef;

            // Get the list of body types.
            System.Collections.ICollection alienBodyTypesCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "alienbodytypes");
            if (alienBodyTypesCollection == null) {
                return null;
            }
            List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>();
            //Log.Message("Body Types for " + raceDef.defName + ":");
            if (alienBodyTypesCollection.Count > 0) {
                foreach (object o in alienBodyTypesCollection) {
                    if (o.GetType() == typeof(BodyTypeDef)) {
                        BodyTypeDef def = o as BodyTypeDef;
                        //Log.Message("  " + def.defName + ", " + def.LabelCap);
                        bodyTypes.Add((BodyTypeDef)o);
                    }
                }
            }
            result.BodyTypes = bodyTypes;

            // Determine if the alien races uses gender-specific heads.
            bool? useGenderedHeads = GetFieldValueAsBool(raceDef, alienPartGeneratorObject, "useGenderedHeads");
            if (useGenderedHeads == null) {
                return null;
            }
            result.GenderSpecificHeads = useGenderedHeads.Value;

            // Get the list of crown types.
            System.Collections.ICollection alienCrownTypesCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "aliencrowntypes");
            if (alienCrownTypesCollection == null) {
                return null;
            }
            List<string> crownTypes = new List<string>();
            //Log.Message("Crown Types for " + raceDef.defName + ":");
            if (alienCrownTypesCollection.Count > 0) {
                foreach (object o in alienCrownTypesCollection) {
                    string crownTypeString = o as string;
                    if (crownTypeString != null) {
                        crownTypes.Add(crownTypeString);
                        //Log.Message("  " + crownTypeString);
                    }
                }
            }
            result.CrownTypes = crownTypes;

            // Go through the graphics paths and find the heads path.
            // TODO: What is this?  
            string graphicsPathForHeads = null;
            string graphicsPathForBodyTypes = null;
            foreach (var graphicsPath in graphicPathsCollection) {
                System.Collections.ICollection lifeStageCollection = GetFieldValueAsCollection(raceDef, graphicsPath, "lifeStageDefs");
                if (lifeStageCollection == null || lifeStageCollection.Count == 0) {
                    string headsPath = GetFieldValueAsString(raceDef, graphicsPath, "head");
                    string bodyTypesPath = GetFieldValueAsString(raceDef, graphicsPath, "body");
                    if (headsPath != null) {
                        graphicsPathForHeads = headsPath;
                    }
                    if (bodyTypesPath != null) {
                        graphicsPathForBodyTypes = bodyTypesPath;
                    }
                }
            }
            result.GraphicsPathForHeads = graphicsPathForHeads;
            result.GraphicsPathForBodyTypes = graphicsPathForBodyTypes;

            // Figure out colors.
            object primaryColorGeneratorValue = GetFieldValue(raceDef, alienPartGeneratorObject, "alienskincolorgen", true);
            result.UseMelaninLevels = true;
            ColorGenerator primaryGenerator = primaryColorGeneratorValue as ColorGenerator;
            if (primaryGenerator != null) {
                result.UseMelaninLevels = false;
                result.PrimaryColors = primaryGenerator.GetColorList();
            }
            else {
                result.PrimaryColors = new List<Color>();
            }
            object secondaryColorGeneratorValue = GetFieldValue(raceDef, alienPartGeneratorObject, "alienskinsecondcolorgen", true);
            result.HasSecondaryColor = false;
            ColorGenerator secondaryGenerator = secondaryColorGeneratorValue as ColorGenerator;
            if (secondaryGenerator != null) {
                result.HasSecondaryColor = true;
                result.SecondaryColors = secondaryGenerator.GetColorList();
            }
            else {
                result.SecondaryColors = new List<Color>();
            }

            // Hair properties.
            object hairSettingsValue = GetFieldValue(raceDef, alienRaceObject, "hairSettings", true);
            result.HasHair = true;
            if (hairSettingsValue != null) {
                bool? hasHair = GetFieldValueAsBool(raceDef, hairSettingsValue, "hasHair");
                if (hasHair != null) {
                    result.HasHair = hasHair.Value;
                }
                var hairTagCollection = GetFieldValueAsCollection(raceDef, hairSettingsValue, "hairTags");
                if (hairTagCollection != null) {
                    var hairTags = new HashSet<string>();
                    foreach (var o in hairTagCollection) {
                        string tag = o as string;
                        if (tag != null) {
                            hairTags.Add(tag);
                        }
                    }
                    if (hairTags.Count > 0) {
                        result.HairTags = hairTags;
                    }
                }
            }
            object hairColorGeneratorValue = GetFieldValue(raceDef, alienPartGeneratorObject, "alienhaircolorgen", true);
            ColorGenerator hairColorGenerator = hairColorGeneratorValue as ColorGenerator;
            if (hairColorGenerator != null) {
                result.HairColors = hairColorGenerator.GetColorList();
            }
            else {
                result.HairColors = null;
            }

            // Apparel properties.
            object restrictionSettingsValue = GetFieldValue(raceDef, alienRaceObject, "raceRestriction", true);
            result.RestrictedApparelOnly = false;
            if (restrictionSettingsValue != null) {
                bool? restrictedApparelOnly = GetFieldValueAsBool(raceDef, restrictionSettingsValue, "onlyUseRaceRestrictedApparel");
                if (restrictedApparelOnly != null) {
                    result.RestrictedApparelOnly = restrictedApparelOnly.Value;
                }
                var restrictedApparelCollection = GetFieldValueAsCollection(raceDef, restrictionSettingsValue, "apparelList");
                if (restrictedApparelCollection != null) {
                    var apparel = new HashSet<string>();
                    foreach (var o in restrictedApparelCollection) {
                        string defName = o as string;
                        if (defName != null) {
                            apparel.Add(defName);
                        }
                    }
                    if (apparel.Count > 0) {
                        result.RestrictedApparel = apparel;
                    }
                }
            }

            var bodyAddonsCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "bodyAddons");
            if (bodyAddonsCollection != null ) {
                var addons = new List<AlienRaceBodyAddon>();
                int index = -1;
                foreach (var o in bodyAddonsCollection) {
                    index++;
                    AlienRaceBodyAddon addon = new AlienRaceBodyAddon();
                    string path = GetFieldValueAsString(raceDef, o, "path");
                    if (path == null) {
                        Log.Warning("Failed to get path for body add-on for alien race: " + raceDef.defName);
                        continue;
                    }
                    addon.Path = path;
                    int? variantCount = GetFieldValueAsInt(raceDef, o, "variantCount");
                    if (variantCount == null) {
                        Log.Warning("Failed to get variant count for body add-on for alien race: " + raceDef.defName);
                        continue;
                    }
                    addon.OptionCount = variantCount.Value;
                    string name = ParseAddonName(path);
                    if (name == null) {
                        Log.Warning("Failed to parse a name from its path for body add-on for alien race: " + raceDef.defName);
                        continue;
                    }
                    addon.Name = name;
                    addon.VariantIndex = index;
                    addons.Add(addon);
                }
                result.addons = addons;
            }

            return result;
        }
        protected string ParseAddonName(string path) {
            string trimmedPath = path.TrimEnd('/').TrimStart('/');
            string[] items = trimmedPath.Split('/');
            if (items.Length > 0) {
                return items[items.Length - 1].Replace("_", " ");
            }
            else {
                return null;
            }
        }
        protected object GetFieldValue(ThingDef raceDef, object source, string name, bool allowNull = false) {
            try {
                FieldInfo field = source.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) {
                    Log.Warning("Prepare carefully could not find " + name + " field for " + raceDef.defName);
                    return null;
                }
                object result = field.GetValue(source);
                if (result == null) {
                    if (!allowNull) {
                        Log.Warning("Prepare carefully could not find " + name + " field value for " + raceDef.defName);
                    }
                    return null;
                }
                else {
                    return result;
                }
            }
            catch (Exception) {
                Log.Warning("Prepare carefully could resolve value of the " + name + " field for " + raceDef.defName);
                return null;
            }
        }
        protected System.Collections.ICollection GetFieldValueAsCollection(ThingDef raceDef, object source, string name) {
            object result = GetFieldValue(raceDef, source, name, true);
            if (result == null) {
                return null;
            }
            System.Collections.ICollection collection = result as System.Collections.ICollection;
            if (collection == null) {
                Log.Warning("Prepare carefully could not convert " + name + " field value into a collection for " + raceDef.defName + ".");
                return null;
            }
            else {
                return collection;
            }
        }
        protected bool? GetFieldValueAsBool(ThingDef raceDef, object source, string name) {
            object result = GetFieldValue(raceDef, source, name, true);
            if (result == null) {
                return null;
            }
            if (result.GetType() == typeof(bool)) {
                return (bool)result;
            }
            else {
                Log.Warning("Prepare carefully could not convert " + name + " field value into a bool for " + raceDef.defName + ".");
                return null;
            }
        }
        protected string GetFieldValueAsString(ThingDef raceDef, object source, string name) {
            object value = GetFieldValue(raceDef, source, name, true);
            if (value == null) {
                return null;
            }
            string result = value as string;
            if (result != null) {
                return result;
            }
            else {
                Log.Warning("Prepare carefully could not convert " + name + " field value into a string for " + raceDef.defName + ".");
                return null;
            }
        }
        protected int? GetFieldValueAsInt(ThingDef raceDef, object source, string name) {
            object value = GetFieldValue(raceDef, source, name, true);
            if (value == null) {
                return null;
            }
            try {
                return (int)value;
            }
            catch (Exception) {
                Log.Warning("Prepare carefully could not convert " + name + " field value into an int for " + raceDef.defName + ".");
                return null;
            }
        }
    }
}
