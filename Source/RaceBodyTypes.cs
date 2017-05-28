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
    public class RaceBodyTypes {
        protected List<BodyType> maleBodyTypes = new List<BodyType>();
        protected List<BodyType> femaleBodyTypes = new List<BodyType>();
        protected List<BodyType> noGenderBodyTypes = new List<BodyType>();
        public RaceBodyTypes() {
        }
        public List<BodyType> MaleBodyTypes {
            get {
                return maleBodyTypes;
            }
            set {
                maleBodyTypes = value;
            }
        }
        public List<BodyType> FemaleBodyTypes {
            get {
                return femaleBodyTypes;
            }
            set {
                femaleBodyTypes = value;
            }
        }
        public List<BodyType> NoGenderBodyTypes {
            get {
                return noGenderBodyTypes;
            }
            set {
                noGenderBodyTypes = value;
            }
        }
        public List<BodyType> GetBodyTypes(Gender gender) {
            if (gender == Gender.Male) {
                return maleBodyTypes;
            }
            else if (gender == Gender.Female) {
                return femaleBodyTypes;
            }
            else {
                return noGenderBodyTypes;
            }
        }
    }
}
