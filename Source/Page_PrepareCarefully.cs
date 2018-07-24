using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class Page_PrepareCarefully : Page {
        public delegate void StartGameHandler();
        public delegate void PresetHandler(string name);
        
        public event StartGameHandler GameStarted;
        public event PresetHandler PresetLoaded;
        public event PresetHandler PresetSaved;

        private TabViewPawns tabViewPawns = new TabViewPawns();
        private TabViewEquipment tabViewEquipment = new TabViewEquipment();
        private TabViewRelationships tabViewRelationships = new TabViewRelationships();
        private List<ITabView> tabViews = new List<ITabView>();
        private List<TabRecord> tabRecords = new List<TabRecord>();
        private bool pawnListActionThisFrame = false;

        private Controller controller;

        public Page_PrepareCarefully() {
            this.closeOnCancel = false;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = false;
            this.doCloseButton = false;
            this.doCloseX = false;

            // Add the tab views to the tab view list.
            tabViews.Add(tabViewPawns);
            tabViews.Add(tabViewRelationships);
            tabViews.Add(tabViewEquipment);
            
            // Create a tab record UI widget for each tab view.
            foreach (var tab in tabViews) {
                ITabView currentTab = tab;
                TabRecord tabRecord = new TabRecord(currentTab.Name, delegate {
                    // When a new tab is selected, mark the previously selected TabRecord as unselected and the current one as selected.
                    // Also, update the State to reflected the newly selected ITabView.
                    if (State.CurrentTab != null) {
                        State.CurrentTab.TabRecord.selected = false;
                    }
                    State.CurrentTab = currentTab;
                    currentTab.TabRecord.selected = true;
                }, false);
                currentTab.TabRecord = tabRecord;
                tabRecords.Add(tabRecord);
            }
        }

        override public void OnAcceptKeyPressed() {
            // Don't close the window if the user clicks the "enter" key.
        }
        override public void OnCancelKeyPressed() {
            // Confirm that the user wants to quit if they click the escape key.
            ConfirmExit();
        }

        public State State {
            get {
                return PrepareCarefully.Instance.State;
            }
        }

        public Configuration Config {
            get {
                return PrepareCarefully.Instance.Config;
            }
        }
        public override string PageTitle {
            get {
                return "EdB.PC.Page.Title".Translate();
            }
        }

        public override void PreOpen() {
            base.PreOpen();
            // Set the default tab view to the first tab and the selected pawn to the first pawn.
            State.CurrentTab = tabViews[0];
            State.CurrentColonyPawn = State.ColonyPawns.FirstOrDefault();
            State.CurrentWorldPawn = State.WorldPawns.FirstOrDefault();

            controller = new Controller(State);
            InstrumentPanels();
        }


        public override void DoWindowContents(Rect inRect) {
            pawnListActionThisFrame = false;
            base.DrawPageTitle(inRect);
            Rect mainRect = base.GetMainRect(inRect, 30f, false);
            Widgets.DrawMenuSection(mainRect);
            TabDrawer.DrawTabs(mainRect, tabRecords);

            // Determine the size of the tab view and draw the current tab.
            Vector2 SizePageMargins = new Vector2(16, 16);
            Rect tabViewRect = new Rect(mainRect.x + SizePageMargins.x, mainRect.y + SizePageMargins.y,
                mainRect.width - (SizePageMargins.x * 2), mainRect.height - (SizePageMargins.y * 2));
            State.CurrentTab.Draw(State, tabViewRect);

            // Display any pending messages.
            if (State.Messages.Count() > 0) {
                foreach (var message in State.Messages) {
                    Messages.Message(message, MessageTypeDefOf.NeutralEvent);
                }
                State.ClearMessages();
            }

            // Display any pending errors.
            if (State.Errors.Count() > 0) {
                foreach (var message in State.Errors) {
                    Messages.Message(message, MessageTypeDefOf.RejectInput);
                }
                State.ClearErrors();
            }

            // Draw other controls.
            DrawPresetButtons();
            DrawPoints(mainRect);
            DoNextBackButtons(inRect, "Start".Translate(),
                delegate {
                    if (controller.ValidateStartGame()) {
                        ShowStartConfirmation();
                    }
                },
                delegate {
                    ConfirmExit();
                }
            );
        }

        protected void ConfirmExit() {
            Find.WindowStack.Add(new Dialog_Confirm("EdB.PC.Page.ConfirmExit".Translate(), delegate {
                PrepareCarefully.Instance.Clear();
                PrepareCarefully.ClearOriginalScenario();
                this.Close(true);
            }, true, null, true));
        }

        public void DoNextBackButtons(Rect innerRect, string nextLabel, Action nextAct, Action backAct) {
            float top = innerRect.height - 38;
            Text.Font = GameFont.Small;
            if (backAct != null) {
                Rect rect = new Rect(0, top, BottomButSize.x, BottomButSize.y);
                if (Widgets.ButtonText(rect, "Back".Translate(), true, false, true)) {
                    backAct();
                }
            }
            if (nextAct != null) {
                Rect rect2 = new Rect(innerRect.width - BottomButSize.x, top, BottomButSize.x, BottomButSize.y);
                if (Widgets.ButtonText(rect2, nextLabel, true, false, true)) {
                    nextAct();
                }
            }
        }

        public void ShowStartConfirmation() {
            Find.WindowStack.Add(new Dialog_Confirm("EdB.PC.Page.ConfirmStart".Translate(), delegate {
                GameStarted();
            }, true, null, true));
        }

        protected void DrawPresetButtons() {
            GUI.color = Color.white;
            float middle = 982f / 2f;
            float buttonWidth = 150;
            float buttonSpacing = 24;
            if (Widgets.ButtonText(new Rect(middle - buttonWidth - buttonSpacing / 2, 692, buttonWidth, 38), "EdB.PC.Page.Button.LoadPreset".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_LoadPreset((string name) => {
                    PresetLoaded(name);
                }));
            }
            if (Widgets.ButtonText(new Rect(middle + buttonSpacing / 2, 692, buttonWidth, 38), "EdB.PC.Page.Button.SavePreset".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_SavePreset((string name) => {
                    PresetSaved(name);
                }));
            }
            GUI.color = Color.white;
        }

        protected void DrawPoints(Rect parentRect) {
            Rect rect = new Rect(parentRect.width - 446, 4, 418, 32);
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            CostDetails cost = PrepareCarefully.Instance.Cost;
            string label;
            if (Config.pointsEnabled) {
                int points = PrepareCarefully.Instance.PointsRemaining;
                if (points < 0) {
                    GUI.color = Color.yellow;
                }
                else {
                    GUI.color = Style.ColorText;
                }
                label = "EdB.PC.Page.Points.Remaining".Translate(new string[] { "" + points });
            }
            else {
                double points = cost.total;
                GUI.color = Style.ColorText;
                label = "EdB.PC.Page.Points.Spent".Translate(new string[] { "" + points });
            }
            Widgets.Label(rect, label);

            string tooltipText = "";
            tooltipText += "EdB.PC.Page.Points.ScenarioPoints".Translate(new object[] { PrepareCarefully.Instance.StartingPoints });
            tooltipText += "\n\n";
            foreach (var c in cost.colonistDetails) {
                tooltipText += "EdB.PC.Page.Points.CostSummary.Colonist".Translate(new object[] { c.name, (c.total - c.apparel - c.bionics) }) + "\n";
            }
            tooltipText += "\n" + "EdB.PC.Page.Points.CostSummary.Apparel".Translate(new object[] { cost.colonistApparel }) + "\n"
                + "EdB.PC.Page.Points.CostSummary.Implants".Translate(new object[] { cost.colonistBionics }) + "\n"
                + "EdB.PC.Page.Points.CostSummary.Equipment".Translate(new object[] { cost.equipment }) + "\n\n"
                + "EdB.PC.Page.Points.CostSummary.Total".Translate(new object[] { cost.total });
            TipSignal tip = new TipSignal(() => tooltipText, tooltipText.GetHashCode());
            TooltipHandler.TipRegion(rect, tip);

            GUI.color = Style.ColorText;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            string optionLabel;
            float optionTop = rect.y;
            optionLabel = "EdB.PC.Page.Points.UsePoints".Translate();
            Vector2 size = Text.CalcSize(optionLabel);
            Rect optionRect = new Rect(620, optionTop, size.x + 10, 32);
            Widgets.Label(optionRect, optionLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(optionRect, "EdB.PC.Page.Points.UsePoints.Tip".Translate());
            Widgets.Checkbox(new Vector2(optionRect.x + optionRect.width, optionRect.y - 3), ref Config.pointsEnabled, 24, false);
        }

        protected void SelectPawn(CustomPawn pawn) {
            if (!pawnListActionThisFrame) {
                pawnListActionThisFrame = true;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                controller.SubcontrollerCharacters.SelectPawn(pawn);
                tabViewPawns.PanelName.ClearSelection();
                tabViewPawns.PanelTraits.ScrollToTop();
                tabViewPawns.PanelSkills.ScrollToTop();
                tabViewPawns.PanelHealth.ScrollToTop();
                tabViewPawns.PanelAppearance.UpdatePawnLayers();
            }
        }

        protected void SwapPawn(CustomPawn pawn) {
            if (!pawnListActionThisFrame) {
                pawnListActionThisFrame = true;
                SoundDefOf.ThingSelected.PlayOneShotOnCamera();
                controller.SubcontrollerCharacters.SwapPawn(pawn);
                PawnListMode newMode = PrepareCarefully.Instance.State.PawnListMode == PawnListMode.ColonyPawnsMaximized ? PawnListMode.WorldPawnsMaximized : PawnListMode.ColonyPawnsMaximized;
                PrepareCarefully.Instance.State.PawnListMode = newMode;
                tabViewPawns.ResizeTabView();
                if (newMode == PawnListMode.ColonyPawnsMaximized) {
                    tabViewPawns.PanelColonyPawns.ScrollToBottom();
                }
                else {
                    tabViewPawns.PanelWorldPawns.ScrollToBottom();
                }
            }
        }

        protected void InstrumentPanels() {
            State state = PrepareCarefully.Instance.State;
            
            GameStarted += controller.StartGame;
            PresetLoaded += controller.LoadPreset;
            PresetSaved += controller.SavePreset;

            // Instrument the characters tab view.
            ControllerPawns pawns = controller.SubcontrollerCharacters;

            tabViewPawns.PanelAge.BiologicalAgeUpdated += pawns.UpdateBiologicalAge;
            tabViewPawns.PanelAge.ChronologicalAgeUpdated += pawns.UpdateChronologicalAge;

            tabViewPawns.PanelAppearance.RandomizeAppearance += pawns.RandomizeAppearance;
            tabViewPawns.PanelAppearance.GenderUpdated += (Gender gender) => {
                pawns.UpdateGender(gender);
                tabViewPawns.PanelAppearance.UpdatePawnLayers();
            };

            tabViewPawns.PanelBackstory.BackstoryUpdated += pawns.UpdateBackstory;
            tabViewPawns.PanelBackstory.BackstoryUpdated += (BackstorySlot slot, Backstory backstory) => { controller.CheckPawnCapabilities(); };
            tabViewPawns.PanelBackstory.BackstoriesRandomized += pawns.RandomizeBackstories;
            tabViewPawns.PanelBackstory.BackstoriesRandomized += () => { controller.CheckPawnCapabilities(); };

            tabViewPawns.PanelColonyPawns.PawnSelected += this.SelectPawn;
            tabViewPawns.PanelColonyPawns.AddingPawn += pawns.AddingPawn;
            tabViewPawns.PanelColonyPawns.AddingPawnWithPawnKind += pawns.AddFactionPawn;
            tabViewPawns.PanelColonyPawns.PawnDeleted += pawns.DeletePawn;
            tabViewPawns.PanelColonyPawns.PawnDeleted += (CustomPawn pawn) => { controller.CheckPawnCapabilities(); };
            tabViewPawns.PanelColonyPawns.PawnSwapped += this.SwapPawn;
            tabViewPawns.PanelWorldPawns.PawnSelected += this.SelectPawn;
            tabViewPawns.PanelWorldPawns.AddingPawn += pawns.AddingPawn;
            tabViewPawns.PanelWorldPawns.AddingPawnWithPawnKind += pawns.AddFactionPawn;
            tabViewPawns.PanelWorldPawns.PawnDeleted += pawns.DeletePawn;
            tabViewPawns.PanelWorldPawns.PawnDeleted += (CustomPawn pawn) => { controller.CheckPawnCapabilities(); };
            tabViewPawns.PanelWorldPawns.PawnSwapped += this.SwapPawn;

            tabViewPawns.PanelColonyPawns.Maximize += () => {
                state.PawnListMode = PawnListMode.ColonyPawnsMaximized;
                tabViewPawns.ResizeTabView();
            };
            tabViewPawns.PanelWorldPawns.Maximize += () => {
                state.PawnListMode = PawnListMode.WorldPawnsMaximized;
                tabViewPawns.ResizeTabView();
            };

            pawns.PawnAdded += (CustomPawn pawn) => {
                PanelPawnList pawnList = null;
                if (pawn.Type == CustomPawnType.Colonist) {
                    pawnList = tabViewPawns.PanelColonyPawns;
                }
                else {
                    pawnList = tabViewPawns.PanelWorldPawns;
                }
                pawnList.ScrollToBottom();
                pawnList.SelectPawn(pawn);
            };
            pawns.PawnAdded += (CustomPawn pawn) => { controller.CheckPawnCapabilities(); };
            pawns.PawnReplaced += (CustomPawn pawn) => { controller.CheckPawnCapabilities(); };

            tabViewPawns.PanelHealth.InjuryAdded += pawns.AddInjury;
            tabViewPawns.PanelHealth.InjuryAdded += (Injury i) => { tabViewPawns.PanelHealth.ScrollToBottom(); };
            tabViewPawns.PanelHealth.ImplantAdded += pawns.AddImplant;
            tabViewPawns.PanelHealth.ImplantAdded += (Implant i) => { tabViewPawns.PanelHealth.ScrollToBottom(); };

            tabViewPawns.PanelName.FirstNameUpdated += pawns.UpdateFirstName;
            tabViewPawns.PanelName.NickNameUpdated += pawns.UpdateNickName;
            tabViewPawns.PanelName.LastNameUpdated += pawns.UpdateLastName;
            tabViewPawns.PanelName.NameRandomized += pawns.RandomizeName;

            tabViewPawns.PanelRandomize.RandomizeAllClicked += pawns.RandomizeAll;
            tabViewPawns.PanelRandomize.RandomizeAllClicked += () => { controller.CheckPawnCapabilities(); };

            tabViewPawns.PanelSaveLoad.CharacterLoaded += pawns.LoadCharacter;
            tabViewPawns.PanelSaveLoad.CharacterLoaded += (string filename) => { controller.CheckPawnCapabilities(); };
            tabViewPawns.PanelSaveLoad.CharacterSaved += pawns.SaveCharacter;

            tabViewPawns.PanelSkills.SkillLevelUpdated += pawns.UpdateSkillLevel;
            tabViewPawns.PanelSkills.SkillPassionUpdated += pawns.UpdateSkillPassion;
            tabViewPawns.PanelSkills.SkillsReset += pawns.ResetSkills;
            tabViewPawns.PanelSkills.SkillsCleared += pawns.ClearSkills;

            tabViewPawns.PanelTraits.TraitAdded += pawns.AddTrait;
            tabViewPawns.PanelTraits.TraitAdded += (Trait t) => { tabViewPawns.PanelTraits.ScrollToBottom(); };
            tabViewPawns.PanelTraits.TraitUpdated += pawns.UpdateTrait;
            tabViewPawns.PanelTraits.TraitRemoved += pawns.RemoveTrait;
            tabViewPawns.PanelTraits.TraitsRandomized += pawns.RandomizeTraits;
            tabViewPawns.PanelTraits.TraitsRandomized += () => { tabViewPawns.PanelTraits.ScrollToTop(); };

            // Instrument the equipment tab view.
            ControllerEquipment equipment = controller.SubcontrollerEquipment;

            tabViewEquipment.PanelAvailable.EquipmentAdded += equipment.AddEquipment;
            tabViewEquipment.PanelAvailable.EquipmentAdded += tabViewEquipment.PanelSelected.EquipmentAdded;

            tabViewEquipment.PanelSelected.EquipmentRemoved += equipment.RemoveEquipment;
            tabViewEquipment.PanelSelected.EquipmentCountUpdated += equipment.UpdateEquipmentCount;

            // Instrument the relationships tab view.
            ControllerRelationships relationships = controller.SubcontrollerRelationships;

            tabViewRelationships.PanelRelationshipsOther.RelationshipAdded += relationships.AddRelationship;
            tabViewRelationships.PanelRelationshipsOther.RelationshipRemoved += relationships.RemoveRelationship;
            tabViewRelationships.PanelRelationshipsParentChild.ParentAddedToGroup += relationships.AddParentToParentChildGroup;
            tabViewRelationships.PanelRelationshipsParentChild.ChildAddedToGroup += relationships.AddChildToParentChildGroup;
            tabViewRelationships.PanelRelationshipsParentChild.ParentRemovedFromGroup += relationships.RemoveParentFromParentChildGroup;
            tabViewRelationships.PanelRelationshipsParentChild.ChildRemovedFromGroup += relationships.RemoveChildFromParentChildGroup;
            tabViewRelationships.PanelRelationshipsParentChild.GroupAdded += relationships.AddParentChildGroup;
            tabViewPawns.PanelColonyPawns.PawnDeleted += relationships.DeleteAllPawnRelationships;
            pawns.PawnAdded += relationships.AddPawn;
            pawns.PawnReplaced += relationships.ReplacePawn;
        }
    }
}
