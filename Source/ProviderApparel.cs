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
                return PrepareCarefully.Instance.Providers.PawnLayers.FindLayerForApparel(def.apparel);
            }
        }
        protected void AddApparel(OptionsApparel options, string defName) {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def != null) {
                AddApparel(options, def);
            }
        }
        protected void AddApparel(OptionsApparel options, ThingDef def) {
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
            // If the alien race does not have a restricted set of apparel, then we'll re-use the
            // humanlike apparel options.
            if (!alienRace.RestrictedApparelOnly && alienRace.RestrictedApparel == null) {
                return HumanlikeApparel;
            }
            OptionsApparel result = new OptionsApparel();
            HashSet<string> addedAlready = new HashSet<string>();
            if (alienRace.RestrictedApparel != null) {
                foreach (var defName in alienRace.RestrictedApparel) {
                    AddApparel(result, defName);
                    addedAlready.Add(defName);
                }
            }
            if (!alienRace.RestrictedApparelOnly) {
                OptionsApparel humanApparel = HumanlikeApparel;
                foreach (var def in humanApparel.AllApparel) {
                    if (!addedAlready.Contains(def.defName)) {
                        AddApparel(result, def);
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
                if (alienRace.RestrictedApparel != null) {
                    foreach (var defName in alienRace.RestrictedApparel) {
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
                    AddApparel(result, apparelDef);
                }
            }
            result.Sort();
            return result;
        }
    }
}
