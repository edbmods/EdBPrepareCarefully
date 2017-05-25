using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class RaceHeadTypes {
        protected ThingDef race;
        protected List<Graphic> heads = new List<Graphic>();
        protected List<string> headPaths = new List<string>();
        protected List<HeadType> maleHeadTypes = new List<HeadType>();
        protected List<HeadType> femaleHeadTypes = new List<HeadType>();
        protected List<HeadType> noGenderHeaderTypes = new List<HeadType>();
        public Dictionary<string, HeadType> pathDictionary = new Dictionary<string, HeadType>();
        public RaceHeadTypes(ThingDef race) {
            this.race = race;
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
        protected void Initialize() {
            MethodInfo headGraphicsMethod = typeof(GraphicDatabaseHeadRecords).GetMethod("BuildDatabaseIfNecessary", BindingFlags.Static | BindingFlags.NonPublic);
            headGraphicsMethod.Invoke(null, null);
            string[] headsFolderPaths = GetFolderPaths();
            for (int i = 0; i < headsFolderPaths.Length; i++) {
                string text = headsFolderPaths[i];
                // TODO: Looks like this doesn't work for modded graphics.  Need to figure out how to get
                // the list of heads from the mod directory.
                IEnumerable<string> graphicsInFolder = GraphicDatabaseUtility.GraphicNamesInFolder(text);
                if (graphicsInFolder.Count() == 0) {
                    //Log.Message("No head graphics in folder: " + text);
                }
                foreach (string current in GraphicDatabaseUtility.GraphicNamesInFolder(text)) {
                    //Log.Message("head in folder: " + current);
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
        protected HeadType CreateHeadTypeFromGraphicPath(string graphicPath) {
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
        protected string[] GetFolderPaths() {
            if (race == ThingDefOf.Human) {
                return GetHumanFolderPaths();
            }
            else {
                // TODO: WIP. Evaluate where this Alien Races custom logic should go.
                List<string> resultPathList = new List<string>();
                FieldInfo alienRaceField = this.race.GetType().GetField("alienRace", BindingFlags.Public | BindingFlags.Instance);
                //Log.Message(" alienRaceField " + (alienRaceField == null ? "is null" : "in not null"));
                if (alienRaceField != null) {
                    object alienRaceObject = alienRaceField.GetValue(race);
                    if (alienRaceObject != null) {
                        FieldInfo graphicPathsField = alienRaceObject.GetType().GetField("graphicPaths", BindingFlags.Public | BindingFlags.Instance);
                        //Log.Message(" graphicPathsField " + (graphicPathsField == null ? "is null" : "in not null"));
                        object graphicPathsObject = graphicPathsField.GetValue(alienRaceObject);
                        if (graphicPathsObject != null) {
                            System.Collections.ICollection graphicPathsList = graphicPathsObject as System.Collections.ICollection;
                            if (graphicPathsList != null) {
                                //Log.Message("List count: " + graphicPathsList.Count);
                                if (graphicPathsList.Count > 0) {
                                    foreach (object o in graphicPathsList) {
                                        //Log.Message(o.GetType().FullName);
                                        FieldInfo headField = o.GetType().GetField("head", BindingFlags.Public | BindingFlags.Instance);
                                        if (headField != null) {
                                            object headsObject = headField.GetValue(o);
                                            if (headsObject != null) {
                                                string headPath = headsObject as string;
                                                if (headPath != null) {
                                                    headPath = headPath.Trim('/');
                                                    resultPathList.Add(headPath);
                                                    //Log.Message("headPath: " + headPath);
                                                }
                                                else {
                                                    //Log.Message("headPath is not a string");
                                                }
                                            }
                                            else {
                                                //Log.Message("headsObject is null");
                                            }
                                        }
                                        else {
                                            //Log.Message("headField is null");
                                        }
                                    }
                                }
                            }
                            else {
                                //Log.Message("graphicPathsList is null");
                            }
                        }
                    }
                    else {
                        //Log.Message("alienRaceObject is null");
                    }
                }
                return resultPathList.ToArray();
            }
        }
        protected string[] GetHumanFolderPaths() {
            return new string[] {
                "Things/Pawn/Humanlike/Heads/Male",
                "Things/Pawn/Humanlike/Heads/Female"
            };
        }
    }
}
