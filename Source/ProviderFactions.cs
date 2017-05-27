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
        private List<PawnKindDef> nonPlayerPawnKinds = new List<PawnKindDef>();
        private List<PawnKindDef> playerPawnKinds = new List<PawnKindDef>();
        private Dictionary<FactionDef, List<PawnKindDef>> factionDefPawnKindLookup = new Dictionary<FactionDef, List<PawnKindDef>>();
        public ProviderFactions() {
            foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs) {
                // Exclude animals, mechanoids and other non-human pawn kinds.
                if (!kindDef.RaceProps.Humanlike) {
                    continue;
                }
                // Exclude any pawn kind that has no faction.
                Faction faction = GetFaction(kindDef);
                if (faction == null) {
                    continue;
                }
                // Double-check that it's a humanlike pawn by looking at the value on the faction.
                if (!faction.def.humanlikeFaction) {
                    continue;
                }
                // Sort the pawn kind into player and non-player buckets.
                if (faction.IsPlayer) {
                    playerPawnKinds.Add(kindDef);
                }
                else {
                    nonPlayerPawnKinds.Add(kindDef);
                }
                // Create a lookup of where you can get the list of pawn kinds given a faction def.
                // If no valid pawn kinds exist for a faction def, this lookup will have no faction def
                // key.
                List<PawnKindDef> factionDefPawnKinds;
                if (!factionDefPawnKindLookup.TryGetValue(faction.def, out factionDefPawnKinds)) {
                    factionDefPawnKinds = new List<PawnKindDef>();
                    factionDefPawnKindLookup.Add(faction.def, factionDefPawnKinds);
                }
                factionDefPawnKinds.Add(kindDef);
            }

            HashSet<string> labels = new HashSet<string>();
            foreach (var def in DefDatabase<FactionDef>.AllDefs) {
                if (!def.humanlikeFaction) {
                    continue;
                }
                if (def.isPlayer || def == Faction.OfPlayer.def) {
                    continue;
                }
                List<PawnKindDef> factionKindDefs;
                if (factionDefPawnKindLookup.TryGetValue(def, out factionKindDefs)) {
                    if (factionKindDefs.Count > 0) {
                        if (!labels.Contains(def.label)) {
                            labels.Add(def.label);
                            nonPlayerHumanlikeFactionDefs.Add(def);
                        }
                    }
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
        public IEnumerable<PawnKindDef> NonPlayerPawnKinds {
            get {
                return nonPlayerPawnKinds;
            }
        }
        public IEnumerable<PawnKindDef> PlayerPawnKinds {
            get {
                return playerPawnKinds;
            }
        }
        public IEnumerable<PawnKindDef> GetPawnKindsForFactionDef(FactionDef def) {
            List<PawnKindDef> factionDefPawnKinds;
            if (factionDefPawnKindLookup.TryGetValue(def, out factionDefPawnKinds)) {
                return factionDefPawnKinds;
            }
            else {
                return null;
            }
        }
        public IEnumerable<PawnKindDef> GetPawnKindsForFactionDefLabel(FactionDef def) {
            var keys = factionDefPawnKindLookup.Keys.Where((FactionDef f) => { return def.LabelCap == f.LabelCap; });
            IEnumerable<PawnKindDef> result = null;
            if (keys != null) {
                foreach (var key in keys) {
                    var list = factionDefPawnKindLookup[key];
                    if (list == null) {
                        continue;
                    }
                    if (result == null) {
                        result = list;
                    }
                    else {
                        result = result.Concat(list);
                    }
                }
            }
            if (result == null) {
                return Enumerable.Empty<PawnKindDef>();
            }
            return result;
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
