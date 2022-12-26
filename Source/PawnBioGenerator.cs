using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    // Contains an duplicate implementation of PawnBioAndNameGenerator.TryGetRandomUnusedSolidBioFor() to give us
    // a fallback to call if other mods patch the original method in a way that makes it return false.
    public class PawnBioGenerator {
        public static bool TryGetRandomUnusedSolidBioFor(List<BackstoryCategoryFilter> backstoryCategories, PawnKindDef kind, Gender gender, string requiredLastName, out PawnBio result) {
            // Pick a random category filter.
            BackstoryCategoryFilter categoryFilter = backstoryCategories.RandomElementByWeight((c) => {
                return c.commonality;
            });
            // Settle for a default category filter if none was chosen.
            if (categoryFilter == null) {
                categoryFilter = Reflection.ReflectorPawnBioAndNameGenerator.GetFallbackCategoryGroup();
            }
            // Choose a weighted bio.
            return (from bio in SolidBioDatabase.allBios.TakeRandom(20)
                    where Reflection.ReflectorPawnBioAndNameGenerator.IsBioUseable(bio, categoryFilter, kind, gender, requiredLastName)
                    select bio).TryRandomElementByWeight(new Func<PawnBio, float>(Reflection.ReflectorPawnBioAndNameGenerator.BioSelectionWeight), out result);
        }
    }
}
