using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class ProviderPawnKinds {
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
                //Logger.Debug("pawnKindDef {0}, {1}", kindDef.defName, kindDef.LabelCap);
                if (kindDef == null) {
                    //Logger.Debug("Excluding pawnKindDef because it was null");
                    continue;
                }
                if (kindDef.RaceProps == null) {
                    //Logger.Debug("Excluding pawnKindDef because its RaceProps was null {0}, {1}", kindDef.defName, kindDef.LabelCap);
                    continue;
                }
                // Exclude animals, mechanoids and other non-human pawn kinds.
                if (!kindDef.RaceProps.Humanlike) {
                    //Logger.Debug("Excluding pawnKindDef because it's non-human {0}, {1}", kindDef.defName, kindDef.LabelCap);
                    continue;
                }
                // TODO: Revisit to see if we can add these in
                if (kindDef is CreepJoinerFormKindDef) {
                    continue;
                }
                if (kindDef.LabelCap.ToString().NullOrEmpty()) {
                    continue;
                }
                if (kindDef.defaultFactionType != null) {
                    if (uniquePawnKindsByFaction.ContainsKey(kindDef.defaultFactionType)) {
                        uniquePawnKindsByFaction[kindDef.defaultFactionType].Add(kindDef);
                    }
                }
                else {
                    otherPawnKinds.Add(kindDef);
                }
                pawnKindDefs.Add(kindDef);

                if (kindDef?.race?.defName != "Human") {
                    anyNonHumanPawnKinds = true;
                }
            }

            foreach (var pair in uniquePawnKindsByFaction) {
                var faction = pair.Key;
                var pawnKinds = new List<PawnKindDef>(pair.Value);
                //Logger.Debug("Sorting unique pawns kinds by faction: " + faction?.defName);
                pawnKinds.Sort((a, b) => {
                    return string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString());
                });
                if (!faction.LabelCap.ToString().NullOrEmpty()) {
                    pawnKindsByFaction.Add(new FactionPawnKinds() {
                        Faction = faction,
                        PawnKinds = pawnKinds
                    });
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
