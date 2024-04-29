using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
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
        public CustomizedPawn PawnForCompatibility { get; set; } = null;

        public DialogSelectParentChildPawn() {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            Resize();
        }

        public CustomizedPawn SelectedPawn {
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
        public Action<CustomizedPawn> SelectAction {
            get;
            set;
        }
        public HashSet<CustomizedPawn> DisabledPawns {
            get;
            set;
        }

        protected WidgetTable<CustomizedPawn> table = new WidgetTable<CustomizedPawn>();

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
            float nameOffset = 2;
            float descriptionOffset = 34;
            float compatibilityOffset = 18;
            if (PawnForCompatibility != null) {
                nameOffset = -6;
            }
            float radioWidth = 36;
            Vector2 nameSize = new Vector2(ContentRect.width - portraitSize.x - radioWidth, portraitSize.y * 0.5f);

            table = new WidgetTable<CustomizedPawn>();
            table.Rect = new Rect(Vector2.zero, ContentRect.size);
            table.RowHeight = portraitSize.y;
            table.RowGroupHeaderHeight = RowGroupHeaderHeight;
            table.RowColor = new Color(28f / 255f, 32f / 255f, 36f / 255f);
            table.AlternateRowColor = new Color(0, 0, 0, 0);
            table.SelectedRowColor = new Color(0, 0, 0, 0);
            table.SupportSelection = true;
            table.SelectedAction = (CustomizedPawn pawn) => {
                if (DisabledPawns == null || !DisabledPawns.Contains(pawn)) {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    Select(pawn);
                }
            };
            table.AddColumn(new WidgetTable<CustomizedPawn>.Column() {
                Name = "Portrait",
                DrawAction = (CustomizedPawn pawn, Rect rect, WidgetTable<CustomizedPawn>.Metadata metadata) => {
                    GUI.color = Color.white;
                    if (IsPawnVisible(pawn)) {
                        WidgetPortrait.Draw(pawn?.Pawn, rect, new Rect(0, 0, portraitSize.x, portraitSize.y).OutsetBy(20, 20).OffsetBy(0, 2));
                    }
                    else {
                        GUI.color = Style.ColorButton;
                        Rect genderRect = new Rect(rect.MiddleX() - GenderSize.HalfX(), rect.MiddleY() - GenderSize.HalfY(), GenderSize.x, GenderSize.y);
                        if (pawn.Pawn?.gender == Gender.Female || pawn.TemporaryPawn?.Gender == Gender.Female) {
                            GUI.DrawTexture(genderRect, Textures.TextureGenderFemaleLarge);
                        }
                        else if (pawn.Pawn?.gender == Gender.Male || pawn.TemporaryPawn?.Gender == Gender.Male) {
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
            table.AddColumn(new WidgetTable<CustomizedPawn>.Column() {
                Name = "Description",
                Width = nameSize.x,
                AdjustForScrollbars = true,
                DrawAction = (CustomizedPawn pawn, Rect rect, WidgetTable<CustomizedPawn>.Metadata metadata) => {
                    Text.Anchor = TextAnchor.LowerLeft;
                    Text.Font = GameFont.Small;
                    Rect nameRect = new Rect(rect.x, rect.y + nameOffset, rect.width, nameSize.y);
                    Widgets.Label(nameRect, FullNameForPawn(pawn));
                    Text.Anchor = TextAnchor.UpperLeft;
                    string description;
                    if (IsPawnVisible(pawn)) {
                        string age = pawn.Pawn.ageTracker.AgeBiologicalYears != pawn.Pawn.ageTracker.AgeChronologicalYears ?
                            "EdB.PC.Pawn.AgeWithChronological".Translate(pawn.Pawn.ageTracker.AgeBiologicalYears, pawn.Pawn.ageTracker.AgeChronologicalYears) :
                            "EdB.PC.Pawn.AgeWithoutChronological".Translate(pawn.Pawn.ageTracker.AgeBiologicalYears);
                        description = pawn.Pawn.gender != Gender.None ?
                            "EdB.PC.Pawn.PawnDescriptionWithGender".Translate(UtilityPawns.GetShortProfessionLabel(pawn.Pawn), pawn.Pawn.gender.GetLabel(), age) :
                            "EdB.PC.Pawn.PawnDescriptionNoGender".Translate(UtilityPawns.GetShortProfessionLabel(pawn.Pawn), age);
                    }
                    else {
                        string profession = "EdB.PC.Pawn.HiddenPawnProfession".Translate();
                        description = (pawn.TemporaryPawn?.Gender ?? Gender.None) != Gender.None ?
                            "EdB.PC.Pawn.HiddenPawnDescriptionWithGender".Translate(profession, pawn.TemporaryPawn?.Gender.GetLabel()) :
                            "EdB.PC.Pawn.HiddenPawnDescriptionNoGender".Translate(profession);
                    }
                    Text.Font = GameFont.Tiny;
                    Rect descriptionRect = new Rect(rect.x, nameRect.y + descriptionOffset, rect.width, nameSize.y);
                    Widgets.Label(descriptionRect, description);

                    if (PawnForCompatibility != null) {
                        string value;
                        if (PawnForCompatibility.Type != CustomizedPawnType.Hidden) {
                            if (pawn.Pawn == null || PawnForCompatibility.Pawn == null) {
                                value = "EdB.PC.AddRelationship.UnknownCompatibility".Translate();
                            }
                            else {
                                float score = (float)Math.Round(PawnForCompatibility?.Pawn?.relations?.CompatibilityWith(pawn.Pawn) ?? 0f, 2);
                                value = score.ToString();
                            }
                        }
                        else {
                            value = "EdB.PC.AddRelationship.UnknownCompatibility".Translate();
                        }
                        Widgets.Label(new Rect(rect.x, descriptionRect.y + compatibilityOffset, rect.width, nameSize.y), "EdB.PC.AddRelationship.Compatibility".Translate(value));
                    }
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
            table.AddColumn(new WidgetTable<CustomizedPawn>.Column() {
                Name = "RadioButton",
                Width = radioWidth,
                DrawAction = (CustomizedPawn pawn, Rect rect, WidgetTable<CustomizedPawn>.Metadata metadata) => {
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

        public bool IsPawnVisible(CustomizedPawn pawn) {
            return pawn.Type != CustomizedPawnType.Hidden && pawn.Type != CustomizedPawnType.Temporary;
        }

        public string FullNameForPawn(CustomizedPawn pawn) {
            if (pawn.Type == CustomizedPawnType.Hidden) {
                return "EdB.PC.Pawn.HiddenPawnNameFull".Translate(pawn.TemporaryPawn.Index);
            }
            else if (pawn.Type == CustomizedPawnType.Temporary) {
                return "EdB.PC.Pawn.TemporaryPawnNameFull".Translate(pawn.TemporaryPawn.Index);
            }
            else {
                return pawn.Pawn.Name.ToStringFull;
            }
        }

        protected void Select(CustomizedPawn pawn) {
            this.SelectedPawn = pawn;
            SelectAction?.Invoke(pawn);
        }

        public IEnumerable<CustomizedPawn> Pawns {
            get; set;
        }

        public IEnumerable<WidgetTable<CustomizedPawn>.RowGroup> RowGroups {
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
                if (confirmed) {
                    CloseAction?.Invoke();
                }
            }
            else {
                CloseAction?.Invoke();
            }
        }
    }
}

