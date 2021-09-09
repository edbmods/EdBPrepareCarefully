using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class ProviderTitles {
        private Dictionary<Faction, List<TitleSet>> titlesByFaction = new Dictionary<Faction, List<TitleSet>>();
        private List<Faction> factionsInOrder = new List<Faction>();
        private List<TitleSet> titleSetsInOrder = new List<TitleSet>();

        public class TitleSet {
            public Faction Faction { get; set; }
            public List<Title> Titles { get; set; }
            public int MaxFavor { get; set; } = 0;
        }

        public class Title {
            public Faction Faction { get; set; }
            public RoyalTitleDef Def { get; set; }
        }

        public IEnumerable<TitleSet> Titles {
            get => titleSetsInOrder;
        }

        public IEnumerable<Faction> FactionsWithTitles {
            get {
                foreach (var faction in Find.World.factionManager.AllFactionsInViewOrder) {
                    var def = faction.def;
                    if (def.HasRoyalTitles && !def.royalTitleTags.NullOrEmpty()) {
                         yield return faction;
                    }
                }
            }
        }

        public ProviderTitles() {
            Initialize();
        }

        public void Initialize() {

            // Go through all of the title definitions and organize them by tag.  A faction indicates which titles it supports
            // by specifying a list of title tags.
            Dictionary<string, List<RoyalTitleDef>> titlesByTag = new Dictionary<string, List<RoyalTitleDef>>();
            foreach (var def in DefDatabase<RoyalTitleDef>.AllDefs) {
                foreach (var tag in def.tags) {
                    titlesByTag.AddToListOfValues(tag, def);
                }
            }
            // Sort all of the titles by seniority in ascending order.
            foreach (var pair in titlesByTag) {
                pair.Value.Sort((a, b) => a.seniority.CompareTo(b.seniority));
            }

            // Create the title sets and store them in an ordered list and in a lookup where you can look up
            // title sets by faction.
            foreach (var faction in Find.World.factionManager.AllFactionsInViewOrder) {
                var factionDef = faction.def;
                if (factionDef.HasRoyalTitles && !factionDef.royalTitleTags.NullOrEmpty()) {
                    factionsInOrder.Add(faction);
                    foreach (var tag in factionDef.royalTitleTags) {
                        if (titlesByTag.TryGetValue(tag, out List<RoyalTitleDef> titles)) {
                            TitleSet set = new TitleSet() {
                                Faction = faction,
                                Titles = titles.Select(royalTitleDef => new Title() { Faction = faction, Def = royalTitleDef }).ToList()
                            };
                            foreach (var title in set.Titles) {
                                set.MaxFavor += title.Def.favorCost;
                            }
                            titleSetsInOrder.Add(set);
                            titlesByFaction.AddToListOfValues(faction, set);
                        }
                    }
                }
            }
        }
    }
}
