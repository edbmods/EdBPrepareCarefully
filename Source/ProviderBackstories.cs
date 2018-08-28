using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderBackstories {
        protected List<Backstory> childhoodBackstories = new List<Backstory>();
        protected List<Backstory> adulthoodBackstories = new List<Backstory>();
        protected List<Backstory> sortedChildhoodBackstories;
        protected List<Backstory> sortedAdulthoodBackstories;

        protected Dictionary<string, List<Backstory>> childhoodBackstoryLookup = new Dictionary<string, List<Backstory>>();
        protected Dictionary<string, List<Backstory>> adulthoodBackstoryLookup = new Dictionary<string, List<Backstory>>();

        public List<Backstory> GetChildhoodBackstoriesForPawn(CustomPawn pawn) {
            if (!childhoodBackstoryLookup.ContainsKey(pawn.Pawn.kindDef.defName)) {
                InitializeBackstoriesForPawnKind(pawn.Pawn.kindDef);
            }
            return childhoodBackstoryLookup[pawn.Pawn.kindDef.defName];
        }
        public List<Backstory> GetAdulthoodBackstoriesForPawn(CustomPawn pawn) {
            if (!adulthoodBackstoryLookup.ContainsKey(pawn.Pawn.kindDef.defName)) {
                InitializeBackstoriesForPawnKind(pawn.Pawn.kindDef);
            }
            return adulthoodBackstoryLookup[pawn.Pawn.kindDef.defName];
        }
        public ProviderBackstories() {
            // Go through all of the backstories and mark them as childhood or adult.
            List<Backstory> backstories = BackstoryDatabase.allBackstories.Values.ToList();
            foreach (Backstory backstory in backstories) {
                if (backstory.slot == BackstorySlot.Childhood) {
                    childhoodBackstories.Add(backstory);
                }
                else {
                    adulthoodBackstories.Add(backstory);
                }
            }

            // Create sorted versions of the backstory lists
            sortedChildhoodBackstories = new List<Backstory>(childhoodBackstories);
            sortedChildhoodBackstories.Sort((b1, b2) => b1.TitleCapFor(Gender.Male).CompareTo(b2.TitleCapFor(Gender.Male)));
            sortedAdulthoodBackstories = new List<Backstory>(adulthoodBackstories);
            sortedAdulthoodBackstories.Sort((b1, b2) => b1.TitleCapFor(Gender.Male).CompareTo(b2.TitleCapFor(Gender.Male)));
        }

        private void InitializeBackstoriesForPawnKind(PawnKindDef def) {

            HashSet<string> pawnKindBackstoryCategories = new HashSet<string>(def.backstoryCategories);
            List<Backstory> childhood = BackstoryDatabase.allBackstories.Values.Where((b) => {
                if (b.slot != BackstorySlot.Childhood) {
                    return false;
                }
                if (def.backstoryCategories == null || def.backstoryCategories.Count == 0) {
                    return true;
                }
                foreach (var c in b.spawnCategories) {
                    if (pawnKindBackstoryCategories.Contains(c)) {
                        return true;
                    }
                }
                return false;
            }).ToList();
            childhood.Sort((b1, b2) => b1.TitleCapFor(Gender.Male).CompareTo(b2.TitleCapFor(Gender.Male)));
            childhoodBackstoryLookup[def.defName] = childhood;

            List<Backstory> adulthood = BackstoryDatabase.allBackstories.Values.Where((b) => {
                if (b.slot != BackstorySlot.Adulthood) {
                    return false;
                }
                if (def.backstoryCategories == null || def.backstoryCategories.Count == 0) {
                    return true;
                }
                foreach (var c in b.spawnCategories) {
                    if (pawnKindBackstoryCategories.Contains(c)) {
                        return true;
                    }
                }
                return false;
            }).ToList();
            adulthood.Sort((b1, b2) => b1.TitleCapFor(Gender.Male).CompareTo(b2.TitleCapFor(Gender.Male)));
            adulthoodBackstoryLookup[def.defName] = adulthood;
        }
    }
}
