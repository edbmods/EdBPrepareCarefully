using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class Mod {
        public static readonly Version MinimumGameVersion = new Version(1, 3, 3102);
        private static Mod instance = new Mod();
        public static Mod Instance {
            get {
                if (instance == null) {
                    instance = new Mod();
                }
                return instance;
            }
        }

        public ModState State { get; set; }

        public void Clear() {
            instance = null;
        }
        
        public void Start(Page_ConfigureStartingPawns configureStartingPawnsPage) {
            ReflectionCache.Instance.Initialize();

            var state = new ModState() {
                OriginalPage = configureStartingPawnsPage
            };
            State = state;

            var equipmentDatabase = new EquipmentDatabase();

            var providerAlienRaces = new ProviderAlienRaces();
            var providerHair = new ProviderHair() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerBeards = new ProviderBeards() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerBodyTypes = new ProviderBodyTypes() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerHeadTypes = new ProviderHeadTypes() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerFaceTattoos = new ProviderFaceTattoos() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerBodyTattoos = new ProviderBodyTattoos() {
                ProviderAlienRaces = providerAlienRaces
            };
            var providerAgeLimits = new ProviderAgeLimits();
            var providerFactions = new ProviderFactions();
            var providerBackstories = new ProviderBackstories() {
                ProviderFactions = providerFactions
            };
            var providerPawnLayers = new ProviderPawnLayers() {
                ProviderAlienRaces = providerAlienRaces,
                ProviderHair = providerHair,
                ProviderBeards = providerBeards,
                ProviderBodyTypes = providerBodyTypes,
                ProviderHeadTypes = providerHeadTypes,
                ProviderBodyTattoos = providerBodyTattoos,
                ProviderFaceTattoos = providerFaceTattoos
            };
            var providerTitles = new ProviderTitles();
            var providerTraits = new ProviderTraits();
            var providerHealthOptions = new ProviderHealthOptions();
            var providerEquipmentTypes = new ProviderEquipment() {
                EquipmentDatabase = equipmentDatabase
            };
            providerEquipmentTypes.PostConstruction();
            var providerPawnKinds = new ProviderPawnKinds();

            var ageModifier = new AgeModifier();

            var viewState = new ViewState();

            var pawnToCustomizationsMapper = new MapperPawnToCustomizations() {
                ProviderHealth = providerHealthOptions,
                ProviderAlienRaces = providerAlienRaces
            };
            var pawnCustomizer = new PawnCustomizer();
            var pawnLoaderV3 = new PawnLoaderV3() {
                EquipmentDatabase = equipmentDatabase,
                ProviderHealthOptions = providerHealthOptions,
            };
            var pawnLoaderV5 = new PawnLoaderV5() {
                ProviderHealthOptions = providerHealthOptions
            };
            var pawnLoader = new PawnLoader() {
                PawnLoaderV3 = pawnLoaderV3,
                PawnLoaderV5 = pawnLoaderV5
            };
            var relationshipDefinitionHelper = new RelationshipDefinitionHelper();
            relationshipDefinitionHelper.PostConstruct();
            var relationshipManager = new ManagerRelationships() {
                State = state,
                RelationshipDefinitionHelper = relationshipDefinitionHelper,
            };
            var presetLoaderV3 = new PresetLoaderV3() {
                PawnLoaderV3 = pawnLoaderV3,
                EquipmentDatabase = equipmentDatabase,
                ManagerRelationships = relationshipManager,
            };
            var presetLoaderV5 = new PresetLoaderV5() {
                PawnLoaderV5 = pawnLoaderV5,
                EquipmentDatabase = equipmentDatabase,
                ManagerRelationships = relationshipManager,
            };
            var presetLoader = new PresetLoader() {
                PresetLoaderV3 = presetLoaderV3,
                PresetLoaderV5 = presetLoaderV5,
            };
            var pawnSaver = new PawnSaver() {
                ProviderHealthOptions = providerHealthOptions
            };
            var presetSaver = new PresetSaver() {
                PawnSaver = pawnSaver
            };
            var costCalculator = new CostCalculator() {
                ProviderHealthOptions = providerHealthOptions,
            };

            var pawnManager = new ManagerPawns() {
                State = state,
                PawnToCustomizationsMapper = pawnToCustomizationsMapper,
                Customizer = pawnCustomizer,
                ProviderAgeLimits = providerAgeLimits,
                ProviderAlienRaces = providerAlienRaces,
                ProviderBackstories = providerBackstories,
                ProviderHeadTypes = providerHeadTypes,
                ProviderBodyTypes = providerBodyTypes,
                AgeModifier = ageModifier,
                EquipmentDatabase = equipmentDatabase,
                PawnLoader = pawnLoader,
                PawnSaver = pawnSaver,
            };


            var equipmentManager = new ManagerEquipment() {
                State = state,
                EquipmentDatabase = equipmentDatabase
            };

            pawnManager.InitializeStateFromStartingPawns();
            relationshipManager.InitializeWithPawns(state.Customizations.AllPawns);
            equipmentManager.InitializeStateFromScenarioAndStartingPawns();
            State.StartingPoints = (int)(costCalculator.Calculate(State.Customizations.ColonyPawns, State.Customizations.Equipment).total);

            var controllerPawns = new ControllerTabViewPawns() {
                State = state,
                ViewState = viewState,
                Customizer = pawnCustomizer,
                PawnManager = pawnManager
            };
            var controllerRelationships = new ControllerTabViewRelationships() {
                State = state,
                ViewState = viewState,
                RelationshipManager = relationshipManager,
            };
            var controllerEquipment = new ControllerTabViewEquipment() {
                State = state,
                ViewState = viewState,
                EquipmentManager = equipmentManager,
            };
            ControllerPage pageController = new ControllerPage {
                State = state,
                ViewState = viewState,
                PawnTabViewController = controllerPawns,
                RelationshipTabViewController = controllerRelationships,
                CostCalculator = costCalculator,
                PawnCustomizer = pawnCustomizer,
                ManagerPawns = pawnManager,
                ManagerEquipment = equipmentManager,
                ManagerRelationships = relationshipManager,
                PresetLoader = presetLoader,
                PresetSaver = presetSaver,
            };
            bool largeUI = pageController.UseLargeUI();
            pageController.PostConstruct();

            var colonyPawnListPanel = new PanelColonyPawnListRefactored() {
                State = state,
                ViewState = viewState,
                ProviderPawnKinds = providerPawnKinds,
            };
            var colonyPawnListMinimizedPanel = new PanelColonyPawnListMinimized() {
                State = state,
                ViewState = viewState
            };
            var worldPawnListPanel = new PanelWorldPawnList() {
                State = state,
                ViewState = viewState,
                ProviderPawnKinds = providerPawnKinds,
            };
            var worldPawnListMinimizedPanel = new PanelWorldPawnListMinimized() {
                State = state,
                ViewState = viewState
            };
            var randomizePanel = new PanelRandomize() {
                State = state,
                ViewState = viewState
            };
            var namePanel = new PanelName() {
                State = state,
                ViewState = viewState
            };
            var saveCharacterPanel = new PanelSaveCharacter() {
                ViewState = viewState
            };
            var appearancePanel = new PanelAppearance() {
                State = state,
                ViewState = viewState,
                ProviderPawnLayers = providerPawnLayers,
                PawnController = controllerPawns
            };
            var apparelPanel = new PanelApparel() {
                State = state,
                ViewState = viewState,
                ProviderEquipmentTypes = providerEquipmentTypes
            }; 
            var possessionsPanel = new PanelPossessions() {
                State = state,
                ViewState = viewState,
            };
            var abilitiesPanel = new PanelAbilitiesRefactored() {
                State = state,
                ViewState = viewState,
            };
            abilitiesPanel.PostConstruct();
            var ideoPanel = new PanelIdeo() {
                State = state,
                ViewState = viewState,
            };
            var xenotypePanel = new PanelXenotype() {
                State = state,
                ViewState = viewState,
            };
            var agePanel = new PanelAgeRefactored() {
                State = state,
                ViewState = viewState,
                ProviderAgeLimits = providerAgeLimits
            };
            var backstoryPanel = new PanelBackstory() {
                State = state,
                ViewState = viewState,
                ProviderBackstories = providerBackstories
            };
            backstoryPanel.PostConstruct();
            var titlesPanel = new PanelTitles() {
                State = state,
                ViewState = viewState,
                ProviderTitles = providerTitles
            };
            var traitsPanel = new PanelTraits() {
                State = state,
                ViewState = viewState,
                ProviderTraits = providerTraits
            };
            var healthPanel = new PanelHealth() {
                State = state,
                ViewState = viewState,
                ProviderHealth = providerHealthOptions
            };
            var skillsPanel = new PanelSkills() {
                State = state,
                ViewState = viewState
            };
            var incapableOfPanel = new PanelIncapableOf() {
                State = state,
                ViewState = viewState
            };
            var parentChildRelationshipsPanel = new PanelRelationshipsParentChild() {
                State = state,
                ViewState = viewState,
                RelationshipManager = relationshipManager,
                Controller = controllerRelationships
            };
            var otherRelationshipsPanel = new PanelRelationshipsOther() {
                State = state,
                ViewState = viewState,
                RelationshipManager = relationshipManager
            };
            var availableEquipmentPanel = new PanelEquipmentAvailable() {
                State = state,
                ViewState = viewState,
                ProviderEquipment = providerEquipmentTypes,
                CostCalculator = costCalculator
            };
            availableEquipmentPanel.PostConstruct();
            var selectedEquipmentPanel = new PanelEquipmentSelected() {
                State = state,
                ViewState = viewState,
                EquipmentDatabase = equipmentDatabase
            };
            TabViewPawns tabViewPawns = new TabViewPawns() {
                State = state,
                ViewState = viewState,
                PanelColonyPawns = colonyPawnListPanel,
                PanelColonyPawnsMinimized = colonyPawnListMinimizedPanel,
                PanelWorldPawns = worldPawnListPanel,
                PanelWorldPawnsMinimized = worldPawnListMinimizedPanel,
                PanelRandomize = randomizePanel,
                PanelName = namePanel,
                PanelSaveCharacter = saveCharacterPanel,
                PanelAppearance = appearancePanel,
                PanelApparel = apparelPanel,
                PanelPossessions = possessionsPanel,
                PanelAge = agePanel,
                PanelBackstory = backstoryPanel,
                PanelTraits = traitsPanel,
                PanelHealth = healthPanel,
                PanelSkills = skillsPanel,
                PanelIncapableOf = incapableOfPanel,
                PanelTitles = titlesPanel,
                PanelAbilities = abilitiesPanel,
                PanelIdeo = ideoPanel,
                PanelXenotype = xenotypePanel,
                LargeUI = largeUI
            };
            tabViewPawns.PostConstruction();
            TabViewRelationships tabViewRelationships = new TabViewRelationships() {
                PanelRelationshipsParentChild = parentChildRelationshipsPanel,
                PanelRelationshipsOther = otherRelationshipsPanel
            };
            TabViewEquipment tabViewEquipment = new TabViewEquipment() {
                PanelAvailable = availableEquipmentPanel,
                PanelSelected = selectedEquipmentPanel,
            };

            PagePrepareCarefully page = new PagePrepareCarefully {
                State = state,
                ViewState = viewState,
                Controller = pageController,
                TabViewPawns = tabViewPawns,
                TabViewRelationships = tabViewRelationships,
                TabViewEquipment = tabViewEquipment,
                LargeUI = largeUI,
                EquipmentDatabase = equipmentDatabase,
            };
            page.PostConstruction();

            pawnManager.CostAffected += pageController.MarkCostsForRecalculation;
            equipmentManager.CostAffected += pageController.MarkCostsForRecalculation;

            colonyPawnListMinimizedPanel.Maximizing += controllerPawns.MaximizeColonyPawnList;
            colonyPawnListPanel.PawnSelected += controllerPawns.SelectPawn;
            colonyPawnListPanel.PawnDeleted += controllerPawns.DeletePawn;
            colonyPawnListPanel.AddingPawn += controllerPawns.AddColonyPawn;
            colonyPawnListPanel.PawnSwapped += controllerPawns.MoveColonyPawnToWorldPawnList;
            colonyPawnListPanel.AddingPawnWithPawnKind += controllerPawns.AddPawnWithPawnKind;
            colonyPawnListPanel.PawnLoaded += controllerPawns.LoadColonyPawn;

            worldPawnListMinimizedPanel.Maximizing += controllerPawns.MaximizeWorldPawnList;
            worldPawnListPanel.PawnSelected += controllerPawns.SelectPawn;
            worldPawnListPanel.PawnDeleted += controllerPawns.DeletePawn;
            worldPawnListPanel.AddingPawn += controllerPawns.AddWorldPawn;
            worldPawnListPanel.PawnSwapped += controllerPawns.MoveWorldPawnToColonyPawnList;
            worldPawnListPanel.AddingPawnWithPawnKind += controllerPawns.AddPawnWithPawnKind;
            worldPawnListPanel.PawnLoaded += controllerPawns.LoadWorldPawn;

            randomizePanel.RandomizeAllClicked += controllerPawns.RandomizeCurrentPawn;

            namePanel.FirstNameUpdated += controllerPawns.UpdateFirstName;
            namePanel.NickNameUpdated += controllerPawns.UpdateNickName;
            namePanel.LastNameUpdated += controllerPawns.UpdateLastName;
            namePanel.NameRandomized += controllerPawns.RandomizeName;

            saveCharacterPanel.CharacterSaved += controllerPawns.SavePawn;

            backstoryPanel.BackstoryUpdated += controllerPawns.UpdateBackstoryHandler;
            backstoryPanel.RandomizeButtonClicked += controllerPawns.RandomizeBackstory;
            backstoryPanel.FavoriteColorUpdated += controllerPawns.UpdateFavoriteColor;

            skillsPanel.PassionButtonClicked += controllerPawns.UpdateSkillPassion;
            skillsPanel.IncrementSkillButtonClicked += controllerPawns.IncrementSkill;
            skillsPanel.DecrementSkillButtonClicked += controllerPawns.DecrementSkill;
            skillsPanel.SkillBarClicked += controllerPawns.SetSkillLevel;
            skillsPanel.ClearSkillsButtonClicked += controllerPawns.ClearSkillsAndPassions;
            skillsPanel.ResetSkillsButtonClicked += controllerPawns.ResetAddedSkillLevelsAndPassions;

            traitsPanel.TraitAdded += controllerPawns.AddTrait;
            traitsPanel.TraitRemoved += controllerPawns.RemoveTrait;
            traitsPanel.TraitsRandomized += controllerPawns.RandomizeTraits;
            traitsPanel.TraitUpdated += controllerPawns.UpdateTrait;
            traitsPanel.TraitsSet += controllerPawns.SetTraits;

            appearancePanel.GenderUpdated += controllerPawns.UpdateGender;
            appearancePanel.SkinColorUpdated += controllerPawns.UpdateSkinColor;
            appearancePanel.RandomizeAppearance += controllerPawns.RandomizeAppearance;

            apparelPanel.ApparelRemoved += controllerPawns.RemoveApparel;
            apparelPanel.ApparelAdded += controllerPawns.AddApparel;
            apparelPanel.ApparelReplaced += controllerPawns.SetApparel;

            agePanel.BiologicalAgeUpdated += controllerPawns.UpdateBiologicalAge;
            agePanel.ChronologicalAgeUpdated += controllerPawns.UpdateChronologicalAge;

            healthPanel.InjuryAdded += controllerPawns.AddInjury;
            healthPanel.HediffsRemoved += controllerPawns.RemoveHediffs;
            healthPanel.ImplantsUpdated += controllerPawns.UpdateImplants;

            abilitiesPanel.AbilitiesSet += controllerPawns.SetAbilities;
            abilitiesPanel.AbilityRemoved += controllerPawns.RemoveAbility;

            ideoPanel.IdeoUpdated += controllerPawns.UpdateIdeo;
            ideoPanel.IdeoRandomized += controllerPawns.RandomizeIdeo;
            ideoPanel.CertaintyUpdated += controllerPawns.UpdateCertainty;

            otherRelationshipsPanel.RelationshipAdded += controllerRelationships.AddRelationship;
            otherRelationshipsPanel.RelationshipRemoved += controllerRelationships.RemoveRelationship;

            parentChildRelationshipsPanel.GroupAdded += controllerRelationships.AddParentChildGroup;
            parentChildRelationshipsPanel.ChildAddedToGroup += controllerRelationships.AddChildToParentChildGroup;
            parentChildRelationshipsPanel.ParentAddedToGroup += controllerRelationships.AddParentToParentChildGroup;
            parentChildRelationshipsPanel.ChildRemovedFromGroup += controllerRelationships.RemoveChildFromParentChildGroup;
            parentChildRelationshipsPanel.ParentRemovedFromGroup += controllerRelationships.RemoveParentFromParentChildGroup;

            // TODO:
            availableEquipmentPanel.EquipmentAdded += controllerEquipment.AddEquipment;
            availableEquipmentPanel.EquipmentAdded += selectedEquipmentPanel.EquipmentAdded;

            selectedEquipmentPanel.EquipmentCountUpdated += controllerEquipment.UpdateEquipmentCount;
            selectedEquipmentPanel.EquipmentRemoved += controllerEquipment.RemoveEquipment;
            selectedEquipmentPanel.PossessionRemoved += controllerPawns.RemovePossession;
            selectedEquipmentPanel.PossessionCountUpdated += controllerPawns.UpdatePossessionCount;

            page.PresetLoaded += pageController.LoadPreset;
            page.PresetSaved += pageController.SavePreset;

            // Register pawn layer handlers
            // Hair
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerHair), (layer, customizedPawn, option) => {
                pawnManager.UpdateHair(customizedPawn, (option as PawnLayerOptionHair).HairDef);
            });
            controllerPawns.RegisterPawnLayerColorUpdateHandler(typeof(PawnLayerHair), (layer, customizedPawn, color) => {
                pawnManager.UpdateHairColor(customizedPawn, color);
            });
            // Head Type
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerHead), (layer, customizedPawn, option) => {
                pawnManager.UpdateHeadType(customizedPawn, (option as PawnLayerOptionHead).HeadType);
            });
            controllerPawns.RegisterPawnLayerColorUpdateHandler(typeof(PawnLayerHead), (layer, customizedPawn, color) => {
                pawnManager.UpdateSkinColor(customizedPawn, color);
            });
            // Body
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerBody), (layer, customizedPawn, option) => {
                pawnManager.UpdateBodyType(customizedPawn, (option as PawnLayerOptionBody).BodyTypeDef);
            });
            controllerPawns.RegisterPawnLayerColorUpdateHandler(typeof(PawnLayerBody), (layer, customizedPawn, color) => {
                pawnManager.UpdateSkinColor(customizedPawn, color);
            });
            // Beard
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerBeard), (layer, customizedPawn, option) => {
                pawnManager.UpdateBeard(customizedPawn, (option as PawnLayerOptionBeard).BeardDef);
            });
            controllerPawns.RegisterPawnLayerColorUpdateHandler(typeof(PawnLayerBeard), (layer, customizedPawn, color) => {
                pawnManager.UpdateHairColor(customizedPawn, color);
            });
            // Face Tattoo
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerFaceTattoo), (layer, customizedPawn, option) => {
                pawnManager.UpdateFaceTattoo(customizedPawn, (option as PawnLayerOptionTattoo).TattooDef);
            });
            // Body Tattoo
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerBodyTattoo), (layer, customizedPawn, option) => {
                pawnManager.UpdateBodyTattoo(customizedPawn, (option as PawnLayerOptionTattoo).TattooDef);
            });
            // Alien Add-on
            controllerPawns.RegisterPawnLayerOptionUpdateHandler(typeof(PawnLayerAlienAddon), (layer, customizedPawn, option) => {
                var addonOption = option as PawnLayerOptionAlienAddon;
                var addonLayer = layer as PawnLayerAlienAddon;
                pawnManager.UpdateAlienAddon(customizedPawn, addonLayer.AlienAddon, addonOption.Index);
            });
            controllerPawns.RegisterPawnLayerColorUpdateHandler(typeof(PawnLayerAlienAddon), (layer, customizedPawn, color) => {
                var addonLayer = layer as PawnLayerAlienAddon;
                if (addonLayer.Skin) {
                    pawnManager.UpdateSkinColor(customizedPawn, color);
                }
                else if (addonLayer.Hair) {
                    pawnManager.UpdateHairColor(customizedPawn, color);
                }
            });

            Find.WindowStack.Add(page);
        }

        public void RestoreScenarioParts() {
            if (State?.OriginalScenarioParts != null) {
                // TODO: exception check?
                try {
                    ReflectionUtil.SetFieldValue(Find.Scenario, "parts", State.OriginalScenarioParts);
                }
                finally {
                    State.OriginalScenarioParts = null;
                }
            }
        }

    }
}
