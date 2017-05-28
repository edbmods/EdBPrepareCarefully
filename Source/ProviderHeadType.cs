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
        protected List<CustomHeadType> maleHeadTypes = new List<CustomHeadType>();
        protected List<CustomHeadType> femaleHeadTypes = new List<CustomHeadType>();
        protected List<CustomHeadType> noGenderHeaderTypes = new List<CustomHeadType>();
        public Dictionary<string, CustomHeadType> pathDictionary = new Dictionary<string, CustomHeadType>();
        protected Dictionary<ThingDef, RaceHeadTypes> headTypeLookup = new Dictionary<ThingDef, RaceHeadTypes>();

        public ProviderHeadType() {
        }
        public List<CustomHeadType> GetHeadTypes(ThingDef race, Gender gender) {
            RaceHeadTypes headTypes = GetHeadTypesForRace(race);
            return headTypes.GetHeadTypes(gender);
        }
        public CustomHeadType FindHeadType(ThingDef race, string graphicsPath) {
            RaceHeadTypes headTypes = GetHeadTypesForRace(race);
            return headTypes.FindHeadType(graphicsPath);
        }
        public CustomHeadType FindHeadTypeForGender(ThingDef race, CustomHeadType headType, Gender gender) {
            RaceHeadTypes headTypes = GetHeadTypesForRace(race);
            return headTypes.FindHeadTypeForGender(headType, gender);
        }
        protected RaceHeadTypes GetHeadTypesForRace(ThingDef race) {
            RaceHeadTypes headTypes = null;
            if (!headTypeLookup.TryGetValue(race, out headTypes)) {
                headTypes = new RaceHeadTypes(race);
                headTypeLookup.Add(race, headTypes);
            }
            if (headTypes == null && race != ThingDefOf.Human) {
                return GetHeadTypesForRace(ThingDefOf.Human);
            }
            return headTypes;
        }
    }
}
