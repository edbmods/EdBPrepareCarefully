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
    public class ProviderApparel {
        protected Dictionary<ThingDef, OptionsApparel> apparelLookup = new Dictionary<ThingDef, OptionsApparel>();
        protected OptionsApparel humanlikeApparel;
        protected OptionsApparel noApparel = new OptionsApparel();
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public List<ThingDef> GetApparel(CustomPawn pawn, PawnLayer layer) {
            return GetApparel(pawn.Pawn.def, layer);
        }
        public List<ThingDef> GetApparel(ThingDef raceDef, PawnLayer layer) {
            OptionsApparel apparel = GetApparelForRace(raceDef);
            return apparel.GetApparel(layer);
        }
        public OptionsApparel GetApparelForRace(CustomPawn pawn) {
            return GetApparelForRace(pawn.Pawn.def);
        }
        public OptionsApparel GetApparelForRace(ThingDef raceDef) {
            OptionsApparel apparel;
            if (apparelLookup.TryGetValue(raceDef, out apparel)) {
                return apparel;
            }
            apparel = InitializeApparel(raceDef);
            if (apparel == null) {
                if (raceDef != ThingDefOf.Human) {
                    return GetApparelForRace(ThingDefOf.Human);
                }
                else {
                    return null;
                }
            }
            else {
                apparelLookup.Add(raceDef, apparel);
                return apparel;
            }
        }
        protected PawnLayer LayerForApparel(ThingDef def) {
            if (def.apparel == null) {
                return null;
            }
            else {
                return PrepareCarefully.Instance.Providers.PawnLayers.FindLayerForApparel(def);
            }
        }
        protected void AddApparelToOptions(OptionsApparel options, string defName) {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def != null) {
                AddApparelToOptions(options, def);
            }
        }
        protected void AddApparelToOptions(OptionsApparel options, ThingDef def) {
            PawnLayer layer = LayerForApparel(def);
            if (layer != null) {
                options.Add(layer, def);
            }
        }
        protected OptionsApparel InitializeApparel(ThingDef raceDef) {
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(raceDef);
            if (alienRace == null) {
                return HumanlikeApparel;
            }
            OptionsApparel result = new OptionsApparel();
            HashSet<string> addedAlready = new HashSet<string>();
            // Add all race-specific apparel.
            foreach (var a in alienRace.RaceSpecificApparel ?? Enumerable.Empty<string>()) {
                if (!addedAlready.Contains(a)) {
                    AddApparelToOptions(result, a);
                    addedAlready.Add(a);
                }
            }
            // Even if we're only allowed to use race-specific apparel, we're also allowed to use anything in the allowed list.
            if (alienRace.RaceSpecificApparelOnly) {
                HashSet<string> allowed = alienRace.AllowedApparel ?? new HashSet<string>();
                foreach (var def in HumanlikeApparel.AllApparel ?? Enumerable.Empty<ThingDef>()) {
                    if (allowed.Contains(def.defName) && !addedAlready.Contains(def.defName)) {
                        AddApparelToOptions(result, def);
                        addedAlready.Add(def.defName);
                    }
                }
            }
            // Even if we're allowed to use more than just race-specific apparel, we can't use anything in the disallowed list.
            else {
                HashSet<string> disallowed = alienRace.DisallowedApparel ?? new HashSet<string>();
                foreach (var def in HumanlikeApparel.AllApparel ?? Enumerable.Empty<ThingDef>()) {
                    if (!addedAlready.Contains(def.defName) && !disallowed.Contains(def.defName)) {
                        AddApparelToOptions(result, def);
                        addedAlready.Add(def.defName);
                    }
                }
            }
            result.Sort();
            return result;
        }
        protected OptionsApparel HumanlikeApparel {
            get {
                if (humanlikeApparel == null) {
                    humanlikeApparel = InitializeHumanlikeApparel();
                }
                return humanlikeApparel;
            }
        }
        protected OptionsApparel InitializeHumanlikeApparel() {
            HashSet<string> nonHumanApparel = new HashSet<string>();
            IEnumerable<ThingDef> alienRaces = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => {
                return def.race != null && ProviderAlienRaces.IsAlienRace(def);
            });
            foreach (var alienRaceDef in alienRaces) {
                AlienRace alienRace = AlienRaceProvider.GetAlienRace(alienRaceDef);
                if (alienRace == null) {
                    continue;
                }
                if (alienRace?.ThingDef?.defName == "Human") {
                    continue;
                }
                if (alienRace?.AllowedApparel != null) {
                    foreach (var defName in alienRace.RaceSpecificApparel) {
                        nonHumanApparel.Add(defName);
                    }
                }
            }
            OptionsApparel result = new OptionsApparel();
            foreach (ThingDef apparelDef in DefDatabase<ThingDef>.AllDefs) {
                if (apparelDef.apparel == null) {
                    continue;
                }
                if (!nonHumanApparel.Contains(apparelDef.defName)) {
                    AddApparelToOptions(result, apparelDef);
                }
            }
            result.Sort();
            return result;
        }
    }
}
