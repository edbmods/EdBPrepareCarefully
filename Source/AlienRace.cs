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
        public ThingDef ThingDef { get; set; }
        public bool UseMelaninLevels { get; set; }
        public bool HasSecondaryColor { get; set; }
        public bool ChangeableColor { get; set; }
        public List<Color> PrimaryColors { get; set; }
        public List<Color> SecondaryColors { get; set; }
        public List<Color> HairColors { get; set; }
        public List<BodyTypeDef> BodyTypes { get; set; }
        public List<HeadTypeDef> HeadTypes { get; set; }
        public string GraphicsPathForHeads { get; set; }
        public string GraphicsPathForBodyTypes { get; set; }
        public bool HasHair { get; set; }
        public bool HasBeards { get; set; }
        public bool HasTattoos { get; set; }
        public HashSet<string> HairTags { get; set; }
        public bool RaceSpecificApparelOnly { get; set; }
        public HashSet<string> RaceSpecificApparel { get; set; }
        public HashSet<string> AllowedApparel { get; set; }
        public HashSet<string> DisallowedApparel { get; set; }
        public float MinAgeForAdulthood { get; set; }
        public List<AlienRaceBodyAddon> Addons { get; set; } = new List<AlienRaceBodyAddon>();
    }
}
