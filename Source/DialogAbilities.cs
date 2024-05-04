using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class DialogAbilities : Window {
        public class Option {
            public AbilityDef Value { get; set; }
            public bool Selected { get; set; }
            public bool Disabled { get; set; }
        }
        public string ConfirmButtonLabel = "EdB.PC.Dialog.Implant.Button.Confirm";
        public string CancelButtonLabel = "EdB.PC.Common.Cancel";
        public Vector2 ContentMargin { get; protected set; }
        public Vector2 WindowSize { get; protected set; }
        public Vector2 ButtonSize { get; protected set; }
        public Vector2 ContentSize { get; protected set; }
        public float HeaderHeight { get; protected set; }
        public float FooterHeight { get; protected set; }
        public float LineHeight { get; protected set; }
        public float LinePadding { get; protected set; }
        public float WindowPadding { get; protected set; }
        public Rect ContentRect { get; protected set; }
        public Rect ScrollRect { get; protected set; }
        public Rect FooterRect { get; protected set; }
        public Rect HeaderRect { get; protected set; }
        public Rect CancelButtonRect { get; protected set; }
        public Rect ConfirmButtonRect { get; protected set; }
        public Rect SingleButtonRect { get; protected set; }
        public Color DottedLineColor = new Color(60f / 255f, 64f / 255f, 67f / 255f);
        public Vector2 DottedLineSize = new Vector2(342, 2);
        protected string headerLabel;
        protected bool resizeDirtyFlag = true;
        protected bool confirmed = false;

        protected WidgetTable<Option> table;
        protected List<Option> options = new List<Option>();
        protected CustomizedPawn pawn = null;
        protected bool disabledOptionsDirtyFlag = false;

        public DialogAbilities(CustomizedPawn pawn) {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            InitializeWithPawn(pawn);
            Resize();
        }

        protected void InitializeWithPawn(CustomizedPawn pawn) {
            this.pawn = pawn;
            InitializeOptions();
        }

        protected void InitializeOptions() {
            options.Clear();
            HashSet<AbilityDef> selected = new HashSet<AbilityDef>();
            selected.AddRange(pawn.Pawn.abilities.abilities.Select(a => a.def));
            options = DefDatabase<AbilityDef>.AllDefs.Where(a => !a.uiIcon?.NullOrBad() ?? false)
                .OrderBy(def => def.label)
                .Select(def => new Option() {
                    Value = def,
                    Selected = selected.Contains(def),
                    Disabled = false,
                }).ToList();
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

        public Action<IEnumerable<AbilityDef>> CloseAction {
            get;
            set;
        }

        public Action<CustomizedPawn> SelectAction {
            get;
            set;
        }

        public override Vector2 InitialSize {
            get {
                return new Vector2(WindowSize.x, WindowSize.y);
            }
        }

        public Func<string> ConfirmValidation = () => {
            return null;
        };

        protected void MarkResizeFlagDirty() {
            resizeDirtyFlag = true;
        }

        public void ClickOptionAction(Option option) {
            if (option.Disabled && !option.Selected) {
                return;
            }
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            option.Selected = !option.Selected;
        }

        protected void Resize() {
            float headerSize = 0;
            headerSize = HeaderHeight;
            if (HeaderLabel != null) {
                headerSize = HeaderHeight;
            }

            LineHeight = 40;
            LinePadding = 4;
            HeaderHeight = 32;
            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(550f, 584f);
            ButtonSize = new Vector2(140f, 40f);

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - headerSize);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

            ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

            HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);

            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
                ContentSize.x, FooterHeight);

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
            float radioWidth = 36;
            Vector2 nameSize = new Vector2(ContentRect.width - portraitSize.x - radioWidth, portraitSize.y * 0.5f);

            table = new WidgetTable<Option>();
            table.Rect = new Rect(Vector2.zero, ContentRect.size);
            table.RowHeight = LineHeight;
            table.RowColor = new Color(0, 0, 0, 0);
            table.AlternateRowColor = new Color(0, 0, 0, 0);
            table.SelectedAction = (Option option) => {
            };
            table.AddColumn(new WidgetTable<Option>.Column() {
                Name = "Icon",
                AdjustForScrollbars = false,
                DrawAction = (Option option, Rect rect, WidgetTable<Option>.Metadata metadata) => {
                    GUI.color = Color.white;
                    Rect iconRect = new Rect(rect.x, rect.y + 4, 32, 32);
                    GUI.DrawTexture(iconRect, option.Value.uiIcon);
                    
                }
            });
            table.AddColumn(new WidgetTable<Option>.Column() {
                Name = "Name",
                AdjustForScrollbars = true,
                DrawAction = (Option option, Rect rect, WidgetTable<Option>.Metadata metadata) => {
                    rect = rect.InsetBy(40, LinePadding, 0, LinePadding);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.LowerLeft;
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                    Rect dottedLineRect = new Rect(labelRect.x, labelRect.y + 21, DottedLineSize.x, DottedLineSize.y);
                    Rect checkboxRect = new Rect(labelRect.width - 22 - 6, labelRect.MiddleY() - 12, 22, 22);
                    Rect clickRect = new Rect(labelRect.x, labelRect.y, labelRect.width - checkboxRect.width, labelRect.height);
                    GUI.color = DottedLineColor;
                    GUI.DrawTexture(dottedLineRect, Textures.TextureDottedLine);
                    Vector2 labelSize = Text.CalcSize(option.Value.LabelCap);
                    GUI.color = Style.ColorWindowBackground;
                    GUI.DrawTexture(checkboxRect.InsetBy(-2, -2, -40, -2), BaseContent.WhiteTex);

                    Rect labelUnderlayRect = new Rect(labelRect.x, labelRect.y, labelSize.x, labelRect.height);
                    Rect subtitleRect = labelRect;
                    string subtitle = null;
                    if (option.Value.IsPsycast) {
                        subtitle = "EdB.PC.Dialog.Abilties.PsycastLevelLabel".Translate(option.Value.level);
                    }
                    if (subtitle != null) {
                        Vector2 subtitleSize = Text.CalcSize(option.Value.LabelCap);
                        if (subtitleSize.x > labelUnderlayRect.width) {
                            labelUnderlayRect = new Rect(labelUnderlayRect.x, labelUnderlayRect.y, subtitleSize.x, labelUnderlayRect.width);
                        }
                        labelRect = labelRect.OffsetBy(0, -10);
                        subtitleRect = new Rect(labelRect.x, labelRect.yMax - 6, labelRect.width, 18);
                        GUI.color = Style.ColorWindowBackground;
                    }
                    GUI.color = Style.ColorWindowBackground;
                    GUI.DrawTexture(labelUnderlayRect.OffsetBy(-1, 0).GrowBy(2, 0), BaseContent.WhiteTex);

                    TooltipHandler.TipRegion(labelRect, new TipSignal(() => option.Value.GetTooltip(pawn.Pawn), 23467344));
                    if (!option.Disabled) {
                        Style.SetGUIColorForButton(rect, option.Selected, Style.ColorText, Style.ColorButtonHighlight, Style.ColorButtonHighlight);
                        Widgets.Label(labelRect.OffsetBy(0, -2), option.Value.LabelCap);
                        Text.Font = GameFont.Tiny;
                        Widgets.Label(subtitleRect, subtitle);
                        Text.Font = GameFont.Small;
                        if (Widgets.ButtonInvisible(clickRect)) {
                            Logger.Debug("Selected " + option.Value.defName);
                            Logger.Debug("  uiIcon.NullOrBad() = " + option.Value.uiIcon?.NullOrBad());
                            Logger.Debug("  comps = " + option.Value.comps?.Select(c => c.compClass.Name).CommaDelimitedList());
                            Logger.Debug("  generated = " + option.Value.generated);
                            Logger.Debug("  hostile = " + option.Value.hostile);
                            Logger.Debug("  showWhenDrafted = " + option.Value.showWhenDrafted);
                            Logger.Debug("  showOnCharacterCard = " + option.Value.showOnCharacterCard);
                            Logger.Debug("  aiCanUse = " + option.Value.aiCanUse);
                            Logger.Debug("  abilityClass = " + option.Value.abilityClass?.Name);
                            Logger.Debug("  category = " + option.Value.category?.defName);
                            Logger.Debug("  groupAbility = " + option.Value.groupAbility);
                            Logger.Debug("  StatSummary() = " + option.Value.StatSummary());
                            ClickOptionAction(option);
                        }
                        GUI.color = Color.white;
                        Texture2D checkboxTexture = Textures.TextureCheckbox;
                        if (option.Selected) {
                            checkboxTexture = Textures.TextureCheckboxSelected;
                        }
                        if (Widgets.ButtonImage(checkboxRect, checkboxTexture)) {
                            ClickOptionAction(option);
                        }
                    }
                    else {
                        if (subtitle == null) {
                            GUI.color = Style.ColorControlDisabled;
                            Widgets.Label(labelRect.OffsetBy(0, -2), option.Value.LabelCap);
                        }
                        else {
                            GUI.color = Style.ColorControlDisabled;
                            Widgets.Label(labelRect, option.Value.LabelCap);
                            Text.Font = GameFont.Tiny;
                            Widgets.Label(subtitleRect, subtitle);
                            Text.Font = GameFont.Small;
                        }
                        GUI.DrawTexture(checkboxRect, option.Selected ? Textures.TextureCheckboxPartiallySelected : Textures.TextureCheckbox);
                        if (Widgets.ButtonInvisible(checkboxRect)) {
                            ClickOptionAction(option);
                        }
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                },
                MeasureAction = (Option option, float width, WidgetTable<Option>.Metadata metadata) => {
                    return LineHeight;
                },
                Width = ContentSize.x,
            });

            resizeDirtyFlag = false;
        }

        public override void DoWindowContents(Rect inRect) {
            if (resizeDirtyFlag) {
                Resize();
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            if (HeaderLabel != null) {
                Rect headerRect = HeaderRect;
                Widgets.Label(headerRect, HeaderLabel);
            }

            Text.Font = GameFont.Small;
            GUI.BeginGroup(ContentRect);

            try {
                table.Draw(this.options);
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
                    CloseAction(options.Where(o => o.Selected).Select(o => o.Value));
                }
            }
            else {
                if (CloseAction != null) {
                    CloseAction(options.Where(o => o.Selected).Select(o => o.Value));
                }
            }
        }
    }
}
