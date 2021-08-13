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
    public class ProviderBeards {
        protected Dictionary<ThingDef, List<BeardDef>> beardsLookup = new Dictionary<ThingDef, List<BeardDef>>();
        protected List<BeardDef> humanlikeBeards;
        protected List<BeardDef> noBeards = new List<BeardDef>();
        public ProviderBeards() {
        }
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public List<BeardDef> GetBeards(CustomPawn pawn) {
            return GetBeards(pawn.Pawn.def, pawn.Gender);
        }
        public List<BeardDef> GetBeards(ThingDef raceDef, Gender gender) {
            List<BeardDef> beards = GetBeardsForRace(raceDef);
            return beards;
        }
        public List<BeardDef> GetBeardsForRace(CustomPawn pawn) {
            return GetBeardsForRace(pawn.Pawn.def);
        }
        public List<BeardDef> GetBeardsForRace(ThingDef raceDef) {
            List<BeardDef> beards;
            if (beardsLookup.TryGetValue(raceDef, out beards)) {
                return beards;
            }
            beards = InitializeBeards(raceDef);
            if (beards == null) {
                if (raceDef != ThingDefOf.Human) {
                    return GetBeardsForRace(ThingDefOf.Human);
                }
                else {
                    return null;
                }
            }
            else {
                beardsLookup.Add(raceDef, beards);
                return beards;
            }
        }
        protected List<BeardDef> InitializeBeards(ThingDef raceDef) {
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(raceDef);
            if (alienRace == null) {
                return HumanlikeBeards;
            }
            if (!alienRace.HasBeards) {
                return noBeards;
            }
            return HumanlikeBeards;
        }
        protected List<BeardDef> HumanlikeBeards {
            get {
                if (humanlikeBeards == null) {
                    humanlikeBeards = InitializeHumanlikeBeards();
                }
                return humanlikeBeards;
            }
        }
        protected List<BeardDef> InitializeHumanlikeBeards() {
            List<BeardDef> result = new List<BeardDef>();
            foreach (BeardDef beardDef in DefDatabase<BeardDef>.AllDefs.Where((BeardDef def) => {
                return true;
            })) {
                result.Add(beardDef);
            }
            return result;
        }
    }
}
