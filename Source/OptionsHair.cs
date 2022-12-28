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
    public class OptionsHair {
        protected List<HairDef> maleHairs = new List<HairDef>();
        protected List<HairDef> femaleHairs = new List<HairDef>();
        protected List<HairDef> noGenderHairs = new List<HairDef>();
        protected List<Color> hairColors = new List<Color>();
        public List<HairDef> MaleHairs {
            get {
                return maleHairs;
            }
            set {
                maleHairs = value;
            }
        }
        public List<HairDef> FemaleHairs {
            get {
                return femaleHairs;
            }
            set {
                femaleHairs = value;
            }
        }
        public List<HairDef> NoGenderHairs {
            get {
                return noGenderHairs;
            }
            set {
                noGenderHairs = value;
            }
        }
        public void AddHair(HairDef def) {
            if (def.styleGender == StyleGender.Male) {
                maleHairs.Add(def);
            }
            else if (def.styleGender == StyleGender.Female) {
                femaleHairs.Add(def);
            }
            else {
                maleHairs.Add(def);
                femaleHairs.Add(def);
                noGenderHairs.Add(def);
            }
        }
        public List<HairDef> GetHairs(Gender gender) {
            if (gender == Gender.Male) {
                return maleHairs;
            }
            else if (gender == Gender.Female) {
                return femaleHairs;
            }
            else {
                return noGenderHairs;
            }
        }
        public List<Color> Colors {
            get {
                return hairColors;
            }
            set {
                hairColors = value;
            }
        }
        public void Sort() {
            Comparison<HairDef> sorter = (HairDef x, HairDef y) => {
                if (x.label == null) {
                    return -1;
                }
                else if (y.label == null) {
                    return 1;
                }
                else {
                    return string.Compare(x.label, y.label);
                }
            };
            maleHairs.Sort(sorter);
            femaleHairs.Sort(sorter);
            noGenderHairs.Sort(sorter);
        }
    }
}
