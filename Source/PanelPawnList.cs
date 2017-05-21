using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelPawnList : PanelBase {
        public delegate void SelectPawnHandler(CustomPawn pawn);
        public delegate void DeletePawnHandler(CustomPawn pawn);
        public delegate void AddPawnHandler();
        public delegate void AddFactionPawnHandler(FactionDef def);

        public event SelectPawnHandler PawnSelected;
        public event DeletePawnHandler PawnDeleted;
        public event AddPawnHandler AddingPawn;
        public event AddFactionPawnHandler AddingFactionPawn;

        protected Rect RectButtonAdd;
        protected Rect RectButtonAdvancedAdd;
        protected Rect RectEntry;
        protected Rect RectPortrait;
        protected Rect RectPawn;
        protected Rect RectName;
        protected Rect RectProfession;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectButtonDelete;
        protected float SizeEntrySpacing = 4;
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected ProviderFactions providerFactions = new ProviderFactions();

        protected float shorterNameWidth = 0;
        protected Dictionary<string, string> shorterNameLookup = new Dictionary<string, string>();

        public override Color ColorPanelBackground {
            get {
                return Style.ColorPanelBackgroundDeep;
            }
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 panelPadding = new Vector2(6, 6);
            Vector2 entryPadding = new Vector2(4, 4);
            float buttonHeight = 22;

            float width = PanelRect.width - panelPadding.x * 2;
            float height = PanelRect.height - panelPadding.y * 2;

            //RectButtonAdvancedAdd = new Rect(panelPadding.x + width - 26, panelPadding.y, 26, buttonHeight);
            //RectButtonAdd = new Rect(panelPadding.x, panelPadding.y, width - RectButtonAdvancedAdd.width - 2, buttonHeight);
            RectButtonAdd = new Rect(panelPadding.x, panelPadding.y, width, buttonHeight);
            //RectButtonAdd = new Rect(panelPadding.x, panelPadding.y + height - buttonHeight, width, buttonHeight);

            //RectScrollFrame = new Rect(panelPadding.x, panelPadding.y, width, height - panelPadding.y - buttonHeight);
            RectScrollFrame = new Rect(panelPadding.x, RectButtonAdd.yMax + panelPadding.y, width, height - panelPadding.y - buttonHeight);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            float widthMinusPadding = width - entryPadding.x * 2;
            RectPortrait = new Rect(entryPadding.x, entryPadding.y, widthMinusPadding, 70);

            RectName = new Rect(entryPadding.x, RectPortrait.yMax - 2, widthMinusPadding, 22);
            RectProfession = new Rect(entryPadding.x, RectName.yMax - 6, widthMinusPadding, 18);
            RectEntry = new Rect(0, 0, width, RectProfession.yMax - RectPortrait.yMin + entryPadding.y);
            RectButtonDelete = new Rect(RectEntry.xMax - 18, 6, 12, 12);

            shorterNameWidth = RectScrollView.width;
        }

        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            CustomPawn currentPawn = state.CurrentPawn;
            CustomPawn newPawnSelection = null;
            int colonistCount = state.Pawns.Count();

            float cursor = 0;
            GUI.BeginGroup(RectScrollFrame);
            scrollView.Begin(RectScrollView);
            try {
                foreach (var pawn in state.Pawns) {
                    bool selected = pawn == currentPawn;
                    Rect rect = RectEntry;
                    rect.y = rect.y + cursor;
                    rect.width = rect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    
                    if (selected || rect.Contains(Event.current.mousePosition)) {
                        GUI.color = Style.ColorPanelBackground;
                        GUI.DrawTexture(rect, BaseContent.WhiteTex);
                        if (selected) {
                            GUI.color = new Color(66f / 255f, 66f / 255f, 66f / 255f);
                            Widgets.DrawBox(rect, 1);
                        }
                        GUI.color = Color.white;
                        Rect deleteRect = RectButtonDelete.OffsetBy(rect.position);
                        deleteRect.x = deleteRect.x - (scrollView.ScrollbarsVisible ? 16 : 0);
                        if (colonistCount > 1) {
                            Style.SetGUIColorForButton(deleteRect);
                            GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                            // For some reason, this GUI.Button call is causing weirdness with text field focus (select
                            // text in one of the name fields and hover over the pawns in the pawn list to see what I mean).
                            // Replacing it with a mousedown event check fixes it for some reason.
                            //if (GUI.Button(deleteRect, string.Empty, Widgets.EmptyStyle)) {
                            if (Event.current.type == EventType.MouseDown && deleteRect.Contains(Event.current.mousePosition)) {
                                CustomPawn localPawn = pawn;
                                Find.WindowStack.Add(
                                    new Dialog_Confirm("EdB.PC.Panel.PawnList.Delete.Confirm".Translate(),
                                    delegate {
                                        PawnDeleted(localPawn);
                                    },
                                    true, null, true)
                                );
                            }
                            GUI.color = Color.white;
                        }
                    }
                    
                    Rect pawnRect = RectPortrait.OffsetBy(rect.position);
                    pawnRect.width = pawnRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    float pawnHeight = Mathf.Floor(pawnRect.height * 1.25f);
                    float pawnWidth = pawnRect.width;
                    pawnRect.x = pawnRect.x + pawnRect.width * 0.5f - pawnWidth * 0.5f;
                    pawnRect.y = pawnRect.y + 8 + pawnRect.height * 0.5f - pawnHeight * 0.5f;
                    pawnRect.width = pawnWidth;
                    pawnRect.height = pawnHeight;
                    GUI.color = Color.white;
                    RenderTexture pawnTexture = pawn.GetPortrait(pawnRect.size);
                    GUI.DrawTexture(pawnRect, (Texture)pawnTexture);

                    GUI.color = new Color(238f / 255f, 238f / 255f, 238f / 255f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.LowerCenter;
                    Rect nameRect = RectName.OffsetBy(rect.position);
                    nameRect.width = nameRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    Vector2 nameSize = Text.CalcSize(pawn.Pawn.LabelShort);
                    string name = pawn.Pawn.LabelShort;
                    if (nameSize.x > nameRect.width) {
                        name = GetShorterName(name, nameRect);
                    }
                    Widgets.Label(nameRect, name);

                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperCenter;
                    GUI.color = new Color(184f / 255f, 184f / 255f, 184f / 255f);
                    Rect professionRect = RectProfession.OffsetBy(rect.position);
                    professionRect.width = professionRect.width - (scrollView.ScrollbarsVisible ? 16 : 0);
                    if (pawn.IsAdult) {
                        if (pawn.Adulthood != null) {
                            Widgets.Label(professionRect, pawn.Adulthood.TitleShort);
                        }
                    }
                    else {
                        Widgets.Label(professionRect, pawn.Childhood.TitleShort);
                    }

                    if (pawn != state.CurrentPawn && Widgets.ButtonInvisible(rect, false)) {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                        newPawnSelection = pawn;
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
                SoundDefOf.SelectDesignator.PlayOneShotOnCamera();
                AddingPawn();
            }
            //if (Widgets.ButtonText(RectButtonAdvancedAdd, "...", true, false, true)) {
            //    OpenAddPawnDialog();
            //}
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (newPawnSelection != null) {
                PawnSelected(newPawnSelection);
            }
        }

        protected string GetShorterName(string name, Rect rect) {
            if (rect.width != shorterNameWidth) {
                shorterNameWidth = rect.width;
                shorterNameLookup.Clear();
            }
            string suffix = "...";
            string shorter;
            if (shorterNameLookup.TryGetValue(name, out shorter)) {
                return shorter + suffix;
            }
            shorter = name;
            while (!shorter.NullOrEmpty()) {
                shorter = shorter.Substring(0, shorter.Length - 1);
                Vector2 size = Text.CalcSize(shorter + suffix);
                if (size.x < rect.width) {
                    shorterNameLookup.Add(name, shorter);
                    return shorter + suffix;
                }
            }
            return name;
        }

        public void SelectPawn(CustomPawn pawn) {
            PawnSelected(pawn);
        }

        protected void OpenAddPawnDialog() {
            FactionDef selectedFaction = Faction.OfPlayer.def;
            var dialog = new Dialog_Options<FactionDef>(providerFactions.Factions) {
                ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                CancelButtonLabel = "EdB.PC.Common.Cancel".Translate(),
                HeaderLabel = "EdB.PC.Panel.PawnList.SelectFaction".Translate(),
                NameFunc = (FactionDef def) => {
                    return def.LabelCap;
                },
                SelectedFunc = (FactionDef def) => {
                    return def.defName == selectedFaction.defName;
                },
                SelectAction = (FactionDef def) => {
                    selectedFaction = def;
                },
                EnabledFunc = (FactionDef def) => {
                    return true;
                },
                ConfirmValidation = () => {
                    if (selectedFaction == null) {
                        return "EdB.PC.Panel.PawnList.Error.MustSelectFaction";
                    }
                    else {
                        return null;
                    }
                },
                CloseAction = () => {
                    SoundDefOf.SelectDesignator.PlayOneShotOnCamera();
                    AddingFactionPawn(selectedFaction);
                }
            };
            Find.WindowStack.Add(dialog);
        }

        public void ScrollToBottom() {
            scrollView.ScrollToBottom();
        }
        
    }
}
