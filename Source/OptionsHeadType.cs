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
    public class OptionsHeadType {
        protected List<Graphic> heads = new List<Graphic>();
        protected List<string> headPaths = new List<string>();
        protected List<CustomHeadType> maleHeadTypes = new List<CustomHeadType>();
        protected List<CustomHeadType> femaleHeadTypes = new List<CustomHeadType>();
        protected List<CustomHeadType> noGenderHeaderTypes = new List<CustomHeadType>();
        public Dictionary<string, CustomHeadType> pathDictionary = new Dictionary<string, CustomHeadType>();
        public List<CustomHeadType> headTypes = new List<CustomHeadType>();
        protected int count = 0;
        public OptionsHeadType() {
        }
        public void AddHeadType(CustomHeadType headType) {
            headTypes.Add(headType);
            pathDictionary.Add(headType.GraphicPath, headType);
            if (headType.AlternateGraphicPath != null) {
                pathDictionary.Add(headType.AlternateGraphicPath, headType);
            }
        }
        public IEnumerable<CustomHeadType> GetHeadTypesForGender(Gender gender) {
            return headTypes.Where((CustomHeadType headType) => {
                return (headType.Gender == gender || headType.Gender == null);
            });
        }
        public CustomHeadType FindHeadTypeForPawn(Pawn pawn) {
            var alienComp = ProviderAlienRaces.FindAlienCompForPawn(pawn);
            if (alienComp == null) {
                var result = FindHeadTypeByGraphicsPath(pawn.story.HeadGraphicPath);
                if (result == null) {
                    Logger.Warning("Did not find head type for path: " + pawn.story.HeadGraphicPath);
                }
                return result;
            }
            else {
                string crownType = ProviderAlienRaces.GetCrownTypeFromComp(alienComp);
                var result = FindHeadTypeByCrownTypeAndGender(crownType, pawn.gender);
                if (result == null) {
                    Logger.Warning("Did not find head type for alien crown type: " + crownType);
                }
                return result;
            }
        }
        public CustomHeadType FindHeadTypeByGraphicsPath(string graphicsPath) {
            CustomHeadType result;
            if (pathDictionary.TryGetValue(graphicsPath, out result)) {
                return result;
            }
            else {
                return null;
            }
        }
        public CustomHeadType FindHeadTypeByCrownType(string crownType) {
            return headTypes.Where(t => {
                return t.AlienCrownType == crownType;
            }).FirstOrDefault();
        }
        public CustomHeadType FindHeadTypeByCrownTypeAndGender(string crownType, Gender gender) {
            return headTypes.Where(t => {
                return t.AlienCrownType == crownType && (t.Gender == null || t.Gender == gender);
            }).FirstOrDefault();
        }
        public CustomHeadType FindHeadTypeForGender(CustomHeadType headType, Gender gender) {
            if (headType.Gender == null || headType.Gender == gender) {
                return headType;
            }

            if (headType.AlienCrownType.NullOrEmpty()) {
                string graphicsPath = headType.GraphicPath;
                string prefixReplacementString = "/" + gender.ToString() + "_";
                string directoryReplacementString = "/" + gender.ToString() + "/";
                if (headType.Gender == Gender.Male) {
                    graphicsPath = graphicsPath.Replace("/Male/", directoryReplacementString);
                    graphicsPath = graphicsPath.Replace("/Male_", prefixReplacementString);
                }
                if (headType.Gender == Gender.Female) {
                    graphicsPath = graphicsPath.Replace("/Female/", directoryReplacementString);
                    graphicsPath = graphicsPath.Replace("/Female_", prefixReplacementString);
                }
                else {
                    graphicsPath = graphicsPath.Replace("/None/", directoryReplacementString);
                    graphicsPath = graphicsPath.Replace("/None_", prefixReplacementString);
                }
                CustomHeadType result = FindHeadTypeByGraphicsPath(graphicsPath);
                if (result == null) {
                    Log.Warning("Could not find head type for gender: " + graphicsPath);
                }
                return result != null ? result : headType;
            }
            else {
                Gender targetGender = gender;
                if (headType.Gender == Gender.Male) {
                    targetGender = Gender.Female;
                }
                if (headType.Gender == Gender.Female) {
                    targetGender = Gender.Male;
                }
                else {
                    return headType;
                }
                var result = headTypes.Where(h => {
                    return h.AlienCrownType == headType.AlienCrownType && h.Gender == targetGender;
                }).FirstOrDefault();
                return result != null ? result : headType;
            }
        }
    }
}
