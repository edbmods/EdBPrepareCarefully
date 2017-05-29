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
        }
        public IEnumerable<CustomHeadType> GetHeadTypes(Gender gender) {
            return headTypes.Where((CustomHeadType headType) => {
                return (headType.Gender == gender || headType.Gender == null);
            });
        }
        public CustomHeadType FindHeadType(string graphicsPath) {
            CustomHeadType result;
            if (pathDictionary.TryGetValue(graphicsPath, out result)) {
                return result;
            }
            else {
                return null;
            }
        }
        public CustomHeadType FindHeadTypeForGender(CustomHeadType headType, Gender gender) {
            if (headType.Gender == null || headType.Gender == gender) {
                return headType;
            }
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
            CustomHeadType result = FindHeadType(graphicsPath);
            return result != null ? result : headType;
        }
    }
}
