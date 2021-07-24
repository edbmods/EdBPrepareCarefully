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
    public class ProviderBodyTattoos {
        protected Dictionary<ThingDef, List<TattooDef>> lookup = new Dictionary<ThingDef, List<TattooDef>>();
        protected List<TattooDef> humanlike;
        protected List<TattooDef> empty = new List<TattooDef>();
        public ProviderBodyTattoos() {
        }
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public List<TattooDef> GetTattoos(CustomPawn pawn) {
            return GetTattoos(pawn.Pawn.def, pawn.Gender);
        }
        public List<TattooDef> GetTattoos(ThingDef raceDef, Gender gender) {
            List<TattooDef> defs = GetTattoosForRace(raceDef);
            return defs;
        }
        public List<TattooDef> GetTattoosForRace(CustomPawn pawn) {
            return GetTattoosForRace(pawn.Pawn.def);
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
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(raceDef);
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
            return DefDatabase<TattooDef>.AllDefs.Where(def => def.tattooType == TattooType.Body).ToList();
        }
    }
}
