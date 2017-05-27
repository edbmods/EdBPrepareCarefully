using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderFactions {
        private List<FactionDef> nonPlayerHumanlikeFactionDefs = new List<FactionDef>();
        private Dictionary<FactionDef, Faction> factionLookup = new Dictionary<FactionDef, Faction>();
        private Dictionary<PawnKindDef, Faction> pawnKindFactionLookup = new Dictionary<PawnKindDef, Faction>();
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
        public Faction GetFaction(FactionDef def) {
            if (def == Faction.OfPlayer.def) {
                return Faction.OfPlayer;
            }
            else {
                Faction faction;
                if (!factionLookup.TryGetValue(def, out faction)) {
                    faction = CreateFaction(def);
                    factionLookup.Add(def, faction);
                }
                return faction;
            }
        }
        public Faction GetFaction(PawnKindDef def) {
            if (def.defaultFactionType != null) {
                return GetFaction(def.defaultFactionType);
            }
            else {
                return null;
            }
        }
        protected Faction CreateFaction(FactionDef def) {
            Faction faction = Faction.OfPlayer;
            if (def != Faction.OfPlayer.def) {
                faction = new Faction() {
                    def = def
                };
                FactionRelation rel = new FactionRelation();
                rel.other = Faction.OfPlayer;
                rel.goodwill = 50;
                rel.hostile = false;
                (typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(faction) as List<FactionRelation>).Add(rel);
            }
            return faction;
        }
    }
}
