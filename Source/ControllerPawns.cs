using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class ControllerPawns {
        public delegate void PawnAddedHandler(CustomPawn pawn);
        public delegate void PawnReplacedHandler(CustomPawn pawn);
        public delegate void ColonyPawnsMaximizedHandler();
        public delegate void WorldPawnsMaximizedHandler();
        public delegate void PawnListsSplitHandler();
        public event PawnAddedHandler PawnAdded;
        public event PawnReplacedHandler PawnReplaced;

        private State state;
        private Randomizer randomizer = new Randomizer();
        private ProviderAgeLimits ProviderAgeLimits = PrepareCarefully.Instance.Providers.AgeLimits;
        public ControllerPawns(State state) {
            this.state = state;
        }

        public void CheckPawnCapabilities() {
            List<string> missingWorkTypes = null;
            foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) {
                // If it's a required work type, then check to make sure at least one pawn can do it.
                if (w.requireCapableColonist) {
                    bool workTypeEnabledOnAtLeastOneColonist = false;
                    foreach (CustomPawn pawn in PrepareCarefully.Instance.Pawns.Where((pawn) => { return pawn.Type == CustomPawnType.Colonist; })) {
                        if (!pawn.Pawn.WorkTypeIsDisabled(w)) {
                            workTypeEnabledOnAtLeastOneColonist = true;
                            break;
                        }
                    }
                    // If the work type is not enabled on at least one pawn, then add it to the missing work types list.
                    if (!workTypeEnabledOnAtLeastOneColonist) {
                        if (missingWorkTypes == null) {
                            missingWorkTypes = new List<string>();
                        }
                        missingWorkTypes.Add(w.gerundLabel.CapitalizeFirst());
                    }
                }
            }
            state.MissingWorkTypes = missingWorkTypes;
        }

        public void RandomizeAll() {
            // Create the pawn.
            Pawn pawn = randomizer.GenerateKindOfPawn(state.CurrentPawn.Pawn.kindDef);
            if (pawn.Faction != Faction.OfPlayer) {
                pawn.SetFactionDirect(Faction.OfPlayer);
            }
            state.CurrentPawn.InitializeWithPawn(pawn);
            state.CurrentPawn.GenerateId();
            PawnReplaced(state.CurrentPawn);
        }

        // Name-related actions.
        public void UpdateFirstName(string name) {
            if (name.Length <= 12 && CharacterCardUtility.ValidNameRegex.IsMatch(name)) {
                state.CurrentPawn.FirstName = name;
            }
        }
        public void UpdateNickName(string name) {
            if (name.Length <= 9 && CharacterCardUtility.ValidNameRegex.IsMatch(name)) {
                state.CurrentPawn.NickName = name;
            }
        }
        public void UpdateLastName(string name) {
            if (name.Length <= 12 && CharacterCardUtility.ValidNameRegex.IsMatch(name)) {
                state.CurrentPawn.LastName = name;
            }
        }
        public void RandomizeName() {
            Pawn sourcePawn = randomizer.GenerateSameKindAndGenderOfPawn(state.CurrentPawn);
            Name name = PawnBioAndNameGenerator.GeneratePawnName(sourcePawn, NameStyle.Full, null);
            NameTriple nameTriple = name as NameTriple;
            state.CurrentPawn.Name = nameTriple;
        }

        // Backstory-related actions.
        public void UpdateBackstory(BackstorySlot slot, Backstory backstory) {
            if (slot == BackstorySlot.Childhood) {
                state.CurrentPawn.Childhood = backstory;
            }
            else if (slot == BackstorySlot.Adulthood) {
                state.CurrentPawn.Adulthood = backstory;
            }
        }

        public void RandomizeBackstories() {
            CustomPawn currentPawn = state.CurrentPawn;
            PawnKindDef kindDef = currentPawn.Pawn.kindDef;
            FactionDef factionDef = kindDef?.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }

            var providerAlienRaces = PrepareCarefully.Instance.Providers.AlienRaces;
            AlienRace alienRace = providerAlienRaces.GetAlienRace(currentPawn.Pawn.def);
            float adultStoryAge = alienRace == null ? providerAlienRaces.DefaultMinAgeForAdulthood : alienRace.MinAgeForAdulthood;
            //Logger.Debug(String.Format("Adulthood age for {0} is {1}", state.CurrentPawn.Pawn.def.defName, adultStoryAge));

            List<BackstoryCategoryFilter> backstoryCategoryFiltersFor = Reflection.PawnBioAndNameGenerator.GetBackstoryCategoryFiltersFor(currentPawn.Pawn, factionDef);
            // Generate a bio from which to get the backstories
            if (!Reflection.PawnBioAndNameGenerator.TryGetRandomUnusedSolidBioFor(backstoryCategoryFiltersFor, kindDef, currentPawn.Gender, null, out PawnBio pawnBio)) {
                // Other mods are patching the vanilla method in ways that cause it to return false.  If that happens,
                // we use our duplicate implementation instead.
                var providerBackstories = PrepareCarefully.Instance.Providers.Backstories;
                if (!PawnBioGenerator.TryGetRandomUnusedSolidBioFor(backstoryCategoryFiltersFor, kindDef, currentPawn.Gender, null, out pawnBio)) {
                    // If we still can't get a bio with our duplicate implementation, we pick backstories completely at random.
                    //Logger.Debug(String.Format("Using fallback method to get solid random backstories for kindDef {0} \"{1}\" and faction {2} \"{3}\"",
                    //    kindDef.defName, kindDef.LabelCap, factionDef.defName, factionDef.LabelCap));
                    currentPawn.Childhood = providerBackstories.GetChildhoodBackstoriesForPawn(currentPawn).RandomElement();
                    if (currentPawn.BiologicalAge >= (int) adultStoryAge) {
                        currentPawn.Adulthood = providerBackstories.GetAdulthoodBackstoriesForPawn(currentPawn).RandomElement();
                    }
                    return;
                }
            }
            currentPawn.Childhood = pawnBio.childhood;
            if (currentPawn.BiologicalAge >= (int) adultStoryAge) {
                currentPawn.Adulthood = pawnBio.adulthood;
            }
        }

        // Trait-related actions.
        public void AddTrait(Trait trait) {
            state.CurrentPawn.AddTrait(trait);
        }

        public void UpdateTrait(int index, Trait trait) {
            state.CurrentPawn.SetTrait(index, trait);
        }

        public void RemoveTrait(Trait trait) {
            state.CurrentPawn.RemoveTrait(trait);
        }

        public void RandomizeTraits() {
            Pawn pawn = randomizer.GenerateSameKindOfPawn(state.CurrentPawn);
            List<Trait> traits = pawn.story.traits.allTraits;
            state.CurrentPawn.ClearTraits();
            foreach (var trait in traits) {
                state.CurrentPawn.AddTrait(trait);
            }
        }

        public void RemoveAbility(Ability ability) {
            state.CurrentPawn.Pawn.abilities?.RemoveAbility(ability.def);
        }
        public void AddAbility(AbilityDef def) {
            state.CurrentPawn.Pawn.abilities?.GainAbility(def);
        }
        public void SetAbilities(IEnumerable<AbilityDef> abilities) {
            List<AbilityDef> toRemove = new List<AbilityDef>(state.CurrentPawn.Pawn.abilities.abilities.Select(a => a.def));
            foreach (var def in toRemove) {
                state.CurrentPawn.Pawn.abilities.RemoveAbility(def);
            }
            foreach (var a in abilities) {
                state.CurrentPawn.Pawn.abilities.GainAbility(a);
            }
        }

        // Age-related actions.
        public void UpdateBiologicalAge(int age) {
            int min = ProviderAgeLimits.MinAgeForPawn(state.CurrentPawn.Pawn);
            int max = ProviderAgeLimits.MaxAgeForPawn(state.CurrentPawn.Pawn);
            if (age < min) {
                age = min;
            }
            else if (age > max || age > state.CurrentPawn.ChronologicalAge) {
                if (age > max) {
                    age = max;
                }
                else {
                    age = state.CurrentPawn.ChronologicalAge;
                }
            }
            state.CurrentPawn.BiologicalAge = age;
        }

        public void UpdateChronologicalAge(int age) {
            if (age < state.CurrentPawn.BiologicalAge) {
                age = state.CurrentPawn.BiologicalAge;
            }
            if (age > Constraints.AgeChronologicalMax) {
                age = Constraints.AgeChronologicalMax;
            }
            state.CurrentPawn.ChronologicalAge = age;
        }

        // Appearance-related actions.
        public void RandomizeAppearance() {
            CustomPawn currentPawn = state.CurrentPawn;
            Pawn pawn = randomizer.GenerateSameKindAndGenderOfPawn(currentPawn);
            currentPawn.CopyAppearance(pawn);
        }
        
        // Skill-related actions.
        public void ResetSkills() {
            state.CurrentPawn.RestoreSkillLevelsAndPassions();
        }
        public void ClearSkills() {
            state.CurrentPawn.ClearSkills();
            state.CurrentPawn.ClearPassions();
        }
        public void UpdateSkillLevel(SkillDef skill, int level) {

        }
        public void UpdateSkillPassion(SkillDef skill, Passion level) {
            state.CurrentPawn.SetPassion(skill, level);
        }

        // Pawn-related actions.
        public void SelectPawn(CustomPawn pawn) {
            state.CurrentPawn = pawn;

        }
        public void AddingPawn(bool startingPawn) {
            CustomPawn pawn = new CustomPawn(randomizer.GenerateColonist());
            pawn.Type = startingPawn ? CustomPawnType.Colonist : CustomPawnType.World;
            PrepareCarefully.Instance.AddPawn(pawn);
            state.CurrentPawn = pawn;
            PawnAdded(pawn);
        }
        public void SwapPawn(CustomPawn pawn) {
            int worldPawnIndex = PrepareCarefully.Instance.WorldPawns.IndexOf(pawn);
            int colonyPawnIndex = PrepareCarefully.Instance.ColonyPawns.IndexOf(pawn);
            PrepareCarefully.Instance.Pawns.Remove(pawn);
            if (state.CurrentWorldPawn == pawn) {
                List<CustomPawn> worldPawns = PrepareCarefully.Instance.WorldPawns;
                if (worldPawnIndex > -1 && worldPawnIndex < worldPawns.Count) {
                    state.CurrentWorldPawn = worldPawns[worldPawnIndex];
                }
                else {
                    state.CurrentWorldPawn = worldPawns.LastOrDefault();
                }
            }
            if (state.CurrentColonyPawn == pawn) {
                List<CustomPawn> colonyPawns = PrepareCarefully.Instance.ColonyPawns;
                if (colonyPawnIndex > -1 && colonyPawnIndex < colonyPawns.Count) {
                    state.CurrentColonyPawn = colonyPawns[colonyPawnIndex];
                }
                else {
                    state.CurrentColonyPawn = colonyPawns.LastOrDefault();
                }
            }
            if (pawn.Type == CustomPawnType.Colonist) {
                pawn.Type = CustomPawnType.World;
                state.CurrentWorldPawn = pawn;
            }
            else {
                pawn.Type = CustomPawnType.Colonist;
                state.CurrentColonyPawn = pawn;
            }
            PrepareCarefully.Instance.Pawns.Add(pawn);
        }
        public void DeletePawn(CustomPawn pawn) {
            int worldPawnIndex = PrepareCarefully.Instance.WorldPawns.IndexOf(pawn);
            int colonyPawnIndex = PrepareCarefully.Instance.ColonyPawns.IndexOf(pawn);
            PrepareCarefully.Instance.Pawns.Remove(pawn);
            if (state.CurrentWorldPawn == pawn) {
                List<CustomPawn> worldPawns = PrepareCarefully.Instance.WorldPawns;
                if (worldPawnIndex > -1 && worldPawnIndex < worldPawns.Count) {
                    state.CurrentWorldPawn = worldPawns[worldPawnIndex];
                }
                else {
                    state.CurrentWorldPawn = worldPawns.LastOrDefault();
                }
            }
            if (state.CurrentColonyPawn == pawn) {
                List<CustomPawn> colonyPawns = PrepareCarefully.Instance.ColonyPawns;
                if (colonyPawnIndex > -1 && colonyPawnIndex < colonyPawns.Count) {
                    state.CurrentColonyPawn = colonyPawns[colonyPawnIndex];
                }
                else {
                    state.CurrentColonyPawn = colonyPawns.LastOrDefault();
                }
            }
            PrepareCarefully.Instance.RelationshipManager.DeletePawn(pawn);
        }
        public void LoadCharacter(string name) {
            if (string.IsNullOrEmpty(name)) {
                Logger.Warning("Trying to load a character without a name");
                return;
            }
            CustomPawn pawn = ColonistLoader.LoadFromFile(PrepareCarefully.Instance, name);
            if (pawn != null) {
                state.AddMessage("EdB.PC.Dialog.PawnPreset.Loaded".Translate(name));
            }
            else {
                state.AddError("Failed to load pawn");
                return;
            }
            bool colonyPawn = state.PawnListMode == PawnListMode.ColonyPawnsMaximized;
            pawn.Type = colonyPawn ? CustomPawnType.Colonist : CustomPawnType.World;
            // Regenerate a unique id in case the user is loading the same pawn more than once.
            pawn.GenerateId();
            PrepareCarefully.Instance.AddPawn(pawn);
            state.CurrentPawn = pawn;
            PawnAdded(pawn);
        }
        public void SaveCharacter(CustomPawn pawn, string filename) {
            if (string.IsNullOrEmpty(filename)) {
                Logger.Warning("Trying to save a character without a name");
                return;
            }
            ColonistSaver.SaveToFile(pawn, filename);
            state.AddMessage("SavedAs".Translate(filename));
        }
        public void AddFactionPawn(PawnKindDef kindDef, bool startingPawn) {
            Pawn pawn = null;
            try {
                //Logger.Debug("Adding new pawn with kindDef = " + kindDef.defName);
                var wrapper = new PawnGenerationRequestWrapper() {
                    Faction = Find.World.factionManager.OfPlayer,
                    KindDef = kindDef,
                    Context = PawnGenerationContext.NonPlayer,
                    WorldPawnFactionDoesntMatter = true
                };
                Ideo ideo = Find.FactionManager.OfPlayer?.ideos?.GetRandomIdeoForNewPawn();

                if (ideo != null) {
                    wrapper.FixedIdeology = ideo;
                }
                pawn = randomizer.GeneratePawn(wrapper.Request);
            }
            catch (Exception e) {
                Logger.Warning("Failed to create faction pawn of kind " + kindDef.defName, e);
                if (pawn != null) {
                    pawn.Destroy();
                }
                state.AddError("EdB.PC.Panel.PawnList.Error.FactionPawnFailed".Translate());
                return;
            }

            // Reset the quality and damage of all apparel.
            foreach (var a in pawn.apparel.WornApparel) {
                a.SetQuality(QualityCategory.Normal);
                a.HitPoints = a.MaxHitPoints;
            }

            // TODO: Revisit this if we add a UI to edit titles.
            // Clear out all titles.
            //if (pawn.royalty != null) {
            //    pawn.royalty = new Pawn_RoyaltyTracker(pawn);
            //}

            CustomPawn customPawn = new CustomPawn(pawn);
            customPawn.OriginalKindDef = kindDef;
            FactionDef factionDef = kindDef.defaultFactionType;
            customPawn.OriginalFactionDef = factionDef;
            if (pawn.Faction != Faction.OfPlayer) {
                pawn.SetFaction(Faction.OfPlayer);
            }

            customPawn.Type = startingPawn ? CustomPawnType.Colonist : CustomPawnType.World;
            if (!startingPawn) {
                CustomFaction customFaction = PrepareCarefully.Instance.Providers.Factions.FindRandomCustomFactionByDef(factionDef);
                if (customFaction != null) {
                    customPawn.Faction = customFaction;
                }
            }

            PrepareCarefully.Instance.AddPawn(customPawn);
            state.CurrentPawn = customPawn;
            PawnAdded(customPawn);
        }

        // Gender-related actions.
        public void UpdateGender(Gender gender) {
            state.CurrentPawn.Gender = gender;
        }

        // Health-related actions.
        public void AddInjury(Injury injury) {
            CustomPawn currentPawn = state.CurrentPawn;
            if (currentPawn != null) {
                state.CurrentPawn.AddInjury(injury);
            }
        }

        public void AddImplant(Implant implant) {
            CustomPawn currentPawn = state.CurrentPawn;
            if (currentPawn != null) {
                currentPawn.AddImplant(implant);
            }
        }

        public void RemoveHediff(Hediff hediff) {
            CustomPawn currentPawn = state.CurrentPawn;
            if (currentPawn != null) {
                Injury injury = currentPawn.Injuries.FirstOrDefault(i => i.Hediff == hediff);
                Implant implant = currentPawn.Implants.FirstOrDefault(i => i.Hediff == hediff);
                if (injury != null) {
                    currentPawn.RemoveCustomBodyParts(injury);
                }
                if (implant != null) {
                    currentPawn.RemoveCustomBodyParts(implant);
                }
            }
        }

        public void UpdateFavoriteColor(Color? color) {
            state.CurrentPawn.Pawn.story.favoriteColor = color;
        }
    }
}
