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
        protected float defaultMinAgeForAdulthood = 20f;

        public float DefaultMinAgeForAdulthood {
            get { return defaultMinAgeForAdulthood; }
        }

        public ProviderAlienRaces() {
            defaultMinAgeForAdulthood = ReflectionUtil.GetNonPublicStatic<float>(typeof(PawnBioAndNameGenerator), "MinAgeForAdulthood");
            if (defaultMinAgeForAdulthood <= 0f) {
                defaultMinAgeForAdulthood = 20.0f;
            }
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

        protected ColorGenerator FindPrimarySkinColorGenerator(ThingDef raceDef, object alienPartGeneratorObject) {
            ColorGenerator generator = FindPrimarySkinColorGeneratorPre12(raceDef, alienPartGeneratorObject);
            if (generator != null) {
                return generator;
            }
            return FindPrimaryColorGenerator(raceDef, alienPartGeneratorObject, "skin");
        }

        protected ColorGenerator FindSecondarySkinColorGenerator(ThingDef raceDef, object alienPartGeneratorObject) {
            ColorGenerator generator = FindSecondarySkinColorGeneratorPre12(raceDef, alienPartGeneratorObject);
            if (generator != null) {
                return generator;
            }
            return FindSecondaryColorGenerator(raceDef, alienPartGeneratorObject, "skin");
        }

        protected ColorGenerator FindPrimarySkinColorGeneratorPre12(ThingDef raceDef, object alienPartGeneratorObject) {
            return QuietReflectionUtil.GetFieldValue<ColorGenerator>(alienPartGeneratorObject, "alienskincolorgen");
        }

        protected ColorGenerator FindSecondarySkinColorGeneratorPre12(ThingDef raceDef, object alienPartGeneratorObject) {
            return QuietReflectionUtil.GetFieldValue<ColorGenerator>(alienPartGeneratorObject, "alienskinsecondcolorgen");
        }

        protected ColorGenerator FindPrimaryColorGenerator(ThingDef raceDef, object alienPartGeneratorObject, string channelName) {
            return FindColorGenerator(raceDef, alienPartGeneratorObject, channelName, "first");
        }

        protected ColorGenerator FindSecondaryColorGenerator(ThingDef raceDef, object alienPartGeneratorObject, string channelName) {
            return FindColorGenerator(raceDef, alienPartGeneratorObject, channelName, "second");
        }

        protected ColorGenerator FindColorGenerator(ThingDef raceDef, object alienPartGeneratorObject, string channelName, string generatorFieldName) {
            object colorChannelsObject = GetFieldValue(raceDef, alienPartGeneratorObject, "colorChannels", true);
            if (colorChannelsObject == null) {
                Logger.Warning("didn't find colorChannels field");
                return null;
            }
            System.Collections.IList colorChannelList = colorChannelsObject as System.Collections.IList;
            if (colorChannelList == null) {
                return null;
            }
            object foundGenerator = null;
            foreach (var generator in colorChannelList) {
                string name = GetFieldValue(raceDef, generator, "name", true) as string;
                if (channelName == name) {
                    foundGenerator = generator;
                    break;
                }
            }
            if (foundGenerator == null) {
                return null;
            }
            return GetFieldValue(raceDef, foundGenerator, generatorFieldName, true) as ColorGenerator;
        }

        protected AlienRace InitializeAlienRace(ThingDef raceDef) {
            try {
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
                Logger.Debug("GraphicsPaths for " + raceDef.defName + ":");
                if (graphicPathsCollection.Count > 0) {
                    foreach (object o in graphicPathsCollection) {
                        Logger.Debug("  GraphicsPath");
                        Logger.Debug("    .body = " + GetFieldValueAsString(raceDef, o, "body"));
                        Logger.Debug("    .head = " + GetFieldValueAsString(raceDef, o, "head"));
                        System.Collections.ICollection lifeStagesCollections = GetFieldValueAsCollection(raceDef, o, "lifeStageDefs");
                    }
                }
                */

                // We have enough to start putting together the result object, so we instantiate it now.
                AlienRace result = new AlienRace();
                result.ThingDef = raceDef;

                //Logger.Debug("InitializeAlienRace: " + raceDef.defName);

                float minAgeForAdulthood = ReflectionUtil.GetFieldValue<float>(generalSettingsObject, "minAgeForAdulthood");
                if (minAgeForAdulthood <= 0) {
                    minAgeForAdulthood = DefaultMinAgeForAdulthood;
                }
                result.MinAgeForAdulthood = minAgeForAdulthood;

                // Get the list of body types.
                System.Collections.ICollection alienBodyTypesCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "alienbodytypes");
                if (alienBodyTypesCollection == null) {
                    return null;
                }
                List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>();
                //Logger.Debug("Body Types for " + raceDef.defName + ":");
                if (alienBodyTypesCollection.Count > 0) {
                    foreach (object o in alienBodyTypesCollection) {
                        if (o.GetType() == typeof(BodyTypeDef)) {
                            BodyTypeDef def = o as BodyTypeDef;
                            //Logger.Debug("  - " + def.defName + ", " + def.LabelCap);
                            bodyTypes.Add((BodyTypeDef)o);
                        }
                    }
                }
                else {
                    //Logger.Debug("  none");
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
                //Logger.Debug("Crown Types for " + raceDef.defName + ":");
                if (alienCrownTypesCollection.Count > 0) {
                    foreach (object o in alienCrownTypesCollection) {
                        string crownTypeString = o as string;
                        if (crownTypeString != null) {
                            crownTypes.Add(crownTypeString);
                            //Logger.Debug("  " + crownTypeString);
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
                ColorGenerator primaryGenerator = FindPrimarySkinColorGenerator(raceDef, alienPartGeneratorObject);
                ColorGenerator secondaryGenerator = FindSecondarySkinColorGenerator(raceDef, alienPartGeneratorObject);
                result.UseMelaninLevels = true;
                result.ChangeableColor = true;
                result.HasSecondaryColor = false;

                if (primaryGenerator != null) {
                    if (primaryGenerator.GetType().Name != "ColorGenerator_SkinColorMelanin") {
                        if (primaryGenerator != null) {
                            result.UseMelaninLevels = false;
                            result.PrimaryColors = primaryGenerator.GetColorList();
                        }
                        else {
                            result.PrimaryColors = new List<Color>();
                        }

                        if (secondaryGenerator != null) {
                            result.HasSecondaryColor = true;
                            result.SecondaryColors = secondaryGenerator.GetColorList();
                        }
                        else {
                            result.SecondaryColors = new List<Color>();
                        }
                    }
                }

                // Style settings
                object styleSettingsValue = GetFieldValue(raceDef, alienRaceObject, "styleSettings", true);

                result.HasHair = true;
                result.HasBeards = true;
                result.HasTattoos = true;
                if (styleSettingsValue is System.Collections.IDictionary styleSettings) {

                    // Hair properties.
                    if (styleSettings.Contains(typeof(HairDef))) {
                        object hairSettings = styleSettings[typeof(HairDef)];
                        bool? hasStyle = GetFieldValueAsBool(raceDef, hairSettings, "hasStyle");
                        if (hasStyle.HasValue && !hasStyle.Value) {
                            result.HasHair = false;
                        }

                        var hairTagCollection = GetFieldValueAsCollection(raceDef, hairSettings, "styleTagsOverride");
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

                    // Beard properties.
                    if (styleSettings.Contains(typeof(BeardDef))) {
                        object settings = styleSettings[typeof(BeardDef)];
                        bool? hasBeards = GetFieldValueAsBool(raceDef, settings, "hasStyle");
                        if (hasBeards.HasValue && !hasBeards.Value) {
                            result.HasBeards = false;
                        }
                    }

                    // Tattoo properties.
                    if (styleSettings.Contains(typeof(TattooDef))) {
                        object settings = styleSettings[typeof(TattooDef)];
                        bool? hasTattoos = GetFieldValueAsBool(raceDef, settings, "hasStyle");
                        if (hasTattoos.HasValue && !hasTattoos.Value) {
                            result.HasTattoos = false;
                        }
                    }
                }

                ColorGenerator hairColorGenerator = FindPrimaryColorGenerator(raceDef, alienPartGeneratorObject, "hair");
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
                if (bodyAddonsCollection != null) {
                    var addons = new List<AlienRaceBodyAddon>();
                    int index = -1;
                    foreach (var o in bodyAddonsCollection) {
                        index++;
                        AlienRaceBodyAddon addon = new AlienRaceBodyAddon();
                        string path = GetFieldValueAsString(raceDef, o, "path");
                        if (path == null) {
                            Logger.Warning("Failed to get path for body add-on for alien race: " + raceDef.defName);
                            continue;
                        }
                        addon.Path = path;
                        int? variantCount = GetFieldValueAsInt(raceDef, o, "variantCount");
                        if (variantCount == null) {
                            Logger.Warning("Failed to get variant count for body add-on for alien race: " + raceDef.defName);
                            continue;
                        }
                        addon.OptionCount = variantCount.Value;
                        string name = ParseAddonName(path);
                        if (name == null) {
                            Logger.Warning("Failed to parse a name from its path for body add-on for alien race: " + raceDef.defName);
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
            catch (Exception e) {
                throw new InitializationException("Failed to initialize an alien race: " + raceDef.defName, e);
            }
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
                    Logger.Warning("Could not find " + name + " field for " + raceDef.defName);
                    return null;
                }
                object result = field.GetValue(source);
                if (result == null) {
                    if (!allowNull) {
                        Logger.Warning("Could not find " + name + " field value for " + raceDef.defName);
                    }
                    return null;
                }
                else {
                    return result;
                }
            }
            catch (Exception) {
                Logger.Warning("Could resolve value of the " + name + " field for " + raceDef.defName);
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
                Logger.Warning("Could not convert " + name + " field value into a collection for " + raceDef.defName + ".");
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
                Logger.Warning("Could not convert " + name + " field value into a bool for " + raceDef.defName + ".");
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
                Logger.Warning("Could not convert " + name + " field value into a string for " + raceDef.defName + ".");
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
                Logger.Warning("Could not convert " + name + " field value into an int for " + raceDef.defName + ".");
                return null;
            }
        }
    }
}
