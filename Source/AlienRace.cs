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
    }
}
