using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderFactions {
        private Dictionary<FactionDef, Faction> factionLookup = new Dictionary<FactionDef, Faction>();
        private List<CustomFaction> customFactions = new List<CustomFaction>();
        private Dictionary<FactionDef, int> factionCounts = new Dictionary<FactionDef, int>();
        private HashSet<FactionDef> eligibleFactionLookup = new HashSet<FactionDef>();
        private Dictionary<Pair<FactionDef, int>, CustomFaction> factionIndexLookup = new Dictionary<Pair<FactionDef, int>, CustomFaction>();
        private CustomFaction randomFaction;

        public List<FactionDef> HumanlikeFactionDefs { get; set; } = new List<FactionDef>();
        public List<FactionDef> NonPlayerHumanlikeFactionDefs { get; set; } = new List<FactionDef>();
        public List<FactionDef> FactionDefsOnWorld { get; set; } = new List<FactionDef>();

        public ProviderFactions() {
            foreach (var kindDef in DefDatabase<PawnKindDef>.AllDefs) {
                //Logger.Debug("pawnKindDef {0}, {1}", kindDef.defName, kindDef.LabelCap);
                if (kindDef == null) {
                    continue;
                }
                try {
                    // Exclude animals, mechanoids and other non-human pawn kinds.
                    if (kindDef.race == null || kindDef.RaceProps == null || !kindDef.RaceProps.Humanlike) {
                        //Logger.Debug(string.Format("Skipping pawnKindDef because it's non-human: {0}, {1}", kindDef?.defName, kindDef?.LabelCap));
                        continue;
                    }
                    if (!kindDef.race.CanHaveFaction) {
                        Logger.Debug(string.Format("Skipping pawnKindDef because it cannot have a faction: {0}, {1}", kindDef?.defName, kindDef?.LabelCap));
                        continue;
                    }
                    // Exclude any pawn kind that has no faction.
                    Faction faction = GetFaction(kindDef);
                    if (faction == null) {
                        Logger.Debug(string.Format("Skipping pawnKindDef because it has no faction: {0}, {1}", kindDef?.defName, kindDef?.LabelCap));
                        continue;
                    }
                    // Double-check that it's a humanlike pawn by looking at the value on the faction.
                    if (!faction.def.humanlikeFaction) {
                        //Logger.Debug(string.Format("Skipping pawnKindDef because its faction is not humanlike: {0}, {1}", kindDef?.defName, kindDef?.LabelCap));
                        continue;
                    }
                }
                catch (Exception) {
                    Logger.Warning("Failed to get a list of factions from a pawnKindDef: " + kindDef.defName);
                }
            }

            // Classify each faction definition as either humanlike or not
            HumanlikeFactionDefs = new List<FactionDef>(DefDatabase<FactionDef>.AllDefs.Where(def => def.humanlikeFaction));

            // Classify each humanlike faction definition as either a player faction or not
            NonPlayerHumanlikeFactionDefs = new List<FactionDef>(HumanlikeFactionDefs.Where(def => !def.isPlayer));

            // Determine all of the faction defs that are for factions on the world
            FactionDefsOnWorld = new List<FactionDef>(Find.World.factionManager.AllFactionsInViewOrder.Select(f => f.def).Where(def => def.humanlikeFaction && !def.isPlayer));

            InitializeCustomFactions();
        }

        private void InitializeCustomFactions() {
            // Add the random faction.
            randomFaction = new CustomFaction();
            customFactions.Add(randomFaction);

            // Create a lookup with all eligible faction defs.
            foreach (var def in NonPlayerHumanlikeFactionDefs) {
                eligibleFactionLookup.Add(def);
            }
            // Add the random custom faction for each faction def on the world
            foreach (var def in FactionDefsOnWorld) {
                CustomFaction faction = new CustomFaction() {
                    Def = def
                };
                customFactions.Add(faction);
            }
            // Go through all of the individual factions to create the specific custom factions and the corresponding leader options.
            // While doing it, build up the data structures to keep track of how many of each faction def there are.
            foreach (var faction in Find.World.factionManager.AllFactions) {
                // Update the number of each faction def in a lookup.
                int currentIndex = 0;
                if (factionCounts.ContainsKey(faction.def)) {
                    int count = factionCounts[faction.def];
                    count++;
                    factionCounts[faction.def] = count;
                    currentIndex = count - 1;
                }
                else {
                    factionCounts[faction.def] = 1;
                    currentIndex = 0;
                }
                // Create the custom faction for each faction if it's an eligible faction def.
                if (eligibleFactionLookup.Contains(faction.def)) {
                    CustomFaction customFaction = new CustomFaction();
                    customFaction.Def = faction.def;
                    customFaction.Index = currentIndex;
                    customFaction.Name = faction.Name;
                    customFaction.Faction = faction;
                    customFaction.Leader = false;
                    customFactions.Add(customFaction);
                    
                    if (faction.leader != null) {
                        // Create the leader version of the faction
                        CustomFaction leaderFaction = new CustomFaction();
                        leaderFaction.Def = faction.def;
                        leaderFaction.Index = currentIndex;
                        leaderFaction.Name = faction.Name;
                        leaderFaction.Faction = faction;
                        leaderFaction.Leader = true;
                        customFactions.Add(leaderFaction);
                    }
                }
            }
            // Go through all of the specific custom factions and add it to a lookup so that we can find it
            // based on an index/faction def pair.
            foreach (var faction in customFactions) {
                if (faction.Faction != null && faction.Index != null && faction.Leader == false) {
                    factionIndexLookup.Add(new Pair<FactionDef, int>(faction.Def, faction.Index.Value), faction);
                }
            }
            // Go through all of the specific custom factions and set on it the total number of factions of the same def.
            foreach (var faction in customFactions) {
                if (faction.Faction != null && faction.Index != null && faction.Leader == false) {
                    int result;
                    if (factionCounts.TryGetValue(faction.Def, out result)) {
                        faction.SimilarFactionCount = result;
                    }
                    else {
                        result = 0;
                    }
                }
            }
            // Go through all of the factions and set their names.
            foreach (var faction in customFactions) {
                if (faction.Faction != null && faction.Index != null) {
                    faction.Name = faction.Name.CapitalizeFirst();
                    if (faction.Faction.Name.ToLower() == faction.Def.label.ToLower()) {
                        if (faction.SimilarFactionCount > 1) {
                            faction.Name = "EdB.PC.Dialog.Faction.NumberedFaction".Translate(faction.Def.LabelCap, (faction.Index.Value + 1));
                        }
                    }
                    if (faction.Leader) {
                        faction.Name = "EdB.PC.Dialog.Faction.LeaderFaction".Translate(faction.Name);
                    }
                }
                else if (faction.Def != null) {
                    faction.Name = "EdB.PC.Dialog.Faction.RandomFaction".Translate(faction.Def.LabelCap);
                }
                else {
                    faction.Name = "EdB.PC.Dialog.Faction.Random".Translate();
                }
            }
        }
        public Faction FindRandomFactionForDef(FactionDef def) {
            if (def == null) {
                return null;
            }
            var list = Find.FactionManager.AllFactionsListForReading;
            if (list == null) {
                return null;
            }
            return list.Where(f => f.def == def).RandomElementWithFallback(null);
        }


        public Faction GetFaction(FactionDef def) {
            if (def == null) {
                return null;
            }
            if (def == Faction.OfPlayer?.def) {
                return Faction.OfPlayer;
            }
            else {
                Faction faction;
                // If we don't find a faction (which means it's not in the world), then create one
                if (!factionLookup.TryGetValue(def, out faction)) {
                    faction = CreateFaction(def);
                    if (faction != null) {
                        factionLookup.Add(def, faction);
                    }
                }
                return faction;
            }
        }
        public Faction GetFaction(PawnKindDef def) {
            if (def == null) {
                return null;
            }
            if (def.defaultFactionDef != null) {
                return GetFaction(def.defaultFactionDef);
            }
            else {
                return null;
            }
        }
        public List<Faction> GetFactions(FactionDef def) {
            return Find.World.factionManager.AllFactions.Where((Faction faction) => { return faction.def == def; }).ToList();
        }

        // We're doing this is solve some problem with backstories or something?
        // You cannot create a pawn with one of these factions.  It will throw a bunch of errors.
        protected Faction CreateFaction(FactionDef def) {
            Logger.Debug("Creating faction: " + def?.defName);
            try {
                Faction faction = Faction.OfPlayer;
                if (def != Faction.OfPlayer?.def) {
                    faction = new Faction() {
                        def = def
                    };
                    FactionRelation rel = new FactionRelation();
                    rel.other = Faction.OfPlayer;
                    rel.baseGoodwill = 50;
                    (typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(faction) as List<FactionRelation>).Add(rel);
                }
                return faction;
            }
            catch (Exception e) {
                Logger.Warning("Failed to create faction for factionDef = " + def?.defName, e);
                return null;
            }
        }
    }
}
