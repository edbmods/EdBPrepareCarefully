using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace EdB.PrepareCarefully {
    public class ProviderPawnKinds {
        HashSet<PawnKindDef> validPawnKindDefs = new HashSet<PawnKindDef>();
        List<PawnKindDef> pawnKindDefs = new List<PawnKindDef>();
        List<FactionPawnKinds> pawnKindsByFaction = new List<FactionPawnKinds>();
        List<PawnKindDef> otherPawnKinds = new List<PawnKindDef>();
        public Dictionary<FactionDef, List<PawnKindDef>> PawnKindByFaction { get; set; } = new Dictionary<FactionDef, List<PawnKindDef>>();
        private bool anyNonHumanPawnKinds = false;

        public IEnumerable<PawnKindDef> AllPawnKinds {
            get {
                return pawnKindDefs;
            }
        }

        public IEnumerable<PawnKindDef> PawnKindsWithNoFaction {
            get {
                return otherPawnKinds;
            }
        }

        public IEnumerable<FactionPawnKinds> PawnKindsByFaction {
            get {
                return pawnKindsByFaction;
            }
        }

        public bool AnyNonHumanPawnKinds {
            get {
                return anyNonHumanPawnKinds;
            }
        }

        public class FactionPawnKinds {
            public FactionDef Faction { get; set; }
            public List<PawnKindDef> PawnKinds { get; set; } = new List<PawnKindDef>();
        }

        public ProviderPawnKinds() {
            Dictionary<FactionDef, HashSet<PawnKindDef>> uniquePawnKindsByFaction = new Dictionary<FactionDef, HashSet<PawnKindDef>>();
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefs) {
                uniquePawnKindsByFaction.Add(factionDef, new HashSet<PawnKindDef>());
            }

            foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs) {
                //Logger.Debug(string.Format("pawnKindDef {0}, {1}", kindDef.defName, kindDef.LabelCap));
                if (kindDef == null) {
                    //Logger.Debug("Excluding PawnKindDef because it was null");
                    continue;
                }
                if (kindDef.race == null) {
                    Logger.Warning(string.Format("Excluding PawnKindDef because its race was unexpectedly null: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                if (kindDef.RaceProps == null) {
                    //Logger.Debug(string.Format("Excluding PawnKindDef because its RaceProps was null: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                if (!kindDef.race.CanHaveFaction) {
                    Logger.Debug(string.Format("Excluding PawnKindDef because its race cannot have a faction: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                // Exclude animals, mechanoids and other non-human pawn kinds.
                if (!kindDef.RaceProps.Humanlike) {
                    //Logger.Debug(string.Format("Excluding PawnKindDef because it's non-human: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                // Exclude mutants that cannot gain XP (i.e. Shamblers)
                if (!(kindDef.mutant?.canGainXP ?? true)) {
                    Logger.Debug(string.Format("Excluding mutant PawnKindDef because it cannot gain XP: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                // TODO: Revisit to see if we can add these in
                if (kindDef is CreepJoinerFormKindDef) {
                    Logger.Debug(string.Format("Excluding CreepJoinerFormKindDef: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                if (kindDef.defName == "WildMan") {
                    Logger.Debug(string.Format("Excluding WildMan: {0}, {1}", kindDef.defName, kindDef.LabelCap));
                    continue;
                }
                if (kindDef.LabelCap.ToString().NullOrEmpty()) {
                    continue;
                }
                if (kindDef.defaultFactionDef != null) {
                    if (uniquePawnKindsByFaction.ContainsKey(kindDef.defaultFactionDef)) {
                        uniquePawnKindsByFaction[kindDef.defaultFactionDef].Add(kindDef);
                    }
                }
                else {
                    otherPawnKinds.Add(kindDef);
                }
                pawnKindDefs.Add(kindDef);
                validPawnKindDefs.Add(kindDef);

                if (kindDef?.race?.defName != "Human") {
                    anyNonHumanPawnKinds = true;
                }
            }

            // Go through all of the factions and build out the list of faction defs => pawn kind lists
            // to be included as options when adding a new pawn
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefs) {
                HashSet<PawnKindDef> pawnKindDeduper = new HashSet<PawnKindDef>();
                List<PawnKindDef> pawnKinds = new List<PawnKindDef>();
                if (factionDef.pawnGroupMakers == null) {
                    continue;
                }
                // Include all of the pawn kinds listed in the pawn group makers
                foreach (var pawnGroupMaker in factionDef.pawnGroupMakers) {
                    foreach (var option in pawnGroupMaker.options) {
                        var pawnKindDef = option.kind;
                        if (pawnKindDef == null) {
                            continue;
                        }
                        if (pawnKindDef.LabelCap == null) {
                            continue;
                        }
                        if (pawnKindDef != null && !pawnKindDeduper.Contains(pawnKindDef) && validPawnKindDefs.Contains(pawnKindDef)) {
                            pawnKinds.Add(pawnKindDef);
                            pawnKindDeduper.Add(pawnKindDef);
                        }
                    }
                }
                // Include all of the pawn kinds that have this faction def listed as its default
                if (uniquePawnKindsByFaction.TryGetValue(factionDef, out var uniquePawnKindsForFaction)) {
                    foreach (var pawnKindDef in uniquePawnKindsForFaction) {
                        if (pawnKindDef != null && !pawnKindDeduper.Contains(pawnKindDef) && validPawnKindDefs.Contains(pawnKindDef)) {
                            pawnKinds.Add(pawnKindDef);
                            pawnKindDeduper.Add(pawnKindDef);
                        }
                    }
                }
                if (!pawnKinds.Empty()) {
                    pawnKinds.Sort((a, b) => {
                        return string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString());
                    });
                    if (!factionDef.LabelCap.ToString().NullOrEmpty()) {
                        pawnKindsByFaction.Add(new FactionPawnKinds() {
                            Faction = factionDef,
                            PawnKinds = pawnKinds
                        });
                    }
                }
            }

            pawnKindsByFaction.Sort((a, b) => {
                return string.Compare(a.Faction?.LabelCap.ToString(), b.Faction?.LabelCap.ToString());
            });
            otherPawnKinds.Sort((a, b) => {
                return string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString());
            });

        }
    }
}
