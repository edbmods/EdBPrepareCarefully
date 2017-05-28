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
        protected List<BodyType> bodyTypes = null;
        public AlienRace() {
        }
        public List<BodyType> BodyTypes {
            get {
                return bodyTypes;
            }
            set {
                bodyTypes = value;
            }
        }
    }
}
