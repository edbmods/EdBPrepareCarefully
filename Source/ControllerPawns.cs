using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private Regex validNameRegex;
        public ControllerPawns(State state) {
            this.state = state;
            validNameRegex = typeof(CharacterCardUtility).GetField("validNameRegex",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Regex;
        }

        public void RandomizeAll() {
            // Start by picking a new pawn kind def from the faction.
            FactionDef factionDef = state.CurrentPawn.Pawn.kindDef.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }
            //PawnKindDef kindDef = PrepareCarefully.Instance.Providers.Factions.GetPawnKindsForFactionDefLabel(factionDef)
            //    .RandomElementWithFallback(factionDef.basicMemberKind);
            // Create the pawn.
            Pawn pawn = randomizer.GenerateKindOfPawn(state.CurrentPawn.Pawn.kindDef);
            state.CurrentPawn.InitializeWithPawn(pawn);
            state.CurrentPawn.GenerateId();
            PawnReplaced(state.CurrentPawn);
        }

        // Name-related actions.
        public void UpdateFirstName(string name) {
            if (name.Length <= 12 && validNameRegex.IsMatch(name)) {
                state.CurrentPawn.FirstName = name;
            }
        }
        public void UpdateNickName(string name) {
            if (name.Length <= 9 && validNameRegex.IsMatch(name)) {
                state.CurrentPawn.NickName = name;
            }
        }
        public void UpdateLastName(string name) {
            if (name.Length <= 12 && validNameRegex.IsMatch(name)) {
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
            FactionDef factionDef = kindDef.defaultFactionType;
            if (factionDef == null) {
                factionDef = Faction.OfPlayer.def;
            }
            MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("FillBackstorySlotShuffled", BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = new object[] { currentPawn.Pawn, BackstorySlot.Childhood, null, factionDef };
            method.Invoke(null, arguments);
            currentPawn.Childhood = arguments[2] as Backstory;
            arguments = new object[] { currentPawn.Pawn, BackstorySlot.Adulthood, null, factionDef };
            method.Invoke(null, arguments);
            currentPawn.Adulthood = arguments[2] as Backstory;
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

        // Age-related actions.
        public void UpdateBiologicalAge(int age) {
            int min = state.CurrentPawn.MinAge;
            int max = state.CurrentPawn.MaxAge;
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
                Log.Warning("Trying to load a character without a name");
                return;
            }
            CustomPawn pawn = ColonistLoader.LoadFromFile(PrepareCarefully.Instance, name);
            if (pawn != null) {
                state.AddMessage("EdB.PC.Dialog.PawnPreset.Loaded".Translate(new object[] { name }));
            }
            bool colonyPawn = state.PawnListMode == PawnListMode.ColonyPawnsMaximized;
            pawn.Type = colonyPawn ? CustomPawnType.Colonist : CustomPawnType.World;
            PrepareCarefully.Instance.AddPawn(pawn);
            state.CurrentPawn = pawn;
            PawnAdded(pawn);
        }
        public void SaveCharacter(CustomPawn pawn, string filename) {
            if (string.IsNullOrEmpty(filename)) {
                Log.Warning("Trying to save a character without a name");
                return;
            }
            ColonistSaver.SaveToFile(pawn, filename);
            state.AddMessage("SavedAs".Translate(new object[] {
                filename
            }));
        }
        public void AddFactionPawn(PawnKindDef kindDef, bool startingPawn) {
            FactionDef factionDef = kindDef.defaultFactionType;
            Faction faction = PrepareCarefully.Instance.Providers.Factions.GetFaction(factionDef);

            // Workaround to force pawn generation to skip adding weapons to the pawn.
            // Might be a slightly risky hack, but the finally block should guarantee that
            // the weapons money range always gets set back to its original value.
            // TODO: Try to remove this at a later date.  It would be nice if the pawn generation
            // request gave you an option to skip weapon and equipment generation.
            FloatRange savedWeaponsMoney = kindDef.weaponMoney;
            kindDef.weaponMoney = new FloatRange(0, 0);
            Pawn pawn = null;
            try {
                pawn = randomizer.GeneratePawn(new PawnGenerationRequestWrapper() {
                    Faction = faction,
                    KindDef = kindDef,
                    Context = PawnGenerationContext.NonPlayer,
                    WorldPawnFactionDoesntMatter = true
                }.Request);
                if (pawn.equipment != null) {
                    pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                }
                if (pawn.inventory != null) {
                    pawn.inventory.DestroyAll(DestroyMode.Vanish);
                }
            }
            catch (Exception e) {
                Log.Warning("Failed to create faction pawn of kind " + kindDef.defName);
                Log.Message(e.Message);
                Log.Message(e.StackTrace);
                if (pawn != null) {
                    pawn.Destroy();
                }
                state.AddError("EdB.PC.Panel.PawnList.Error.FactionPawnFailed".Translate());
                return;
            }
            finally {
                kindDef.weaponMoney = savedWeaponsMoney;
            }
            // Reset the quality and damage of all apparel.
            foreach (var a in pawn.apparel.WornApparel) {
                a.SetQuality(QualityCategory.Normal);
                a.HitPoints = a.MaxHitPoints;
            }

            CustomPawn customPawn = new CustomPawn(pawn);
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
            state.CurrentPawn.AddInjury(injury);
        }
        public void AddImplant(Implant implant) {
            state.CurrentPawn.AddImplant(implant);
        }
    }
}
