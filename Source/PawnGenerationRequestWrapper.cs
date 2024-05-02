using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class PawnGenerationRequestWrapper {
        public PawnKindDef KindDef { get; set; } = Find.GameInitData.startingPawnKind ?? Faction.OfPlayer.def.basicMemberKind;
        public Faction Faction { get; set; } = Faction.OfPlayer;
        public PawnGenerationContext Context { get; set; } = PawnGenerationContext.PlayerStarter;
        public float? FixedBiologicalAge { get; set; } = null;
        public float? FixedChronologicalAge { get; set; } = null;
        public Gender? FixedGender { get; set; } = null;
        public bool WorldPawnFactionDoesntMatter { get; set; } = false;
        public bool MustBeCapableOfViolence { get; set; } = false;
        public Ideo FixedIdeology { get; set; } = null;
        public XenotypeDef ForcedXenotype { get; set; } = null;
        public CustomXenotype ForcedCustomXenotype { get; set; } = null;
        public List<XenotypeDef> AllowedXenotypes { get; set; } = null;
        public float ForceBaselinerChance { get; set; } = 0f;
        public IEnumerable<TraitDef> ForcedTraits { get; set; } = Enumerable.Empty<TraitDef>();
        public DevelopmentalStage DevelopmentalStage { get; set; } = DevelopmentalStage.Adult;
        public List<GeneDef> ForcedXenogenes { get; set; } = null;
        public List<GeneDef> ForcedEndogenes { get; set; } = null;
        public string FixedLastName { get; set; } = null;
        public string FixedBirthName { get; set; } = null;
        public RoyalTitleDef FixedTitle { get; set; } = null;
        public bool AllowDowned { get; set; } = false;
        public bool IsCreepJoiner { get; set; } = false;

        public MutantDef ForcedMutant { get; set; } = null;

        public BodyTypeDef ForceBodyType { get; set; } = null;
        public PawnGenerationRequestWrapper() {
        }
        private PawnGenerationRequest CreateRequest() {
            // TODO: Should dynamically look at all of the life stages in the developmental stage to see if they have any "always downed" life stages like the "baby" life stage.
            bool allowDowned = AllowDowned;
            if (DevelopmentalStage == DevelopmentalStage.Baby) {
                allowDowned = true;
            }
            var dedupedForcedXenogenes = RemoveDuplicateXenogenes(ForcedXenogenes);
            var dedupedForcedEndogenes = RemoveDuplicateEndogenes(ForcedEndogenes);
            var result = new PawnGenerationRequest(
                KindDef ?? Find.GameInitData.startingPawnKind ?? Faction.OfPlayer.def.basicMemberKind, //PawnKindDef kind,
                Faction, //Faction faction = null,
                Context, //PawnGenerationContext context = PawnGenerationContext.NonPlayer,
                -1, //int tile = -1,
                true, //bool forceGenerateNewPawn = false,
                false, //bool allowDead = false,
                allowDowned, //bool allowDowned = false,
                false, //bool canGeneratePawnRelations = true,
                MustBeCapableOfViolence, //bool mustBeCapableOfViolence = false,
                0f, //float colonistRelationChanceFactor = 1f,
                false, //bool forceAddFreeWarmLayerIfNeeded = false,
                true, //bool allowGay = true,
                false, //bool allowPregnant = false,
                false, //bool allowFood = true,
                false, //bool allowAddictions = true,
                false, //bool inhabitant = false,
                false, //bool certainlyBeenInCryptosleep = false,
                false, //bool forceRedressWorldPawnIfFormerColonist = false,
                WorldPawnFactionDoesntMatter, //bool worldPawnFactionDoesntMatter = false,
                0f, //float biocodeWeaponChance = 0f,
                0f, //float biocodeApparelChance = 0f,
                null, //Pawn extraPawnForExtraRelationChance = null,
                1f, //float relationWithExtraPawnChanceFactor = 1f,
                null, //Predicate < Pawn > validatorPreGear = null,
                null, //Predicate < Pawn > validatorPostGear = null,
                ForcedTraits, //IEnumerable < TraitDef > forcedTraits = null,
                Enumerable.Empty<TraitDef>(), //IEnumerable < TraitDef > prohibitedTraits = null,
                null, //float ? minChanceToRedressWorldPawn = null,
                FixedBiologicalAge, //float ? fixedBiologicalAge = null,
                FixedChronologicalAge, //float ? fixedChronologicalAge = null,
                FixedGender, //Gender ? fixedGender = null,
                FixedLastName, //string fixedLastName = null,
                FixedBirthName, //string fixedBirthName = null,
                FixedTitle, //RoyalTitleDef fixedTitle = null,
                null, //Ideo fixedIdeo = null,
                false, //bool forceNoIdeo = false,
                false, //bool forceNoBackstory = false,
                true, //bool forbidAnyTitle = false,
                false, //bool forceDead = false,
                dedupedForcedXenogenes, //List < GeneDef > forcedXenogenes = null,
                dedupedForcedEndogenes, //List < GeneDef > forcedEndogenes = null,
                ForcedXenotype, //XenotypeDef forcedXenotype = null,
                ForcedCustomXenotype, //CustomXenotype forcedCustomXenotype = null,
                AllowedXenotypes, //List < XenotypeDef > allowedXenotypes = null,
                ForceBaselinerChance, //float forceBaselinerChance = 0f,
                DevelopmentalStage, //DevelopmentalStage developmentalStages = DevelopmentalStage.Adult,
                null, //Func < XenotypeDef, PawnKindDef > pawnKindDefGetter = null,
                null, //FloatRange ? excludeBiologicalAgeRange = null,
                null, //FloatRange ? biologicalAgeRange = null,
                false //bool forceRecruitable = false
            );
            result.ForceBodyType = ForceBodyType;
            result.IsCreepJoiner = IsCreepJoiner;
            result.ForcedMutant = ForcedMutant;
            return result;
        }
        public PawnGenerationRequest Request {
            get {
                return CreateRequest();
            }
        }

        // If the generation request specifies a forced xenotype and forced xenogenes, the default pawn generator will not deduplicate
        // the genes and will instead add multiple copies.  We dedupe them here by removing duplicates from the forced xenogenes before
        // we do the generation.
        protected List<GeneDef> RemoveDuplicateXenogenes(List<GeneDef> forcedGenes) {
            if (forcedGenes == null || forcedGenes.Count == 0 || (ForcedXenotype == null && ForcedCustomXenotype == null)) {
                return forcedGenes;
            }
            //Logger.Debug("Starting forced xenogenes: " + forcedGenes.Count);
            if (ForcedXenotype != null && !ForcedXenotype.inheritable) {
                List<GeneDef> genesToRemove = new List<GeneDef>();
                foreach (var g in ForcedXenotype.genes) {
                    genesToRemove.Add(g);
                }
                foreach (var g in genesToRemove) {
                    forcedGenes.Remove(g);
                }
            }
            //Logger.Debug("Remaining forced xenogenes: " + forcedGenes.Count);
            return forcedGenes;
        }

        protected List<GeneDef> RemoveDuplicateEndogenes(List<GeneDef> forcedGenes) {
            if (forcedGenes == null || forcedGenes.Count == 0 || (ForcedXenotype == null && ForcedCustomXenotype == null)) {
                return forcedGenes;
            }
            //Logger.Debug("Starting forced endogenes: " + forcedGenes.Count);
            if (ForcedXenotype != null && ForcedXenotype.inheritable) {
                List<GeneDef> genesToRemove = new List<GeneDef>();
                foreach (var g in ForcedXenotype.genes) {
                    genesToRemove.Add(g);
                }
                foreach (var g in genesToRemove) {
                    forcedGenes.Remove(g);
                }
            }
            //Logger.Debug("Remaining forced endogenes: " + forcedGenes.Count);
            return forcedGenes;
        }
    }
}
