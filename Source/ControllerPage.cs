using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ControllerPage {
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ControllerTabViewPawns PawnTabViewController { get; set; }
        public ControllerTabViewRelationships RelationshipTabViewController { get; set; }
        public CostCalculator CostCalculator { get; set; }
        public PawnCustomizer PawnCustomizer { get; set; }
        public ManagerPawns ManagerPawns { get; set; }
        public ManagerEquipment ManagerEquipment { get; set; }
        public ManagerRelationships ManagerRelationships { get; set; }
        public PresetLoader PresetLoader { get; set; }
        public PresetSaver PresetSaver { get; set; }

        public void PostConstruct() {
            PawnTabViewController.Initialize();
        }

        public bool UseLargeUI() {
            Logger.Debug("Prepare Carefully window size: [" + Page.StandardSize.x + " x " + Page.StandardSize.y + "]");
            Logger.Debug("Screen: [" + Screen.width + ", " + Screen.height + "], dpi = " + Screen.dpi + ", resolution = " + Screen.currentResolution);
            Logger.Debug("Screen safe area: " + Screen.safeArea);
            Logger.Debug("UI scale: " + Prefs.UIScale);

            Vector2 maxSize = new Vector2(Screen.safeArea.width / Prefs.UIScale, Screen.safeArea.height / Prefs.UIScale);
            Vector2 padding = new Vector2(64, 64) / Prefs.UIScale;
            maxSize -= padding;

            Vector2 largeSize = new Vector2(1350, Page.StandardSize.y);
            if (maxSize.x >= largeSize.x && maxSize.y >= largeSize.y) {
                return true;
            }
            else {
                return false;
            }
        }

        public bool CancellingRequiresConfirmation() {
            return true;
            // TODO: Revisit this
            //if (State.OriginalPawnCustomizations.Count != State.Customizations.AllPawns.Count()) {
            //    return true;
            //}
            //foreach (var startingPawn in State.OriginalPawnCustomizations.Keys) {
            //    if (!State.Customizations.AllPawns.Select(p => p.Pawn).Contains(startingPawn)) {
            //        return true;
            //    }
            //}
            //foreach (var customizedPawn in State.Customizations.AllPawns) {
            //    if (!State.OriginalPawnCustomizations.TryGetValue(customizedPawn.Pawn, out var customizations)) {
            //        return true;
            //    }
            //    if (!Equals(customizedPawn.Customizations, customizations)) {
            //        return true;
            //    }
            //}
            //return false;
        }

        public void CancelCustomizations() {
            foreach (var pawn in Find.GameInitData.startingAndOptionalPawns) {
                if (State.OriginalPawnCustomizations.TryGetValue(pawn, out var customization)) {
                    PawnCustomizer.ApplyAllCustomizationsToPawn(pawn, customization);
                }
                else {
                    Logger.Warning(string.Format("There may have been a problem undoing all pawn customizations.  The original state of a pawn was not stored properly"));
                }
            }
            foreach (var pawn in State.Customizations.AllPawns) {
                if (pawn.Pawn != null && !Find.GameInitData.startingAndOptionalPawns.Contains(pawn.Pawn)) {
                    ManagerPawns.DestroyPawn(pawn.Pawn);
                }
            }
        }

        // TODO: Move into Validator class?
        public bool Validate(/*out string confirmationType*/) {

            // TODO: This is no good as it is.  In vanilla, if the player only has a nickname, it copies that nickname into the
            // first and last names.  We need to do something similar and adjust this validation accordingly.
            //foreach (CustomPawn current in PrepareCarefully.Instance.Pawns) {
            //    if (!current.Name.IsValid) {
            //        Messages.Message("EveryoneNeedsValidName".Translate(), MessageTypeDefOf.RejectInput, false);
            //        return false;
            //    }
            //}
            //confirmationType = "Standard";
            return true;
        }

        public void StartGame() {

            PreparePawns();
            PrepareRelationships();
            PrepareEquipment();

            // Copy of method logic for Page.DoNext()
            // Performs the logic from the Page.DoNext() method in the base Page class instead of calling the DoNext()
            // override in Page_ConfigureStartingPawns.  We want to prevent the missing required work type dialog from
            // appearing in the context of the configure pawns page.
            var page = State.OriginalPage;
            if (page != null) {
                Page next = page.next;
                Action nextAction = page.nextAct;
                if (next != null) {
                    Verse.Find.WindowStack.Add(next);
                }
                nextAction?.Invoke();
                TutorSystem.Notify_Event("PageClosed");
                TutorSystem.Notify_Event("GoToNextPage");
                page.Close(true);
            }
        }

        public void PreparePawns() {
            List<Pawn> pawns = new List<Pawn>();
            HashSet<Pawn> pawnLookup = new HashSet<Pawn>();
            foreach (var customPawn in State.Customizations.AllPawns) {
                customPawn.Pawn.SetFactionDirect(Faction.OfPlayer);
                if (customPawn.Type == CustomizedPawnType.Colony) {
                    if (customPawn.Pawn.workSettings == null) {
                        customPawn.Pawn.workSettings = new Pawn_WorkSettings(customPawn.Pawn);
                    }
                    customPawn.Pawn.workSettings.EnableAndInitialize();
                }
                pawns.Add(customPawn.Pawn);
                pawnLookup.Add(customPawn.Pawn);
                var possessions = customPawn.Customizations.Possessions.Where(p => p.ThingDef != null && p.Count > 0);
                Find.GameInitData.startingPossessions[customPawn.Pawn] = possessions.Select(p => new ThingDefCount(p.ThingDef, p.Count)).ToList();
            }
            // Remove any starting possessions that don't belong to one of our customized pawns
            List<Pawn> keysToRemove = new List<Pawn>();
            foreach (var key in Find.GameInitData.startingPossessions.Keys) {
                if (!pawnLookup.Contains(key)) {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove) {
                Find.GameInitData.startingPossessions.Remove(key);
            }
            // Destroy any starting pawn that are not in our customized pawn list
            foreach (var pawn in Find.GameInitData.startingAndOptionalPawns) {
                if (!State.Customizations.AllPawns.Select(p => p.Pawn).Contains(pawn)) {
                    Logger.Debug("Destroyed starting pawn: " + pawn.LabelCap);
                    ManagerPawns.DestroyPawn(pawn);
                }
                else {
                    Logger.Debug("Kept starting pawn: " + pawn.LabelCap);
                }
            }
            Find.GameInitData.startingPawnCount = State.Customizations.ColonyPawns.Count;
            Find.GameInitData.startingAndOptionalPawns = pawns;
        }

        public void PrepareRelationships() {
            var builder = RelationshipTabViewController.RelationshipManager.GetRelationshipBuilder();
            builder.Build();
        }

        public void PrepareEquipment() {
            List<ScenPart> originalScenarioParts = ReflectionUtil.GetFieldValue<List<ScenPart>>(Find.Scenario, "parts");

            // Create a list of scenario parts that we're going to use to spawn into the map
            List<ScenPart> scenarioPartsToUse = new List<ScenPart>();

            // Sort the equipment from highest count to lowest so that gear is less likely to get blocked
            // if there's a bulk item included.  If you don't do this, then a large number of an item (meals,
            // for example) could fill up the spawn area right away and then the rest of the items would have
            // nowhere to spawn.
            // TODO: Do we really need this?
            //State.Equipment.Sort((CustomizedEquipment a, CustomizedEquipment b) => {
            //    return a.Count.CompareTo(b.Count);
            //});

            // Remove any scenario parts that we're going to replace, i.e. parts that add equipment
            foreach (var part in originalScenarioParts) {
                //Logger.Debug("Scenario part defName: " + part.def?.defName);
                if (!State.ReplacedScenarioParts.Contains(part)) {
                    scenarioPartsToUse.Add(part);
                }
            }

            // Add scenario parts for all of our customized equipment selections
            foreach (var equipment in State.Customizations.Equipment) {
                ScenPart part = CreateScenarioPartForCustomizedEquipment(equipment);
                if (part != null) {
                    scenarioPartsToUse.Add(part);
                }
            }

            ReflectionUtil.SetFieldValue(Find.Scenario, "parts", scenarioPartsToUse);
        }
        public bool ShouldReplaceScenarioPart(ScenPart part) {
            if (part.GetType() == typeof(ScenPart_ScatterThingsNearPlayerStart)) {
                return true;
            }
            if (part.GetType() == typeof(ScenPart_StartingThing_Defined)) {
                return true;
            }
            return false;
        }

        public ScenPart CreateScenarioPartForCustomizedEquipment(CustomizedEquipment equipment) {
            Logger.Debug(string.Format("AddScenarioPartForCustomizedEquipment({0}), Animal = {1}", equipment.EquipmentOption?.ThingDef?.defName, equipment.Animal));
            if (equipment.Animal) {
                return CreateStartingAnimalScenarioPart(equipment);
            }
            if (equipment.SpawnType == EquipmentSpawnType.SpawnsWith) {
                return CreateStartsWithScenarioPart(equipment);
            }
            else if (equipment.SpawnType == EquipmentSpawnType.SpawnsNear) {
                return CreateScatterThingsNearScenarioPart(equipment);
            }
            else {
                return null;
            }
        }

        public ScenPart CreateStartsWithScenarioPart(CustomizedEquipment equipment) {
            ScenPart_StartingThing_Defined part = new ScenPart_StartingThing_Defined() {
                def = DefDatabase<ScenPartDef>.GetNamedSilentFail("StartingThing_Defined")
            };
            part.SetPrivateField("thingDef", equipment.EquipmentOption?.ThingDef);
            part.SetPrivateField("stuff", equipment.StuffDef);
            part.SetPrivateField("count", equipment.Count);
            part.SetPrivateField("quality", equipment.Quality);
            return part;
        }

        public ScenPart CreateScatterThingsNearScenarioPart(CustomizedEquipment equipment) {
            var part = new ScenPart_ScatterThingsNearPlayerStart() {
                def = DefDatabase<ScenPartDef>.GetNamedSilentFail("ScatterThingsNearPlayerStart")
            };
            part.SetPrivateField("thingDef", equipment.EquipmentOption?.ThingDef);
            part.SetPrivateField("stuff", equipment.StuffDef);
            part.SetPrivateField("count", equipment.Count);
            part.SetPrivateField("quality", equipment.Quality);
            return part;
        }

        public ScenPart CreateStartingAnimalScenarioPart(CustomizedEquipment equipment) {
            if (equipment.EquipmentOption.RandomAnimal) {
                return CreateRandomStartingAnimalScenarioPart(equipment);
            }
            else if (equipment.Gender.HasValue) {
                return CreateStartingAnimalWithSpecificGenderScenarioPart(equipment);
            }
            else {
                return CreateStartingAnimalWithRandomGenderScenarioPart(equipment);
            }
        }
        public ScenPart CreateRandomStartingAnimalScenarioPart(CustomizedEquipment equipment) {
            ScenPartDef scenPartDef = DefDatabase<ScenPartDef>.GetNamedSilentFail("StartingAnimal");
            if (scenPartDef == null) {
                Logger.Warning("Could not find definition for starting animal scenario part.  Cannot add scenario part");
                return null;
            }
            var part = new ScenPart_StartingAnimal() {
                def = scenPartDef
            };
            part.SetPrivateField("count", equipment.Count);
            return part;
        }
        public ScenPart CreateStartingAnimalWithRandomGenderScenarioPart(CustomizedEquipment equipment) {
            ScenPartDef scenPartDef = DefDatabase<ScenPartDef>.GetNamedSilentFail("StartingAnimal");
            if (scenPartDef == null) {
                Logger.Warning("Could not find definition for starting animal scenario part.  Cannot add scenario part");
                return null;
            }
            PawnKindDef pawnKindDef = FindPawnKindDefForAnimal(equipment);
            if (pawnKindDef == null) {
                Logger.Warning(string.Format("Could not spawn selected animal ({0}). Could not find matching pawn kind", equipment.EquipmentOption?.ThingDef?.defName));
                return null;
            }
            var part = new ScenPart_StartingAnimal() {
                def = scenPartDef
            };
            part.SetPrivateField("animalKind", pawnKindDef);
            part.SetPrivateField("count", equipment.Count);
            return part;
        }
        public ScenPart CreateStartingAnimalWithSpecificGenderScenarioPart(CustomizedEquipment equipment) {
            PawnKindDef pawnKindDef = FindPawnKindDefForAnimal(equipment);
            if (pawnKindDef == null) {
                Logger.Warning(string.Format("Could not spawn selected animal ({0}). Could not find matching pawn kind", equipment.EquipmentOption?.ThingDef?.defName));
                return null;
            }
            ScenPart_CustomAnimal part = new ScenPart_CustomAnimal() {
                Count = equipment.Count,
                Gender = equipment.Gender.Value,
                KindDef = pawnKindDef
            };
            return part;
        }
        public PawnKindDef FindPawnKindDefForAnimal(CustomizedEquipment equipment) {
            return (from td in DefDatabase<PawnKindDef>.AllDefs where td.race == equipment.EquipmentOption.ThingDef select td).FirstOrDefault();
        }

        public void MarkCostsForRecalculation() {
            ViewState.CostCalculationDirtyFlag = true;
        }

        public void RecalculateCosts() {
            State.PointCost = CostCalculator.Calculate(State.Customizations.ColonyPawns, State.Customizations.Equipment);
        }

        public void LoadPreset(string filename) {
            PresetLoaderResult result = PresetLoader.LoadFromFile(filename);
            Customizations customizations = result?.Customizations;

            result.Problems?.ForEach(p => {
                if (p.Severity == 1) {
                    Logger.Warning(p.Message);
                }
                else {
                    Logger.Debug(p.Message);
                }
            });

            if (customizations == null) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.FailedToLoad".Translate(filename), MessageTypeDefOf.ThreatBig);
                return;
            }
            if (customizations.ColonyPawns.Count < 1) {
                Messages.Message("EdB.PC.Dialog.Preset.Error.FailedToLoad".Translate(filename), MessageTypeDefOf.ThreatBig);
                Logger.Warning("Could not load preset because no colony pawns were loaded");
                return;
            }

            Messages.Message("EdB.PC.Dialog.Preset.Loaded".Translate(filename), MessageTypeDefOf.TaskCompletion);

            ManagerPawns.ClearPawns();
            foreach (var customizedPawn in customizations.AllPawns) {
                customizedPawn.Pawn = ManagerPawns.Customizer.CreatePawnFromCustomizations(customizedPawn.Customizations);
                ManagerPawns.AddPawnToPawnList(customizedPawn);
            }
            ManagerEquipment.ClearEquipment();
            foreach (var customizedEquipment in customizations.Equipment) {
                ManagerEquipment.AddEquipment(customizedEquipment);
            }
            ManagerRelationships.Clear();
            State.Customizations.Relationships = customizations.Relationships;
            State.Customizations.ParentChildGroups = customizations.ParentChildGroups;
            PawnTabViewController.SelectPawn(result.Customizations.ColonyPawns.FirstOrDefault());
        }

        public void SavePreset(string filename) {
            PresetSaver.SaveToFile(State, filename);
        }
    }
}
