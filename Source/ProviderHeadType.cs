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
    public class ProviderHeadType {
        protected List<Graphic> heads = new List<Graphic>();
        protected List<string> headPaths = new List<string>();
        protected List<HeadType> maleHeadTypes = new List<HeadType>();
        protected List<HeadType> femaleHeadTypes = new List<HeadType>();
        protected List<HeadType> noGenderHeaderTypes = new List<HeadType>();
        public Dictionary<string, HeadType> pathDictionary = new Dictionary<string, HeadType>();

        public ProviderHeadType() {
            Initialize();
        }
        public List<HeadType> GetHeadTypes(Gender gender) {
            if (gender == Gender.Male) {
                return maleHeadTypes;
            }
            else if (gender == Gender.Female) {
                return femaleHeadTypes;
            }
            else {
                return noGenderHeaderTypes;
            }
        }
        
        protected void Initialize() {
            MethodInfo headGraphicsMethod = typeof(GraphicDatabaseHeadRecords).GetMethod("BuildDatabaseIfNecessary", BindingFlags.Static | BindingFlags.NonPublic);
            headGraphicsMethod.Invoke(null, null);

            string[] headsFolderPaths = new string[] {
                "Things/Pawn/Humanlike/Heads/Male",
                "Things/Pawn/Humanlike/Heads/Female"
            };
            for (int i = 0; i < headsFolderPaths.Length; i++) {
                string text = headsFolderPaths[i];
                foreach (string current in GraphicDatabaseUtility.GraphicNamesInFolder(text)) {
                    string fullPath = text + "/" + current;
                    HeadType headType = CreateHeadTypeFromGraphicPath(fullPath);
                    if (headType.Gender == Gender.Male) {
                        maleHeadTypes.Add(headType);
                    }
                    else if (headType.Gender == Gender.Female) {
                        femaleHeadTypes.Add(headType);
                    }
                    else {
                        noGenderHeaderTypes.Add(headType);
                    }
                    pathDictionary.Add(fullPath, headType);
                }
            }
        }

        public HeadType CreateHeadTypeFromGraphicPath(string graphicPath) {
            HeadType result = new HeadType();
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

        public HeadType FindHeadType(string graphicsPath) {
            HeadType result;
            if (pathDictionary.TryGetValue(graphicsPath, out result)) {
                return result;
            }
            else {
                return null;
            }
        }

        public HeadType FindHeadTypeForGender(HeadType headType, Gender gender) {
            if (headType.Gender == gender) {
                return headType;
            }
            string graphicsPath = headType.GraphicPath;
            if (gender == Gender.Male) {
                graphicsPath = graphicsPath.Replace("Female", "Male");
            }
            else {
                graphicsPath = graphicsPath.Replace("Male", "Female");
            }
            HeadType result = FindHeadType(graphicsPath);
            return result != null ? result : headType;
        }
    }
}
