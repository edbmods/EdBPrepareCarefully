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

        public List<Backstory> ChildhoodBackstories {
            get {
                return sortedChildhoodBackstories;
            }
        }
        public List<Backstory> AdulthoodBackstories {
            get {
                return sortedAdulthoodBackstories;
            }
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
            sortedChildhoodBackstories.Sort((b1, b2) => b1.Title.CompareTo(b2.Title));
            sortedAdulthoodBackstories = new List<Backstory>(adulthoodBackstories);
            sortedAdulthoodBackstories.Sort((b1, b2) => b1.Title.CompareTo(b2.Title));
        }
    }
}
