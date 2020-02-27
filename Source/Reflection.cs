using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    namespace Reflection {
        public static class CharacterCardUtility {
            public static IEnumerable<WorkTags> WorkTagsFrom(WorkTags workTags) {
                return (IEnumerable<WorkTags>)ReflectionCache.Instance.CharacterCardUtility_WorkTagsFrom.Invoke(null, new object[] { workTags });
            }
        }
        public static class ScenPart_StartingAnimal {
            public static IEnumerable<PawnKindDef> RandomPets(RimWorld.ScenPart_StartingAnimal scenPart) {
                return (IEnumerable<PawnKindDef>)ReflectionCache.Instance.ScenPart_StartingAnimal_RandomPets.Invoke(scenPart, null);
            }
        }
        public static class GenFilePaths {
            public static string FolderUnderSaveData(string name) {
                return (string)ReflectionCache.Instance.GenFilePaths_FolderUnderSaveData.Invoke(null, new object[] { name });
            }
        }
        public static class ScenPart_ForcedHediff {
            public static IEnumerable<HediffDef> PossibleHediffs(RimWorld.ScenPart_ForcedHediff scenPart) {
                return (IEnumerable<HediffDef>)ReflectionCache.Instance.ScenPart_ForcedHediff_PossibleHediffs.Invoke(scenPart, null);
            }
        }
        public static class PawnSkinColors {
            public static int GetSkinDataIndexOfMelanin(float value) {
                return (int)ReflectionCache.Instance.PawnSkinColors_GetSkinDataIndexOfMelanin.Invoke(null, new object[] { value });
            }
        }
        public static class GraphicDatabaseHeadRecords {
            public static void BuildDatabaseIfNecessary() {
                ReflectionCache.Instance.GraphicDatabaseHeadRecords_BuildDatabaseIfNecessary.Invoke(null, null);
            }
        }
        public static class Pawn {
            public static void ClearCachedDisabledWorkTypes(Verse.Pawn pawn) {
                ReflectionCache.Instance.Pawn_CachedDisabledWorkTypes.SetValue(pawn, null);
            }
            public static void ClearCachedDisabledWorkTypesPermanent(Verse.Pawn pawn) {
                ReflectionCache.Instance.Pawn_CachedDisabledWorkTypesPermanent.SetValue(pawn, null);
            }
        }
        public static class PawnBioAndNameGenerator {
            public static float BioSelectionWeight(PawnBio b) {
                return (float)ReflectionCache.Instance.PawnBioAndNameGenerator_BioSelectionWeight.Invoke(null,
                    new object[] { b }
                );
            }
            public static void FillBackstorySlotShuffled(Verse.Pawn pawn, BackstorySlot slot, ref Backstory backstory, Backstory backstoryOtherSlot, List<BackstoryCategoryFilter> backstoryCategories, FactionDef factionType) {
                ReflectionCache.Instance.PawnBioAndNameGenerator_FillBackstorySlotShuffled.Invoke(null,
                    new object[] {
                        pawn, slot, backstory, backstoryOtherSlot, backstoryCategories, factionType
                    }
                );
            }
            public static List<BackstoryCategoryFilter> GetBackstoryCategoryFiltersFor(Verse.Pawn pawn, FactionDef faction) {
                return (List<BackstoryCategoryFilter>)ReflectionCache.Instance.PawnBioAndNameGenerator_GetBackstoryCategoryFiltersFor.Invoke(null,
                    new object[] { pawn, faction }
                );
            }
            public static bool IsBioUseable(PawnBio bio, BackstoryCategoryFilter categoryFilter, PawnKindDef kind, Gender gender, string requiredLastName) {
                return (bool)ReflectionCache.Instance.PawnBioAndNameGenerator_IsBioUseable.Invoke(null,
                    new object[] { bio, categoryFilter, kind, gender, requiredLastName }
                );
            }
            public static bool TryGetRandomUnusedSolidBioFor(List<BackstoryCategoryFilter> backstoryCategories, PawnKindDef kind, Gender gender, string requiredLastName, out PawnBio result) {
                Object[] args = new object[] { backstoryCategories, kind, gender, requiredLastName, null };
                bool value = (bool)ReflectionCache.Instance.PawnBioAndNameGenerator_TryGetRandomUnusedSolidBioFor.Invoke(null, args);
                result = args[4] as PawnBio;
                return value;
            }
            public static BackstoryCategoryFilter GetFallbackCategoryGroup() {
                return (BackstoryCategoryFilter)ReflectionCache.Instance.PawnBioAndNameGenerator_FallbackCategoryGroup.GetValue(null);
            }
            public static List<string> GetTmpNames() {
                return (List<string>)ReflectionCache.Instance.PawnBioAndNameGenerator_tmpNames.GetValue(null);
            }
        }
        public static class PostLoadIniter {
            public static void ClearSaveablesToPostLoad(Verse.PostLoadIniter initer) {
                HashSet<IExposable> saveables = (HashSet<IExposable>)ReflectionCache.Instance.PostLoadIniter_SaveablesToPostLoad.GetValue(Scribe.loader.initer);
                if (saveables != null) {
                    saveables.Clear();
                }
            }
        }

        public static class HediffComp_GetsPermanent {
            public static void SetPainCategory(Verse.HediffComp_GetsPermanent comp, PainCategory painCategory) {
                ReflectionCache.Instance.HediffComp_GetsPermanent_PainCategory.SetValue(comp, painCategory);
            }
        }

    }
}
