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
    public class ProviderFaceTattoos {
        protected Dictionary<ThingDef, List<TattooDef>> lookup = new Dictionary<ThingDef, List<TattooDef>>();
        protected List<TattooDef> humanlike;
        protected List<TattooDef> empty = new List<TattooDef>();
        public ProviderFaceTattoos() {
        }
        public ProviderAlienRaces ProviderAlienRaces {
            get; set;
        }
        public List<TattooDef> GetTattoos(ThingDef raceDef, Gender gender) {
            List<TattooDef> defs = GetTattoosForRace(raceDef);
            return defs;
        }
        public List<TattooDef> GetTattoosForRace(ThingDef raceDef) {
            if (lookup.TryGetValue(raceDef, out var defs)) {
                return defs;
            }
            defs = InitializeForRace(raceDef);
            if (defs == null) {
                if (raceDef != ThingDefOf.Human) {
                    return GetTattoosForRace(ThingDefOf.Human);
                }
                else {
                    return null;
                }
            }
            else {
                lookup.Add(raceDef, defs);
                return defs;
            }
        }
        // TODO: Handle tattoos for alien races
        protected List<TattooDef> InitializeForRace(ThingDef raceDef) {
            AlienRace alienRace = ProviderAlienRaces.GetAlienRace(raceDef);
            if (alienRace == null) {
                return Humanlike;
            }
            return Humanlike;
        }
        protected List<TattooDef> Humanlike {
            get {
                if (humanlike == null) {
                    humanlike = InitializeForHumanlike();
                }
                return humanlike;
            }
        }
        protected List<TattooDef> InitializeForHumanlike() {
            List<TattooDef> result = new List<TattooDef>();
            foreach (TattooDef TattooDef in DefDatabase<TattooDef>.AllDefs.Where(def => def.tattooType == TattooType.Face)) {
                result.Add(TattooDef);
            }
            return result;
        }
    }
}
