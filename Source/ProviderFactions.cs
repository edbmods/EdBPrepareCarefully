using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderFactions {
        private List<FactionDef> nonPlayerHumanlikeFactionDefs = new List<FactionDef>();
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
                    nonPlayerHumanlikeFactionDefs.Add(def);
                }
            }
            nonPlayerHumanlikeFactionDefs.Sort((FactionDef a, FactionDef b) => {
                return a.defName.CompareTo(b.defName);
            });
        }
        public List<FactionDef> NonPlayerHumanlikeFactionDefs {
            get {
                return nonPlayerHumanlikeFactionDefs;
            }
        }
    }
}
