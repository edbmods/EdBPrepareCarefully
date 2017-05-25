using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderFactions {
        private List<FactionDef> factions = new List<FactionDef>();
        public ProviderFactions() {
            HashSet<string> labels = new HashSet<string>();
            foreach (var def in DefDatabase<FactionDef>.AllDefs) {
                if (!def.humanlikeFaction) {
                    continue;
                }
                if (def.isPlayer || def == Faction.OfPlayer.def) {
                    continue;
                }
                if (!labels.Contains(def.label)) {
                    labels.Add(def.label);
                    factions.Add(def);
                }
            }
            factions.Sort((FactionDef a, FactionDef b) => {
                return a.defName.CompareTo(b.defName);
            });
        }
        public List<FactionDef> Factions {
            get {
                return factions;
            }
        }
    }
}
