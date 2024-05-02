using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ManagerPawns {
        public delegate void CostAffectedHandler();
        public event CostAffectedHandler CostAffected;

        public delegate void SkillsAffectedHandler();
        public event SkillsAffectedHandler SkillsAffected;

        public ModState State { get; set; }


        public PawnCustomizer Customizer { get; set; }
        public MapperPawnToCustomizations PawnToCustomizationsMapper { get; set; }
        public ProviderAgeLimits ProviderAgeLimits { get; set; }
        public ProviderAlienRaces ProviderAlienRaces { get; set; }
        public ProviderBackstories ProviderBackstories { get; set; }
        public ProviderBodyTypes ProviderBodyTypes { get; set; }
        public ProviderHeadTypes ProviderHeadTypes { get; set; }
        public AgeModifier AgeModifier { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }
        public PawnLoader PawnLoader { get; set; }
        public PawnSaver PawnSaver { get; set; }
        public PawnGenerationRequestBuilder PawnGenerationRequestBuilder { get; set; } = new PawnGenerationRequestBuilder();

        public void InitializeStateFromStartingPawns() {
            GameInitData gameInitData = Find.GameInitData;
            State.ColonyPawnCount = gameInitData.startingPawnCount;
            State.TotalPawnCount = gameInitData.startingAndOptionalPawns.Count;
            State.Customizations.ColonyPawns = new List<CustomizedPawn>();
            State.Customizations.WorldPawns = new List<CustomizedPawn>();
            State.OriginalPawnCustomizations = new Dictionary<Pawn, CustomizationsPawn>();

            var startingAndOptionalPawns = gameInitData.startingAndOptionalPawns;
            for (int i = 0; i < startingAndOptionalPawns.Count; i++) {
                var pawn = startingAndOptionalPawns[i];
                CustomizationsPawn originalCustomizations = PawnToCustomizationsMapper.Map(pawn);
                State.OriginalPawnCustomizations.Add(pawn, originalCustomizations);
                CustomizationsPawn workingCustomizations = PawnToCustomizationsMapper.Map(pawn);
                PawnToCustomizationsMapper.MapStartingPossessions(gameInitData.startingPossessions[pawn], workingCustomizations);
                CustomizedPawn customizedPawn = new CustomizedPawn() {
                    Customizations = workingCustomizations,
                    Pawn = pawn,
                    Type = i < State.ColonyPawnCount ? CustomizedPawnType.Colony : CustomizedPawnType.World,
                };
                if (customizedPawn.Type == CustomizedPawnType.Colony) {
                    State.Customizations.ColonyPawns.Add(customizedPawn);
                }
                else if (customizedPawn.Type == CustomizedPawnType.World) {
                    State.Customizations.WorldPawns.Add(customizedPawn);
                }
                StoreSkillGains(customizedPawn);
            }
        }

        public void DestroyPawn(Pawn pawn) {
            if (pawn == null) {
                return;
            }
            // Don't destroy the original pawns
            if (State.OriginalPawnCustomizations?.ContainsKey(pawn) ?? false) {
                return;
            }
            UtilityPawns.DestroyPawn(pawn);
        }

        public void ClearPawns() {
            foreach (var pawn in State.Customizations.ColonyPawns) {
                DestroyPawn(pawn.Pawn);
            }
            foreach (var pawn in State.Customizations.WorldPawns) {
                DestroyPawn(pawn.Pawn);
            }
            State.Customizations.ColonyPawns.Clear();
            State.Customizations.WorldPawns.Clear();
        }

        // Unfortunately, using the colony faction when creating a pawn with a different kind def results in
        // bad apparel generation, i.e. synthread tribalwear or too much missing clothing.  So after we generate
        // the pawn with the colony faction, we temporarily set the pawns faction to null and regenerate the apparel.
        public void FixNonColonyPawnKindApparel(Pawn pawn, PawnGenerationRequest generationRequest) {
            // We only need to null out the faction if the pawn kind def is not the colony's pawn kind def.
            if (generationRequest.KindDef == null || generationRequest.KindDef?.defaultFactionType == Find.World.factionManager.OfPlayer.def) {
                return;
            }
            pawn.apparel?.DestroyAll(DestroyMode.Vanish);
            pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.inventory?.DestroyAll(DestroyMode.Vanish);

            // Using SetFactionDirect() hopefully will avoid any side-effects from this approach.  We're immediately restoring
            // it back to its previous setting.
            Faction generationRequestFaction = generationRequest.Faction;
            generationRequest.Faction = null;
            Faction faction = pawn.Faction;
            pawn.SetFactionDirect(null);
            try {
                PawnApparelGenerator.GenerateStartingApparelFor(pawn, generationRequest);
            }
            finally {
                if (pawn.Faction != faction) {
                    pawn.SetFactionDirect(faction);
                }
                generationRequest.Faction = generationRequestFaction;
            }
        }

        public Pawn CreatePawn(PawnKindDef kindDef, FactionDef factionDef) {
            PawnGenerationRequestWrapper wrapper = new PawnGenerationRequestWrapper();
            if (kindDef != null) {
                wrapper.KindDef = kindDef;
                if (kindDef is CreepJoinerFormKindDef) {
                    wrapper.IsCreepJoiner = true;
                }
            }
            if (factionDef != null) {
                wrapper.Faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            }
            PawnGenerationRequest generationRequest = wrapper.Request;
            Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
            FixNonColonyPawnKindApparel(pawn, generationRequest);
            pawn.SetFactionDirect(Find.FactionManager.OfPlayer);
            return pawn;
        }

        public string GeneratePawnId() {
            return System.Guid.NewGuid().ToString();
        }

        public CustomizedPawn AddPawn(CustomizedPawnType pawnType, PawnKindOption option = null) {
            Pawn pawn = CreatePawn(option?.KindDef, option?.FactionDef);
            CustomizationsPawn customizations = PawnToCustomizationsMapper.Map(pawn);
            CustomizedPawn customizedPawn = new CustomizedPawn() {
                Pawn = pawn,
                Customizations = customizations,
                Type = pawnType,
                Id = GeneratePawnId(),
            };
            return AddPawnToPawnList(customizedPawn);
        }
        public CustomizedPawn AddPawnToPawnList(CustomizedPawn customizedPawn) {
            if (customizedPawn.Type == CustomizedPawnType.Colony) {
                State.Customizations.ColonyPawns.Add(customizedPawn);
            }
            else {
                State.Customizations.WorldPawns.Add(customizedPawn);
            }
            StoreSkillGains(customizedPawn);
            CostAffected?.Invoke();
            return customizedPawn;
        }
        public bool RemovePawn(CustomizedPawn pawn) {
            List<CustomizedPawn> pawnList = null;
            if (pawn.Type == CustomizedPawnType.Colony) {
                pawnList = State.Customizations.ColonyPawns;
                if (pawnList.Count < 2) {
                    return false;
                }
            }
            else if (pawn.Type == CustomizedPawnType.World) {
                pawnList = State.Customizations.WorldPawns;
            }
            CostAffected?.Invoke();
            bool result = pawnList.Remove(pawn);
            State.CachedSkillGains.Remove(pawn);
            DestroyPawn(pawn.Pawn);
            return result;
        }
        public void ChangeColonyPawnToWorldPawn(CustomizedPawn pawn) {
            if (State.Customizations.ColonyPawns.Count <= 1) {
                return;
            }
            State.Customizations.ColonyPawns.Remove(pawn);
            pawn.Type = CustomizedPawnType.World;
            State.Customizations.WorldPawns.Add(pawn);
            CostAffected?.Invoke();
        }
        public void ChangeWorldPawnToColonyPawn(CustomizedPawn pawn) {
            State.Customizations.WorldPawns.Remove(pawn);
            pawn.Type = CustomizedPawnType.Colony;
            State.Customizations.ColonyPawns.Add(pawn);
            CostAffected?.Invoke();
        }
        public PawnLoaderResult LoadPawn(CustomizedPawnType pawnType, string file) {
            if (string.IsNullOrEmpty(file)) {
                Logger.Warning("Trying to load a character without a file name");
                return new PawnLoaderResult();
            }
            PawnLoaderResult pawnLoaderResult = PawnLoader.Load(file);
            if (pawnLoaderResult == null) {
                Logger.Warning("Got no pawn loader result");
                return new PawnLoaderResult();
            }

            CustomizedPawn customizedPawn = pawnLoaderResult?.Pawn;
            if (customizedPawn == null) {
                Logger.Warning("Failed to load pawn");
                return pawnLoaderResult;
            }
            customizedPawn.Type = pawnType;
            customizedPawn.Pawn = Customizer.CreatePawnFromCustomizations(customizedPawn.Customizations);
            pawnLoaderResult.Pawn = AddPawnToPawnList(customizedPawn);

            //bool colonyPawn = state.PawnListMode == PawnListMode.ColonyPawnsMaximized;
            //pawn.Type = colonyPawn ? CustomPawnType.Colonist : CustomPawnType.World;
            // Regenerate a unique id in case the user is loading the same pawn more than once.
            //pawn.GenerateId();
            return pawnLoaderResult;
        }

        public void UpdateFirstName(CustomizedPawn customizedPawn, string name) {
            if (customizedPawn == null) {
                return;
            }
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (customizations == null) {
                return;
            }
            customizations.FirstName = name;
            ApplyNameCustomizationsToPawn(customizations, customizedPawn.Pawn);
        }

        public void UpdateNickName(CustomizedPawn customizedPawn, string name) {
            if (customizedPawn == null) {
                return;
            }
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (customizations == null) {
                return;
            }
            if (customizations.NameType == "Triple") {
                customizations.NickName = name;
            }
            else if (customizations.NameType == "Single") {
                customizations.SingleName = name;
            }
            else {
                return;
            }
            ApplyNameCustomizationsToPawn(customizations, customizedPawn.Pawn);
        }

        public void UpdateLastName(CustomizedPawn customizedPawn, string name) {
            if (customizedPawn == null) {
                return;
            }
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (customizations == null) {
                return;
            }
            customizations.LastName = name;
            ApplyNameCustomizationsToPawn(customizations, customizedPawn.Pawn);
        }

        public void ApplyNameCustomizationsToPawn(CustomizationsPawn customizations, Pawn pawn) {
            if (pawn == null || customizations == null) {
                return;
            }
            if (customizations.NameType == "Triple") {
                pawn.Name = new NameTriple(customizations.FirstName, customizations.NickName, customizations.LastName);
            }
            else if (customizations.NameType == "Single") {
                pawn.Name = new NameSingle(customizations.SingleName);
            }
            else {
                // TODO: how to manage mapping problems?
            }
        }

        public void RandomizePawn(CustomizedPawn customizedPawn, PawnRandomizerOptions options = null) {
            if (customizedPawn == null) {
                return;
            }
            Pawn previousPawn = customizedPawn?.Pawn;
            PawnGenerationRequestWrapper wrapper = new PawnGenerationRequestWrapper() {
                KindDef = previousPawn.kindDef
            };
            
            if (options != null) {
                wrapper.ForcedXenotype = options.Xenotype;
                wrapper.ForcedCustomXenotype = options.CustomXenotype;
                wrapper.DevelopmentalStage = options.DevelopmentalStage;
                if (options.AnyNonArchite) {
                    wrapper.AllowedXenotypes = DefDatabase<XenotypeDef>.AllDefs.Where((XenotypeDef x) => !x.Archite && x != XenotypeDefOf.Baseliner).ToList();
                    wrapper.ForceBaselinerChance = 0.5f;
                }
            }

            if (ModsConfig.IdeologyActive) {
                Ideo ideo = Find.FactionManager.OfPlayer?.ideos?.GetRandomIdeoForNewPawn();
                if (ideo != null) {
                    wrapper.FixedIdeology = ideo;
                }
            }

            var request = wrapper.Request;
            if (ModsConfig.AnomalyActive) {
                request.ForcedMutant = previousPawn?.mutant?.Def;
            }
            customizedPawn.Pawn = PawnGenerator.GeneratePawn(request);
            customizedPawn.Customizations = PawnToCustomizationsMapper.Map(customizedPawn.Pawn);
            if (previousPawn != null) {
                DestroyPawn(previousPawn);
            }
            StoreSkillGains(customizedPawn);
            CostAffected?.Invoke();
        }

        public void UpdatePawnBiologicalAge(CustomizedPawn customizedPawn, int years, int days) {
            if (customizedPawn == null || customizedPawn.Pawn == null || customizedPawn.Customizations == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            int min = ProviderAgeLimits.MinAgeForPawn(pawn);
            int max = ProviderAgeLimits.MaxAgeForPawn(pawn);
            if (years < min) {
                years = min;
                days = 0;
            }
            else if (years > max) {
                years = max;
                days = (int)AgeModifier.DaysPerYear - 1;
            }
            long ticks = AgeModifier.TicksFromYearsAndDays(years, days) + AgeModifier.TicksOfDay(pawn.ageTracker.AgeBiologicalTicks);
            AgeModifier.ModifyBiologicalAge(customizedPawn, ticks);
            if (ticks > pawn.ageTracker.AgeChronologicalTicks) {
                AgeModifier.ModifyChronologicalAge(customizedPawn, ticks);
            }
            PawnToCustomizationsMapper.MapAge(pawn, customizedPawn.Customizations);
            CostAffected?.Invoke();
        }
        public void UpdatePawnChronologicalAge(CustomizedPawn customizedPawn, int years, int days) {
            if (customizedPawn == null || customizedPawn.Pawn == null || customizedPawn.Customizations == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            int min = ProviderAgeLimits.MinAgeForPawn(pawn);
            if (years < min) {
                years = min;
                days = 0;
            }
            long ticks = AgeModifier.TicksFromYearsAndDays(years, days) + AgeModifier.TicksOfDay(pawn.ageTracker.AgeChronologicalTicks);
            AgeModifier.ModifyChronologicalAge(customizedPawn, ticks);
            if (ticks < pawn.ageTracker.AgeBiologicalTicks) {
                AgeModifier.ModifyBiologicalAge(customizedPawn, ticks);
            }
            PawnToCustomizationsMapper.MapAge(pawn, customizedPawn.Customizations);
            CostAffected?.Invoke();
        }

        public void UpdatePawnBackstory(CustomizedPawn pawn, BackstorySlot slot, BackstoryDef backstory) {
            if (pawn == null || pawn.Customizations == null) {
                return;
            }
            if (slot == BackstorySlot.Adulthood) {
                pawn.Customizations.AdulthoodBackstory = backstory;
                pawn.Pawn.story.Adulthood = backstory;
                RecalculateSkillLevelsFromSkillGains(pawn);
            }
            else {
                pawn.Customizations.ChildhoodBackstory = backstory;
                pawn.Pawn.story.Childhood = backstory;
                RecalculateSkillLevelsFromSkillGains(pawn);
            }
            CostAffected?.Invoke();
        }

        public void RandomizeAppearance(CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            PawnGenerationRequest request = PawnGenerationRequestBuilder.BuildFromCustomizationsWithoutAppearance(customizedPawn.Customizations);
            Pawn temporaryPawn = PawnGenerator.GeneratePawn(request);
            PawnToCustomizationsMapper.MapAppearance(temporaryPawn, customizations);
            DestroyPawn(temporaryPawn);
            Customizer.ApplyAppearanceCustomizationsToPawn(pawn, customizations);
            ClearPawnGraphicsCache(pawn);
        }

        public void UpdatePawnBackstories(CustomizedPawn customizedPawn, BackstoryDef childhood, BackstoryDef adulthood) {
            if (customizedPawn == null || customizedPawn.Customizations == null) {
                return;
            }
            customizedPawn.Pawn.story.Childhood = childhood;
            customizedPawn.Pawn.story.Adulthood = adulthood;
            PawnToCustomizationsMapper.MapBackstories(customizedPawn.Pawn, customizedPawn.Customizations);
            RecalculateSkillLevelsFromSkillGains(customizedPawn);
            CostAffected?.Invoke();
        }

        public void ClearSkillCaches(CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn?.Pawn;
            if (pawn == null) {
                return;
            }
            pawn.ClearCachedDisabledSkillRecords();
        }

        public void RandomizePawnBackstories(CustomizedPawn customizedPawn) {
            if (customizedPawn?.Pawn == null || customizedPawn?.Customizations == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            PawnGenerationRequest request = PawnGenerationRequestBuilder.BuildFromCustomizations(customizedPawn.Customizations);
            Pawn temporaryPawn = PawnGenerator.GeneratePawn(request);
            UpdatePawnBackstories(customizedPawn, temporaryPawn.story.Childhood, temporaryPawn.story.Adulthood);
            DestroyPawn(temporaryPawn);
            CostAffected?.Invoke();
        }

        public void SetSkillLevel(CustomizedPawn customizedPawn, SkillDef skill, int level) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (pawn == null || customizations == null || skill == null) {
                return;
            }
            SkillRecord record = pawn.skills.GetSkill(skill);
            if (record == null) {
                Logger.Debug("Couldn't find skill record for skill " + skill.defName);
                return;
            }
            Dictionary<SkillDef, int> skillModifiers = UtilityPawns.ComputeSkillGains(customizedPawn.Pawn);
            int minimum = 0;
            if (skillModifiers.TryGetValue(skill, out var modifier)) {
                minimum = modifier;
            }
            else {
                Logger.Debug("Couldn't find skill gains for skill " + skill.defName);
            }
            if (level < minimum) {
                level = minimum;
            }
            record.Level = level - record.Aptitude;
            var customizedSkill = customizations.Skills.FirstOrDefault(s => s.SkillDef == skill);
            if (customizedSkill != null) {
                customizedSkill.Level = level;
            }
            StoreSkillGains(customizedPawn);
            CostAffected?.Invoke();
        }

        public void StoreSkillGains(CustomizedPawn customizedPawn) {
            if (customizedPawn?.Pawn == null) {
                return;
            }
            State.CachedSkillGains[customizedPawn] = UtilityPawns.ComputeSkillGains(customizedPawn.Pawn);
        }

        public void UpdatePawnSkillPassion(CustomizedPawn pawn, SkillDef skill, Passion passion) {
            if (pawn == null || pawn.Customizations == null || skill == null) {
                return;
            }
            CustomizationsPawn customizations = pawn.Customizations;
            var customizedSkill = customizations.Skills.FirstOrDefault(s => s.SkillDef == skill);
            if (customizedSkill != null) {
                customizedSkill.Passion = passion;
            }
            if (pawn.Pawn != null) {
                var skillRecord = pawn.Pawn.skills.GetSkill(skill);
                if (skillRecord != null) {
                    skillRecord.passion = passion;
                }
            }
            CostAffected?.Invoke();
        }

        public void ClearSkillLevels(CustomizedPawn pawn) {
            if (pawn?.Pawn == null || pawn.Customizations == null) {
                return;
            }
            var modifiers = UtilityPawns.ComputeSkillGains(pawn.Pawn);
            foreach (var customizedSkill in pawn.Customizations.Skills) {
                if (modifiers.TryGetValue(customizedSkill.SkillDef, out int minimumLevel)) {
                    customizedSkill.Level = minimumLevel;
                    var skillRecord = pawn.Pawn.skills.GetSkill(customizedSkill.SkillDef);
                    if (skillRecord != null) {
                        skillRecord.Level = minimumLevel;
                    }
                }
            }
            CostAffected?.Invoke();
        }
        public void ResetSkillLevels(CustomizedPawn pawn) {
            if (pawn == null || pawn.Customizations == null) {
                return;
            }
            foreach (var customizedSkill in pawn.Customizations.Skills) {
                customizedSkill.Level = customizedSkill.OriginalLevel;
                var skillRecord = pawn.Pawn.skills.GetSkill(customizedSkill.SkillDef);
                if (skillRecord != null) {
                    skillRecord.Level = customizedSkill.OriginalLevel;
                }
            }
            CostAffected?.Invoke();
        }
        
        // When something changes that affects skill gains/modifiers (backstory, traits, etc.), we want to adjust
        // the skill levels to match.  We do this by maintaining the same delta between the skill gain and the
        // selected skill level.  For example, if the pawn's skill gain is +1 and their skill level is 4, and we
        // then change their backstory to one that gives them a skill gain of +3, then their skill level should
        // change to 6, maintaining a delta of 2 between the values.  To do this, we store the skill gains for each
        // pawn and adjust all of the skill levels accordingly everytime those skill gains change.
        public void RecalculateSkillLevelsFromSkillGains(CustomizedPawn customizedPawn) {
            ClearSkillCaches(customizedPawn);
            Pawn pawn = customizedPawn.Pawn;
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            if (!State.CachedSkillGains.TryGetValue(customizedPawn, out var previousSkillGains)) {
                return;
            }
            var skillGains = UtilityPawns.ComputeSkillGains(pawn);
            foreach (var skillGainPair in skillGains) {
                var skill = skillGainPair.Key;
                var record = pawn.skills.GetSkill(skill);
                if (record == null) {
                    continue;
                }
                if (!previousSkillGains.TryGetValue(skill, out int previousGain)) {
                    continue;
                }
                if (!skillGains.TryGetValue(skill, out int currentGain)) {
                    continue;
                }
                int delta = currentGain - previousGain;
                //Logger.Debug("previous gains: " + previousGain + ", current gains: " + currentGain + ", delta = " + delta + ", level = " + record.Level);
                int currentLevel = record.Level;
                int newLevel = currentLevel + delta;
                record.Level = newLevel - record.Aptitude;
                var customizedSkill = customizations.Skills.FirstOrDefault(s => s.SkillDef == skill);
                if (customizedSkill != null) {
                    customizedSkill.Level = record.Level;
                }
            }
            StoreSkillGains(customizedPawn);
            SkillsAffected?.Invoke();
        }

        public void AddTrait(CustomizedPawn customizedPawn, Trait trait) {
            if (customizedPawn?.Pawn == null) {
                return;
            }
            Trait traitToAdd = new Trait(trait.def, trait.Degree);
            var traits = customizedPawn.Pawn.story.traits;
            if (!traits.HasTrait(traitToAdd.def, traitToAdd.Degree)) {
                customizedPawn.Pawn.story.traits.GainTrait(traitToAdd);
                RecalculateSkillLevelsFromSkillGains(customizedPawn);
                PawnToCustomizationsMapper.MapTraits(customizedPawn.Pawn, customizedPawn.Customizations);
            }
            CostAffected?.Invoke();
        }
        public static bool CanRemoveTrait(Pawn pawn, Trait trait) {
            return trait.sourceGene == null;
        }
        public void RemoveTrait(CustomizedPawn customizedPawn, Trait trait) {
            if (customizedPawn?.Pawn == null) {
                return;
            }
            if (trait.sourceGene != null) {
                return;
            }
            var traits = customizedPawn.Pawn.story.traits;
            Trait traitToRemove = null;
            foreach (var existingTrait in customizedPawn.Pawn.story.traits.allTraits) {
                if (existingTrait.def == trait.def && existingTrait.Degree == trait.Degree) {
                    traitToRemove = existingTrait;
                    break;
                }
            }
            if (traitToRemove == null) {
                return;
            }
            if (traitToRemove.Suppressed) {
                traits.allTraits.Remove(traitToRemove);
            }
            else {
                traits.RemoveTrait(traitToRemove);
            }
            UtilityPawns.ClearTraitCaches(customizedPawn.Pawn);
            RecalculateSkillLevelsFromSkillGains(customizedPawn);
            PawnToCustomizationsMapper.MapTraits(customizedPawn.Pawn, customizedPawn.Customizations);
            CostAffected?.Invoke();
        }
        public void ReplaceTrait(CustomizedPawn customizedPawn, Trait replace, Trait with) {
            if (customizedPawn?.Customizations == null || customizedPawn?.Pawn == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            var traits = pawn.story.traits.allTraits;
            int index = traits.IndexOf(replace);
            if (index == -1) {
                return;
            }
            traits[index] = with;
            RecalculateSkillLevelsFromSkillGains(customizedPawn);
            PawnToCustomizationsMapper.MapTraits(pawn, customizedPawn.Customizations);
            CostAffected?.Invoke();
        }
        public void RandomizeTraits(CustomizedPawn customizedPawn) {
            if (customizedPawn?.Customizations == null || customizedPawn?.Pawn == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            if (pawn.story?.traits?.allTraits == null) {
                return;
            }

            List<Trait> forcedTraits = GetForcedTraitsNotAddedByPawnGenerator(pawn);

            // Not calling the TraitSet.RemoveTrait() method to avoid its side effects (removing genes; removing abilities).
            // Hopefully there are not any side effects from not calling it
            pawn.story.traits.allTraits.Clear();

            PawnGenerationRequest request = PawnGenerationRequestBuilder.BuildFromCustomizations(customizedPawn.Customizations);
            Reflection.ReflectorPawnGenerator.GenerateTraits(pawn, request);

            List<Trait> traitsToRemove = new List<Trait>();
            // If one of the traits that was added is one of the forced traits that we're about to add, remove it.
            foreach (var trait in pawn.story.traits.allTraits) {
                if (forcedTraits.ContainsAny(t => t.def == trait.def && t.Degree == trait.Degree)) {
                    traitsToRemove.Add(trait);
                }
            }
            foreach (var trait in traitsToRemove) {
                pawn.story.traits.allTraits.Remove(trait);
            }

            // Add all of the forced traits
            foreach (var trait in forcedTraits) {
                pawn.story.traits.GainTrait(trait, true);
            }

            UtilityPawns.ClearTraitCaches(pawn);
            RecalculateSkillLevelsFromSkillGains(customizedPawn);
            PawnToCustomizationsMapper.MapTraits(pawn, customizedPawn.Customizations);
            CostAffected?.Invoke();
        }

        public List<Trait> GetForcedTraitsNotAddedByPawnGenerator(Pawn pawn) {
            List<Trait> result = new List<Trait>();
            foreach (var gene in pawn.genes?.GenesListForReading) {
                if (!gene.Overridden && gene.def.forcedTraits?.CountAllowNull() > 0) {
                    foreach (var trait in gene.def.forcedTraits) {
                        Trait traitToAdd = new Trait(trait.def, trait.degree);
                        traitToAdd.sourceGene = gene;
                        result.Add(traitToAdd);
                    }
                }
            }
            return result;
        }

        public void SetTraits(CustomizedPawn customizedPawn, IEnumerable<Trait> traits) {
            if (customizedPawn?.Pawn == null) {
                return;
            }
            TraitSet traitSet = customizedPawn?.Pawn?.story?.traits;
            if (traitSet == null) {
                return;
            }
            int matchingTraitCount = 0;
            foreach (var t in traits) {
                Logger.Debug("trait def label: " + t.Label + ", " + t.def.LabelCap);
                if (traitSet.HasTrait(t.def, t.Degree)) {
                    matchingTraitCount++;
                }
            }
            if (matchingTraitCount == traitSet.allTraits.Count) {
                return;
            }
            List<Trait> currentTraits = new List<Trait>();
            traitSet.allTraits.ForEach(t => {
                currentTraits.Add(t);
            });
            currentTraits.ForEach(t => {
                traitSet.RemoveTrait(t);
            });
            traitSet.allTraits.Clear();
            foreach (var trait in traits) {
                traitSet.GainTrait(trait);
            }
            PawnToCustomizationsMapper.MapTraits(customizedPawn.Pawn, customizedPawn.Customizations);
            RecalculateSkillLevelsFromSkillGains(customizedPawn);
            CostAffected?.Invoke();
        }

        public void AddPawnInjury(CustomizedPawn customizedPawn, Injury injury) {
            Pawn pawn = customizedPawn.Pawn;
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            customizations.Injuries.Add(injury);
            customizations.BodyParts.Add(injury);
            Customizer.ApplyInjuryAndImplantCustomizationsToPawn(pawn, customizations);
            PawnToCustomizationsMapper.MapInjuriesAndImplants(pawn, customizations);
            CostAffected?.Invoke();
        }
        public void AddPawnImplant(CustomizedPawn customizedPawn, Implant implant) {
            Pawn pawn = customizedPawn.Pawn;
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            customizations.Implants.Add(implant);
            customizations.BodyParts.Add(implant);
            Customizer.ApplyInjuryAndImplantCustomizationsToPawn(pawn, customizations);
            PawnToCustomizationsMapper.MapInjuriesAndImplants(pawn, customizations);
            CostAffected?.Invoke();
        }
        public void UpdateImplants(CustomizedPawn customizedPawn, IEnumerable<Implant> implants) {
            Pawn pawn = customizedPawn.Pawn;
            CustomizationsPawn customizations = customizedPawn.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            List<Implant> implantsToRemove = new List<Implant>();
            foreach (var bodyPart in customizations.BodyParts) {
                Implant asImplant = bodyPart as Implant;
                implantsToRemove.Add(asImplant);
            }
            foreach (var implant in implantsToRemove) {
                customizations.BodyParts.Remove(implant);
            }
            customizations.Implants.Clear();
            foreach (var implant in implants) {
                customizations.BodyParts.Add(implant);
                customizations.Implants.Add(implant);
            }
            Customizer.ApplyInjuryAndImplantCustomizationsToPawn(pawn, customizations);
            PawnToCustomizationsMapper.MapInjuriesAndImplants(pawn, customizations);
            ClearPawnGraphicsCache(pawn);
            CostAffected?.Invoke();
        }

        public void RemovePawnHediffs(CustomizedPawn customizedPawn, IEnumerable<Hediff> hediffs) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            foreach (Hediff hediff in hediffs) {
                pawn.health.RemoveHediff(hediff);
                Injury injury = customizations.Injuries.FirstOrDefault(i => i.Hediff == hediff);
                Implant implant = customizations.Implants.FirstOrDefault(i => i.Hediff == hediff);
                if (injury != null) {
                    RemoveCustomBodyParts(injury, customizations);
                }
                if (implant != null) {
                    RemoveCustomBodyParts(implant, customizations);
                }
            }
            Customizer.ApplyInjuryAndImplantCustomizationsToPawn(pawn, customizations);
            PawnToCustomizationsMapper.MapInjuriesAndImplants(pawn, customizations);
            ClearPawnGraphicsCache(pawn);
            CostAffected?.Invoke();
        }
        public void RemoveCustomBodyParts(CustomizedHediff part, CustomizationsPawn customizations) {
            Implant implant = part as Implant;
            Injury injury = part as Injury;
            if (implant != null) {
                customizations.Implants.Remove(implant);
            }
            if (injury != null) {
                customizations.Injuries.Remove(injury);
            }
            customizations.BodyParts.Remove(part);
            CostAffected?.Invoke();
        }
        public void RemovePawnHediff(CustomizedPawn customizedPawn, Hediff hediff) {
            Pawn pawn = customizedPawn?.Pawn;
            if (pawn == null) {
                return;
            }
            pawn.health.RemoveHediff(hediff);
            CostAffected?.Invoke();
        }
        public void UpdateBodyType(CustomizedPawn customizedPawn, BodyTypeDef bodyTypeDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.story.bodyType = bodyTypeDef;
            customizations.BodyType = bodyTypeDef;

            var bodyTypeGenes = DefDatabase<GeneDef>.AllDefs.Where(g => g.bodyType.HasValue);
            foreach (var g in bodyTypeGenes) {
                //BodyTypeDef bodyType = g.bodyType.Value;
                BodyTypeDef bodyType = g.bodyType.Value.ToBodyType(pawn);
                Logger.Debug("Body type gene: " + g.LabelCap + ", " + g.bodyType?.ToString() + ", " + bodyType?.LabelCap.ToString());
            }

            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateHeadType(CustomizedPawn customizedPawn, HeadTypeDef headTypeDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.story.headType = headTypeDef;
            customizations.HeadType = headTypeDef;
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateHair(CustomizedPawn customizedPawn, HairDef hairDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            customizations.Hair = hairDef;
            Customizer.ApplyHairCustomizationsToPawn(pawn, customizations);
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateHairColor(CustomizedPawn customizedPawn, Color color) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.story.HairColor = color;
            customizations.HairColor = color;
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateBeard(CustomizedPawn customizedPawn, BeardDef beardDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.style.beardDef = beardDef;
            customizations.Beard = beardDef;
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateFaceTattoo(CustomizedPawn customizedPawn, TattooDef tattooDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.style.FaceTattoo = tattooDef;
            customizations.FaceTattoo = tattooDef;
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateBodyTattoo(CustomizedPawn customizedPawn, TattooDef tattooDef) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.style.BodyTattoo = tattooDef;
            customizations.BodyTattoo = tattooDef;
            ClearPawnGraphicsCache(pawn);
        }
        public void UpdateSkinColor(CustomizedPawn customizedPawn, Color color) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            if (color == pawn.story.SkinColor) {
                return;
            }
            bool removeOverride = false;
            var melaninGeneDef = pawn.genes.GetMelaninGene();
            Gene activeSkinColorGene = null;
            if (pawn?.genes?.GenesListForReading != null) {
                activeSkinColorGene = pawn?.genes.GenesListForReading.Where(g => g.Active && g.def.skinColorOverride.HasValue && g.overriddenByGene == null).FirstOrDefault();
            }
            if (activeSkinColorGene == null && melaninGeneDef?.skinColorBase != null && melaninGeneDef.skinColorBase == color) {
                removeOverride = true;
            }
            if (removeOverride) {
                pawn.story.skinColorOverride = null;
            }
            else {
                pawn.story.skinColorOverride = color;
            }
            ClearPawnGraphicsCache(pawn);
            customizations.SkinColor = pawn.story.SkinColor;
            customizations.SkinColorOverride = pawn.story.skinColorOverride;
        }
        public void UpdateAlienAddon(CustomizedPawn customizedPawn, AlienRaceBodyAddon addon, int index) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            Logger.Debug("addon variant index = " + addon.VariantIndex + ", index = " + index);
            if (customizations.AlienRace != null) {
                ThingComp alienComp = pawn.AllComps.FirstOrDefault((ThingComp comp) => {
                    return (comp.GetType().Name == "AlienComp");
                });
                if (alienComp == null) {
                    return;
                }
                FieldInfo variantsField = ReflectionUtil.GetPublicField(alienComp, "addonVariants");
                if (variantsField == null) {
                    return;
                }
                List<int> variants = null;
                try {
                    variants = (List<int>)variantsField.GetValue(alienComp);
                }
                catch (Exception) {
                    return;
                }
                variants[addon.VariantIndex] = index;
            }
            ClearPawnGraphicsCache(pawn);
        }

        public void UpdateGender(CustomizedPawn customizedPawn, Gender gender) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null || gender == pawn.gender) {
                return;
            }
            Gender previousGender = pawn.gender;
            pawn.gender = gender;
            List<BodyTypeDef> bodyTypes = ProviderBodyTypes.GetBodyTypesForPawn(pawn);
            if (gender == Gender.Female) {
                if (pawn.story.hairDef.styleGender == StyleGender.Male) {
                    pawn.story.hairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.styleGender != StyleGender.Male;
                    });
                }
                if (pawn.story.bodyType == BodyTypeDefOf.Male) {
                    if (bodyTypes.Contains(BodyTypeDefOf.Female)) {
                        pawn.story.bodyType = BodyTypeDefOf.Female;
                    }
                }
                if (pawn.story.headType?.gender == Gender.Male) {
                    var matchingHeadType = ProviderHeadTypes.FindMatchingHeadTypeForOtherGenderOrDefault(pawn.story.headType, gender);
                    if (matchingHeadType != null) {
                        pawn.story.headType = matchingHeadType;
                    }
                }
            }
            else if (gender == Gender.Male) {
                if (pawn.story.hairDef.styleGender == StyleGender.Female) {
                    pawn.story.hairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.styleGender != StyleGender.Female;
                    });
                }
                if (pawn.story.bodyType == BodyTypeDefOf.Female) {
                    if (bodyTypes.Contains(BodyTypeDefOf.Male)) {
                        pawn.story.bodyType = BodyTypeDefOf.Male;
                    }
                }
                if (pawn.story.headType?.gender == Gender.Female) {
                    var matchingHeadType = ProviderHeadTypes.FindMatchingHeadTypeForOtherGenderOrDefault(pawn.story.headType, gender);
                    if (matchingHeadType != null) {
                        pawn.story.headType = matchingHeadType;
                    }
                }
            }
            ClearPawnGraphicsCache(pawn);
            PawnToCustomizationsMapper.MapGender(pawn, customizations);
            PawnToCustomizationsMapper.MapAppearance(pawn, customizations);
        }

        public void ClearPawnGraphicsCache(Pawn pawn) {
            UtilityPawns.ClearPawnGraphicsCache(pawn);
        }

        public void ClearPawnGraphicsCache(CustomizedPawn pawn) {
            UtilityPawns.ClearPawnGraphicsCache(pawn?.Pawn);
        }

        public void RemoveApparel(CustomizedPawn customizedPawn, Thing thing) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            if (pawn.apparel.WornApparel.Contains(thing)) {
                pawn.apparel.Remove(thing as Apparel);
                PawnToCustomizationsMapper.MapApparel(pawn, customizedPawn.Customizations);
                ClearPawnGraphicsCache(customizedPawn.Pawn);
            }
            CostAffected?.Invoke();
        }
        public void AddApparel(CustomizedPawn customizedPawn, CustomizationsApparel apparel) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            Customizer.ApplyApparelCustomizationToPawn(pawn, apparel);
            PawnToCustomizationsMapper.MapApparel(pawn, customizedPawn.Customizations);
            ClearPawnGraphicsCache(customizedPawn.Pawn);
            CostAffected?.Invoke();
        }
        public void SetApparel(CustomizedPawn customizedPawn, List<CustomizationsApparel> apparelList) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (apparelList == null || pawn == null || customizations == null) {
                return;
            }
            customizedPawn.Customizations.Apparel = apparelList.ConvertAll(x => x);
            Customizer.ApplyApparelCustomizationsToPawn(pawn, customizedPawn.Customizations);
            PawnToCustomizationsMapper.MapApparel(pawn, customizedPawn.Customizations);
            ClearPawnGraphicsCache(customizedPawn.Pawn);
            CostAffected?.Invoke();
        }

        public void RemovePossession(CustomizedPawn customizedPawn, ThingDef thingDef) {
            if (customizedPawn?.Customizations?.Possessions == null || thingDef == null) {
                return;
            }
            customizedPawn.Customizations.Possessions.RemoveAll(t => t.ThingDef == thingDef);
            CostAffected?.Invoke();
        }

        public void UpdatePossessionCount(CustomizedPawn customizedPawn, ThingDef thingDef, int newCount) {
            if (customizedPawn?.Customizations?.Possessions == null || thingDef == null) {
                return;
            }
            var possession = customizedPawn.Customizations.Possessions.FirstOrDefault(p => p.ThingDef == thingDef);
            if (possession != null) {
                possession.Count = newCount;
            }
            CostAffected?.Invoke();
        }

        public void RemoveAbility(CustomizedPawn customizedPawn, Ability ability) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.abilities?.RemoveAbility(ability.def);
            PawnToCustomizationsMapper.MapAbilities(pawn, customizations);
            CostAffected?.Invoke();
        }
        public void AddAbility(CustomizedPawn customizedPawn, AbilityDef def) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            pawn.abilities?.GainAbility(def);
            PawnToCustomizationsMapper.MapAbilities(pawn, customizations);
            CostAffected?.Invoke();
        }
        public void SetAbilities(CustomizedPawn customizedPawn, IEnumerable<AbilityDef> abilities) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            List<AbilityDef> toRemove = new List<AbilityDef>(pawn.abilities.abilities.Select(a => a.def));
            foreach (var def in toRemove) {
                pawn.abilities.RemoveAbility(def);
            }
            foreach (var a in abilities) {
                pawn.abilities.GainAbility(a);
            }
            PawnToCustomizationsMapper.MapAbilities(pawn, customizations);

            int minLevel = 0;
            foreach (var a in pawn.abilities.AllAbilitiesForReading) {
                if (a.def.IsPsycast) {
                    if (a.def.level > minLevel) {
                        minLevel = a.def.level;
                    }
                }
            }
            if (minLevel > 0) {
                int currentLevel = 0;
                var hediffs = pawn.health?.hediffSet?.hediffs?.Where(h => h is Hediff_Psylink);
                foreach (var hediff in hediffs) {
                    Hediff_Psylink psylink = hediff as Hediff_Psylink;
                    if (psylink.level > currentLevel) {
                        currentLevel = psylink.level;
                    }
                }
                if (currentLevel < minLevel) {
                    //Logger.Debug(string.Format("Need to add psylink.  Current level: {0}, Minimum needed level: {1}", currentLevel, minLevel));
                    AddPsylinkOrSetLevel(pawn, minLevel);
                }
                else {
                    //Logger.Debug(string.Format("Already have sufficent psylink level.  Current level: {0}, Minimum needed level: {1}", currentLevel, minLevel));
                }
            }
            else {
                //Logger.Debug("No abilities require a psylink");
            }

            CostAffected?.Invoke();
        }

        public void AddPsylinkOrSetLevel(Pawn pawn, int level) {
            Hediff_Psylink mainPsylinkSource = pawn.GetMainPsylinkSource();
            if (mainPsylinkSource == null) {
                mainPsylinkSource = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
                mainPsylinkSource.level = level;
                try {
                    mainPsylinkSource.suppressPostAddLetter = true;
                    pawn.health.AddHediff(mainPsylinkSource, pawn.health.hediffSet.GetBrain());
                    return;
                }
                finally {
                    mainPsylinkSource.suppressPostAddLetter = false;
                }
            }
            else {
                mainPsylinkSource.level = level;
            }
        }

        public void UpdateIdeo(CustomizedPawn customizedPawn, Ideo ideo) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            customizations.Ideo = ideo;
            Customizer.ApplyIdeoCustomizationToPawn(pawn, customizations);
        }

        public void UpdateCertainty(CustomizedPawn customizedPawn, float value) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            customizations.Certainty = value;
            Customizer.ApplyCertaintyCustomizationToPawn(pawn, customizations);
        }

        public void RandomizeIdeo(CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            Pawn_IdeoTracker ideo = pawn.ideo;
            if (ideo != null) {
                Ideo currentIdeo = ideo.Ideo;
                float certainty = ideo.Certainty;
                Ideo newIdeo = Find.IdeoManager.IdeosInViewOrder.Where(i => i != currentIdeo).RandomElement();
                ideo.SetIdeo(newIdeo);
            }
            // TODO 
            // set it in customizations
        }

        public void MapCustomizationsForPawn(CustomizedPawn customizedPawn) {
            customizedPawn.Customizations = PawnToCustomizationsMapper.Map(customizedPawn.Pawn);
        }

        public void SavePawn(CustomizedPawn customizedPawn, string filename) {
            MapCustomizationsForPawn(customizedPawn);
            PawnSaver.SaveToFile(customizedPawn, filename);
        }

        public void UpdateFavoriteColor(CustomizedPawn customizedPawn, Color? color) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            if (ModsConfig.IdeologyActive) {
                pawn.story.favoriteColor = color;
                PawnToCustomizationsMapper.MapFavoriteColor(pawn, customizations);
            }
        }

        public void RandomizeName(CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn?.Pawn;
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (pawn == null || customizations == null) {
                return;
            }
            Name name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null, false, pawn.genes.Xenotype);
            NameSingle currentNameSingle = pawn.Name as NameSingle;
            NameTriple currentNameTriple = pawn.Name as NameTriple;
            NameSingle newNameSingle = name as NameSingle;
            NameTriple newNameTriple = name as NameTriple;
            if (currentNameSingle != null) {
                if (newNameSingle != null) {
                    pawn.Name = newNameSingle;
                }
                else if (newNameTriple != null) {
                    pawn.Name = new NameSingle(newNameTriple.First);
                }
            }
            else if (currentNameTriple != null) {
                if (newNameTriple != null) {
                    pawn.Name = newNameTriple;
                }
                else if (newNameSingle != null) {
                    pawn.Name = new NameTriple(newNameSingle.Name, newNameSingle.Name, newNameSingle.Name);
                }
            }
            PawnToCustomizationsMapper.MapName(pawn, customizations);
        }

        public static bool CanWearApparel(CustomizedPawn customizedPawn) {
            Pawn pawn = customizedPawn.Pawn;
            if (pawn == null || pawn.apparel == null) {
                return false;
            }
            if (!(pawn.mutant?.Def.canWearApparel ?? true)) {
                return false;
            }
            return true;
        }
    }
}
