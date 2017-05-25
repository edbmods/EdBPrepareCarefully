using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderTraits {
        protected List<Trait> traits = new List<Trait>();
        protected List<Trait> sortedTraits = new List<Trait>();

        public List<Trait> Traits {
            get {
                return sortedTraits;
            }
        }
        public ProviderTraits() {
            // Get all trait options.  If a traits has multiple degrees, create a separate trait for each degree.
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
                List<TraitDegreeData> degreeData = def.degreeDatas;
                int count = degreeData.Count;
                if (count > 0) {
                    for (int i = 0; i < count; i++) {
                        Trait trait = new Trait(def, degreeData[i].degree, true);
                        traits.Add(trait);
                    }
                }
                else {
                    traits.Add(new Trait(def, 0, true));
                }
            }

            // Create a sorted version of the trait list.
            sortedTraits = new List<Trait>(traits);
            sortedTraits.Sort((t1, t2) => t1.LabelCap.CompareTo(t2.LabelCap));
        }
    }
}
