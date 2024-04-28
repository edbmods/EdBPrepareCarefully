using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class DialogApparel : Window {

        public delegate void ApparelRemovedHandler(Thing thing);
        public delegate void ApparelAddedHandler(CustomizationsApparel apparel);
        public delegate void ApparelReplacedHandler(List<CustomizationsApparel> apparelList);

        public event ApparelRemovedHandler ApparelRemoved;
        public event ApparelAddedHandler ApparelAdded;
        public event ApparelReplacedHandler ApparelReplaced;

        public ProviderEquipment ProviderEquipment { get; set; }
        public IEnumerable<EquipmentOption> ApparelList { get; set; }
        public CustomizedPawn CustomizedPawn { get; set; }
        public WidgetTable<EquipmentRecord> Table { get; private set; }
        public WidgetEquipmentLoadingProgressBar ProgressBar { get; private set; } = new WidgetEquipmentLoadingProgressBar();
        public float WindowPadding { get; private set; }
        public Vector2 WindowSize { get; private set; }
        public Vector2 ButtonSize {  get; private set; }
        public Vector2 ContentMargin { get; private set; }
        public Vector2 ContentSize { get; private set; }
        public float FooterHeight { get; private set; }
        public Rect FooterRect { get; private set; }
        public Rect ContentRect { get; private set; }
        public Rect SelectedApparelRect { get; private set; }
        public Rect SelectedApparelScrollRect { get; private set; }
        public Rect AvailableApparelRect { get; private set; }
        public Rect AvailableApparelScrollRect { get; private set; }
        public Rect AvailableApparelFilterRect { get; private set; }
        public Rect PortraitRect { get; private set; }
        public Rect ScrollRect { get; private set; }
        public Rect CancelButtonRect { get; private set; }
        public Rect ConfirmButtonRect { get; private set; }
        public Rect HitPointsLabelRect { get; private set; }
        public Rect PercentLabelRect { get; private set; }
        public Rect HitPointsSliderRect { get; private set; }

        public Rect RectButtonRotateView { get; private set; }
        public string ConfirmButtonLabel { get; private set; } = "EdB.PC.Dialog.Implant.Button.Confirm";
        public string CancelButtonLabel { get; private set; } = "EdB.PC.Common.Cancel";
        public WidgetScrollViewVertical ScrollViewAvailableApparel { get; private set; } = new WidgetScrollViewVertical();
        public WidgetScrollViewVertical ScrollViewSelectedApparel { get; private set; } = new WidgetScrollViewVertical();
        public ThingDef ScrollToThingDef { get; set; }

        public static Color ColorPortraitBorder = new Color(0.3843f, 0.3843f, 0.3843f);

        public List<CustomizationsApparel> OriginalApparelSelections;

        private List<FloatMenuOption> QualityFloatMenuOptions = new List<FloatMenuOption>();
        private List<FloatMenuOption> ApparelLayerFloatMenuOptions = new List<FloatMenuOption>();

        private bool ApparelOptionsLoaded = false;
        private EquipmentOption SelectedOption = null;
        private Apparel SelectedApparel = null;
        private ThingDef SelectedStuff = null;
        private QualityCategory? SelectedQuality = null;
        private Color? SelectedColor = null;
        private float SelectedHitPoints = 1.0f;
        private ApparelLayerDef SelectedLayer = null;
        private StyleCategoryDef SelectedStyle = null;

        private bool ColorSelectionEnabled = false;
        private List<Color> CurrentSwatches = new List<Color>();
        private bool CheckSelectionVisibility = false;
        private LinkedList<ThingDef> PreferredMaterials = new LinkedList<ThingDef>();
        private Rot4 pawnViewRotation = Rot4.South;

        public DialogApparel() {
            this.closeOnCancel = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

            foreach (var quality in Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>()) {
                QualityFloatMenuOptions.Add(new FloatMenuOption(quality.GetLabel().CapitalizeFirst(), () => { SelectQuality(quality); }, MenuOptionPriority.Default, null, null, 0, null, null));
            }

            ApparelLayerFloatMenuOptions.Add(new FloatMenuOption("EdB.PC.Dialog.ManageApparel.AllLayers".Translate(), () => { SelectedLayer = null; }, MenuOptionPriority.Default, null, null, 0, null, null));
            foreach (var layer in DefDatabase<ApparelLayerDef>.AllDefs) {
                ApparelLayerFloatMenuOptions.Add(new FloatMenuOption(layer.LabelCap, () => { SelectedLayer = layer; }, MenuOptionPriority.Default, null, null, 0, null, null));
            }

            foreach (var m in Find.FactionManager.OfPlayer.def.apparelStuffFilter.AllowedThingDefs) {
                PreferredMaterials.AddLast(m);
            }
            foreach (var m in new ThingDef[] { DefDatabase<ThingDef>.GetNamedSilentFail("Synthread"),
                    DefDatabase<ThingDef>.GetNamedSilentFail("Steel"),
                    DefDatabase<ThingDef>.GetNamedSilentFail("Plasteel"),
                    DefDatabase<ThingDef>.GetNamedSilentFail("WoodLog"),
                }) {
                if (m != null) {
                    PreferredMaterials.AddLast(m);
                }
            }
            Resize();
        }

        public void InitializeWithPawn(CustomizedPawn pawn) {
            CustomizedPawn = pawn;
            OriginalApparelSelections = pawn.Customizations.Apparel.ConvertAll(c => c);
            SelectedOption = null;
            pawnViewRotation = Rot4.South;
        }

        public override Vector2 InitialSize {
            get => new Vector2(WindowSize.x, WindowSize.y);
        }

        protected void Resize() {
            float headerSize = 0;

            FooterHeight = 40f;
            WindowPadding = 18;
            ContentMargin = new Vector2(10f, 18f);
            WindowSize = new Vector2(750f, 584f);
            ButtonSize = new Vector2(140f, 40f);

            ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
                WindowSize.y - WindowPadding * 2 - ContentMargin.y * 2 - FooterHeight - headerSize);

            ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

            float selectedApparelWidth = 250;
            SelectedApparelRect = new Rect(ContentRect.xMax - selectedApparelWidth, 0, selectedApparelWidth, ContentRect.height);
            PortraitRect = new Rect(SelectedApparelRect.x, SelectedApparelRect.y, SelectedApparelRect.width, 200);
            SelectedApparelScrollRect = new Rect(SelectedApparelRect.x, PortraitRect.yMax + 12, SelectedApparelRect.width, SelectedApparelRect.height - PortraitRect.height - 12);

            float availableApparelWidth = ContentRect.width - ContentMargin.y - SelectedApparelRect.width;
            AvailableApparelRect = new Rect(0, 0, availableApparelWidth, ContentRect.height);
            AvailableApparelFilterRect = new Rect(AvailableApparelRect.x, AvailableApparelRect.y, AvailableApparelRect.width, 24);
            AvailableApparelScrollRect = new Rect(AvailableApparelRect.x, AvailableApparelFilterRect.yMax, AvailableApparelRect.width, AvailableApparelRect.height - AvailableApparelFilterRect.yMax);

            FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
                ContentSize.x, FooterHeight);

            CancelButtonRect = new Rect(0,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);
            ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
                (FooterHeight / 2) - (ButtonSize.y / 2),
                ButtonSize.x, ButtonSize.y);

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Tiny;
            Vector2 sizeHitPoints = Text.CalcSize("EdB.PC.Equipment.AvailableEquipment.HitPointsLabel".Translate());
            Vector2 sizePercent = Text.CalcSize("100%");
            Text.Font = savedFont;
            float labelHeight = 18f;
            HitPointsLabelRect = new Rect(0, 0, sizeHitPoints.x, labelHeight);

            float sliderHeight = 8f;
            float fieldPadding = 4f;
            float sliderWidth = 150f;
            HitPointsSliderRect = new Rect(HitPointsLabelRect.xMax + fieldPadding, HitPointsLabelRect.yMin + HitPointsLabelRect.height * 0.5f - sliderHeight * 0.5f - 1,
                sliderWidth, sliderHeight);
            PercentLabelRect = new Rect(HitPointsSliderRect.xMax + fieldPadding, HitPointsLabelRect.yMin, sizePercent.x, labelHeight);

            Vector2 rotateViewButtonSize = new Vector2(24, 12);
            RectButtonRotateView = new Rect(PortraitRect.x + 10, PortraitRect.yMax - rotateViewButtonSize.y - 10, rotateViewButtonSize.x, rotateViewButtonSize.y);
        }

        public override void DoWindowContents(Rect inRect) {

            if (!ApparelOptionsLoaded) {
                if (ProviderEquipment != null && ProviderEquipment.DatabaseReady) {
                    ApparelOptionsLoaded = true;
                    ApparelList = ProviderEquipment.Apparel;
                }
                else {
                    ProgressBar.Draw(ContentRect, ProviderEquipment);
                    return;
                }
            }
            DrawPawn(CustomizedPawn, PortraitRect);

            // Rotate view button
            if (RectButtonRotateView.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorButtonHighlight;
            }
            else {
                GUI.color = Style.ColorButton;
            }
            GUI.DrawTexture(RectButtonRotateView, Textures.TextureButtonRotateView);
            if (Widgets.ButtonInvisible(RectButtonRotateView, false)) {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                if (pawnViewRotation == Rot4.South) {
                    pawnViewRotation = Rot4.East;
                }
                else if (pawnViewRotation == Rot4.East) {
                    pawnViewRotation = Rot4.North;
                }
                else if (pawnViewRotation == Rot4.North) {
                    pawnViewRotation = Rot4.West;
                }
                else if (pawnViewRotation == Rot4.West) {
                    pawnViewRotation = Rot4.South;
                }
            }
            GUI.color = Color.white;

            // Draw selected apparel
            Pawn pawn = CustomizedPawn?.Pawn;
            float y = 0;
            ScrollViewSelectedApparel.Begin(SelectedApparelScrollRect);
            var savedGUIState = UtilityGUIState.Save();
            Thing apparelToRemove = null;
            try {
                if (pawn != null && pawn.apparel.WornApparelCount > 0) {
                    int index = 0;
                    
                    foreach (Apparel item in pawn.apparel.WornApparel.OrderByDescending(a => a.def.apparel.bodyPartGroups[0].listOrder)) {
                        if (index > 0) {
                            y += 4;
                        }
                        float width = SelectedApparelScrollRect.width;
                        void labelClickAction() {
                            SelectApparel(item);
                        }
                        void deleteAction() {
                            apparelToRemove = item;
                        }
                        y += WidgetEquipmentField.DrawSelectedEquipment(0, y, ScrollViewSelectedApparel.CurrentViewWidth, item, labelClickAction, deleteAction);
                        index++;
                    }
                }
                else {
                    GUI.color = Style.ColorText;
                    Vector2 margin = new Vector2(10, 3);
                    Rect rectText = new Rect(margin.x, y, SelectedApparelScrollRect.width - margin.x * 2, 20);
                    Widgets.Label(rectText, "EdB.PC.Panel.Incapable.None".Translate());
                    y += rectText.height;
                    GUI.color = Color.white;
                }
            }
            finally {
                ScrollViewSelectedApparel.End(y);
                savedGUIState.Restore();
            }

            if (apparelToRemove != null) {
                ApparelRemoved?.Invoke(apparelToRemove);
            }

            // If we're meant to scroll to a specific item, make sure that it's available in the current filtered list of apparel.
            // If it's not, remove the filter.  If it is, mark it as the selected option
            if (ScrollToThingDef != null) {
                if (SelectedLayer != null) {
                    if (!ApparelList.Where(e => e.ThingDef.apparel.layers.Contains(SelectedLayer)).Select(e => e.ThingDef).Contains(ScrollToThingDef)) {
                        SelectedLayer = null;
                    }
                }
                SelectedOption = ApparelList.Where(e => SelectedLayer == null || e.ThingDef.apparel.layers.Contains(SelectedLayer))
                        .FirstOrDefault(e => e.ThingDef == ScrollToThingDef);
            }

            // Layer filter
            if (WidgetDropdown.SmallDropdown(AvailableApparelFilterRect, SelectedLayer?.LabelCap ?? "EdB.PC.Dialog.ManageApparel.AllLayers".Translate())) {
                Find.WindowStack.Add(new FloatMenu(ApparelLayerFloatMenuOptions, null, false));
            }

            float rowHeight = 36;
            float lineHeight = 32;
            float lineWidth = ScrollViewAvailableApparel.CurrentViewWidth;
            Rect RowRect = new Rect(0, 0, lineWidth, rowHeight);
            Rect IconRect = new Rect(2, 2, lineHeight, lineHeight);
            Rect LabelRect = new Rect(42f, 3, lineWidth - 42f, lineHeight - 1);
            y = 0;
            Color savedColor = GUI.color;
            TextAnchor savedAlignment = Text.Anchor;
            ScrollViewAvailableApparel.Begin(AvailableApparelScrollRect);
            try {
                Text.Anchor = TextAnchor.MiddleLeft;
                foreach (EquipmentOption option in ApparelList.Where(e => SelectedLayer == null || e.ThingDef.apparel.layers.Contains(SelectedLayer))) {
                    if (SelectedOption == option) {
                        float topOfBox = y;
                        float scrollBoxTopPosition = ScrollViewAvailableApparel.Position.y;
                        float height = DrawSelectedRow(option, y, ScrollViewAvailableApparel.CurrentViewWidth);
                        y += height;
                        if (ScrollToThingDef == option.ThingDef) {
                            float scrollToY = topOfBox - ScrollViewAvailableApparel.ViewHeight * 0.5f + height * 0.5f;
                            ScrollViewAvailableApparel.ScrollTo(scrollToY);
                        }
                        // Make sure the selected item is fully visible.  If not scroll to it
                        else if (CheckSelectionVisibility) {
                            if (y > scrollBoxTopPosition + ScrollViewAvailableApparel.ViewHeight) {
                                float amount = y - (scrollBoxTopPosition + ScrollViewAvailableApparel.ViewHeight);
                                ScrollViewAvailableApparel.ScrollTo(scrollBoxTopPosition + amount);
                            }
                            else if (y < ScrollViewAvailableApparel.Position.y) {
                                ScrollViewAvailableApparel.ScrollTo(topOfBox);
                            }
                        }
                    }
                    else {
                        y += DrawRow(option, y, ScrollViewAvailableApparel.CurrentViewWidth);
                    }
                }
            }
            finally {
                ScrollViewAvailableApparel.End(y);
                Text.Anchor = savedAlignment;
                GUI.color = savedColor;
                ScrollToThingDef = null;
                CheckSelectionVisibility = false;
            }


            GUI.BeginGroup(FooterRect);
            try {
                if (CancelButtonLabel != null) {
                    if (Widgets.ButtonText(CancelButtonRect, CancelButtonLabel.Translate(), true, true, true)) {
                        ApparelReplaced?.Invoke(OriginalApparelSelections);
                        this.Close(true);
                    }
                }
                if (Widgets.ButtonText(ConfirmButtonRect, ConfirmButtonLabel.Translate(), true, true, true)) {
                    this.Close(true);
                }
            }
            finally {
                GUI.EndGroup();
            }
        }

        public void SelectApparel(Apparel apparel) {
            EquipmentOption option = ProviderEquipment.EquipmentDatabase.FindOptionForThingDef(apparel.def);
            if (option == null) {
                return;
            }
            SelectedApparel = apparel;
            SelectOption(option);
            ScrollToSelectedOption(option);
            SelectedStuff = apparel.Stuff;
            SelectedStyle = apparel.StyleDef?.Category;
            if (apparel.TryGetQuality(out QualityCategory quality)) {
                SelectedQuality = quality;
            }
            else {
                SelectedQuality = QualityCategory.Normal;
            }
            SelectedHitPoints = UtilityApparel.HitPointPercentForApparel(apparel);
            if (ColorSelectionEnabled) {
                if (!apparel.def.MadeFromStuff) {
                    SelectedColor = apparel.GetColor();
                }
                else {
                    Color stuffColor = apparel.def.GetColorForStuff(apparel.Stuff);
                    SelectedColor = stuffColor;
                    CompColorable comp = apparel.TryGetComp<CompColorable>();
                    if (comp?.Active ?? false) {
                        SelectedColor = comp.Color;
                    }
                }
                UpdateColorSwatchesForSelectedThing();
            }
            else {
                SelectedColor = null;
            }
        }

        public void ScrollToSelectedOption(EquipmentOption option) {
            if (option == null) {
                return;
            }
            ScrollToThingDef = option.ThingDef;
            CheckSelectionVisibility = true;
        }

        public void SelectOption(EquipmentOption option) {
            bool matchColorSelectionToMaterial = SelectedColorMatchesDefault();
            var PreviousOption = SelectedOption;
            Color? PreviousColor = SelectedColor;
            var PreviousMaterial = SelectedStuff;
            SelectedOption = option;
            SelectedApparel = FindApparelOnPawn(option.ThingDef);
            if (!option.Materials.NullOrEmpty()) {
                if (SelectedStuff == null || !option.Materials.Contains(SelectedStuff)) {
                    SelectedStuff = null;
                    foreach (var m in PreferredMaterials) {
                        if (option.Materials.Contains(m)) {
                            SelectedStuff = m;
                            break;
                        }
                    }
                    if (SelectedStuff == null) {
                        SelectedStuff = option.Materials.First();
                    }
                }
            }
            if (SelectedQuality == null) {
                SelectedQuality = QualityCategory.Normal;
            }
            if (SelectedStyle == null || option.Styles == null || !option.Styles.ContainsAny(s => s == SelectedStyle)) {
                SelectedStyle = null;
            }
            if (SelectedOption?.ThingDef?.HasComp<CompColorable>() ?? false) {
                ColorSelectionEnabled = true;
                UpdateColorSwatchesForSelectedThing();
                if (matchColorSelectionToMaterial) {
                    SelectedColor = DefaultColorForSelectedOption();
                }
            }
            else {
                ColorSelectionEnabled = false;
            }
            CheckSelectionVisibility = true;
        }

        public bool SelectedColorMatchesDefault() {
            if (SelectedOption?.ThingDef == null) {
                return false;
            }
            Color defaultColor = DefaultColorForSelectedOption();
            if (SelectedColor == null) {
                return defaultColor.IndistinguishableFrom(Color.white);
            }
            else {
                return defaultColor.IndistinguishableFrom(SelectedColor.Value);
            }
        }

        public void UpdateColorSwatchesForSelectedThing() {
            CurrentSwatches.Clear();
            if (!(SelectedOption.ThingDef?.HasComp<CompColorable>() ?? false)) {
                return;
            }
            if (SelectedOption.ThingDef?.MadeFromStuff ?? false || SelectedStuff != null) {
                CurrentSwatches.Add(SelectedOption.ThingDef.GetColorForStuff(SelectedStuff));
            }
            else {
                CurrentSwatches.Add(Color.white);
            }
            if (!SelectedOption.ThingDef?.colorGenerator.GetColorList().NullOrEmpty() ?? true) {
                CurrentSwatches.AddRange(SelectedOption.ThingDef.colorGenerator.GetColorList());
            }
            if (CustomizedPawn?.Pawn?.story?.favoriteColor != null) {
                CurrentSwatches.Add(CustomizedPawn.Pawn.story.favoriteColor.Value);
            }
            if (UtilityIdeo.IdeoEnabledForPawn(CustomizedPawn) && CustomizedPawn?.Pawn?.Ideo?.ApparelColor != null) {
                CurrentSwatches.Add(CustomizedPawn.Pawn.Ideo.ApparelColor);
            }
            foreach (var item in CustomizedPawn?.Pawn?.apparel?.WornApparel) {
                CurrentSwatches.Add(UtilityApparel.ColorForApparel(item));
            }
            CurrentSwatches = CurrentSwatches.Distinct().ToList();
        }


        protected void DrawPawn(CustomizedPawn customizedPawn, Rect rect) {
            GUI.DrawTexture(rect, Textures.TexturePortraitBackground);
            GUI.color = ColorPortraitBorder;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            if (customizedPawn == null || customizedPawn.Pawn == null) {
                return;
            }
            Rect pawnRect = rect.OffsetBy(-rect.x, -rect.y + 12);
            RenderTexture pawnTexture = PortraitsCache.Get(customizedPawn.Pawn, rect.size, pawnViewRotation);
            try {
                GUI.BeginClip(rect);
                GUI.DrawTexture(pawnRect, (Texture)pawnTexture);
            }
            catch (Exception e) {
                Logger.Error("Failed to draw pawn", e);
            }
            finally {
                GUI.EndClip();
                GUI.color = Color.white;
            }
        }

        public float DrawRow(EquipmentOption option, float y, float width) {
            float rowHeight = 36;
            float lineHeight = 32;
            float lineWidth = width;
            Rect RowRect = new Rect(0, 0, lineWidth, rowHeight);
            Rect IconRect = new Rect(2, 2, lineHeight, lineHeight);
            Rect LabelRect = new Rect(42f, 3, lineWidth - 42f, lineHeight - 1);

            float top = y;
            Rect rowRect = RowRect.OffsetBy(0, y);
            float detailBoxHeight = 0;
            Rect labelRect = LabelRect.OffsetBy(0, y);
            Rect iconRect = IconRect.OffsetBy(0, y);
            if (option.ThingDef.DrawMatSingle != null && option.ThingDef.DrawMatSingle.mainTexture != null) {
                Widgets.ThingIcon(iconRect, option.ThingDef, option.ThingDef.MadeFromStuff ? option.ThingDef.defaultStuff : null);
            }
            GUI.color = Style.ColorText;
            if (option == SelectedOption || rowRect.Contains(Event.current.mousePosition)) {
                GUI.color = Style.ColorTextPanelHeader;
            }
            Widgets.Label(labelRect, option.ThingDef.LabelCap);
            if (Widgets.ButtonInvisible(rowRect, false) && SelectedOption != option) {
                Apparel apparel = CustomizedPawn.Pawn?.apparel?.WornApparel.FirstOrDefault(a => a.def == option.ThingDef);
                if (apparel == null) {
                    SelectOption(option);
                }
                else {
                    SelectApparel(apparel);
                }
            }
            y += rowHeight + detailBoxHeight;
            return y - top;
        }

        public float DrawSelectedRow(EquipmentOption option, float y, float width) {
            var guiState = UtilityGUIState.Save();
            bool optionDeselected = false;
            try {
                float top = y;
                float boxHeight = MeasureRowForSelectedOption(option, y, width);
                Rect BoxRect = new Rect(0, y, width, boxHeight);
                GUI.color = Color.white;
                Widgets.DrawAtlas(BoxRect, Textures.TextureFieldAtlas);

                float labelHeight = 36;
                Vector2 iconSize = new Vector2(48, 48);
                float insetMargin = iconSize.x + 14;
                Rect RowRect = new Rect(0, 0, width, labelHeight);
                Rect IconRect = new Rect(4, 4, iconSize.x, iconSize.y);
                Rect LabelRect = new Rect(insetMargin, 3, width - insetMargin, labelHeight - 1);

                // Draw Apparel Label
                Rect rowRect = RowRect.OffsetBy(0, y);
                Rect labelRect = LabelRect.OffsetBy(0, y);
                Rect iconRect = IconRect.OffsetBy(0, y);
                if (option.ThingDef.DrawMatSingle != null && option.ThingDef.DrawMatSingle.mainTexture != null) {
                    ThingDef stuff = null;
                    if (option.ThingDef.MadeFromStuff) {
                        stuff = SelectedStuff ?? option.ThingDef.defaultStuff;
                    }
                    var thingDef = option.ThingDef;
                    ThingStyleDef thingStyleDef = null;
                    if (SelectedStyle != null) {
                        thingStyleDef = SelectedStyle.GetStyleForThingDef(thingDef);
                    }
                    if (ColorSelectionEnabled) {
                        Widgets.ThingIcon(iconRect, thingDef, stuff, thingStyleDef, 1f, SelectedColor);
                    }
                    else {
                        Widgets.ThingIcon(iconRect, thingDef, stuff, thingStyleDef);
                    }
                }
                GUI.color = Style.ColorText;
                if (option == SelectedOption || rowRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorTextPanelHeader;
                }
                Widgets.Label(labelRect, option.ThingDef.LabelCap);
                if (Widgets.ButtonInvisible(rowRect, false)) {
                    optionDeselected = true;
                }
                y += labelHeight;

                // Draw Styles Dropdown
                if (SelectedOption.Styles.CountAllowNull() > 0) {
                    Rect materialDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedLabel = "";
                    if (SelectedStyle != null) {
                        selectedLabel = SelectedStyle.LabelCap;
                    }
                    else {
                        selectedLabel = "EdB.PC.Equipment.AvailableEquipment.DefaultStyleOption".Translate();
                    }
                    if (WidgetDropdown.SmallDropdown(materialDropdownRect, selectedLabel, "EdB.PC.Equipment.AvailableEquipment.StyleLabel".Translate())) {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        list.Add(new FloatMenuOption("EdB.PC.Equipment.AvailableEquipment.DefaultStyleOption".Translate(), () => { SelectStyle(null); }, MenuOptionPriority.Default, null, null, 0, null, null));
                        foreach (var style in SelectedOption.Styles) {
                            string label = style.LabelCap;
                            list.Add(new FloatMenuOption(label, () => { SelectStyle(style); }, MenuOptionPriority.Default, null, null, 0, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, null, false));
                    }
                    y += 24;
                }

                // Draw Material Dropdown
                if (SelectedOption.ThingDef.MadeFromStuff && SelectedOption.Materials.CountAllowNull() > 0) {
                    Rect materialDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedLabel = "";
                    if (SelectedStuff != null) {
                        selectedLabel = SelectedStuff?.LabelCap;
                    }
                    if (WidgetDropdown.SmallDropdown(materialDropdownRect, selectedLabel, "EdB.PC.Equipment.AvailableEquipment.MaterialLabel".Translate())) {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (var stuff in SelectedOption.Materials) {
                            string label = stuff.LabelCap;
                            list.Add(new FloatMenuOption(label, () => { SelectMaterial(stuff); }, MenuOptionPriority.Default, null, null, 0, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, null, false));
                    }
                    y += 24;
                }

                // Draw Quality Dropdown
                if (SelectedOption.SupportsQuality) {
                    Rect qualityDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedQualityLabel = "";
                    if (SelectedQuality != null) {
                        selectedQualityLabel = SelectedQuality.Value.GetLabel().CapitalizeFirst();
                    }
                    if (WidgetDropdown.SmallDropdown(qualityDropdownRect, selectedQualityLabel, "EdB.PC.Equipment.AvailableEquipment.QualityLabel".Translate())) {
                        Find.WindowStack.Add(new FloatMenu(QualityFloatMenuOptions, null, false));
                    }
                    y += 24;
                }

                // Draw Hit Points
                if (SelectedOption.ThingDef.useHitPoints) {
                    // Draw the hit points slider
                    Text.Font = GameFont.Tiny;
                    GUI.color = Style.ColorText;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(HitPointsLabelRect.OffsetBy(insetMargin, y), "EdB.PC.Equipment.AvailableEquipment.HitPointsLabel".Translate());
                    Text.Anchor = TextAnchor.MiddleRight;
                    Text.Font = GameFont.Tiny;
                    int calculatedPercent = ((int)(SelectedHitPoints * 100f));
                    if (calculatedPercent < 1) {
                        calculatedPercent = 1;
                    }
                    Widgets.Label(PercentLabelRect.OffsetBy(insetMargin, y), calculatedPercent.ToString() + "%");
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    float hitPoints = GUI.HorizontalSlider(HitPointsSliderRect.OffsetBy(insetMargin, y), SelectedHitPoints, 0.01f, 1f);
                    y += HitPointsLabelRect.height;

                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;

                    y += 8;

                    // Update the hit points based on the slider value
                    SelectHitPoints(hitPoints);
                }

                // Color selector
                if (ColorSelectionEnabled) {
                    Color currentColor = SelectedColor ?? DefaultColorForSelectedOption();
                    y += WidgetColorSelector.DrawSwatches(insetMargin, y, width - 12, 22f, currentColor, CurrentSwatches, (c) => { currentColor = c; }, CustomizedPawn);
                    y += 8;
                    WidgetColorSelector.DrawSelector(new Rect(insetMargin, y, HitPointsSliderRect.xMax - HitPointsLabelRect.xMin, 50f), currentColor, (c) => { currentColor = c; });
                    y += 50f;
                    y += 12;
                    SelectColor(currentColor);
                }

                // Draw Add/Update Button
                Vector2 buttonSize = new Vector2(120, 22);
                Rect addButtonRect = new Rect(insetMargin, y, buttonSize.x, buttonSize.y);
                Text.Font = GameFont.Tiny;
                if (SelectedApparel == null) {
                    if (Widgets.ButtonText(addButtonRect, "EdB.PC.Dialog.ManageApparel.AddApparelButton".Translate(), true, false, true)) {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        AddApparel();
                    }
                }
                y += buttonSize.y + 8;

                if (optionDeselected) {
                    SelectedOption = null;
                }

                return y - top;
            }
            finally {
                guiState.Restore();
            }
        }

        public void SelectHitPoints(float hitPoints) {
            if (SelectedHitPoints == hitPoints) {
                return;
            }
            SelectedHitPoints = hitPoints;
            if (SelectedApparel != null) {
                int hitPointValue = (int)((float)SelectedApparel.MaxHitPoints * SelectedHitPoints);
                if (SelectedApparel.HitPoints != hitPointValue) {
                    SelectedApparel.HitPoints = hitPointValue;
                }
            }
        }

        public void SelectColor(Color color) {
            if (SelectedColor == color) {
                return;
            }
            SelectedColor = color;
            if (SelectedApparel != null) {
                Logger.Debug("SelectColor for SelectedApparel");
                Color currentColor = SelectedApparel.GetColor();
                if (currentColor != color) {

                    Color? newCompColor = color;
                    // Don't bother coloring something made from stuff if the selected color is the same color as the stuff
                    if (SelectedApparel.def.MadeFromStuff && SelectedApparel.Stuff != null) {
                        Color stuffColor = SelectedApparel.def.GetColorForStuff(SelectedApparel.Stuff);
                        if (stuffColor.IndistinguishableFrom(color)) {
                            newCompColor = null;
                        }
                    }
                    if (newCompColor != null) {
                        SelectedApparel.SetColor(color);
                    }
                    else {
                        CompColorable comp = SelectedApparel.TryGetComp<CompColorable>();
                        if (comp != null) {
                            comp.Disable();
                        }
                        else {
                            SelectedApparel.SetColor(color);
                        }
                    }
                    UtilityPawns.ClearPawnGraphicsCache(CustomizedPawn?.Pawn);
                }
            }
        }

        public void SelectMaterial(ThingDef stuff) {
            ThingDef previousStuff = SelectedStuff;
            Logger.Debug("SelectedColor = " + SelectedColor);
            Color? colorForPreviousStuff = SelectedOption.ThingDef.GetColorForStuff(previousStuff);
            //Logger.Debug(previousStuff?.defName + ", colorForPreviousStuff = " + colorForPreviousStuff.Value);
            Color? colorForCurrentStuff = SelectedOption.ThingDef.GetColorForStuff(stuff);
            //Logger.Debug(stuff?.defName + ", colorForCurrentStuff = " + colorForCurrentStuff.Value);
            SelectedStuff = stuff;
            UpdateColorSwatchesForSelectedThing();
            if (previousStuff != null && stuff != null && SelectedColor != null) {
                if (UtilityColor.AlmostEqual(SelectedColor.Value, colorForPreviousStuff.Value)) {
                    SelectedColor = colorForCurrentStuff.Value;
                }
            }
            if (SelectedApparel != null) {
                if (SelectedApparel.Stuff != stuff) {
                    AddApparel();
                }
            }
        }

        public void SelectStyle(StyleCategoryDef def) {
            SelectedStyle = def;
            if (SelectedApparel != null) {
                if (SelectedApparel.StyleDef?.Category != SelectedStyle) {
                    AddApparel();
                }
            }
        }

        public void SelectQuality(QualityCategory? quality) {
            SelectedQuality = quality;
            if (SelectedApparel != null) {
                QualityCategory newQuality = quality ?? QualityCategory.Normal;
                if (SelectedApparel.TryGetQuality(out var currentQuality)) {
                    if (currentQuality != newQuality) {
                        AddApparel();
                    }
                }
            }
        }

        public Color DefaultColorForSelectedOption() {
            if (SelectedOption.ThingDef?.MadeFromStuff ?? false && SelectedStuff != null) {
                return SelectedOption.ThingDef.GetColorForStuff(SelectedStuff);
            }
            return SelectedOption.ThingDef?.colorGenerator?.GetColorList().FirstOrDefault() ?? Color.white;
        }

        public void AddApparel() {
            ThingDef stuffDef = SelectedStuff;
            if (!SelectedOption.ThingDef.MadeFromStuff) {
                stuffDef = null;
            }
            QualityCategory? quality = SelectedQuality;
            if (!SelectedOption.SupportsQuality) {
                quality = null;
            }
            CustomizationsApparel c = new CustomizationsApparel() {
                ThingDef = SelectedOption.ThingDef,
                StuffDef = stuffDef,
                StyleCategoryDef = SelectedStyle,
                Color = SelectedColor,
                Quality = quality,
                HitPoints = SelectedHitPoints
            };
            if (ColorSelectionEnabled) {
                c.Color = SelectedColor;
            }
            //Logger.Debug("Added apparel with hitpoints = " + SelectedHitPoints);
            ApparelAdded?.Invoke(c);
            Apparel a = FindApparelOnPawn(c.ThingDef);
            if (a != null) {
                SelectApparel(a);
            }
            else {
                ScrollToSelectedOption(SelectedOption);
            }
        }

        public Apparel FindApparelOnPawn(ThingDef thingDef) {
            if (thingDef == null) {
                return null;
            }
            return CustomizedPawn.Pawn.apparel.WornApparel.FirstOrDefault(a => a.def == thingDef);
        }

        public float MeasureRowForSelectedOption(EquipmentOption option, float y, float width) {
            float rowHeight = 36;
            float buttonHeight = 22;
            float result = rowHeight + buttonHeight;

            if (SelectedOption.ThingDef.MadeFromStuff && SelectedOption.Materials.CountAllowNull() > 0) {
                result += 24;
            }
            if (SelectedOption.SupportsQuality) {
                result += 24;
            }
            if (SelectedOption.Styles.CountAllowNull() > 0) {
                result += 24;
            }
            if (SelectedOption.ThingDef.useHitPoints) {
                result += HitPointsLabelRect.height + 8;
            }
            if (ColorSelectionEnabled) {
                result += WidgetColorSelector.Measure(width, CurrentSwatches) + 12;
            }
            result += 8;

            return result;
        }

    }

}
