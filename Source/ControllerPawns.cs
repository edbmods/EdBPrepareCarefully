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
            Pawn pawn = randomizer.GenerateSameKindOfColonist(state.CurrentPawn);
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
            randomizer.RandomizeName(state.CurrentPawn);
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
            randomizer.RandomizeBackstory(state.CurrentPawn);
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
            randomizer.RandomizeTraits(state.CurrentPawn);
        }

        // Age-related actions.
        public void UpdateBiologicalAge(int age) {
            if (age < Constraints.AgeBiologicalMin) {
                age = Constraints.AgeBiologicalMin;
            }
            else if (age > Constraints.AgeBiologicalMax || age > state.CurrentPawn.ChronologicalAge) {
                if (age > Constraints.AgeBiologicalMax) {
                    age = Constraints.AgeBiologicalMax;
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
            randomizer.RandomizeAppearance(state.CurrentPawn);
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
            int index = PrepareCarefully.Instance.Pawns.IndexOf(pawn);
            if (index != -1) {
                state.CurrentPawnIndex = index;
            }
        }
        public void AddingPawn() {
            CustomPawn pawn = new CustomPawn(randomizer.GenerateColonist());
            PrepareCarefully.Instance.AddPawn(pawn);
            state.CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
            PawnAdded(pawn);
        }
        public void DeletePawn(CustomPawn pawn) {
            PrepareCarefully.Instance.Pawns.Remove(pawn);
            if (state.CurrentPawnIndex >= PrepareCarefully.Instance.Pawns.Count) {
                state.CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
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
            PrepareCarefully.Instance.AddPawn(pawn);
            state.CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
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
        public void AddFactionPawn(FactionDef def) {
            var kinds = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef arg) => {
                return (arg.defaultFactionType != null && arg.defaultFactionType.LabelCap == def.LabelCap);
            });
            PawnKindDef kindDef = kinds.RandomElementWithFallback(def.basicMemberKind);
            Faction faction = Faction.OfPlayer;
            if (def != Faction.OfPlayer.def) {
                faction = new Faction() {
                    def = def
                };
                FactionRelation rel = new FactionRelation();
                rel.other = Faction.OfPlayer;
                rel.goodwill = 50;
                rel.hostile = false;
                (typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(faction) as List<FactionRelation>).Add(rel);
            }
            Pawn pawn = randomizer.GeneratePawn(new PawnGenerationRequestWrapper() {
                Faction = faction,
                KindDef = kindDef,
                Context = PawnGenerationContext.NonPlayer
            }.Request);
            pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.inventory.DestroyAll(DestroyMode.Vanish);
            CustomPawn customPawn = new CustomPawn(pawn);

            customPawn.Pawn.SetFactionDirect(Faction.OfPlayer);
            PrepareCarefully.Instance.AddPawn(customPawn);
            state.CurrentPawnIndex = PrepareCarefully.Instance.Pawns.Count - 1;
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
