using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public abstract class PanelPawnList : PanelBase {
        public delegate void SelectPawnHandler(CustomizedPawn pawn);
        public delegate void DeletePawnHandler(CustomizedPawn pawn);
        public delegate void SwapPawnHandler(CustomizedPawn pawn, bool activatePawn);
        public delegate void AddPawnHandler();
        public delegate void AddFactionPawnHandler(FactionDef def, bool startingPawn);
        public delegate void AddPawnWithPawnKindHandler(CustomizedPawnType type, PawnKindOption def);
        public delegate void LoadPawnHandler(string name);

        public event SelectPawnHandler PawnSelected;
        public event DeletePawnHandler PawnDeleted;
        public event SwapPawnHandler PawnSwapped;
        public event AddPawnHandler AddingPawn;
        public event AddPawnWithPawnKindHandler AddingPawnWithPawnKind;
        public event LoadPawnHandler PawnLoaded;

        protected Rect RectButtonQuickAdd;
        protected Rect RectButtonAdvancedAdd;
        protected Rect RectButtonLoad;
        protected Rect RectEntry;
        protected Rect RectPortrait;
        protected Rect RectPortraitClip;
        protected Rect RectPawn;
        protected Rect RectName;
        protected Rect RectDescription;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectButtonDelete;
        protected Rect RectButtonSwap;
        protected float SizeEntrySpacing = 8;
        protected WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        protected FactionDef previousFaction = null;
        protected Rect RectHeader;
        protected CustomizedPawn previousTickSelectedPawn = null;

        protected LabelTrimmer nameTrimmerNoScrollbar = new LabelTrimmer();
        protected LabelTrimmer nameTrimmerWithScrollbar = new LabelTrimmer();
        protected LabelTrimmer descriptionTrimmerNoScrollbar = new LabelTrimmer();
        protected LabelTrimmer descriptionTrimmerWithScrollbar = new LabelTrimmer();
        public override Color ColorPanelBackground => Style.ColorPanelBackgroundDeep;
        public override string PanelHeader => base.PanelHeader;

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderPawnKinds ProviderPawnKinds { get; set; }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelPadding = new Vector2(6, 6);
            Vector2 entryPadding = new Vector2(4, 4);
            float buttonHeight = 22;

            float width = PanelRect.width - panelPadding.x * 2;
            float height = PanelRect.height - panelPadding.y * 2;
            
            float headerHeight = 36;
            RectScrollFrame = new Rect(panelPadding.x, headerHeight, width + 1, height - panelPadding.y - headerHeight - buttonHeight + 6);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            float buttonWidth = width / 2f - entryPadding.x / 2f;
            RectButtonQuickAdd = new Rect(PanelRect.width - 27, 10, 16, 16);
            RectButtonLoad = new Rect(panelPadding.x, height - buttonHeight + 6, buttonWidth, buttonHeight);
            RectButtonAdvancedAdd = new Rect(panelPadding.x + buttonWidth + entryPadding.x, RectButtonLoad.y, buttonWidth, buttonHeight);

            float portraitWidth = 68;
            float portraitHeight = Mathf.Floor(portraitWidth * 1.4f);
            RectPortrait = new Rect(-15, -14, portraitWidth, portraitHeight);
            RectPortrait = new Rect(-14, -13, 64, 90);

            RectEntry = new Rect(0, 0, width, 48);
            RectPortraitClip = new Rect(RectEntry.x, RectEntry.y - 8, RectEntry.width, RectEntry.height + 8);
            RectName = new Rect(44, 8, 92, 22);
            nameTrimmerNoScrollbar.Width = RectName.width;
            nameTrimmerWithScrollbar.Width = RectName.width - 16;
            RectDescription = new Rect(RectName.x, RectName.yMax - 6, RectName.width, 18);
            descriptionTrimmerNoScrollbar.Width = RectDescription.width;
            descriptionTrimmerWithScrollbar.Width = RectDescription.width - 16;
            RectButtonDelete = new Rect(RectEntry.xMax - 18, 6, 12, 12);
            RectButtonSwap = new Rect(RectEntry.xMax - 20, RectEntry.yMax - 20, 16, 16);

            RectHeader = new Rect(0, 0, rect.width, headerHeight);
        }

        protected override void DrawPanelContent() {
            base.DrawPanelContent();

            CustomizedPawn currentPawn = ViewState.CurrentPawn;
            CustomizedPawn pawnToSelect = null;
            CustomizedPawn pawnToSwap = null;
            CustomizedPawn pawnToDelete = null;
            IEnumerable<CustomizedPawn> pawns = GetPawns();
            int pawnCount = pawns.Count();

            float? scrollTo = null;
            float cursor = 0;
            GUI.BeginGroup(RectScrollFrame);
            scrollView.Begin(RectScrollView);
            try {
                LabelTrimmer nameTrimmer = scrollView.ScrollbarsVisible ? nameTrimmerWithScrollbar : nameTrimmerNoScrollbar;
                LabelTrimmer descriptionTrimmer = scrollView.ScrollbarsVisible ? descriptionTrimmerWithScrollbar : descriptionTrimmerNoScrollbar;
                foreach (var pawn in pawns) {
                    bool selected = pawn == currentPawn;
                    Rect rect = RectEntry;
                    rect.y += cursor;
                    rect.width -= (scrollView.ScrollbarsVisible ? 16 : 0);

                    if (pawn == currentPawn && currentPawn != previousTickSelectedPawn) {
                        scrollTo = cursor;
                        previousTickSelectedPawn = currentPawn;
                    }

                    GUI.color = Style.ColorPanelBackground;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);
                    GUI.color = Color.white;

                    if (selected || rect.Contains(Event.current.mousePosition)) {
                        if (selected) {
                            GUI.color = new Color(66f / 255f, 66f / 255f, 66f / 255f);
                            Widgets.DrawBox(rect, 1);
                        }
                        GUI.color = Color.white;
                        Rect deleteRect = RectButtonDelete.OffsetBy(rect.position);
                        deleteRect.x = deleteRect.x - (scrollView.ScrollbarsVisible ? 16 : 0);
                        if (CanDeleteLastPawn || pawnCount > 1) {
                            Style.SetGUIColorForButton(deleteRect);
                            GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                            // For some reason, this GUI.Button call is causing weirdness with text field focus (select
                            // text in one of the name fields and hover over the pawns in the pawn list to see what I mean).
                            // Replacing it with a mousedown event check fixes it for some reason.
                            //if (GUI.Button(deleteRect, string.Empty, Widgets.EmptyStyle)) {
                            if (Event.current.type == EventType.MouseDown && deleteRect.Contains(Event.current.mousePosition)) {
                                // Shift-click skips the confirmation dialog
                                if (Event.current.shift) {
                                    // Delete after we've iterated and drawn everything
                                    pawnToDelete = pawn;
                                }
                                else {
                                    CustomizedPawn localPawn = pawn;
                                    Find.WindowStack.Add(
                                        new DialogConfirm("EdB.PC.Panel.PawnList.Delete.Confirm".Translate(),
                                        delegate {
                                            PawnDeleted?.Invoke(localPawn);
                                        },
                                        true, null, true)
                                    );
                                }
                            }
                            GUI.color = Color.white;
                        }
                        if (rect.Contains(Event.current.mousePosition)) {
                            Rect swapRect = RectButtonSwap.OffsetBy(rect.position);
                            swapRect.x -= (scrollView.ScrollbarsVisible ? 16 : 0);
                            if (CanDeleteLastPawn || pawnCount > 1) {
                                Style.SetGUIColorForButton(swapRect);
                                GUI.DrawTexture(swapRect, pawn.Type == CustomizedPawnType.Colony ? Textures.TextureButtonWorldPawn : Textures.TextureButtonColonyPawn);
                                if (Event.current.type == EventType.MouseDown && swapRect.Contains(Event.current.mousePosition)) {
                                    pawnToSwap = pawn;
                                }
                                GUI.color = Color.white;
                            }
                        }
                    }

                    GUI.color = Color.white;

                    Rect pawnRect = RectPortrait.OffsetBy(rect.position);
                    Rect clipRect = RectEntry.OffsetBy(rect.position);
                    RenderTexture pawnTexture = PortraitsCache.Get(pawn.Pawn, pawnRect.size, Rot4.South);
                    try {
                        GUI.BeginClip(clipRect);
                        GUI.DrawTexture(RectPortrait, (Texture)pawnTexture);
                    }
                    catch (Exception e) {
                        Logger.Error("Failed to draw pawn", e);
                    }
                    finally {
                        GUI.EndClip();
                    }

                    GUI.color = new Color(238f / 255f, 238f / 255f, 238f / 255f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.LowerLeft;
                    Rect nameRect = RectName.OffsetBy(rect.position);
                    nameRect.width = nameRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    string name = pawn.Pawn.LabelShort;
                    Vector2 nameSize = Text.CalcSize(name);
                    Widgets.Label(nameRect, nameTrimmer.TrimLabelIfNeeded(name));

                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = new Color(184f / 255f, 184f / 255f, 184f / 255f);
                    Rect professionRect = RectDescription.OffsetBy(rect.position);
                    professionRect.width = professionRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    string description = null;
                    if (pawn.Pawn.story.Adulthood != null) {
                        description = pawn.Pawn.story.Adulthood.TitleShortFor(pawn.Customizations.Gender).CapitalizeFirst();
                    }
                    else if (pawn.Pawn.story.Childhood != null) {
                        description = pawn.Pawn.story.Childhood.TitleShortFor(pawn.Customizations.Gender).CapitalizeFirst();
                    }
                    if (!description.NullOrEmpty()) {
                        Widgets.Label(professionRect, descriptionTrimmer.TrimLabelIfNeeded(description));
                    }

                    if (pawn != ViewState.CurrentPawn && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && pawnToSwap == null) {
                        pawnToSelect = pawn;
                    }

                    cursor += rect.height + SizeEntrySpacing;
                }
                cursor -= SizeEntrySpacing;
            }
            finally {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            if (scrollTo != null) {
                float top = scrollTo.Value;
                float bottom = top + RectEntry.height;
                float min = scrollView.Position.y;
                float max = min + scrollView.ViewHeight;
                if (top < min) {
                    scrollView.ScrollTo(top);
                }
                else if (bottom > max) {
                    float position = scrollView.Position.y + (bottom - max);
                    scrollView.ScrollTo(position);
                }
            }

            // Quick Add button.
            if (RectButtonQuickAdd.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonQuickAdd, Textures.TextureButtonAdd);
            if (Widgets.ButtonInvisible(RectButtonQuickAdd, false)) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                AddingPawn?.Invoke();
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;

            // Load button
            if (Widgets.ButtonText(RectButtonLoad, "EdB.PC.Panel.PawnList.Load".Translate(), true, false, true)) {
                Find.WindowStack.Add(new DialogLoadColonist(
                    (string name) => {
                        PawnLoaded?.Invoke(name);
                    }
                ));
            }

            // Advanced Add button
            if (Widgets.ButtonText(RectButtonAdvancedAdd, "EdB.PC.Panel.PawnList.Add".Translate(), true, false, true)) {
                ShowPawnKindDialog();
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (pawnToDelete != null && PawnDeleted != null) {
                PawnDeleted?.Invoke(pawnToDelete);
            }
            else if (pawnToSwap != null && PawnSwapped != null) {
                PawnSwapped?.Invoke(pawnToSwap, !Event.current.shift);
            }
            else if (pawnToSelect != null && PawnSelected != null) {
                PawnSelected?.Invoke(pawnToSelect);
            }
        }

        protected abstract IEnumerable<CustomizedPawn> GetPawns();

        protected abstract bool IsTopPanel();

        protected abstract bool StartingPawns {
            get;
        }

        protected abstract bool CanDeleteLastPawn {
            get;
        }

        public void SelectPawn(CustomizedPawn pawn) {
            PawnSelected?.Invoke(pawn);
        }

        public IEnumerable<PawnKindDef> ColonyKindDefs(PawnKindDef basicKind, IEnumerable<PawnKindDef> factionKinds) {
            if (basicKind != null) {
                yield return basicKind;
            }
            if (factionKinds != null) {
                foreach (var f in factionKinds) {
                    if (f != basicKind) {
                        yield return f;
                    }
                }
            }
        }

        public IEnumerable<PawnKindDef> AllPawnKinds(PawnKindDef basicKind, IEnumerable<PawnKindDef> factionKinds) {
            if (basicKind != null) {
                yield return basicKind;
            }
            if (factionKinds != null) {
                foreach (var f in factionKinds) {
                    if (f != basicKind) {
                        yield return f;
                    }
                }
            }
        }

        protected List<WidgetTable<PawnKindOption>.RowGroup> rowGroups = new List<WidgetTable<PawnKindOption>.RowGroup>();
        protected void ShowPawnKindDialog() {
            var disabled = new HashSet<PawnKindOption>();
            rowGroups.Clear();

            PawnKindOption selected = ViewState.LastSelectedPawnKindDef;

            List<ProviderPawnKinds.FactionPawnKinds> factionPawnKindsList = new List<ProviderPawnKinds.FactionPawnKinds>(ProviderPawnKinds.PawnKindsByFaction);
            // Sort the pawn kinds to put the colony faction at the top.
            factionPawnKindsList.Sort((a, b) => {
                if (a.Faction == Find.FactionManager.OfPlayer.def && b.Faction != Find.FactionManager.OfPlayer.def) {
                    return -1;
                }
                else if (b.Faction == Find.FactionManager.OfPlayer.def && a.Faction != Find.FactionManager.OfPlayer.def) {
                    return 1;
                }
                else {
                    return string.Compare(a.Faction.LabelCap, b.Faction.LabelCap);
                }
            });
            //Logger.Debug(String.Join("\n", factionPawnKindsList.Select(k => k.Faction.LabelCap + ", " + k.Faction.defName)));

            // If no pawn kind has been selected, select the colony's basic pawn kind by default.
            if (selected == null) {
                var faction = factionPawnKindsList?.FirstOrDefault(f => f != null)?.Faction;
                var kind = factionPawnKindsList?.FirstOrDefault(f => f != null)?.PawnKinds?.FirstOrDefault(k => k != null);
                if (faction != null && kind != null) {
                    selected = new PawnKindOption(faction, kind);
                }
            }

            foreach (var factionPawnKinds in factionPawnKindsList) {
                if (factionPawnKinds.PawnKinds.Count > 0) {
                    rowGroups.Add(new WidgetTable<PawnKindOption>.RowGroup("<b>" + factionPawnKinds.Faction.LabelCap.ToString() + "</b>",
                        factionPawnKinds.PawnKinds.ConvertAll(f => new PawnKindOption(factionPawnKinds.Faction, f))));
                }
            }
            if (!ProviderPawnKinds.PawnKindsWithNoFaction.EnumerableNullOrEmpty()) {
                rowGroups.Add(new WidgetTable<PawnKindOption>.RowGroup("<b>Other</b>", ProviderPawnKinds.PawnKindsWithNoFaction.Select(k => new PawnKindOption(null, k))));
            }

            DialogPawnKinds dialog = new DialogPawnKinds() {
                HeaderLabel = "EdB.PC.Panel.PawnList.SelectFaction".Translate(),
                SelectAction = (PawnKindOption option) => { selected = option; },
                RowGroups = rowGroups,
                DisabledOptions = disabled,
                CloseAction = () => {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    if (selected != null) {
                        ViewState.LastSelectedPawnKindDef = selected;
                        AddingPawnWithPawnKind?.Invoke(StartingPawns ? CustomizedPawnType.Colony : CustomizedPawnType.World, selected);
                    }
                },
                Selected = selected,
                ShowRace = ProviderPawnKinds.AnyNonHumanPawnKinds && !PawnKindRaceDiversificationEnabled
            };
            dialog.ScrollToWhenOpened(ViewState.LastSelectedPawnKindDef);
            //Logger.Debug("ScrollToWhenOpened = " + ViewState.LastSelectedPawnKindDef);
            Find.WindowStack.Add(dialog);
        }

        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }

        public bool PawnKindRaceDiversificationEnabled {
            get {
                return ModsConfig.ActiveModsInLoadOrder?.FirstOrDefault(m => m.PackageId == "solidwires.pawnkindracediversification") != null;
            }
        }
    }
}
