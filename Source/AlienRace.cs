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
    public class AlienRace {
        public AlienRace() {
        }
        public bool UseMelaninLevels {
            get; set;
        }
        public bool HasSecondaryColor {
            get; set;
        }
        public List<Color> PrimaryColors {
            get; set;
        }
        public List<Color> SecondaryColors {
            get; set;
        }
        public List<Color> HairColors {
            get; set;
        }
        public List<BodyType> BodyTypes {
            get; set;
        }
        public List<string> CrownTypes {
            get; set;
        }
        public bool GenderSpecificHeads {
            get; set;
        }
        public string GraphicsPathForHeads {
            get; set;
        }
        public bool HasHair {
            get; set;
        }
        public HashSet<string> HairTags {
            get; set;
        }
        public bool RestrictedApparelOnly {
            get; set;
        }
        public HashSet<string> RestrictedApparel {
            get; set;
        }
    }
}
