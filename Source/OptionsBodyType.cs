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
    public class OptionsBodyType {
        protected List<BodyTypeDef> maleBodyTypes = new List<BodyTypeDef>();
        protected List<BodyTypeDef> femaleBodyTypes = new List<BodyTypeDef>();
        protected List<BodyTypeDef> noGenderBodyTypes = new List<BodyTypeDef>();
        public OptionsBodyType() {
        }
        public List<BodyTypeDef> MaleBodyTypes {
            get {
                return maleBodyTypes;
            }
            set {
                maleBodyTypes = value;
            }
        }
        public List<BodyTypeDef> FemaleBodyTypes {
            get {
                return femaleBodyTypes;
            }
            set {
                femaleBodyTypes = value;
            }
        }
        public List<BodyTypeDef> NoGenderBodyTypes {
            get {
                return noGenderBodyTypes;
            }
            set {
                noGenderBodyTypes = value;
            }
        }
        public List<BodyTypeDef> GetBodyTypes(Gender gender) {
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
