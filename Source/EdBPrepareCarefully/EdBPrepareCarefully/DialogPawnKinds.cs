using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class DialogPawnKinds : Window {
        public Vector2 ContentMargin { get; protected set; }
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ButtonSize { get; protected set; }
        public Vector2 ContentSize { get; protected set; }
        public Vector2 GenderSize { get; protected set; }
        public float HeaderHeight { get; protected set; }
        public float RowGroupHeaderHeight { get; protected set; }
        public float FooterHeight { get; protected set; }
        public float WindowPadding { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public Rect ScrollRect { get; protected set; }
        public Rect FooterRect { get; protected set; }
        public Rect HeaderRect { get; protected set; }
        public Rect CancelButtonRect { get; protected set; }
        public Rect ConfirmButtonRect { get; protected set; }
        public Rect SingleButtonRect { get; protected set; }
        protected string headerLabel;
        protected bool resizeDirtyFlag = true;
        protected bool confirmed = false;
        protected PawnKindDef scrollTo = null;
        protected LabelTrimmer labelTrimmer = new LabelTrimmer();
        public DialogPawnKinds() {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            Resize();
        }

        public PawnKindDef Selected {
            get;
            set;
        }

        public string HeaderLabel {
            get {
                return headerLabel;
            }
            set {
                headerLabel = value;
                MarkResizeFlagDirty();
            }
        }
        public Func<string> ConfirmValidation = () => {
            return null;
        };
        public Action CloseAction {
            get;
            set;
        }
        public Action<PawnKindDef> SelectAction {
            get;
            set;
        }
        public HashSet<PawnKindDef> DisabledOptions {
            get;
            set;
        }

        protected WidgetTable<PawnKindDef> table = new WidgetTable<PawnKindDef>();

        public string ConfirmButtonLabel = "EdB.PC.Common.Select";
        public string CancelButtonLabel = "EdB.PC.Common.Cancel";

        public void ScrollTo(PawnKindDef kindDef) {
            this.scrollTo = kindDef;
        }
        protected void MarkResizeFlagDirty() {
            resizeDirtyFlag = true;
        }

        protected void Resize() {
            float headerSize = 0;
            headerSize = HeaderHeight;
            if (HeaderLabel != null) {
                headerSize = HeaderHeight;
            }

            HeaderHeight = 32;
            RowGroupHeaderHeight = 36;
            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(440f, 584f);
            ButtonSize = new Vector2(140f, 40f);

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - headerSize);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);

            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
                ContentSize.x, FooterHeight);

            GenderSize = new Vector2(48, 48);

            SingleButtonRect = new Rect(ContentSize.x / 2 - ButtonSize.x / 2,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

            CancelButtonRect = new Rect(0,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
            ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

            float nameOffset = 16;
            float radioWidth = 36;
            Vector2 nameSize = new Vector2(ContentRect.width - radioWidth, 42);

            labelTrimmer.Rect = new Rect(0, 0, nameSize.x, nameSize.y);

            table = new WidgetTable<PawnKindDef>();
            table.Rect = new Rect(Vector2.zero, ContentRect.size);
            table.RowHeight = 42;
            table.RowGroupHeaderHeight = RowGroupHeaderHeight;
            table.RowColor = new Color(28f / 255f, 32f / 255f, 36f / 255f);
            table.AlternateRowColor = new Color(0, 0, 0, 0);
            table.SelectedRowColor = new Color(0, 0, 0, 0);
            table.SupportSelection = true;
            table.SelectedAction = (PawnKindDef pawnKind) => {
                if (!DisabledOptions.Contains(pawnKind)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    Select(pawnKind);
                }
            };
            table.AddColumn(new WidgetTable<PawnKindDef>.Column() {
                Name = "Name",
                Width = nameSize.x,
                AdjustForScrollbars = true,
                DrawAction = (PawnKindDef pawnKind, Rect rect, WidgetTable<PawnKindDef>.Metadata metadata) => {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    if (this.ShowRace && pawnKind.race != null) {
                        Rect nameRect = new Rect(rect.x + nameOffset, rect.y + 5, rect.width, 22);
                        Widgets.Label(nameRect, labelTrimmer.TrimLabelIfNeeded(pawnKind.LabelCap));
                        Rect raceRect = new Rect(rect.x + nameOffset, nameRect.yMax - 5, rect.width, nameSize.y - 25);
                        Text.Font = GameFont.Tiny;
                        GUI.color = Style.ColorTextSecondary;
                        Widgets.Label(raceRect, labelTrimmer.TrimLabelIfNeeded(pawnKind.race.LabelCap));
                        GUI.color = Color.white;
                        Text.Font = GameFont.Small;
                    }
                    else {
                        Widgets.Label(new Rect(rect.x + nameOffset, rect.y + 1, rect.width, nameSize.y), pawnKind.LabelCap);
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
            table.AddColumn(new WidgetTable<PawnKindDef>.Column() {
                Name = "RadioButton",
                Width = radioWidth,
                DrawAction = (PawnKindDef pawnKind, Rect rect, WidgetTable<PawnKindDef>.Metadata metadata) => {
                    if (DisabledOptions != null && DisabledOptions.Contains(pawnKind)) {
                        GUI.color = Style.ColorControlDisabled;
                        GUI.color = new Color(1, 1, 1, 0.28f);
                        GUI.DrawTexture(new Rect(rect.x, rect.MiddleY() - 12, 24, 24), Textures.TextureRadioButtonOff);
                        GUI.color = Color.white;
                    }
                    else {
                        if (Widgets.RadioButton(new Vector2(rect.x, rect.MiddleY() - 12), this.Selected == pawnKind)) {
                            Select(pawnKind);
                        }
                    }
                }
            });
            resizeDirtyFlag = false;
        }

        protected void Select(PawnKindDef pawnKind) {
            this.Selected = pawnKind;
            if (SelectAction != null) {
                SelectAction(pawnKind);
            }
        }

        public IEnumerable<PawnKindDef> PawnKinds {
            get; set;
        }

        public IEnumerable<WidgetTable<PawnKindDef>.RowGroup> RowGroups {
            get; set;
        }

        public bool ShowRace { get; set; }

        public override Vector2 InitialSize {
            get {
                return new Vector2(WindowSize.x, WindowSize.y);
            }
        }

        public override void DoWindowContents(Rect inRect) {
            if (resizeDirtyFlag) {
                Resize();
            }
            if (scrollTo != null) {
                table.ScrollTo(scrollTo);
                scrollTo = null;
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            if (HeaderLabel != null) {
                Widgets.Label(HeaderRect, HeaderLabel);
            }

            Text.Font = GameFont.Small;
            GUI.BeginGroup(ContentRect);

            try {
                if (RowGroups != null) {
                    table.Draw(RowGroups);
                }
                else {
                    table.Draw(PawnKinds);
                }
            }
            finally {
                GUI.EndGroup();
                GUI.color = Color.white;
            }

            GUI.BeginGroup(FooterRect);
            try {
                Rect buttonRect = SingleButtonRect;
                if (CancelButtonLabel != null) {
                    if (Widgets.ButtonText(CancelButtonRect, CancelButtonLabel.Translate(), true, true, true)) {
                        this.Close(true);
                    }
                    buttonRect = ConfirmButtonRect;
                }
                if (Widgets.ButtonText(buttonRect, ConfirmButtonLabel.Translate(), true, true, true)) {
                    string validationMessage = ConfirmValidation();
                    if (validationMessage != null) {
                        Messages.Message(validationMessage.Translate(), MessageTypeDefOf.RejectInput);
                    }
                    else {
                        this.Confirm();
                    }
                }
            }
            finally {
                GUI.EndGroup();
            }
        }

        protected void Confirm() {
            confirmed = true;
            this.Close(true);
        }

        public override void PostClose() {
            if (ConfirmButtonLabel != null) {
                if (confirmed && CloseAction != null) {
                    CloseAction();
                }
            }
            else {
                if (CloseAction != null) {
                    CloseAction();
                }
            }
        }
    }
}

