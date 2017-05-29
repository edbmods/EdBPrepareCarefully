using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            return headTypes.GetHeadTypes(gender);
        }
        public CustomHeadType FindHeadType(ThingDef race, string graphicsPath) {
            OptionsHeadType headTypes = GetHeadTypesForRace(race);
            return headTypes.FindHeadType(graphicsPath);
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
            if (race == ThingDefOf.Human) {
                return InitializeHumanHeadTypes();
            }
            else {
                return InitializeAlienHeadTypes(race);
            }
        }
        protected OptionsHeadType InitializeHumanHeadTypes() {
            MethodInfo headGraphicsMethod = typeof(GraphicDatabaseHeadRecords).GetMethod("BuildDatabaseIfNecessary", BindingFlags.Static | BindingFlags.NonPublic);
            headGraphicsMethod.Invoke(null, null);
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
                    CustomHeadType headType = CreateHeadTypeFromGenderedGraphicPath(fullPath);
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
        protected CustomHeadType CreateGenderedAlienHeadTypeFromCrownType(string graphicsPath, string crownType, Gender gender) {
            CustomHeadType result = new CustomHeadType();
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
            try {
                result.GraphicPath = pathValue;
                result.CrownType = CrownType.Average;
                result.Gender = gender;
                return result;
            }
            catch (Exception ex) {
                Log.Warning("Prepare carefully failed to create a head type when trying to parse crown type value: " + crownType + ": " + ex.Message);
                return null;
            }
        }
        protected CustomHeadType CreateMultiGenderAlienHeadTypeFromCrownType(string graphicsPath, string crownType) {
            CustomHeadType result = new CustomHeadType();
            string pathValue = string.Copy(graphicsPath);
            if (!pathValue.EndsWith("/")) {
                pathValue += "/";
            }
            pathValue += crownType;
            try {
                result.GraphicPath = pathValue;
                result.CrownType = CrownType.Average;
                result.Gender = null;
                return result;
            }
            catch (Exception ex) {
                Log.Warning("Prepare carefully failed to create a head type when trying to parse crown type value: " + crownType + ": " + ex.Message);
                return null;
            }
        }
        protected CustomHeadType CreateHeadTypeFromGenderedGraphicPath(string graphicPath) {
            CustomHeadType result = new CustomHeadType();
            result.GraphicPath = graphicPath;
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
    }
}
