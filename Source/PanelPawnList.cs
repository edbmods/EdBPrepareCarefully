using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public abstract class PanelPawnList : PanelBase {
        public delegate void SelectPawnHandler(CustomPawn pawn);
        public delegate void DeletePawnHandler(CustomPawn pawn);
        public delegate void SwapPawnHandler(CustomPawn pawn);
        public delegate void MaximizeHandler();
        public delegate void MinimizeHandler();
        public delegate void AddPawnHandler(bool startingPawn);
        public delegate void AddFactionPawnHandler(FactionDef def, bool startingPawn);
        public delegate void AddPawnWithPawnKindHandler(PawnKindDef def, bool startingPawn);

        public event SelectPawnHandler PawnSelected;
        public event DeletePawnHandler PawnDeleted;
        public event SwapPawnHandler PawnSwapped;
        public event AddPawnHandler AddingPawn;
        public event AddPawnWithPawnKindHandler AddingPawnWithPawnKind;
        public event MaximizeHandler Maximize;

        protected Rect RectButtonAdd;
        protected Rect RectButtonAdvancedAdd;
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
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected FactionDef previousFaction = null;
        protected Rect RectMinimize;
        protected Rect RectMaximize;
        protected Rect RectHeader;

        protected LabelTrimmer nameTrimmerNoScrollbar = new LabelTrimmer();
        protected LabelTrimmer nameTrimmerWithScrollbar = new LabelTrimmer();
        protected LabelTrimmer descriptionTrimmerNoScrollbar = new LabelTrimmer();
        protected LabelTrimmer descriptionTrimmerWithScrollbar = new LabelTrimmer();

        public override Color ColorPanelBackground {
            get {
                return Style.ColorPanelBackgroundDeep;
            }
        }
        public override string PanelHeader {
            get {
                return base.PanelHeader;
            }
        }
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

            RectButtonAdvancedAdd = new Rect(panelPadding.x + width - 26, height - buttonHeight + 6, 26, buttonHeight);
            float addButtonWidth = 64;
            RectButtonAdd = new Rect(RectButtonAdvancedAdd.x - 2 - addButtonWidth, RectButtonAdvancedAdd.y, addButtonWidth, buttonHeight);

            float widthMinusPadding = width - entryPadding.x * 2;
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

            Vector2 resizeButtonSize = new Vector2(18, 18);
            RectMinimize = new Rect(rect.width - 25, 4, resizeButtonSize.x, resizeButtonSize.y);
            RectMaximize = new Rect(rect.width - 25, 9, resizeButtonSize.x, resizeButtonSize.y);
            RectHeader = new Rect(0, 0, rect.width, headerHeight);
        }

        protected override void DrawPanelContent(State state) {
            /*
            // Test code for adjusting the size and position of the portrait.
            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.shift) {
                    if (Event.current.keyCode == KeyCode.LeftArrow) {
                        float portraitWidth = RectPortrait.width;
                        portraitWidth -= 1f;
                        float portraitHeight = Mathf.Floor(portraitWidth * 1.4f);
                        RectPortrait = new Rect(RectPortrait.x, RectPortrait.y, portraitWidth, portraitHeight);
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                    else if (Event.current.keyCode == KeyCode.RightArrow) {
                        float portraitWidth = RectPortrait.width;
                        portraitWidth += 1f;
                        float portraitHeight = Mathf.Floor(portraitWidth * 1.4f);
                        RectPortrait = new Rect(RectPortrait.x, RectPortrait.y, portraitWidth, portraitHeight);
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                }
                else {
                    if (Event.current.keyCode == KeyCode.LeftArrow) {
                        RectPortrait = RectPortrait.OffsetBy(new Vector2(-1, 0));
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                    else if (Event.current.keyCode == KeyCode.RightArrow) {
                        RectPortrait = RectPortrait.OffsetBy(new Vector2(1, 0));
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow) {
                        RectPortrait = RectPortrait.OffsetBy(new Vector2(0, -1));
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                    else if (Event.current.keyCode == KeyCode.DownArrow) {
                        RectPortrait = RectPortrait.OffsetBy(new Vector2(0, 1));
                        Logger.Debug("RectPortrait = " + RectPortrait);
                    }
                }
            }
            */
            base.DrawPanelContent(state);

            CustomPawn currentPawn = state.CurrentPawn;
            CustomPawn pawnToSelect = null;
            CustomPawn pawnToSwap = null;
            CustomPawn pawnToDelete = null;
            List<CustomPawn> pawns = GetPawnListFromState(state);
            int colonistCount = pawns.Count();

            if (IsMinimized(state)) {
                // Count label.
                Text.Font = GameFont.Medium;
                float headerWidth = Text.CalcSize(PanelHeader).x;
                Rect countRect = new Rect(10 + headerWidth + 3, 3, 50, 27);
                GUI.color = Style.ColorTextPanelHeader;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(countRect, "EdB.PC.Panel.PawnList.PawnCount".Translate(colonistCount ));
                GUI.color = Color.white;

                // Maximize button.
                if (RectHeader.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(RectMaximize, IsTopPanel() ? Textures.TextureMaximizeDown : Textures.TextureMaximizeUp);
                if (Widgets.ButtonInvisible(RectHeader, false)) {
                    SoundDefOf.ThingSelected.PlayOneShotOnCamera();
                    Maximize();
                }
                return;
            }

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
                        if (CanDeleteLastPawn || colonistCount > 1) {
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
                                    CustomPawn localPawn = pawn;
                                    Find.WindowStack.Add(
                                        new Dialog_Confirm("EdB.PC.Panel.PawnList.Delete.Confirm".Translate(),
                                        delegate {
                                            PawnDeleted(localPawn);
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
                            if (CanDeleteLastPawn || colonistCount > 1) {
                                Style.SetGUIColorForButton(swapRect);
                                GUI.DrawTexture(swapRect, pawn.Type == CustomPawnType.Colonist ? Textures.TextureButtonWorldPawn : Textures.TextureButtonColonyPawn);
                                if (Event.current.type == EventType.MouseDown && swapRect.Contains(Event.current.mousePosition)) {
                                    pawnToSwap = pawn;
                                }
                                GUI.color = Color.white;
                            }
                        }
                    }
                    
                    Rect pawnRect = RectPortrait.OffsetBy(rect.position);
                    GUI.color = Color.white;
                    RenderTexture pawnTexture = pawn.GetPortrait(pawnRect.size);
                    Rect clipRect = RectEntry.OffsetBy(rect.position);
                    try {
                        GUI.BeginClip(clipRect);
                        GUI.DrawTexture(RectPortrait, (Texture)pawnTexture);
                    }
                    finally {
                        GUI.EndClip();
                    }

                    GUI.color = new Color(238f / 255f, 238f / 255f, 238f / 255f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.LowerLeft;
                    Rect nameRect = RectName.OffsetBy(rect.position);
                    nameRect.width = nameRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    Vector2 nameSize = Text.CalcSize(pawn.Pawn.LabelShort);
                    Widgets.Label(nameRect, nameTrimmer.TrimLabelIfNeeded(pawn.Pawn.LabelShort));

                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = new Color(184f / 255f, 184f / 255f, 184f / 255f);
                    Rect professionRect = RectDescription.OffsetBy(rect.position);
                    professionRect.width = professionRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    string description = null;
                    if (pawn.IsAdult) {
                        if (pawn.Adulthood != null) {
                            description = pawn.Adulthood.TitleShortCapFor(pawn.Gender);
                        }
                    }
                    else {
                        description = pawn.Childhood.TitleShortCapFor(pawn.Gender);
                    }
                    if (!description.NullOrEmpty()) {
                        Widgets.Label(professionRect, descriptionTrimmer.TrimLabelIfNeeded(description));
                    }

                    if (pawn != state.CurrentPawn && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && pawnToSwap == null) {
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
            
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            if (Widgets.ButtonText(RectButtonAdd, "EdB.PC.Common.Add".Translate(), true, false, true)) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                AddingPawn(StartingPawns);
            }
            if (Widgets.ButtonText(RectButtonAdvancedAdd, "...", true, false, true)) {
                ShowPawnKindDialog();
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (pawnToDelete != null) {
                PawnDeleted(pawnToDelete);
            }
            else if (pawnToSwap != null) {
                PawnSwapped(pawnToSwap);
            }
            else if (pawnToSelect != null) {
                PawnSelected(pawnToSelect);
            }

        }

        protected abstract bool IsMaximized(State state);

        protected abstract bool IsMinimized(State state);

        protected abstract List<CustomPawn> GetPawnListFromState(State state);

        protected abstract bool IsTopPanel();

        protected abstract bool StartingPawns {
            get;
        }

        protected abstract bool CanDeleteLastPawn {
            get;
        }

        public void SelectPawn(CustomPawn pawn) {
            PawnSelected(pawn);
        }

        protected List<WidgetTable<PawnKindDef>.RowGroup> rowGroups = new List<WidgetTable<PawnKindDef>.RowGroup>();
        protected void ShowPawnKindDialog() {
            HashSet<PawnKindDef> disabled = new HashSet<PawnKindDef>();
            rowGroups.Clear();
            List<FactionDef> factionDefs = PrepareCarefully.Instance.Providers.Factions.NonPlayerHumanlikeFactionDefs;
            foreach (var factionDef in factionDefs) {
                var pawnKindsEnumerable = PrepareCarefully.Instance.Providers.Factions.GetPawnKindsForFactionDef(factionDef);
                if (pawnKindsEnumerable != null) {
                    List<PawnKindDef> pawnKinds = pawnKindsEnumerable.ToList();
                    if (pawnKinds.Count > 0) {
                        rowGroups.Add(new WidgetTable<PawnKindDef>.RowGroup("<b>" + factionDef.LabelCap + "</b>", pawnKinds));
                    }
                }
            }
            PawnKindDef selected = PrepareCarefully.Instance.State.LastSelectedPawnKindDef;
            DialogPawnKinds dialog = new DialogPawnKinds() {
                HeaderLabel = "EdB.PC.Panel.PawnList.SelectFaction".Translate(),
                SelectAction = (PawnKindDef pawnKind) => { selected = pawnKind; },
                RowGroups = rowGroups,
                DisabledOptions = disabled,
                CloseAction = () => {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    if (selected != null) {
                        PrepareCarefully.Instance.State.LastSelectedPawnKindDef = selected;
                        AddingPawnWithPawnKind(selected, StartingPawns);
                    }
                },
                Selected = selected
            };
            dialog.ScrollTo(PrepareCarefully.Instance.State.LastSelectedPawnKindDef);
            Find.WindowStack.Add(dialog);
        }

        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }
        
    }
}
