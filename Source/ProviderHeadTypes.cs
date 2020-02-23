using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class ProviderHeadTypes {
        protected List<Graphic> heads = new List<Graphic>();
        protected List<string> headPaths = new List<string>();
        public Dictionary<string, CustomHeadType> pathDictionary = new Dictionary<string, CustomHeadType>();
        protected Dictionary<ThingDef, OptionsHeadType> headTypeLookup = new Dictionary<ThingDef, OptionsHeadType>();
        public ProviderHeadTypes() {
        }
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public IEnumerable<CustomHeadType> GetHeadTypes(CustomPawn pawn) {
            return GetHeadTypes(pawn.Pawn.def, pawn.Gender);
        }
        public IEnumerable<CustomHeadType> GetHeadTypes(ThingDef race, Gender gender) {
            OptionsHeadType headTypes = GetHeadTypesForRace(race);
            return headTypes.GetHeadTypesForGender(gender);
        }
        public CustomHeadType FindHeadTypeForPawn(Pawn pawn) {
            OptionsHeadType headTypes = GetHeadTypesForRace(pawn.def);
            var result = headTypes.FindHeadTypeForPawn(pawn);
            if (result == null) {
                Log.Warning("Prepare Carefully could not find a head type for the pawn: " + pawn.def.defName + ". Head type selection disabled for this pawn");
            }
            return result;
        }
        public CustomHeadType FindHeadType(ThingDef race, string graphicsPath) {
            OptionsHeadType headTypes = GetHeadTypesForRace(race);
            return headTypes.FindHeadTypeByGraphicsPath(graphicsPath);
        }
        public CustomHeadType FindHeadTypeForGender(ThingDef race, CustomHeadType headType, Gender gender) {
            OptionsHeadType headTypes = GetHeadTypesForRace(race);
            return headTypes.FindHeadTypeForGender(headType, gender);
        }
        protected OptionsHeadType GetHeadTypesForRace(ThingDef race) {
            OptionsHeadType headTypes = null;
            if (!headTypeLookup.TryGetValue(race, out headTypes)) {
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
            //Log.Warning("Head Types for " + race.defName + ":");
            //Log.Warning("  Male: ");
            //foreach (var h in result.GetHeadTypesForGender(Gender.Male)) {
            //    Log.Message("    " + h.ToString());
            //}
            //Log.Warning("  Female: ");
            //foreach (var h in result.GetHeadTypesForGender(Gender.Female)) {
            //    Log.Message("    " + h.ToString());
            //}
            return result;
        }
        protected OptionsHeadType InitializeHumanHeadTypes() {
            Reflection.GraphicDatabaseHeadRecords.BuildDatabaseIfNecessary();
            string[] headsFolderPaths = new string[] {
                "Things/Pawn/Humanlike/Heads/Male",
                "Things/Pawn/Humanlike/Heads/Female"
            };
            OptionsHeadType result = new OptionsHeadType();
            for (int i = 0; i < headsFolderPaths.Length; i++) {
                string text = headsFolderPaths[i];
                IEnumerable<string> graphicsInFolder = GraphicDatabaseUtility.GraphicNamesInFolder(text);
                foreach (string current in GraphicDatabaseUtility.GraphicNamesInFolder(text)) {
                    string fullPath = text + "/" + current;
                    CustomHeadType headType = CreateHumanHeadTypeFromGenderedGraphicPath(fullPath);
                    result.AddHeadType(headType);
                    pathDictionary.Add(fullPath, headType);
                }
            }
            return result;
        }
        protected OptionsHeadType InitializeAlienHeadTypes(ThingDef raceDef) {
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(raceDef);
            OptionsHeadType result = new OptionsHeadType();
            if (alienRace == null) {
                Log.Warning("Prepare Carefully could not initialize head types for alien race, " + raceDef);
                return result;
            }
            if (alienRace.GraphicsPathForHeads == null) {
                Log.Warning("Prepare Carefully could not initialize head types for alien race, " + raceDef + ", because no path for head graphics was found.");
                return result;
            }
            foreach (var crownType in alienRace.CrownTypes) {
                if (alienRace.GenderSpecificHeads) {
                    CustomHeadType maleHead = CreateGenderedAlienHeadTypeFromCrownType(alienRace.GraphicsPathForHeads, crownType, Gender.Male);
                    CustomHeadType femaleHead = CreateGenderedAlienHeadTypeFromCrownType(alienRace.GraphicsPathForHeads, crownType, Gender.Female);
                    if (maleHead != null) {
                        result.AddHeadType(maleHead);
                    }
                    if (femaleHead != null) {
                        result.AddHeadType(femaleHead);
                    }
                }
                else {
                    CustomHeadType head = CreateMultiGenderAlienHeadTypeFromCrownType(alienRace.GraphicsPathForHeads, crownType);
                    if (head != null) {
                        result.AddHeadType(head);
                    }
                }
            }
            return result;
        }

        protected CrownType FindCrownTypeEnumValue(string crownType) {
            if (crownType.Contains(CrownType.Average.ToString() + "_")) {
                return CrownType.Average;
            }
            else if (crownType.Contains(CrownType.Narrow.ToString() + "_")) {
                return CrownType.Narrow;
            }
            else {
                return CrownType.Undefined;
            }
        }

        protected CustomHeadType CreateGenderedAlienHeadTypeFromCrownType(string graphicsPath, string crownType, Gender gender) {
            CustomHeadType result = new CustomHeadType();
            result.Gender = gender;
            result.Label = LabelFromCrownType(crownType);

            // Build the full graphics path for this head type
            string pathValue = string.Copy(graphicsPath);
            if (!pathValue.EndsWith("/")) {
                pathValue += "/";
            }
            if (gender == Gender.Female) {
                pathValue += "Female_";
            }
            else if (gender == Gender.Male) {
                pathValue += "Male_";
            }
            else {
                pathValue += "None_";
            }
            pathValue += crownType;
            result.GraphicPath = pathValue;
            result.CrownType = FindCrownTypeEnumValue(crownType);
            result.AlienCrownType = crownType;
            return result;
        }

        protected CustomHeadType CreateMultiGenderAlienHeadTypeFromCrownType(string graphicsPath, string crownType) {
            CustomHeadType result = new CustomHeadType();
            string pathValue = string.Copy(graphicsPath);
            if (!pathValue.EndsWith("/")) {
                pathValue += "/";
            }
            pathValue += crownType;
            result.GraphicPath = pathValue;
            result.Label = LabelFromCrownType(crownType);
            result.Gender = null;
            result.CrownType = FindCrownTypeEnumValue(crownType);
            result.AlienCrownType = crownType;
            return result;
        }

        protected CustomHeadType CreateHumanHeadTypeFromGenderedGraphicPath(string graphicPath) {
            CustomHeadType result = new CustomHeadType();
            result.GraphicPath = graphicPath;
            result.Label = LabelFromGraphicsPath(graphicPath);
            string[] strArray = Path.GetFileNameWithoutExtension(graphicPath).Split('_');
            try {
                result.CrownType = (CrownType)ParseHelper.FromString(strArray[strArray.Length - 2], typeof(CrownType));
                result.Gender = (Gender)ParseHelper.FromString(strArray[strArray.Length - 3], typeof(Gender));
            }
            catch (Exception ex) {
                Log.Warning("Parse error with head graphic at " + graphicPath + ": " + ex.Message);
                result.CrownType = CrownType.Undefined;
                result.Gender = Gender.None;
            }
            return result;
        }

        protected string LabelFromGraphicsPath(string path) {
            try {
                string[] pathValues = path.Split('/');
                string crownType = pathValues[pathValues.Length - 1];
                string[] values = crownType.Split('_');
                return values[values.Count() - 2] + ", " + values[values.Count() - 1];
            }
            catch (Exception) {
                Log.Warning("Prepare Carefully could not determine head type label from graphics path: " + path);
                return "EdB.PC.Common.Default".Translate();
            }
        }

        protected string LabelFromCrownType(string crownType) {
            string value;
            value = Regex.Replace(crownType, "(\\B[A-Z]+?(?=[A-Z][^A-Z])|\\B[A-Z]+?(?=[^A-Z]))", " $1");
            value = value.Replace("_", " ");
            value = Regex.Replace(value, "\\s+", " ");
            return value;
        }

    }
}
