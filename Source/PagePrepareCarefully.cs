using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PagePrepareCarefully : Page {
        public delegate void PresetHandler(string name);

        public event PresetHandler PresetLoaded;
        public event PresetHandler PresetSaved;

        public ControllerPage Controller { get; set; }
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }
        public bool LargeUI { get; set; }
        public override string PageTitle => "EdB.PC.Page.Title".Translate();
        private List<ITabView> TabViews { get; set; } = new List<ITabView>();
        private List<TabRecord> TabRecords { get; set; } = new List<TabRecord>();
        public TabViewPawns TabViewPawns { get; set; }
        public TabViewRelationships TabViewRelationships { get; set; }
        public TabViewEquipment TabViewEquipment { get; set; }
        public ITabView CurrentTab { get; set; }
        public DialogSavePreset Dialog { get; set; }

        public float? CostLabelWidth { get; set; }

        public override Vector2 InitialSize {
            get {
                Vector2 largeSize = new Vector2(1350, Page.StandardSize.y);
                if (LargeUI) {
                    return largeSize;
                }
                else {
                    return Page.StandardSize;
                }
            }
        }

        public PagePrepareCarefully() {
            this.closeOnCancel = false;
            this.closeOnAccept = false;
            this.closeOnClickedOutside = false;
            this.doCloseButton = false;
            this.doCloseX = false;
            Dialog = new DialogSavePreset() {
                Action = (string name) => { PresetSaved?.Invoke(name); }
            };
        }

        public void PostConstruction() {
            TabViews.Add(TabViewPawns);
            TabViews.Add(TabViewRelationships);
            TabViews.Add(TabViewEquipment);

            // Create a tab record UI widget for each tab view.
            foreach (var tab in TabViews) {
                ITabView currentTab = tab;
                TabRecord tabRecord = new TabRecord(currentTab.Name, delegate {
                    // When a new tab is selected, mark the previously selected TabRecord as unselected and the current one as selected.
                    // Also, update the State to reflected the newly selected ITabView.
                    if (CurrentTab != null) {
                        CurrentTab.TabRecord.selected = false;
                    }
                    CurrentTab = currentTab;
                    currentTab.TabRecord.selected = true;
                }, false);
                currentTab.TabRecord = tabRecord;
                TabRecords.Add(tabRecord);
            }
            CurrentTab = TabViewPawns;
            TabViewPawns.TabRecord.selected = true;
        }

        override public void OnAcceptKeyPressed() {
            // Don't close the window if the user clicks the "enter" key.
        }
        override public void OnCancelKeyPressed() {
            // Confirm that the user wants to quit if they click the escape key.
            ConfirmExit();
        }

        public override void Notify_ResolutionChanged() {
            Logger.Debug("Resolution changed to: [" + Screen.width + " x " + Screen.height + "]");
            base.Notify_ResolutionChanged();
        }

        public override void PreOpen() {
            base.PreOpen();
        }

        public override void DoWindowContents(Rect inRect) {
            if (ViewState.CostCalculationDirtyFlag) {
                Controller.RecalculateCosts();
                ViewState.CostCalculationDirtyFlag = false;
            }
            base.DrawPageTitle(inRect);
            Rect mainRect = base.GetMainRect(inRect, 0, false).InsetBy(0, -6, 0, 0);
            Widgets.DrawMenuSection(mainRect);
            DrawTabViews(mainRect);
            DrawPresetButtons(inRect);
            DrawPoints(mainRect);

            DoNextBackButtons(inRect, "Start".Translate(),
                delegate {
                    if (Controller.Validate()) {
                        ShowStartConfirmation();
                    }
                },
                delegate {
                    ConfirmExit();
                }
            );

            EquipmentDatabase.LoadFrame();
        }
        protected void DrawTabViews(Rect rect) {
            float tabWidth = 180.0f;
            float tabGroupWidth = TabRecords.Count * tabWidth;
            float tabRectX = (rect.width * 0.5f) - (tabGroupWidth * 0.5f);
            TabDrawer.DrawTabs(new Rect(tabRectX, rect.y, tabGroupWidth, rect.height), TabRecords, 180.0f);

            // Determine the size of the tab view and draw the current tab.
            Vector2 SizePageMargins = new Vector2(16, 16);
            Rect tabViewRect = new Rect(rect.x + SizePageMargins.x, rect.y + SizePageMargins.y,
                rect.width - (SizePageMargins.x * 2), rect.height - (SizePageMargins.y * 2));
            CurrentTab.Draw(tabViewRect);
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

        protected void DrawPresetButtons(Rect rect) {
            GUI.color = Color.white;
            float middle = rect.width / 2f;
            float top = rect.height - 38;
            //float middle = this.windowRect.width / 2f;
            float buttonWidth = 150;
            float buttonSpacing = 24;
            if (Widgets.ButtonText(new Rect(middle - buttonWidth - buttonSpacing / 2, top, buttonWidth, 38), "EdB.PC.Page.Button.LoadPreset".Translate(), true, false, true)) {
                Find.WindowStack.Add(new DialogLoadPreset((string name) => {
                    PresetLoaded?.Invoke(name);
                }));
            }
            if (Widgets.ButtonText(new Rect(middle + buttonSpacing / 2, top, buttonWidth, 38), "EdB.PC.Page.Button.SavePreset".Translate(), true, false, true)) {
                Dialog.ViewState = ViewState;
                Find.WindowStack.Add(Dialog);
            }
            GUI.color = Color.white;
        }

        protected void DrawPoints(Rect parentRect) {
            var savedGUIState = UtilityGUIState.Save();
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            try {
                if (CostLabelWidth == null) {
                    string max = Int32.MaxValue.ToString();
                    string translated1 = "EdB.PC.Page.Points.Spent".Translate(max);
                    string translated2 = "EdB.PC.Page.Points.Remaining".Translate(max);
                    CostLabelWidth = Mathf.Max(Text.CalcSize(translated1).x, Text.CalcSize(translated2).x);
                }
                CostDetailsRefactored cost = State.PointCost;
                string label;
                if (ViewState.PointsEnabled) {
                    int pointsRemaining = State.StartingPoints - (int)State.PointCost.total;
                    if (pointsRemaining < 0) {
                        GUI.color = Color.yellow;
                    }
                    else {
                        GUI.color = Style.ColorText;
                    }
                    label = "EdB.PC.Page.Points.Remaining".Translate(pointsRemaining);
                }
                else {
                    double points = cost.total;
                    GUI.color = Style.ColorText;
                    label = "EdB.PC.Page.Points.Spent".Translate(points);
                }
                Rect rect = new Rect(parentRect.width - CostLabelWidth.Value - 40, 4, CostLabelWidth.Value, 32);
                Widgets.Label(rect, label);

                string tooltipText = "";
                tooltipText += "EdB.PC.Page.Points.ScenarioPoints".Translate(State.StartingPoints);
                tooltipText += "\n\n";
                foreach (var c in cost.colonistDetails) {
                    tooltipText += "EdB.PC.Page.Points.CostSummary.Colonist".Translate(c.name, (c.total - c.apparel - c.possessions - c.bionics)) + "\n";
                }
                tooltipText += "\n" + "EdB.PC.Page.Points.CostSummary.Apparel".Translate(cost.colonistApparel) + "\n"
                    + "EdB.PC.Page.Points.CostSummary.Possessions".Translate(cost.colonistPossessions) + "\n"
                    + "EdB.PC.Page.Points.CostSummary.Implants".Translate(cost.colonistBionics) + "\n"
                    + "EdB.PC.Page.Points.CostSummary.Equipment".Translate(cost.equipment) + "\n\n"
                    + "EdB.PC.Page.Points.CostSummary.Total".Translate(cost.total);
                TipSignal tip = new TipSignal(() => tooltipText, tooltipText.GetHashCode());
                TooltipHandler.TipRegion(rect, tip);

                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;

                if (WidgetDropdown.Button(new Rect(rect.xMax + 8, rect.yMin - 4, 31, 31), "", true, false, true)) {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    if (ViewState.PointsEnabled) {
                        list.Add(new FloatMenuOption("EdB.PC.Page.Points.DisablePoints".Translate(), () => {
                            ViewState.PointsEnabled = false;
                        }));
                    }
                    else {
                        list.Add(new FloatMenuOption("EdB.PC.Page.Points.UsePoints".Translate(), () => {
                            ViewState.PointsEnabled = true;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(list, null, false));
                }
            }
            finally {
                savedGUIState.Restore();
            }
        }

        protected void ConfirmExit() {
            if (Controller.CancellingRequiresConfirmation()) {
                Find.WindowStack.Add(new DialogConfirm("EdB.PC.Page.ConfirmExit".Translate(), delegate {
                    Controller.CancelCustomizations(); this.Close(true);
                }, true, null, true));
            }
            else {
                Close(true);
            }
        }

        protected void ShowStartConfirmation() {
            // TODO: How do the missing work types get populated?  Should we be calling a validation method here?
            // Show the missing required work dialog if necessary.  Otherwise, just show the standard confirmation.
            if (State.MissingWorkTypes != null && State.MissingWorkTypes.Count > 0) {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string current in State.MissingWorkTypes) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append("  - " + current.CapitalizeFirst());
                }
                string text = "ConfirmRequiredWorkTypeDisabledForEveryone".Translate(stringBuilder.ToString());
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate {
                    Controller.StartGame();
                }, false, null));
            }
            else {
                Find.WindowStack.Add(new DialogConfirm("EdB.PC.Page.ConfirmStart".Translate(), delegate {
                    Controller.StartGame();
                }, false, null, true));
            }

        }
    }
}
