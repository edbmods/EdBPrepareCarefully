using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ProviderPassions {
        public Type PassionManagerType { get; set; }
        public List<Texture2D> Icons { get; set; } = new List<Texture2D>();
        public List<Def> PassionDefs { get; set; } = new List<Def>();
        public List<int> NextValues { get; set; } = new List<int>() { 1, 2, 0 };
        public List<int> PreviousValues { get; set; } = new List<int>() { 2, 0, 1 };
        public int PassionCount { get; set; } = 3;
        public void PostConstruct() {
            InitializeExtendedPassions();
        }
        public bool UsingExtendedPassions {
            get {
                return PassionManagerType != null;
            }
        }
        public void InitializeExtendedPassions() {
            //Logger.Debug("InitializeExtendedPassions()");
            PassionManagerType = ReflectionUtil.TypeByName("VSE.Passions.PassionManager");
            if (PassionManagerType == null) {
                //Logger.Debug("   Didn't find VSE.Passions.PassionManager class");
                return;
            }
            //Logger.Debug("   Found VFE PassionManager class");
            Array passionDefArray = ReflectionUtil.GetStaticFieldValue<Array>(PassionManagerType, "Passions");
            if (passionDefArray == null) {
                Logger.Debug("   Didn't find PassionManager.Passions array");
                return;
            }
            //Logger.Debug("   Found VFE PassionManager.Passions array with " + passionDefArray.Length + " items");
            PassionCount = passionDefArray.Length;
            for (int i = 0; i < PassionCount; i++) {
                Texture2D icon = Textures.TexturePassionNone;
                Def passionDef = passionDefArray.GetValue(i) as Def;
                if (passionDef != null) {
                    if (passionDef.defName == "None") {
                        icon = Textures.TexturePassionNone;
                    }
                    else {
                        icon = ReflectionUtil.GetPropertyValue<Texture2D>(passionDef, "Icon");
                    }
                    if (icon != null) {
                        //Logger.Debug("   Found icon for passion " + i + ", " + passionDef.defName + ": " + (icon?.name ?? "no name"));
                    }
                    else {
                        //Logger.Debug("   Did not find icon for passion " + i + ", " + passionDef.defName);
                    }
                }
                else {
                    //Logger.Debug("   Did not find passion def for index " + i);
                }
                PassionDefs.Add(passionDef);
                Icons.Add(icon);
            }

            if (PassionCount == 6
                && PassionDefs[0]?.defName == "None"
                && PassionDefs[1]?.defName == "Minor"
                && PassionDefs[2]?.defName == "Major"
                && PassionDefs[3]?.defName == "VSE_Apathy"
                && PassionDefs[4]?.defName == "VSE_Natural"
                && PassionDefs[5]?.defName == "VSE_Critical") {
                NextValues = new List<int> {
                    1, 2, 5, 0, 3, 4
                };
                PreviousValues = new List<int> {
                    3, 0, 1, 4, 5, 2
                };
            }
            else {
                NextValues.Clear();
                for (int i=0; i<PassionDefs.Count - 1; i++) {
                    NextValues.Add(i+1);
                }
                NextValues.Add(0);
                PreviousValues.Clear();
                PreviousValues.Add(PassionDefs.Count - 1);
                for (int i = 1; i < PassionDefs.Count; i++) {
                    PreviousValues.Add(i-1);
                }
            }

        }
        public Texture2D TextureForPassion(Passion passion) {
            if (PassionManagerType == null) {
                if (passion == Passion.Minor) {
                    return Textures.TexturePassionMinor;
                }
                else if (passion == Passion.Major) {
                    return Textures.TexturePassionMajor;
                }
                else {
                    return Textures.TexturePassionNone;
                }
            }
            else {
                int value = (int)passion;
                if (value >= 0 && value < Icons.Count) {
                    return Icons[value];
                }
            }
            return Textures.TexturePassionNone;
        }

        public Passion NextPassionValue(Passion passion) {
            int value = (int)passion;
            if (value < 0 || value >= NextValues.Count) {
                return Passion.None;
            }
            return (Passion)NextValues[value];
        }

        public Passion PreviousPassionValue(Passion passion) {
            int value = (int)passion;
            if (value < 0 || value >= PreviousValues.Count) {
                return Passion.None;
            }
            return (Passion)PreviousValues[value];
        }

        public Passion MapFromString(string value) {
            if (UsingExtendedPassions && PassionDefs.CountAllowNull() > 0) {
                int index = PassionDefs.FindIndex(d => d.defName == value);
                if (index != -1) {
                    return (Passion)index;
                }
                else {
                    return Passion.None;
                }
            }
            else {
                Passion? passion = null;
                try {
                    passion = (Passion)Enum.Parse(typeof(Passion), value);
                }
                catch (Exception) { }
                if (passion.HasValue) {
                    return passion.Value;
                }
                if (value == "VSE_Critical" || value == "VSE_Natural") {
                    return Passion.Major;
                }
                return Passion.None;
            }
        }

        public string MapToString(Passion passion) {
            int index = (int)passion;
            if (UsingExtendedPassions && PassionDefs.CountAllowNull() > index) {
                return PassionDefs[index].defName;
            }
            else {
                return passion.ToString();
            }
        }
    }
}
