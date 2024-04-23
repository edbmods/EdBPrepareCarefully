using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class DialogManageTraits : Window {
        public class TraitOption {
            public Trait Trait { get; set; }
            public bool Selected { get; set; } = false;
            public bool Disabled { get; set; } = false;
            public bool Forced { get; set; } = false;
        }

        public class TipCache {
            public Dictionary<Trait, string> Lookup = new Dictionary<Trait, string>();
            private bool ready = false;
            public void Invalidate() {
                this.ready = false;
                Lookup.Clear();
            }
            public void MakeReady() {
                this.ready = true;
            }
            public bool Ready {
                get {
                    return ready;
                }
            }
        }

        public delegate void AddTraitHandler(Trait trait);
        public delegate void RemoveTraitHandler(Trait trait);
        public delegate void SetTraitsHandler(IEnumerable<Trait> traits);

        public event AddTraitHandler TraitAdded;
        public event RemoveTraitHandler TraitRemoved;
        public event SetTraitsHandler TraitsSet;

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
        protected WidgetTable<TraitOption> table;
        protected List<TraitOption> traitOptionList = new List<TraitOption>();
        protected CustomizedPawn customizedPawn = null;
        protected bool optionStateDirtyFlag = true;
        protected List<Implant> validImplants = new List<Implant>();
        protected string cachedBlockedSelectionAlert = null;
        protected ProviderTraits providerTraits = null;
        protected TipCache tipCache = new TipCache();
        public List<Trait> originalTraits = new List<Trait>();

        public DialogManageTraits(ProviderTraits providerTraits) {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            this.providerTraits = providerTraits;
            Resize();
        }

        public void InitializeWithCustomizedPawn(CustomizedPawn customizedPawn) {
            this.customizedPawn = customizedPawn;
            InitializeTraitOptionList();
            tipCache.Invalidate();
            tipCache.MakeReady();
            optionStateDirtyFlag = true;
            originalTraits = customizedPawn?.Pawn?.story?.traits?.allTraits?.ConvertAll(t => new Trait(t.def, t.Degree));
        }

        protected void InitializeTraitOptionList() {
            traitOptionList.Clear();
            foreach (var trait in providerTraits.Traits) {
                traitOptionList.Add(new TraitOption() {
                    Trait = trait,
                });
            }
        }

        protected void ResetOptionState() {
            HashSet<TraitDef> disabledTraitDefs = new HashSet<TraitDef>();
            customizedPawn?.Pawn?.story?.traits?.allTraits?.ForEach((t) => {
                disabledTraitDefs.Add(t.def);
            });
            customizedPawn.Pawn?.kindDef?.disallowedTraits?.ForEach(def => {
                disabledTraitDefs.Add(def);
            });
            foreach (var trait in customizedPawn?.Pawn?.story?.traits?.allTraits) {
                if (trait?.def?.conflictingTraits?.CountAllowNull() > 0) {
                    foreach (var conflictingTrait in trait?.def?.conflictingTraits) {
                        disabledTraitDefs.Add(conflictingTrait);
                    }
                }
            }
            traitOptionList.ForEach(trait => { trait.Selected = false; trait.Disabled = false; });
            foreach (var traitOption in traitOptionList) {
                if (disabledTraitDefs.Contains(traitOption.Trait.def) ) {
                    traitOption.Disabled = true;
                }
                customizedPawn?.Pawn?.story?.traits?.allTraits?.ForEach(trait => {
                    if (UtilityTraits.TraitsMatch(trait, traitOption.Trait)) {
                        traitOption.Selected = true;
                        traitOption.Disabled = false;
                    }
                });
                customizedPawn.Pawn?.kindDef?.disallowedTraitsWithDegree?.ForEach(req => {
                    if (req.Matches(traitOption.Trait)) {
                        traitOption.Disabled = true;
                    }
                });
                customizedPawn.Pawn?.story?.Childhood?.disallowedTraits?.ForEach(backstoryTrait => {
                    if (UtilityTraits.TraitsMatch(backstoryTrait, traitOption.Trait)) {
                        traitOption.Disabled = true;
                    }
                });
                customizedPawn.Pawn?.story?.Adulthood?.disallowedTraits?.ForEach(backstoryTrait => {
                    if (UtilityTraits.TraitsMatch(backstoryTrait, traitOption.Trait)) {
                        traitOption.Disabled = true;
                    }
                });
            }
        }

        protected void MarkOptionsAsDirty() {
            this.optionStateDirtyFlag = true;
        }

        protected void EvaluateOptionsDirtyState() {
            if (optionStateDirtyFlag) {
                ResetOptionState();
                optionStateDirtyFlag = false;
            }
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

        public Action<List<Implant>> CloseAction {
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

        public void ClickTraitAction(TraitOption traitOption) {
            if (traitOption.Disabled && !traitOption.Selected) {
                return;
            }
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            if (traitOption.Selected) {
                RemoveTrait(traitOption);
            }
            else {
                AddTrait(traitOption);
            }
            MarkOptionsAsDirty();
        }

        protected void AddTrait(TraitOption traitOption) {
            TraitAdded?.Invoke(traitOption.Trait);
            MarkOptionsAsDirty();
        }

        protected void RemoveTrait(TraitOption traitOption) {
            TraitRemoved?.Invoke(traitOption.Trait);
            MarkOptionsAsDirty();
        }

        protected void Resize() {
            float headerSize = 0;
            headerSize = HeaderHeight;
            if (HeaderLabel != null) {
                headerSize = HeaderHeight;
            }

            LineHeight = 30;
            LinePadding = 2;
            HeaderHeight = 32;
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

            table = new WidgetTable<TraitOption>();
            table.Rect = new Rect(Vector2.zero, ContentRect.size);
            table.RowHeight = LineHeight;
            table.RowColor = new Color(0, 0, 0, 0);
            table.AlternateRowColor = new Color(0, 0, 0, 0);
            table.SelectedAction = (TraitOption trait) => {
            };
            table.AddColumn(new WidgetTable<TraitOption>.Column() {
                Name = "Trait",
                AdjustForScrollbars = true,
                DrawAction = (TraitOption traitOption, Rect rect, WidgetTable<TraitOption>.Metadata metadata) => {
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.LowerLeft;
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width, LineHeight);
                    Rect dottedLineRect = new Rect(labelRect.x, labelRect.y + 21, DottedLineSize.x, DottedLineSize.y);
                    Rect checkboxRect = new Rect(labelRect.width - 22 - 6, labelRect.MiddleY() - 12, 22, 22);
                    Rect clickRect = new Rect(labelRect.x, labelRect.y, labelRect.width - checkboxRect.width, labelRect.height);
                    GUI.color = DottedLineColor;
                    GUI.DrawTexture(dottedLineRect, Textures.TextureDottedLine);
                    Vector2 labelSize = Text.CalcSize(traitOption.Trait.LabelCap);
                    GUI.color = Style.ColorWindowBackground;
                    GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, labelSize.x + 2, labelRect.height), BaseContent.WhiteTex);
                    GUI.DrawTexture(checkboxRect.InsetBy(-2, -2, -40, -2), BaseContent.WhiteTex);
                    if (!traitOption.Disabled) {
                        Style.SetGUIColorForButton(labelRect, traitOption.Selected, Style.ColorText, Style.ColorButtonHighlight, Style.ColorButtonHighlight);
                        Widgets.Label(labelRect, traitOption.Trait.LabelCap);

                        TooltipHandler.TipRegion(labelRect, GetTraitTip(traitOption.Trait, customizedPawn));
                        if (Widgets.ButtonInvisible(clickRect)) {
                            ClickTraitAction(traitOption);
                        }
                        GUI.color = Color.white;
                        Texture2D checkboxTexture = Textures.TextureCheckbox;
                        if (traitOption.Selected) {
                            checkboxTexture = Textures.TextureCheckboxSelected;
                        }
                        if (Widgets.ButtonImage(checkboxRect, checkboxTexture)) {
                            ClickTraitAction(traitOption);
                        }
                    }
                    else {
                        GUI.color = Style.ColorControlDisabled;
                        Widgets.Label(labelRect, traitOption.Trait.LabelCap);
                        GUI.DrawTexture(checkboxRect, traitOption.Selected ? Textures.TextureCheckboxPartiallySelected : Textures.TextureCheckbox);
                        if (Widgets.ButtonInvisible(checkboxRect)) {
                            ClickTraitAction(traitOption);
                        }
                        TooltipHandler.TipRegion(labelRect, GetTraitTip(traitOption.Trait, customizedPawn));
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                },
                MeasureAction = (TraitOption recipe, float width, WidgetTable<TraitOption>.Metadata metadata) => {
                    return LineHeight;
                },
                Width = ContentSize.x
            });

            resizeDirtyFlag = false;
        }

        public override void DoWindowContents(Rect inRect) {
            if (resizeDirtyFlag) {
                Resize();
            }
            EvaluateOptionsDirtyState();
            GUI.color = Color.white;
            Text.Font = GameFont.Medium;
            if (HeaderLabel != null) {
                Rect headerRect = HeaderRect;
                if (cachedBlockedSelectionAlert != null) {
                    Rect alertRect = new Rect(headerRect.xMin, headerRect.yMin + 5, 20, 20);
                    GUI.DrawTexture(alertRect, Textures.TextureAlertSmall);
                    TooltipHandler.TipRegion(alertRect, cachedBlockedSelectionAlert);
                    headerRect = headerRect.InsetBy(26, 0, 0, 0);
                }
                Widgets.Label(headerRect, HeaderLabel);
            }

            Text.Font = GameFont.Small;
            GUI.BeginGroup(ContentRect);

            try {
                table.Draw(this.traitOptionList);
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
                        TraitsSet?.Invoke(originalTraits);
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

        protected string GetTraitTip(Trait trait, CustomizedPawn customizedPawn) {
            if (!tipCache.Ready || !tipCache.Lookup.ContainsKey(trait)) {
                string value = GenerateTraitTip(trait, customizedPawn);
                tipCache.Lookup.Add(trait, value);
                return value;
            }
            else {
                return tipCache.Lookup[trait];
            }
        }
        protected string GenerateTraitTip(Trait trait, CustomizedPawn customizedPawn) {
            try {
                //Logger.Debug(string.Format("Generated trait ({0}) tip for pawn ({1})", trait.LabelCap, customizedPawn.Pawn.LabelCap));
                string baseTip = trait.TipString(customizedPawn.Pawn);
                return baseTip;
            }
            catch (Exception e) {
                Logger.Warning("There was an error when trying to generate a mouseover tip for trait {" + (trait?.LabelCap ?? "null") + "}\n" + e);
                return null;
            }
        }
    }
}
