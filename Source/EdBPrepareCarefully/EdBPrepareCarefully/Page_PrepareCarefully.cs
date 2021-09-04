using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private TabViewPawns tabViewPawns;
        private TabViewEquipment tabViewEquipment;
        private TabViewRelationships tabViewRelationships;
        private List<ITabView> tabViews = new List<ITabView>();
        private List<TabRecord> tabRecords = new List<TabRecord>();
        private bool pawnListActionThisFrame = false;

        private float? costLabelWidth = null;

        private Controller controller;

        public bool LargeUI { get; set; }

        public override Vector2 InitialSize {
            get {
                //Logger.Debug("Prepare Carefully window size: [" + Page.StandardSize.x + " x " + Page.StandardSize.y + "]");
                //Logger.Debug("Screen: [" + Screen.width + ", " + Screen.height + "], dpi = " + Screen.dpi + ", resolution = " + Screen.currentResolution);
                //Logger.Debug("Screen safe area: " + Screen.safeArea);
                //Logger.Debug("UI scale: " + Prefs.UIScale);

                Vector2 maxSize = new Vector2(Screen.safeArea.width / Prefs.UIScale, Screen.safeArea.height / Prefs.UIScale);
                Vector2 minSize = Page.StandardSize / Prefs.UIScale;

                Vector2 padding = new Vector2(64, 64) / Prefs.UIScale;
                maxSize -= padding;

                Vector2 largeSize = new Vector2(1350, Page.StandardSize.y);

                if (maxSize.x >= largeSize.x && maxSize.y >= largeSize.y) {
                    LargeUI = true;
                    return largeSize;
                }
                else {
                    LargeUI = false;
                    return Page.StandardSize;
                }

            }
        }

        public override void Notify_ResolutionChanged() {
            //Logger.Debug("Resolution changed to: [" + Screen.width + " x " + Screen.height + "]");
            base.Notify_ResolutionChanged();
        }

        public Page_PrepareCarefully() {
            this.closeOnCancel = false;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = false;
            this.doCloseButton = false;
            this.doCloseX = false;

            Vector2 initialSize = InitialSize;
            tabViewPawns = new TabViewPawns(LargeUI);
            tabViewEquipment = new TabViewEquipment();
            tabViewRelationships = new TabViewRelationships();

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
            //Logger.Debug("windowRect: " + windowRect);

            // Set the default tab view to the first tab and the selected pawn to the first pawn.
            State.CurrentTab = tabViews[0];
            State.CurrentColonyPawn = State.ColonyPawns.FirstOrDefault();
            State.CurrentWorldPawn = State.WorldPawns.FirstOrDefault();

            costLabelWidth = null;
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
            DrawPresetButtons(inRect);
            DrawPoints(mainRect);
            DoNextBackButtons(inRect, "Start".Translate(),
                delegate {
                    if (controller.CanDoNext()) {
                        ShowStartConfirmation();
                    }
                },
                delegate {
                    ConfirmExit();
                }
            );

            PrepareCarefully.Instance.EquipmentDatabase.LoadFrame();
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
            // Show the missing required work dialog if necessary.  Otherwise, just show the standard confirmation.
            if (State.MissingWorkTypes != null && State.MissingWorkTypes.Count > 0) {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string current in State.MissingWorkTypes) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append("  - " + current.CapitalizeFirst());
                }
                string text = "ConfirmRequiredWorkTypeDisabledForEveryone".Translate(stringBuilder.ToString ());
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate {
                    GameStarted();
                }, false, null));
            }
            else {
                Find.WindowStack.Add(new Dialog_Confirm("EdB.PC.Page.ConfirmStart".Translate(), delegate {
                    GameStarted();
                }, false, null, true));
            }
        }

        protected void DrawPresetButtons(Rect rect) {
            GUI.color = Color.white;
            float middle = rect.width / 2f;
            float top = rect.height - 38;
            //float middle = this.windowRect.width / 2f;
            float buttonWidth = 150;
            float buttonSpacing = 24;
            if (Widgets.ButtonText(new Rect(middle - buttonWidth - buttonSpacing / 2, top, buttonWidth, 38), "EdB.PC.Page.Button.LoadPreset".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_LoadPreset((string name) => {
                    PresetLoaded(name);
                }));
            }
            if (Widgets.ButtonText(new Rect(middle + buttonSpacing / 2, top, buttonWidth, 38), "EdB.PC.Page.Button.SavePreset".Translate(), true, false, true)) {
                Find.WindowStack.Add(new Dialog_SavePreset((string name) => {
                    PresetSaved(name);
                }));
            }
            GUI.color = Color.white;
        }

        protected void DrawPoints(Rect parentRect) {
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            try {
                if (costLabelWidth == null) {
                    string max = Int32.MaxValue.ToString();
                    string translated1 = "EdB.PC.Page.Points.Spent".Translate(max);
                    string translated2 = "EdB.PC.Page.Points.Remaining".Translate(max);
                    costLabelWidth = Mathf.Max(Text.CalcSize(translated1).x, Text.CalcSize(translated2).x);
                }
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
                    label = "EdB.PC.Page.Points.Remaining".Translate(points);
                }
                else {
                    double points = cost.total;
                    GUI.color = Style.ColorText;
                    label = "EdB.PC.Page.Points.Spent".Translate(points);
                }
                Rect rect = new Rect(parentRect.width - costLabelWidth.Value, 2, costLabelWidth.Value, 32);
                Widgets.Label(rect, label);

                string tooltipText = "";
                tooltipText += "EdB.PC.Page.Points.ScenarioPoints".Translate(PrepareCarefully.Instance.StartingPoints);
                tooltipText += "\n\n";
                foreach (var c in cost.colonistDetails) {
                    tooltipText += "EdB.PC.Page.Points.CostSummary.Colonist".Translate(c.name, (c.total - c.apparel - c.bionics)) + "\n";
                }
                tooltipText += "\n" + "EdB.PC.Page.Points.CostSummary.Apparel".Translate(cost.colonistApparel) + "\n"
                    + "EdB.PC.Page.Points.CostSummary.Implants".Translate(cost.colonistBionics) + "\n"
                    + "EdB.PC.Page.Points.CostSummary.Equipment".Translate(cost.equipment) + "\n\n"
                    + "EdB.PC.Page.Points.CostSummary.Total".Translate(cost.total);
                TipSignal tip = new TipSignal(() => tooltipText, tooltipText.GetHashCode());
                TooltipHandler.TipRegion(rect, tip);

                GUI.color = Style.ColorText;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;

                string optionLabel;
                float optionTop = rect.y;
                optionLabel = "EdB.PC.Page.Points.UsePoints".Translate();
                Vector2 size = Text.CalcSize(optionLabel);
                Rect optionRect = new Rect(parentRect.width - costLabelWidth.Value - size.x - 100, optionTop, size.x + 10, 32);
                Widgets.Label(optionRect, optionLabel);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(optionRect, "EdB.PC.Page.Points.UsePoints.Tip".Translate());
                Widgets.Checkbox(new Vector2(optionRect.x + optionRect.width, optionRect.y - 3), ref Config.pointsEnabled, 24, false);
            }
            finally {
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        protected void SelectPawn(CustomPawn pawn) {
            if (!pawnListActionThisFrame) {
                pawnListActionThisFrame = true;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                controller.SubcontrollerCharacters.SelectPawn(pawn);
                tabViewPawns.PanelName.ClearSelection();
                tabViewPawns.PanelColumn1?.ScrollToTop();
                tabViewPawns.PanelColumn2?.ScrollToTop();
                tabViewPawns.PanelSkills.ScrollToTop();
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
            ControllerPawns pawnController = controller.SubcontrollerCharacters;

            tabViewPawns.PanelAge.BiologicalAgeUpdated += pawnController.UpdateBiologicalAge;
            tabViewPawns.PanelAge.ChronologicalAgeUpdated += pawnController.UpdateChronologicalAge;

            tabViewPawns.PanelAppearance.RandomizeAppearance += pawnController.RandomizeAppearance;
            tabViewPawns.PanelAppearance.GenderUpdated += (Gender gender) => {
                pawnController.UpdateGender(gender);
                tabViewPawns.PanelAppearance.UpdatePawnLayers();
            };

            tabViewPawns.PanelBackstory.BackstoryUpdated += pawnController.UpdateBackstory;
            tabViewPawns.PanelBackstory.BackstoryUpdated += (BackstorySlot slot, Backstory backstory) => { pawnController.CheckPawnCapabilities(); };
            tabViewPawns.PanelBackstory.BackstoriesRandomized += pawnController.RandomizeBackstories;
            tabViewPawns.PanelBackstory.BackstoriesRandomized += () => { pawnController.CheckPawnCapabilities(); };

            tabViewPawns.PanelColonyPawns.PawnSelected += this.SelectPawn;
            tabViewPawns.PanelColonyPawns.AddingPawn += pawnController.AddingPawn;
            tabViewPawns.PanelColonyPawns.AddingPawnWithPawnKind += pawnController.AddFactionPawn;
            tabViewPawns.PanelColonyPawns.PawnDeleted += pawnController.DeletePawn;
            tabViewPawns.PanelColonyPawns.PawnDeleted += (CustomPawn pawn) => { pawnController.CheckPawnCapabilities(); };
            tabViewPawns.PanelColonyPawns.PawnSwapped += this.SwapPawn;
            tabViewPawns.PanelColonyPawns.CharacterLoaded += pawnController.LoadCharacter;
            tabViewPawns.PanelColonyPawns.CharacterLoaded += (string filename) => { pawnController.CheckPawnCapabilities(); };

            tabViewPawns.PanelWorldPawns.PawnSelected += this.SelectPawn;
            tabViewPawns.PanelWorldPawns.AddingPawn += pawnController.AddingPawn;
            tabViewPawns.PanelWorldPawns.AddingPawnWithPawnKind += pawnController.AddFactionPawn;
            tabViewPawns.PanelWorldPawns.PawnDeleted += pawnController.DeletePawn;
            tabViewPawns.PanelWorldPawns.PawnDeleted += (CustomPawn pawn) => { pawnController.CheckPawnCapabilities(); };
            tabViewPawns.PanelWorldPawns.PawnSwapped += this.SwapPawn;
            tabViewPawns.PanelWorldPawns.CharacterLoaded += pawnController.LoadCharacter;
            tabViewPawns.PanelWorldPawns.CharacterLoaded += (string filename) => { pawnController.CheckPawnCapabilities(); };

            tabViewPawns.PanelColonyPawns.Maximize += () => {
                state.PawnListMode = PawnListMode.ColonyPawnsMaximized;
                tabViewPawns.ResizeTabView();
            };
            tabViewPawns.PanelWorldPawns.Maximize += () => {
                state.PawnListMode = PawnListMode.WorldPawnsMaximized;
                tabViewPawns.ResizeTabView();
            };

            pawnController.PawnAdded += (CustomPawn pawn) => {
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
            pawnController.PawnAdded += (CustomPawn pawn) => { pawnController.CheckPawnCapabilities(); };
            pawnController.PawnReplaced += (CustomPawn pawn) => { pawnController.CheckPawnCapabilities(); };

            tabViewPawns.PanelHealth.InjuryAdded += pawnController.AddInjury;
            tabViewPawns.PanelHealth.ImplantAdded += pawnController.AddImplant;
            tabViewPawns.PanelHealth.HediffRemoved += pawnController.RemoveHediff;

            tabViewPawns.PanelName.FirstNameUpdated += pawnController.UpdateFirstName;
            tabViewPawns.PanelName.NickNameUpdated += pawnController.UpdateNickName;
            tabViewPawns.PanelName.LastNameUpdated += pawnController.UpdateLastName;
            tabViewPawns.PanelName.NameRandomized += pawnController.RandomizeName;

            tabViewPawns.PanelRandomize.RandomizeAllClicked += pawnController.RandomizeAll;
            tabViewPawns.PanelRandomize.RandomizeAllClicked += () => { pawnController.CheckPawnCapabilities(); };

            tabViewPawns.PanelFavoriteColor.FavoriteColorUpdated += pawnController.UpdateFavoriteColor;

            //tabViewPawns.PanelSaveLoad.CharacterLoaded += pawnController.LoadCharacter;
            //tabViewPawns.PanelSaveLoad.CharacterLoaded += (string filename) => { pawnController.CheckPawnCapabilities(); };
            tabViewPawns.PanelSaveLoad.CharacterSaved += pawnController.SaveCharacter;

            tabViewPawns.PanelSkills.SkillLevelUpdated += pawnController.UpdateSkillLevel;
            tabViewPawns.PanelSkills.SkillPassionUpdated += pawnController.UpdateSkillPassion;
            tabViewPawns.PanelSkills.SkillsReset += pawnController.ResetSkills;
            tabViewPawns.PanelSkills.SkillsCleared += pawnController.ClearSkills;

            tabViewPawns.PanelTraits.TraitAdded += pawnController.AddTrait;
            tabViewPawns.PanelTraits.TraitUpdated += pawnController.UpdateTrait;
            tabViewPawns.PanelTraits.TraitRemoved += pawnController.RemoveTrait;
            tabViewPawns.PanelTraits.TraitsRandomized += pawnController.RandomizeTraits;

            tabViewPawns.PanelAbilities.AbilityAdded += pawnController.AddAbility;
            tabViewPawns.PanelAbilities.AbilityRemoved += pawnController.RemoveAbility;
            tabViewPawns.PanelAbilities.AbilitiesSet += pawnController.SetAbilities;

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
            pawnController.PawnAdded += relationships.AddPawn;
            pawnController.PawnReplaced += relationships.ReplacePawn;
        }
    }
}
