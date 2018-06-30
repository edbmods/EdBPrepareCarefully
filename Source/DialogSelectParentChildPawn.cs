using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    // TODO: This is largely a copy/paste of DialogSelectPawn.  Should figure out a way to leverage the
    // existing code in that class instead of duplicating it here.
    public class DialogSelectParentChildPawn : Window {
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

        public DialogSelectParentChildPawn() {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            Resize();
        }

        public CustomParentChildPawn SelectedPawn {
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
        public Action<CustomParentChildPawn> SelectAction {
            get;
            set;
        }
        public HashSet<CustomParentChildPawn> DisabledPawns {
            get;
            set;
        }

        protected WidgetTable<CustomParentChildPawn> table = new WidgetTable<CustomParentChildPawn>();

        public string ConfirmButtonLabel = "EdB.PC.Common.Add";
        public string CancelButtonLabel = "EdB.PC.Common.Cancel";

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

            Vector2 portraitSize = new Vector2(70, 70);
            float portraitOverflow = 8;
            float nameOffset = 2;
            float descriptionOffset = -2;
            float radioWidth = 36;
            Vector2 nameSize = new Vector2(ContentRect.width - portraitSize.x - radioWidth, portraitSize.y * 0.5f);

            table = new WidgetTable<CustomParentChildPawn>();
            table.Rect = new Rect(Vector2.zero, ContentRect.size);
            table.RowHeight = portraitSize.y;
            table.RowGroupHeaderHeight = RowGroupHeaderHeight;
            table.RowColor = new Color(28f / 255f, 32f / 255f, 36f / 255f);
            table.AlternateRowColor = new Color(0, 0, 0, 0);
            table.SelectedRowColor = new Color(0, 0, 0, 0);
            table.SupportSelection = true;
            table.SelectedAction = (CustomParentChildPawn pawn) => {
                if (!DisabledPawns.Contains(pawn)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    Select(pawn);
                }
            };
            table.AddColumn(new WidgetTable<CustomParentChildPawn>.Column() {
                Name = "Portrait",
                DrawAction = (CustomParentChildPawn pawn, Rect rect, WidgetTable<CustomParentChildPawn>.Metadata metadata) => {
                    GUI.color = Color.white;
                    if (!pawn.Hidden) {
                        var texture = pawn.Pawn.GetPortrait(new Vector2(portraitSize.x, portraitSize.y + portraitOverflow * 2));
                        GUI.DrawTexture(new Rect(rect.position.x, rect.position.y - portraitOverflow, portraitSize.x, portraitSize.y + portraitOverflow * 2), texture as Texture);
                    }
                    else {
                        GUI.color = Style.ColorButton;
                        Rect genderRect = new Rect(rect.MiddleX() - GenderSize.HalfX(), rect.MiddleY() - GenderSize.HalfY(), GenderSize.x, GenderSize.y);
                        if (pawn.Pawn.Gender == Gender.Female) {
                            GUI.DrawTexture(genderRect, Textures.TextureGenderFemaleLarge);
                        }
                        else if (pawn.Pawn.Gender == Gender.Male) {
                            GUI.DrawTexture(genderRect, Textures.TextureGenderMaleLarge);
                        }
                        else {
                            GUI.DrawTexture(genderRect, Textures.TextureGenderlessLarge);
                        }
                        GUI.color = Color.white;
                    }
                },
                Width = portraitSize.x
            });
            table.AddColumn(new WidgetTable<CustomParentChildPawn>.Column() {
                Name = "Description",
                Width = nameSize.x,
                AdjustForScrollbars = true,
                DrawAction = (CustomParentChildPawn parentChildPawn, Rect rect, WidgetTable<CustomParentChildPawn>.Metadata metadata) => {
                    CustomPawn pawn = parentChildPawn.Pawn;
                    Text.Anchor = TextAnchor.LowerLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(new Rect(rect.x, rect.y + nameOffset, rect.width, nameSize.y), parentChildPawn.Name);
                    Text.Anchor = TextAnchor.UpperLeft;
                    string description;
                    if (!parentChildPawn.Hidden) {
                        string age = pawn.BiologicalAge != pawn.ChronologicalAge ?
                            "EdB.PC.Pawn.AgeWithChronological".Translate(new object[] { pawn.BiologicalAge, pawn.ChronologicalAge }) :
                            "EdB.PC.Pawn.AgeWithoutChronological".Translate(new object[] { pawn.BiologicalAge });
                        description = pawn.Gender != Gender.None ?
                            "EdB.PC.Pawn.PawnDescriptionWithGender".Translate(new object[] { pawn.ProfessionLabelShort, pawn.Gender.GetLabel(), age }) :
                            "EdB.PC.Pawn.PawnDescriptionNoGender".Translate(new object[] { pawn.ProfessionLabelShort, age });
                    }
                    else {
                        string profession = "EdB.PC.Pawn.HiddenPawnProfession".Translate();
                        description = pawn.Gender != Gender.None ?
                            "EdB.PC.Pawn.HiddenPawnDescriptionWithGender".Translate(new object[] { profession, pawn.Gender.GetLabel() }) :
                            "EdB.PC.Pawn.HiddenPawnDescriptionNoGender".Translate(new object[] { profession });
                    }
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(rect.x, rect.y + nameSize.y + descriptionOffset, rect.width, nameSize.y), description);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
            table.AddColumn(new WidgetTable<CustomParentChildPawn>.Column() {
                Name = "RadioButton",
                Width = radioWidth,
                DrawAction = (CustomParentChildPawn pawn, Rect rect, WidgetTable<CustomParentChildPawn>.Metadata metadata) => {
                    if (DisabledPawns != null && DisabledPawns.Contains(pawn)) {
                        GUI.color = Style.ColorControlDisabled;
                        GUI.color = new Color(1, 1, 1, 0.28f);
                        GUI.DrawTexture(new Rect(rect.x, rect.MiddleY() - 12, 24, 24), Textures.TextureRadioButtonOff);
                        GUI.color = Color.white;
                    }
                    else {
                        if (Widgets.RadioButton(new Vector2(rect.x, rect.MiddleY() - 12), this.SelectedPawn == pawn)) {
                            Select(pawn);
                        }
                    }
                }
            });
            resizeDirtyFlag = false;
        }

        protected void Select(CustomParentChildPawn pawn) {
            this.SelectedPawn = pawn;
            if (SelectAction != null) {
                SelectAction(pawn);
            }
        }

        public IEnumerable<CustomParentChildPawn> Pawns {
            get; set;
        }

        public IEnumerable<WidgetTable<CustomParentChildPawn>.RowGroup> RowGroups {
            get; set;
        }

        public override Vector2 InitialSize {
            get {
                return new Vector2(WindowSize.x, WindowSize.y);
            }
        }

        public override void DoWindowContents(Rect inRect) {
            if (resizeDirtyFlag) {
                Resize();
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
                    table.Draw(Pawns);
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

