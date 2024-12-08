using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ProviderHeadTypes {
        protected List<HeadTypeDef> defs = new List<HeadTypeDef>();
        protected List<Graphic> heads = new List<Graphic>();
        protected List<string> headPaths = new List<string>();
        public Dictionary<string, HeadTypeDef> pathDictionary = new Dictionary<string, HeadTypeDef>();
        protected Dictionary<ThingDef, OptionsHeadType> headTypeLookup = new Dictionary<ThingDef, OptionsHeadType>();
        public ProviderHeadTypes() {
        }
        public ProviderAlienRaces ProviderAlienRaces {
            get; set;
        }
        public IEnumerable<HeadTypeDef> GetHeadTypes(Pawn pawn) {
            return GetHeadTypes(pawn.def, pawn.gender);
        }
        public IEnumerable<HeadTypeDef> GetHeadTypes(ThingDef race, Gender gender) {
            OptionsHeadType headTypes = GetHeadTypesForRace(race);
            return headTypes.GetHeadTypesForGender(gender);
        }
        public HeadTypeDef FindHeadTypeForPawn(Pawn pawn) {
            OptionsHeadType headTypes = GetHeadTypesForRace(pawn.def);
            var result = headTypes.FindHeadTypeForPawn(pawn);
            if (result == null) {
                Logger.Warning("Could not find a head type for the pawn: " + pawn.def.defName + ". Head type selection disabled for this pawn");
            }
            return result;
        }
        protected OptionsHeadType GetHeadTypesForRace(ThingDef race) {
            if (!headTypeLookup.TryGetValue(race, out OptionsHeadType headTypes)) {
                headTypes = InitializeHeadTypes(race);
                headTypeLookup.Add(race, headTypes);
            }
            if (headTypes == null && race != ThingDefOf.Human) {
                return GetHeadTypesForRace(ThingDefOf.Human);
            }
            return headTypes;
        }
        protected OptionsHeadType InitializeHeadTypes(ThingDef race) {
            OptionsHeadType result;
            // If the race definition has an alien comp, then look for the head types in it.  If not, then use the default human head types.
            CompProperties alienCompProperties = null;
            if (race != null && race.comps != null) {
                alienCompProperties = race.comps.FirstOrDefault((CompProperties comp) => {
                    return (comp.compClass.Name == "AlienComp");
                });
            }
            if (alienCompProperties == null) {
                result = InitializeHumanHeadTypes();
            }
            else {
                result = InitializeAlienHeadTypes(race);
            }
            //Logger.Debug("Head Types for " + race.defName + ":");
            //Logger.Debug("  Male: ");
            //foreach (var h in result.GetHeadTypesForGender(Gender.Male)) {
            //    Logger.Debug("    " + h.ToString());
            //}
            //Logger.Debug("  Female: ");
            //foreach (var h in result.GetHeadTypesForGender(Gender.Female)) {
            //    Logger.Debug("    " + h.ToString());
            //}
            return result;
        }
        protected OptionsHeadType InitializeHumanHeadTypes() {
            OptionsHeadType result = new OptionsHeadType();
            foreach (var d in DefDatabase<HeadTypeDef>.AllDefs) {
                if (d == HeadTypeDefOf.Skull || d.defName == "Stump") {
                    continue;
                }
                result.AddHeadType(d);
            }
            return result;
        }
        protected OptionsHeadType InitializeAlienHeadTypes(ThingDef raceDef) {
            OptionsHeadType result = new OptionsHeadType();
            //Logger.Debug("InitializeAlienHeadTypes(" + raceDef.defName + ")");
            AlienRace alienRace = ProviderAlienRaces.GetAlienRace(raceDef);
            if (alienRace == null) {
                Logger.Warning("Could not initialize head types for alien race, " + raceDef + ", because the race's thing definition was missing");
                return result;
            }
            foreach (var headType in alienRace.HeadTypes) {
                result.AddHeadType(headType);
            }
            return result;
        }

        public HeadTypeDef FindMatchingHeadTypeForOtherGenderOrDefault(HeadTypeDef def, Gender targetGender) {
            if (def.gender == targetGender) {
                return def;
            }
            if (targetGender == Gender.None) {
                return def;
            }

            string replaceWith = targetGender.ToString();
            string stringToReplace = targetGender.Equals(Gender.Male) ? Gender.Female.ToString() : Gender.Male.ToString();
            string targetDefName = def.defName.Replace(stringToReplace + "_", replaceWith + "_");
            HeadTypeDef matchingDef = DefDatabase<HeadTypeDef>.GetNamedSilentFail(targetDefName);
            if (matchingDef != null) {
                Logger.Debug("Swapped gender-specific head type with matching: " + matchingDef.defName);
                return matchingDef;
            }
            HeadTypeDef result = DefDatabase<HeadTypeDef>.AllDefs.Where(d => d.gender == targetGender).FirstOrDefault(null);
            if (result != null) {
                Logger.Debug("Swapped gender-specific head type with first found: " + result.defName);
                return result;
            }
            else {
                return def;
            }
        }
        public string GetHeadTypeLabel(HeadTypeDef headType) {
            // Try get xml label first
            string headTypeCustomLabel = $"EdB.PC.HeadType.{headType.defName}.label".Translate();
            if (headType.label.NullOrEmpty() && !headTypeCustomLabel.NullOrEmpty()) {
                return headTypeCustomLabel;
            }
            
            if (headTypeCustomLabel.NullOrEmpty()) {
                return ConvertHeadTypeDefNameToLabel(headType.defName);
            }

            return headType.LabelCap;
        }

        protected string ConvertHeadTypeDefNameToLabel(string defName) {
            string result = defName
                .Replace("StarWarsRaces_", "")
                .Replace("_", " ")
                ;
            return result;
        }

        //protected string LabelFromGraphicsPath(string path) {
        //    try {
        //        string[] pathValues = path.Split('/');
        //        string crownType = pathValues[pathValues.Length - 1];
        //        string[] values = crownType.Split('_');
        //        return values[values.Count() - 2] + ", " + values[values.Count() - 1];
        //    }
        //    catch (Exception) {
        //        Logger.Warning("Could not determine head type label from graphics path: " + path);
        //        return "EdB.PC.Common.Default".Translate();
        //    }
        //}

        //protected string LabelFromCrownType(string crownType) {
        //    string value;
        //    value = Regex.Replace(crownType, "(\\B[A-Z]+?(?=[A-Z][^A-Z])|\\B[A-Z]+?(?=[^A-Z]))", " $1");
        //    value = value.Replace("_", " ");
        //    value = Regex.Replace(value, "\\s+", " ");
        //    return value;
        //}

    }
}
