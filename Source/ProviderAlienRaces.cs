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
        public AlienRace GetAlienRaceForPawn(Pawn pawn) {
            return GetAlienRace(pawn.def);
        }

        public AlienRace GetAlienRace(ThingDef def) {
            AlienRace result;
            if (lookup.TryGetValue(def, out result)) {
                return result;
            }
            else {
                if (IsAlienRace(def)) {
                    //Logger.Debug(def.defName + " is an alien race");
                    result = InitializeAlienRace(def);
                    if (result != null) {
                        lookup.Add(def, result);
                    }
                    else {
                        Logger.Debug("Failed to initialize " + def.defName + " alien race");
                    }
                    return result;
                }
                else {
                    //Logger.Debug(def.defName + " is not an alien race");
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
            System.Collections.IList colorChannelGeneratorCategoryList = GetFieldValueAsCollection(raceDef, foundGenerator, "entries") as System.Collections.IList;
            if (colorChannelGeneratorCategoryList == null || colorChannelGeneratorCategoryList.Count == 0) {
                return null;
            }
            object colorChannelGeneratorCategory = colorChannelGeneratorCategoryList[0];
            return GetFieldValue(raceDef, colorChannelGeneratorCategory, generatorFieldName, true) as ColorGenerator;
        }

        protected AlienRace InitializeAlienRace(ThingDef raceDef) {
            try {
                object alienSettingsObject = GetFieldValue(raceDef, raceDef, "alienRace");
                if (alienSettingsObject == null) {
                    Logger.Debug("Didn't find AlienSettings object in ThingDef_AlienRace.alienRace field");
                    return null;
                }
                object generalSettingsObject = GetFieldValue(raceDef, alienSettingsObject, "generalSettings");
                if (generalSettingsObject == null) {
                    Logger.Debug("Didn't find GeneralSettings object in ThingDef_AlienRace.AlienSettings.generalSettings field");
                    return null;
                }
                object alienPartGeneratorObject = GetFieldValue(raceDef, generalSettingsObject, "alienPartGenerator");
                if (alienPartGeneratorObject == null) {
                    Logger.Debug("Didn't find AlienPartGenerator object in GeneralSettings.alienPartGenerator field");
                    return null;
                }
                object graphicsPathsObject = GetFieldValue(raceDef, alienSettingsObject, "graphicPaths");
                if (graphicsPathsObject == null) {
                    Logger.Debug("Didn't find GraphicsPaths object in ThingDef_AlienRace.graphicPaths field");
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
                System.Collections.ICollection alienBodyTypesCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "bodyTypes");
                if (alienBodyTypesCollection == null) {
                    return null;
                }
                List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>();
                if (alienBodyTypesCollection.Count > 0) {
                    foreach (object o in alienBodyTypesCollection) {
                        if (o.GetType() == typeof(BodyTypeDef)) {
                            BodyTypeDef def = o as BodyTypeDef;
                            bodyTypes.Add((BodyTypeDef)o);
                        }
                    }
                }
                //Logger.Debug($"Body types for alien race {raceDef.defName}: {string.Join(", ", bodyTypes.Select(b => b.defName + ", " + b.LabelCap))}");
                result.BodyTypes = bodyTypes;

                // Get the list of head types.
                System.Collections.ICollection alienHeadTypesCollection = GetFieldValueAsCollection(raceDef, alienPartGeneratorObject, "headTypes");
                if (alienHeadTypesCollection == null) {
                    Logger.Debug("Didn't find List<HeadTypeDef> object in AlienPartGenerator.headTypes field");
                    return null;
                }
                List<HeadTypeDef> headTypes = new List<HeadTypeDef>();
                if (alienHeadTypesCollection.Count > 0) {
                    foreach (object o in alienHeadTypesCollection) {
                        if (o.GetType() == typeof(HeadTypeDef)) {
                            HeadTypeDef def = o as HeadTypeDef;
                            headTypes.Add((HeadTypeDef)o);
                        }
                    }
                }
                result.HeadTypes = headTypes;

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
                object styleSettingsValue = GetFieldValue(raceDef, alienSettingsObject, "styleSettings", true);

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
                object restrictionSettingsValue = GetFieldValue(raceDef, alienSettingsObject, "raceRestriction", true);
                result.RaceSpecificApparelOnly = false;
                result.RaceSpecificApparel = new HashSet<string>();
                result.AllowedApparel = new HashSet<string>();
                result.DisallowedApparel = new HashSet<string>();
                if (restrictionSettingsValue != null) {
                    bool? restrictedApparelOnly = GetFieldValueAsBool(raceDef, restrictionSettingsValue, "onlyUseRaceRestrictedApparel");
                    if (restrictedApparelOnly != null) {
                        result.RaceSpecificApparelOnly = restrictedApparelOnly.Value;
                    }

                    var restrictedApparelCollection = GetFieldValueAsCollection(raceDef, restrictionSettingsValue, "apparelList");
                    if (restrictedApparelCollection != null) {
                        foreach (var o in restrictedApparelCollection) {
                            if (o is ThingDef def) {
                                result.RaceSpecificApparel.Add(def.defName);
                            }
                        }
                    }

                    var allowedApparelCollection = GetFieldValueAsCollection(raceDef, restrictionSettingsValue, "whiteApparelList");
                    if (allowedApparelCollection != null) {
                        foreach (var o in allowedApparelCollection) {
                            if (o is ThingDef def) {
                                result.AllowedApparel.Add(def.defName);
                            }
                        }
                    }

                    var disallowedApparelCollection = GetFieldValueAsCollection(raceDef, restrictionSettingsValue, "blackApparelList");
                    if (disallowedApparelCollection != null) {
                        foreach (var o in disallowedApparelCollection) {
                            if (o is ThingDef def) {
                                result.DisallowedApparel.Add(def.defName);
                            }
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
                throw new InitializationException("Exception when trying to initialize an alien race: " + raceDef.defName, e);
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
