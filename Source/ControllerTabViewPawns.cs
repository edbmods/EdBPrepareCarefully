using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ControllerTabViewPawns {
        public delegate void PawnLayerOptionUpdatedHandler(PawnLayer pawnLayer, CustomizedPawn pawn, PawnLayerOption pawnLayerOption);
        public delegate void PawnLayerColorUpdatedHandler(PawnLayer pawnLayer, CustomizedPawn pawn, Color color);

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public PawnCustomizer Customizer { get; set; }
        public ManagerPawns PawnManager { get; set; }
        public ManagerRelationships RelationshipManager { get; set; }

        protected Dictionary<Type, PawnLayerOptionUpdatedHandler> PawnLayerOptionUpdateHandlers { get; set; } = new Dictionary<Type, PawnLayerOptionUpdatedHandler>();
        protected Dictionary<Type, PawnLayerColorUpdatedHandler> PawnLayerColorUpdateHandlers { get; set; } = new Dictionary<Type, PawnLayerColorUpdatedHandler>();


        public void Initialize() {
            // TODO: Move to page controller?
            ViewState.CurrentPawn = State.Customizations.ColonyPawns.FirstOrDefault();
        }


        public void SelectPawn(CustomizedPawn pawn) {
            ViewState.CurrentPawn = pawn;
        }

        public void MaximizeColonyPawnList() {
            ViewState.PawnListMode = PawnListMode.ColonyPawnsMaximized;
        }

        public void MaximizeWorldPawnList() {
            ViewState.PawnListMode = PawnListMode.WorldPawnsMaximized;
        }

        public void AddColonyPawn() {
            AddPawnWithPawnKind(CustomizedPawnType.Colony, null);
        }


        public void AddWorldPawn() {
            AddPawnWithPawnKind(CustomizedPawnType.World, null);
        }

        public void AddPawnWithPawnKind(CustomizedPawnType pawnType, PawnKindOption option) {
            CustomizedPawn customizedPawn = PawnManager.AddPawn(pawnType, option);
            if (pawnType == CustomizedPawnType.Colony) {
                SelectPawn(customizedPawn);
                if (ViewState.PawnListMode != PawnListMode.ColonyPawnsMaximized) {
                    ViewState.PawnListMode = PawnListMode.ColonyPawnsMaximized;
                }
            }
            else {
                SelectPawn(customizedPawn);
                if (ViewState.PawnListMode != PawnListMode.WorldPawnsMaximized) {
                    ViewState.PawnListMode = PawnListMode.WorldPawnsMaximized;
                }
            }
        }

        public void DeletePawn(CustomizedPawn pawn) {
            List<CustomizedPawn> pawnList = null;
            if (pawn.Type == CustomizedPawnType.Colony) {
                pawnList = State.Customizations.ColonyPawns;
                if (pawnList.Count < 2) {
                    return;
                }
            }
            else if (pawn.Type == CustomizedPawnType.World) {
                pawnList = State.Customizations.WorldPawns;
            }
            int index = pawnList.IndexOf(pawn);
            if (index == -1) {
                return;
            }
            RelationshipManager.RemoveAllRelationshipsForPawn(pawn.Pawn);
            if (PawnManager.RemovePawn(pawn)) {
                if (pawnList.Count > 0) {
                    if (index >= pawnList.Count) {
                        index = pawnList.Count - 1;
                    }
                    SelectPawn(pawnList[index]);
                }
                else {
                    SelectPawn(null);
                }
                ViewState.PawnRandomizerOptions.Remove(pawn);
            }
        }

        public void MoveColonyPawnToWorldPawnList(CustomizedPawn pawn, bool activatePawn) {
            if (State.Customizations.ColonyPawns.Count <= 1) {
                return;
            }
            PawnManager.ChangeColonyPawnToWorldPawn(pawn);
            if (activatePawn) {
                SelectPawn(pawn);
                ViewState.PawnListMode = PawnListMode.WorldPawnsMaximized;
            }
        }

        public void MoveWorldPawnToColonyPawnList(CustomizedPawn pawn, bool activatePawn) {
            PawnManager.ChangeWorldPawnToColonyPawn(pawn);
            if (activatePawn) {
                SelectPawn(pawn);
                ViewState.PawnListMode = PawnListMode.ColonyPawnsMaximized;
            }
        }
        public void LoadColonyPawn(string file) {
            LoadPawn(CustomizedPawnType.Colony, file);
        }
        public void LoadWorldPawn(string file) {
            LoadPawn(CustomizedPawnType.World, file);
        }

        public void LoadPawn(CustomizedPawnType type, string file) {
            var result = PawnManager.LoadPawn(type, file);
            result.Problems?.ForEach(p => {
                if (p.Severity == 1) {
                    Logger.Warning(p.Message);
                }
                else {
                    Logger.Debug(p.Message);
                }
            });
            if (result?.Pawn?.Pawn != null) {
                Messages.Message("EdB.PC.Dialog.PawnPreset.Loaded".Translate(file), MessageTypeDefOf.TaskCompletion);
                SelectPawn(result.Pawn);
            }
            else {
                Messages.Message("EdB.PC.Dialog.PawnPreset.Error.Failed".Translate(), MessageTypeDefOf.ThreatBig);
            }
        }

        public void SavePawn(string filename) {
            if (string.IsNullOrEmpty(filename)) {
                Logger.Warning("Trying to save a character without a name");
                return;
            }
            PawnManager.SavePawn(ViewState.CurrentPawn, filename);
            Messages.Message("SavedAs".Translate(filename), MessageTypeDefOf.TaskCompletion);
        }

        public void RandomizeCurrentPawn() {
            RelationshipManager.RemoveAllRelationshipsForPawn(ViewState.CurrentPawn.Pawn);
            if (ViewState.PawnRandomizerOptions.TryGetValue(ViewState.CurrentPawn, out var randomizerOptions)) {
                PawnManager.RandomizePawn(ViewState.CurrentPawn, randomizerOptions);
            }
            else {
                PawnManager.RandomizePawn(ViewState.CurrentPawn);
            }
        }

        public void UpdateFirstName(string name) {
            PawnManager.UpdateFirstName(ViewState.CurrentPawn, name);
        }
        public void UpdateNickName(string name) {
            PawnManager.UpdateNickName(ViewState.CurrentPawn, name);
        }
        public void UpdateLastName(string name) {
            PawnManager.UpdateLastName(ViewState.CurrentPawn, name);
        }

        public void UpdateBiologicalAge(int? ageYears, int? ageDays) {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn == null || customizedPawn.Pawn == null) {
                return;
            }
            int years = ageYears ?? customizedPawn.Pawn.ageTracker.AgeBiologicalYears;
            int days = ageDays ?? AgeModifier.TicksToDayOfYear(customizedPawn.Pawn.ageTracker.AgeBiologicalTicks);
            PawnManager.UpdatePawnBiologicalAge(ViewState.CurrentPawn, years, days);
        }

        public void UpdateChronologicalAge(int? ageYears, int? ageDays) {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn == null || customizedPawn.Pawn == null) {
                return;
            }
            int years = ageYears ?? customizedPawn.Pawn.ageTracker.AgeChronologicalYears;
            int days = ageDays ?? AgeModifier.TicksToDayOfYear(customizedPawn.Pawn.ageTracker.AgeChronologicalTicks);
            PawnManager.UpdatePawnChronologicalAge(ViewState.CurrentPawn, years, days);
        }

        public void UpdateBackstoryHandler(BackstorySlot slot, BackstoryDef backstory) {
            PawnManager.UpdatePawnBackstory(ViewState.CurrentPawn, slot, backstory);
        }

        public void RandomizeBackstory() {
            PawnManager.RandomizePawnBackstories(ViewState.CurrentPawn);
        }

        public void IncrementSkill(SkillDef skill) {
            AdjustSkillLevelByOne(skill, 1);
        }

        public void DecrementSkill(SkillDef skill) {
            AdjustSkillLevelByOne(skill, -1);
        }

        public void AdjustSkillLevelByOne(SkillDef skill, int plusOrMinusOne) {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            Pawn pawn = customizedPawn?.Pawn;
            if (pawn == null) {
                return;
            }
            CustomizationsPawn customizations = customizedPawn?.Customizations;
            if (customizations == null) {
                return;
            }
            var record = pawn.skills.GetSkill(skill);
            if (record != null) {
                PawnManager.SetSkillLevel(ViewState?.CurrentPawn, skill, record.Level + plusOrMinusOne);
            }
        }

        public void SetSkillLevel(SkillDef skill, int value) {
            PawnManager.SetSkillLevel(ViewState?.CurrentPawn, skill, value);
        }

        public void UpdateSkillPassion(SkillDef skill) {
            // Left-click increases passion. Right-click decreases it
            bool increase = Event.current.button != 1 ? true : false;
            if (Event.current.button != 1) {
                AdjustSkillPassion(skill, 1);
            }
            else {
                AdjustSkillPassion(skill, -1);
            }
        }

        public void AdjustSkillPassion(SkillDef skill, int direction) {
            if (direction == 0) {
                return;
            }
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn == null) {
                return;
            }
            Pawn pawn = customizedPawn.Pawn;
            if (pawn == null) {
                return;
            }
            SkillRecord record = pawn.skills.GetSkill(skill);
            if (record == null) {
                return;
            }
            Passion currentPassion = record.passion;
            Passion nextPassion = currentPassion;
            if (currentPassion == Passion.None) {
                nextPassion = direction > 0 ? Passion.Minor : Passion.Major;
            }
            else if (currentPassion == Passion.Minor) {
                nextPassion = direction > 0 ? Passion.Major : Passion.None;
            }
            else if (currentPassion == Passion.Major) {
                nextPassion = direction > 0 ? Passion.None : Passion.Minor;
            }
            PawnManager.UpdatePawnSkillPassion(customizedPawn, skill, nextPassion);
        }

        public void ClearSkillsAndPassions() {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn == null) {
                return;
            }
            PawnManager.ClearSkillLevels(customizedPawn);
            foreach (var skill in customizedPawn.Customizations.Skills) {
                PawnManager.UpdatePawnSkillPassion(customizedPawn, skill.SkillDef, Passion.None);
            }
        }
        public void ResetAddedSkillLevelsAndPassions() {
            CustomizedPawn customizedPawn = ViewState.CurrentPawn;
            if (customizedPawn == null) {
                return;
            }
            PawnManager.ResetSkillLevels(customizedPawn);
            foreach (var skill in customizedPawn.Customizations.Skills) {
                PawnManager.UpdatePawnSkillPassion(customizedPawn, skill.SkillDef, skill.OriginalPassion);
            }
        }

        public void AddTrait(Trait trait) {
            PawnManager.AddTrait(ViewState?.CurrentPawn, trait);
        }

        public void RemoveTrait(Trait trait) {
            PawnManager.RemoveTrait(ViewState?.CurrentPawn, trait);
        }

        public void RandomizeTraits() {
            PawnManager.RandomizeTraits(ViewState?.CurrentPawn);
        }

        public void UpdateTrait(int index, Trait trait) {
            Pawn pawn = ViewState?.CurrentPawn?.Pawn;
            if (pawn == null) {
                return;
            }
            var traits = pawn.story.traits.allTraits;
            if (index >= traits.Count) {
                return;
            }
            PawnManager.ReplaceTrait(ViewState?.CurrentPawn, traits[index], trait);
        }
        public void AddInjury(Injury injury) {
            PawnManager.AddPawnInjury(ViewState?.CurrentPawn, injury);
        }
        public void AddImplant(Implant implant) {
            PawnManager.AddPawnImplant(ViewState?.CurrentPawn, implant);
        }
        public void RemoveHediff(Hediff hediff) {
            PawnManager.RemovePawnHediff(ViewState?.CurrentPawn, hediff);
        }
        public void RemoveHediffs(IEnumerable<Hediff> hediffs) {
            PawnManager.RemovePawnHediffs(ViewState?.CurrentPawn, hediffs);
        }

        public void RandomizeAppearance() {
            PawnManager.RandomizeAppearance(ViewState?.CurrentPawn);
        }

        public void UpdateSkinColor(Color color) {
            PawnManager.UpdateSkinColor(ViewState?.CurrentPawn, color);
        }
        public void UpdateGender(Gender gender) {
            PawnManager.UpdateGender(ViewState?.CurrentPawn, gender);
        }

        public void UpdateImplants(IEnumerable<Implant> implants) {
            PawnManager.UpdateImplants(ViewState?.CurrentPawn, implants);
        }

        public void SetTraits(IEnumerable<Trait> traits) {
            PawnManager.SetTraits(ViewState?.CurrentPawn, traits);
        }

        public void RemoveApparel(Thing thing) {
            PawnManager.RemoveApparel(ViewState?.CurrentPawn, thing);
        }
        public void AddApparel(CustomizationsApparel apparel) {
            PawnManager.AddApparel(ViewState?.CurrentPawn, apparel);
        }

        public void SetApparel(List<CustomizationsApparel> apparelList) {
            PawnManager.SetApparel(ViewState?.CurrentPawn, apparelList);
        }
        public void RemovePossession(CustomizedPawn pawn, ThingDef thingDef) {
            PawnManager.RemovePossession(pawn, thingDef);
        }
        public void UpdatePossessionCount(CustomizedPawn pawn, ThingDef thingDef, int count) {
            PawnManager.UpdatePossessionCount(pawn, thingDef, count);
        }
        public void RemoveAbility(Ability ability) {
            PawnManager.RemoveAbility(ViewState?.CurrentPawn, ability);
        }
        public void AddAbility(AbilityDef def) {
            PawnManager.AddAbility(ViewState?.CurrentPawn, def);
        }
        public void SetAbilities(IEnumerable<AbilityDef> abilities) {
            PawnManager.SetAbilities(ViewState?.CurrentPawn, abilities);
        }

        public void UpdateIdeo(Ideo ideo) {
            PawnManager.UpdateIdeo(ViewState?.CurrentPawn, ideo);
        }

        public void RandomizeIdeo() {
            PawnManager.RandomizeIdeo(ViewState?.CurrentPawn);
        }

        public void UpdateCertainty(float value) {
            PawnManager.UpdateCertainty(ViewState?.CurrentPawn, value);
        }
        public void RegisterPawnLayerUpdateHandlers(Type type, PawnLayerOptionUpdatedHandler optionHandler, PawnLayerColorUpdatedHandler colorHandler) {
            RegisterPawnLayerOptionUpdateHandler(type, optionHandler);
            RegisterPawnLayerColorUpdateHandler(type, colorHandler);
        }

        public void RegisterPawnLayerOptionUpdateHandler(Type type, PawnLayerOptionUpdatedHandler handler) {
            PawnLayerOptionUpdateHandlers[type] = handler;
        }

        public void RegisterPawnLayerColorUpdateHandler(Type type, PawnLayerColorUpdatedHandler handler) {
            PawnLayerColorUpdateHandlers[type] = handler;
        }

        public void UpdatePawnLayerOption(PawnLayer pawnLayer, PawnLayerOption option) {
            if (PawnLayerOptionUpdateHandlers.TryGetValue(pawnLayer.GetType(), out PawnLayerOptionUpdatedHandler handler)) {
                handler(pawnLayer, ViewState?.CurrentPawn, option);
            }
        }
        public void UpdatePawnLayerColor(PawnLayer pawnLayer, Color color) {
            if (PawnLayerColorUpdateHandlers.TryGetValue(pawnLayer.GetType(), out PawnLayerColorUpdatedHandler handler)) {
                handler(pawnLayer, ViewState?.CurrentPawn, color);
            }
        }

        public void UpdateFavoriteColor(Color? color) {
            PawnManager.UpdateFavoriteColor(ViewState?.CurrentPawn, color);
        }

        public void RandomizeName() {
            PawnManager.RandomizeName(ViewState?.CurrentPawn);
        }
    }
}
