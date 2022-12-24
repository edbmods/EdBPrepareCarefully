using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class ReflectionCache {
        private static ReflectionCache instance;

        public MethodInfo GraphicDatabaseHeadRecords_BuildDatabaseIfNecessary { get; set; }
        public MethodInfo CharacterCardUtility_WorkTagsFrom { get; set; }
        public MethodInfo GenFilePaths_FolderUnderSaveData { get; set; }
        public MethodInfo PawnBioAndNameGenerator_BioSelectionWeight { get; set; }
        public MethodInfo PawnBioAndNameGenerator_GetBackstoryCategoryFiltersFor { get; set; }
        public MethodInfo PawnBioAndNameGenerator_IsBioUseable { get; set; }
        public MethodInfo PawnBioAndNameGenerator_TryGetRandomUnusedSolidBioFor { get; set; }
        public MethodInfo ScenPart_ForcedHediff_PossibleHediffs { get; set; }
        public MethodInfo ScenPart_StartingAnimal_RandomPets { get; set; }

        public FieldInfo Pawn_CachedDisabledWorkTypes { get; set; }
        public FieldInfo Pawn_CachedDisabledWorkTypesPermanent { get; set; }
        public FieldInfo PostLoadIniter_SaveablesToPostLoad { get; set; }
        public FieldInfo HediffComp_GetsPermanent_PainCategory { get; set; }
        public FieldInfo PawnBioAndNameGenerator_FallbackCategoryGroup { get; set; }
        public FieldInfo PawnBioAndNameGenerator_tmpNames { get; set; }

        public static ReflectionCache Instance {
            get {
                if (instance == null) {
                    instance = new ReflectionCache();
                }
                return instance;
            }
        }

        public void Initialize() {
            CharacterCardUtility_WorkTagsFrom = ReflectionUtil.RequiredMethod(typeof(CharacterCardUtility), "WorkTagsFrom");
            GenFilePaths_FolderUnderSaveData = ReflectionUtil.RequiredMethod(typeof(GenFilePaths), "FolderUnderSaveData", new Type[] { typeof(string) });

            PawnBioAndNameGenerator_GetBackstoryCategoryFiltersFor = ReflectionUtil.RequiredMethod(typeof(PawnBioAndNameGenerator), "GetBackstoryCategoryFiltersFor",
                new Type[] { typeof(Pawn), typeof(FactionDef) });

            PawnBioAndNameGenerator_TryGetRandomUnusedSolidBioFor = ReflectionUtil.RequiredMethod(typeof(PawnBioAndNameGenerator), "TryGetRandomUnusedSolidBioFor",
                new Type[] { typeof(List<BackstoryCategoryFilter>), typeof(PawnKindDef), typeof(Gender), typeof(string), typeof(PawnBio).MakeByRefType() });
            PawnBioAndNameGenerator_IsBioUseable = ReflectionUtil.RequiredMethod(typeof(PawnBioAndNameGenerator), "IsBioUseable");
            PawnBioAndNameGenerator_BioSelectionWeight = ReflectionUtil.RequiredMethod(typeof(PawnBioAndNameGenerator), "BioSelectionWeight");

            ScenPart_StartingAnimal_RandomPets = ReflectionUtil.RequiredMethod(typeof(ScenPart_StartingAnimal), "RandomPets");
            ScenPart_ForcedHediff_PossibleHediffs = ReflectionUtil.RequiredMethod(typeof(ScenPart_ForcedHediff), "PossibleHediffs");

            Pawn_CachedDisabledWorkTypes = ReflectionUtil.RequiredField(typeof(Pawn), "cachedDisabledWorkTypes");
            Pawn_CachedDisabledWorkTypesPermanent = ReflectionUtil.RequiredField(typeof(Pawn), "cachedDisabledWorkTypesPermanent");
            PostLoadIniter_SaveablesToPostLoad = ReflectionUtil.RequiredField(typeof(PostLoadIniter), "saveablesToPostLoad");
            HediffComp_GetsPermanent_PainCategory = ReflectionUtil.RequiredField(typeof(HediffComp_GetsPermanent), "painCategory");
            PawnBioAndNameGenerator_FallbackCategoryGroup = ReflectionUtil.RequiredField(typeof(PawnBioAndNameGenerator), "FallbackCategoryGroup");
            PawnBioAndNameGenerator_tmpNames = ReflectionUtil.RequiredField(typeof(PawnBioAndNameGenerator), "tmpNames");
        }
    }
}
