using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelEquipmentAvailable : PanelBase {
        public delegate void AddEquipmentHandler(CustomizedEquipment equipment);

        public event AddEquipmentHandler EquipmentAdded;

        public class ViewEquipmentList {
            public WidgetTable<EquipmentOption> Table;
            public List<EquipmentOption> List;
        }
        public static readonly string ColumnNameInfo = "Info";
        public static readonly string ColumnNameIcon = "Icon";
        public static readonly string ColumnNameName = "Name";
        public static readonly string ColumnNameCost = "Cost";

        public StatDef marketValueStatDef = null;

        protected Rect RectDropdownTypes;
        protected Rect RectDropdownMaterials;
        protected Rect RectDropdownQuality;
        protected Rect RectListHeader;
        protected Rect RectListBody;
        protected Rect RectTable;
        protected Rect RectInfoButton;
        protected Rect RectColumnHeaderName;
        protected Rect RectColumnHeaderCost;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectRow;
        protected Rect RectItem;
        protected Vector2 AddButtonSize;
        protected static Vector2 SizeTextureSortIndicator = new Vector2(8, 4);
        protected EquipmentType selectedType = null;
        protected WidgetScrollViewVertical scrollView = new WidgetScrollViewVertical();
        private HashSet<ThingDef> stuffFilterSet = new HashSet<ThingDef>();
        private bool loading = true;
        private WidgetScrollViewVertical ScrollView = new WidgetScrollViewVertical();
        private EquipmentOption SelectedOption = null;
        private CustomizedEquipment SelectedValues = new CustomizedEquipment();
        private EquipmentOption LastDrawnEquipmentOption = null;

        private ThingCategoryDef FilterThingCategory = null;
        private string FilterModName = null;
        private string FilterSearchTerm = null;
        private List<EquipmentOption> FilteredOptions = new List<EquipmentOption>();
        private IEnumerator<EquipmentOption> FilteringEnumerator = null;

        private List<FloatMenuOption> QualityFloatMenuOptions = new List<FloatMenuOption>();
        private List<FloatMenuOption> GenderFloatMenuOptions = new List<FloatMenuOption>();
        private List<FloatMenuOption> SpawnTypeFloatMenuOptions = new List<FloatMenuOption>();
        private List<FloatMenuOption> ThingCategoryFloatMenuOptions;
        private List<FloatMenuOption> ModNameFloatMenuOptions;
        private QuickSearchWidget SearchWidget = new QuickSearchWidget();
        private LinkedList<ThingDef> PreferredMaterials = new LinkedList<ThingDef>();

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ProviderEquipment ProviderEquipment { get; set; }
        public CostCalculator CostCalculator { get; set; }
        public double CachedCost { get; set; }

        public PanelEquipmentAvailable() {
            var synthread = DefDatabase<ThingDef>.GetNamedSilentFail("Synthread");
            var wood = DefDatabase<ThingDef>.GetNamedSilentFail("WoodLog");
            var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
            var plasteel = DefDatabase<ThingDef>.GetNamedSilentFail("Plasteel");
            foreach (var m in new ThingDef[] { synthread, steel, wood, plasteel }) {
                if (m != null) {
                    PreferredMaterials.AddLast(m);
                }
            }
        }
        public void PostConstruct() {
            QualityFloatMenuOptions.Add(new FloatMenuOption("EdB.PC.Equipment.DefaultOption".Translate(), () => { SelectedValues.Quality = null; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
            foreach (var quality in Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>()) {
                QualityFloatMenuOptions.Add(new FloatMenuOption(quality.GetLabel().CapitalizeFirst(), () => { SelectedValues.Quality = quality; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
            }

            SpawnTypeFloatMenuOptions.Add(new FloatMenuOption("EdB.PC.Equipment.AvailableEquipment.DefaultSpawnTypeOption".Translate(), () => { SelectedValues.SpawnType = null; }, MenuOptionPriority.Default, null, null, 0, null, null));
            SpawnTypeFloatMenuOptions.Add(new FloatMenuOption(UtilityEquipmentSpawnType.LabelForSpawnTypeHeader(EquipmentSpawnType.SpawnsNear), () => { SelectedValues.SpawnType = EquipmentSpawnType.SpawnsNear; }, MenuOptionPriority.Default, null, null, 0, null, null));
            SpawnTypeFloatMenuOptions.Add(new FloatMenuOption(UtilityEquipmentSpawnType.LabelForSpawnTypeHeader(EquipmentSpawnType.SpawnsWith), () => { SelectedValues.SpawnType = EquipmentSpawnType.SpawnsWith; }, MenuOptionPriority.Default, null, null, 0, null, null));

            GenderFloatMenuOptions.Add(new FloatMenuOption("Random".Translate(), () => { SelectedValues.Quality = null; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
            GenderFloatMenuOptions.Add(new FloatMenuOption(Gender.Female.GetLabel().CapitalizeFirst(), () => { SelectedValues.Gender = Gender.Female; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
            GenderFloatMenuOptions.Add(new FloatMenuOption(Gender.Male.GetLabel().CapitalizeFirst(), () => { SelectedValues.Gender = Gender.Male; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
        }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            if (marketValueStatDef ==  null) {
                marketValueStatDef = StatDefOf.MarketValue;
            }

            Vector2 padding = new Vector2(12, 12);
            
            RectDropdownTypes = new Rect(padding.x, padding.y, 140, 28);
            RectDropdownMaterials = new Rect(RectDropdownTypes.xMax + 8, RectDropdownTypes.yMin, 160, 28);

            Vector2 sizeInfoButton = new Vector2(24, 24);

            Vector2 buttonLabelSize = Text.CalcSize("EdB.PC.Panel.AvailableEquipment.AddButtonLabel".Translate());
            AddButtonSize = new Vector2(buttonLabelSize.x + 96, buttonLabelSize.y + 4);

            Vector2 listSize = new Vector2();
            listSize.x = rect.width - padding.x * 2;
            listSize.y = rect.height - RectDropdownTypes.yMax - (padding.y * 2);
            float listHeaderHeight = 20;
            float listBodyHeight = listSize.y - listHeaderHeight;

            RectTable = new Rect(padding.x, padding.y + RectDropdownTypes.yMax, listSize.x, listSize.y);

            RectListHeader = new Rect(padding.x, RectDropdownTypes.yMax + 4, listSize.x, listHeaderHeight);
            RectListBody = new Rect(padding.x, RectListHeader.yMax, listSize.x, listBodyHeight);

            RectColumnHeaderName = new Rect(RectListHeader.x + 64, RectListHeader.y, 240, RectListHeader.height);
            RectColumnHeaderCost = new Rect(RectListHeader.xMax - 100, RectListHeader.y, 100, RectListHeader.height);

            RectScrollFrame = RectListBody;
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            RectRow = new Rect(0, 0, RectScrollView.width, 42);
            RectItem = new Rect(10, 2, 38, 38);
        }

        protected bool FilterTick() {
            int itemsToProcessPerTick = 500;
            for (int i = 0; i < itemsToProcessPerTick; i++) {
                if (!FilteringEnumerator.MoveNext()) {
                    FilteringEnumerator = null;
                    return true;
                }
                EquipmentOption option = FilteringEnumerator.Current;
                if (option != null) {
                    FilteredOptions.Add(option);
                }
            }
            return false;
        }

        protected void ApplyCurrentFilters() {
            FilteredOptions.Clear();
            IEnumerable<EquipmentOption> options;
            if (FilterThingCategory != null) {
                options = ProviderEquipment.EquipmentDatabase.EquipmentOptionsByCategory(FilterThingCategory);
            }
            else {
                options = ProviderEquipment.Equipment;
            }
            if (FilterModName != null) {
                options = ProviderEquipment.EquipmentDatabase.ApplyModNameFilter(options, FilterModName);
            }
            if (FilterSearchTerm != null) {
                options = ProviderEquipment.EquipmentDatabase.ApplySearchTermFilter(options, FilterSearchTerm);
            }
            FilteringEnumerator = options.GetEnumerator();
        }

        protected void PrepareThingCategoryFilterOptions() {
            ThingCategoryFloatMenuOptions = new List<FloatMenuOption>();
            ThingCategoryFloatMenuOptions.Add(new FloatMenuOption("EdB.PC.Equipment.AvailableEquipment.AllCategoriesOption".Translate(), () => {
                FilterThingCategory = null;
                ApplyCurrentFilters();
            }, MenuOptionPriority.Default, null, null, 0, null, null));
            foreach (var thingCategory in ProviderEquipment.EquipmentDatabase.ThingCategories) {
                if (thingCategory.defName == "Root") {
                    continue;
                }
                ThingCategoryFloatMenuOptions.Add(new FloatMenuOption(LabelForThingCategory(thingCategory), () => {
                    FilterThingCategory = thingCategory;
                    ApplyCurrentFilters();
                }, MenuOptionPriority.Default, null, null, 0, null, null));
                ThingCategoryFloatMenuOptions = ThingCategoryFloatMenuOptions.OrderBy(m => m.Label).ToList();
            }
        }
        protected void PrepareModNameFilterOptions() {
            ModNameFloatMenuOptions = new List<FloatMenuOption>();
            ModNameFloatMenuOptions.Add(new FloatMenuOption("EdB.PC.Equipment.AvailableEquipment.AllModsOption".Translate(), () => {
                FilterModName = null;
                ApplyCurrentFilters();
            }, MenuOptionPriority.Default, null, null, 0, null, null));
            foreach (var modName in ProviderEquipment.EquipmentDatabase.ModNames) {
                ModNameFloatMenuOptions.Add(new FloatMenuOption(modName, () => {
                    FilterModName = modName;
                    ApplyCurrentFilters();
                }, MenuOptionPriority.Default, null, null, 0, null, null));
                ModNameFloatMenuOptions = ModNameFloatMenuOptions.OrderBy(m => m.Label).ToList();
            }
        }

        protected string LabelForThingCategory(ThingCategoryDef def) {
            string result = def.LabelCap;
            foreach (var parent in def.Parents) {
                if (parent.defName == "Root") {
                    continue;
                }
                result = parent.LabelCap + " > " + result;
            }
            return result;
        }
        protected string ShortLabelForThingCategory(ThingCategoryDef def) {
            return def.LabelCap;
        }

        protected override void DrawPanelContent() {
            base.DrawPanelContent();

            if (loading) {
                if (ProviderEquipment != null && ProviderEquipment.DatabaseReady) {
                    loading = false;
                    Resize(this.PanelRect);
                    ApplyCurrentFilters();
                    return;
                }
                else {
                    DrawLoadingProgress();
                    return;
                }
            }

            if (FilteringEnumerator != null) {
                if (!FilterTick()) {
                    return;
                }
            }

            var savedGUIState = UtilityGUIState.Save();

            if (ThingCategoryFloatMenuOptions == null) {
                PrepareThingCategoryFilterOptions();
            }
            if (ModNameFloatMenuOptions == null) {
                PrepareModNameFilterOptions();
            }
            float cursorX = RectDropdownTypes.xMin;
            if (!ThingCategoryFloatMenuOptions.Empty()) {
                string dropdownLabel;
                if (FilterThingCategory == null) {
                    dropdownLabel = "EdB.PC.Equipment.AvailableEquipment.AllCategoriesOption".Translate();
                }
                else {
                    dropdownLabel = ShortLabelForThingCategory(FilterThingCategory);
                }
                Text.Font = GameFont.Tiny;
                Vector2 labelSize = Text.CalcSize(dropdownLabel);
                cursorX += labelSize.x + 36;
                Text.Font = GameFont.Small;
                if (WidgetDropdown.SmallDropdown(RectDropdownTypes, dropdownLabel)) {
                    Find.WindowStack.Add(new FloatMenu(ThingCategoryFloatMenuOptions, null, false));
                }
            }
            if (!ModNameFloatMenuOptions.Empty()) {
                string dropdownLabel;
                if (FilterModName == null) {
                    dropdownLabel = "EdB.PC.Equipment.AvailableEquipment.AllModsOption".Translate();
                }
                else {
                    dropdownLabel = FilterModName;
                }
                if (WidgetDropdown.SmallDropdown(new Rect(cursorX, RectDropdownTypes.y, RectDropdownTypes.width, RectDropdownTypes.height), dropdownLabel)) {
                    Find.WindowStack.Add(new FloatMenu(ModNameFloatMenuOptions, null, false));
                }
            }

            SearchWidget.OnGUI(new Rect(BodyRect.xMax - 220, BodyRect.y + 10, 210, 24), () => { FilterSearchTerm = SearchWidget.filter.Text; ApplyCurrentFilters(); }, () => { FilterSearchTerm = null; ApplyCurrentFilters(); });

            float y = 0;
            ScrollView.Begin(RectTable);
            try {
                float width = ScrollView.CurrentViewWidth;
                int index = 0;
                foreach (var equipment in FilteredOptions) {
                    if (equipment == SelectedOption) {
                        float topOfBox = y;
                        float scrollBoxTopPosition = ScrollView.Position.y;
                        y += DrawSelectedRow(y, width, index, equipment);

                        if (LastDrawnEquipmentOption != equipment && y > scrollBoxTopPosition + ScrollView.ViewHeight) {
                            float amount = y - (scrollBoxTopPosition + ScrollView.ViewHeight);
                            ScrollView.ScrollTo(scrollBoxTopPosition + amount);
                        }
                        LastDrawnEquipmentOption = equipment;
                    }
                    else {
                        y += DrawRow(y, width, index, equipment);
                    }

                    y += 6f;
                    index++;
                }
            }
            finally {
                ScrollView.End(y);
                savedGUIState.Restore();
            }
        }

        public float DrawRow(float y, float width, int index, EquipmentOption equipment) {
            float top = y;
            Rect rowRect = new Rect(0, y, width, 36);

            var guiState = UtilityGUIState.Save();
            try {
                GUI.color = Color.white;
                if (index % 2 == 0) {
                    GUI.color = Style.ColorTableRow1;
                }
                else {
                    GUI.color = Style.ColorTableRow2;
                }
                GUI.DrawTexture(rowRect, BaseContent.WhiteTex);

                DrawIconForEquipmentOption(equipment, y);

                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Style.ColorText;
                Text.WordWrap = false;
                Text.Font = GameFont.Small;
                if (rowRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorTextPanelHeader;
                }
                float labelLeftMargin = 48f;
                Rect labelRect = new Rect(labelLeftMargin, y, rowRect.width - labelLeftMargin, rowRect.height);
                string text = equipment.Label;
                if (FilterSearchTerm != null) {
                    text = HighlightSearchTermInLabel(text);
                }
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Text.WordWrap = true;
            }
            finally {
                guiState.Restore();
            }

            if (Widgets.ButtonInvisible(rowRect, false)) {
                SelectOption(equipment);
            }

            //Widgets.InfoCardButton(rect.width - 24f, y, thing);

            y += rowRect.height;
            return y - top;
        }

        public void SelectOption(EquipmentOption option) {
            SelectedOption = option;
            ThingDef stuff = null;
            if (option.ThingDef?.MadeFromStuff ?? false) {
                Logger.Debug("  Option is made from stuff");
                if (SelectedValues.StuffDef == null) {
                    Logger.Debug("  No previously selected stuff");
                }
                else {
                    Logger.Debug("  Previously selected stuff: " + SelectedValues.StuffDef?.defName);
                    if (option.Materials.Contains(SelectedValues.StuffDef)) {
                        Logger.Debug("  Option materials contains previously selected stuff");
                    }
                    else {
                        Logger.Debug("  Option materials does not contain previously selected stuff");
                    }
                }
                if (option.Materials.Contains(SelectedValues.StuffDef)) {
                    stuff = SelectedValues.StuffDef;
                }
                if (stuff == null) {
                    stuff = option.ThingDef.defaultStuff;
                    Logger.Debug("  Default stuff: " + stuff?.defName);
                    if (stuff == null) {
                        foreach (var m in PreferredMaterials) {
                            if (option.Materials.Contains(m)) {
                                stuff = m;
                                break;
                            }
                        }
                        Logger.Debug("  After checking default materials: " + stuff?.defName);
                    }
                    if (stuff == null) {
                        stuff = option.Materials.FirstOrDefault();
                        Logger.Debug("  First material: " + stuff?.defName);
                    }
                }
            }
            SelectedValues = new CustomizedEquipment() {
                EquipmentOption = option,
                StuffDef = stuff,
                Quality = null,
                SpawnType = null,
                Count = 1,
                Gender = null
            };
            RecalculateSelectedOptionCost();
        }

        public void RecalculateSelectedOptionCost() {
            CachedCost = CostCalculator.CalculateEquipmentCost(SelectedValues);
        }

        public void DrawIconForEquipmentOption(EquipmentOption equipment, float y) {
            if (equipment.ThingDef != null) {
                if (equipment.ThingDef.DrawMatSingle != null && equipment.ThingDef.DrawMatSingle.mainTexture != null) {
                    Rect iconRect = new Rect(8f, y + 2, 32f, 32f);
                    Widgets.ThingIcon(iconRect, equipment.ThingDef, equipment.ThingDef.defaultStuff);
                }
            }
            else if (equipment.RandomAnimal) {
                GUI.color = Style.ColorTextSecondary;
                Rect iconRect = new Rect(12f, y + 6, 24f, 24f);
                GUI.DrawTexture(iconRect, Textures.TextureButtonRandom);
                GUI.color = Color.white;
            }
        }

        public string HighlightSearchTermInLabel(string label) {
            int index = label.IndexOf(FilterSearchTerm, StringComparison.CurrentCultureIgnoreCase);
            if (index != -1) {
                string substring = label.Substring(index, FilterSearchTerm.Length);
                return label.ReplaceFirst(substring, substring.Colorize(Color.white));
            }
            else {
                return label;
            }
        }

        public float DrawSelectedRow(float y, float width, int index, EquipmentOption equipment) {
            float top = y;
            float dropdownHeight = 24;
            float insetMargin = 47;
            Rect rowRect = new Rect(0, y, width, 36);
            Rect backgroundRect = new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height + AddButtonSize.y + 8);
            if (equipment.ThingDef?.MadeFromStuff ?? false) {
                backgroundRect = backgroundRect.GrowBy(0, dropdownHeight);
            }
            if (equipment.SupportsQuality) {
                backgroundRect = backgroundRect.GrowBy(0, dropdownHeight);
            }
            backgroundRect = backgroundRect.GrowBy(0, dropdownHeight); // Add space for the cost label
            if (SelectedOption.DefaultSpawnType == EquipmentSpawnType.SpawnsWith || SelectedOption.DefaultSpawnType == EquipmentSpawnType.SpawnsNear) {
                backgroundRect = backgroundRect.GrowBy(0, dropdownHeight); // Add space for spawn type dropdown
            }
            if (SelectedOption.Animal && (SelectedOption?.ThingDef?.race?.hasGenders ?? false)) {
                backgroundRect = backgroundRect.GrowBy(0, dropdownHeight); // Add space for gender dropdown
            }

                var guiState = UtilityGUIState.Save();
            try {
                GUI.color = Style.ColorPanelBackground;
                GUI.DrawTexture(backgroundRect, BaseContent.WhiteTex);

                DrawIconForEquipmentOption(equipment, y);

                // Draw label
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;
                Text.WordWrap = false;
                Text.Font = GameFont.Small;
                float labelLeftMargin = 48f;
                Rect labelRect = new Rect(labelLeftMargin, y, rowRect.width - labelLeftMargin, rowRect.height);
                string text = equipment.Label;
                if (FilterSearchTerm != null) {
                    text = HighlightSearchTermInLabel(text);
                }
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Text.WordWrap = true;
                y += rowRect.height;

                //Widgets.InfoCardButton(rect.width - 24f, y, thing);

                Text.Font = GameFont.Tiny;
                GUI.color = Style.ColorText;
                Rect costLabelRect = new Rect(insetMargin, y, width, 18);
                string costLabel = "EdB.PC.Equipment.AvailableEquipment.CostLabelAndValue".Translate(Math.Round(CachedCost, 2));
                Widgets.Label(costLabelRect, costLabel);
                y += 24;

                // Draw Spawn type
                if (SelectedOption.DefaultSpawnType == EquipmentSpawnType.SpawnsWith || SelectedOption.DefaultSpawnType == EquipmentSpawnType.SpawnsNear) {
                    GUI.color = Color.white;
                    Rect spawnTypeDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedSpawnTypeLabel = SelectedValues.SpawnType != null ? UtilityEquipmentSpawnType.LabelForSpawnTypeHeader(SelectedValues.SpawnType.Value) : "EdB.PC.Equipment.DefaultOption".Translate().ToString();
                    if (WidgetDropdown.SmallDropdown(spawnTypeDropdownRect, selectedSpawnTypeLabel, "EdB.PC.Equipment.AvailableEquipment.SpawnTypeLabel".Translate())) {
                        Find.WindowStack.Add(new FloatMenu(SpawnTypeFloatMenuOptions, null, false));
                    }
                    y += 24;
                }

                // Draw Material Dropdown
                if (equipment.ThingDef?.MadeFromStuff ?? false && !equipment.Materials.NullOrEmpty()) {
                    Rect materialDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedLabel = SelectedValues?.StuffDef?.LabelCap;
                    if (WidgetDropdown.SmallDropdown(materialDropdownRect, selectedLabel, "EdB.PC.Equipment.AvailableEquipment.MaterialLabel".Translate())) {
                        Logger.Debug(string.Format("Default stuff for {0}: {1}", equipment.ThingDef?.defName, equipment.ThingDef?.defaultStuff?.defName));
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (var stuff in SelectedOption.Materials) {
                            string label = stuff.LabelCap;
                            list.Add(new FloatMenuOption(label, () => { SelectedValues.StuffDef = stuff; RecalculateSelectedOptionCost(); }, MenuOptionPriority.Default, null, null, 0, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, null, false));
                    }
                    y += 24;
                }

                // Draw Quality Dropdown
                if (SelectedOption.SupportsQuality) {
                    Rect qualityDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedQualityLabel = "EdB.PC.Equipment.DefaultOption".Translate();
                    if (SelectedValues.Quality != null) {
                        selectedQualityLabel = SelectedValues.Quality.Value.GetLabel().CapitalizeFirst();
                    }
                    if (WidgetDropdown.SmallDropdown(qualityDropdownRect, selectedQualityLabel, "EdB.PC.Equipment.AvailableEquipment.QualityLabel".Translate())) {
                        Find.WindowStack.Add(new FloatMenu(QualityFloatMenuOptions, null, false));
                    }
                    y += 24;
                }

                // Gender dropdown
                if (SelectedOption.Animal && (SelectedOption?.ThingDef?.race?.hasGenders ?? false)) {
                    Rect genderDropdownRect = new Rect(insetMargin, y, width, 18);
                    string selectedGenderLabel = "Random".Translate();
                    if (SelectedValues.Gender != null) {
                        selectedGenderLabel = SelectedValues.Gender.Value.GetLabel().CapitalizeFirst();
                    }
                    if (WidgetDropdown.SmallDropdown(genderDropdownRect, selectedGenderLabel, "EdB.PC.Equipment.AvailableEquipment.GenderLabel".Translate())) {
                        Find.WindowStack.Add(new FloatMenu(GenderFloatMenuOptions, null, false));
                    }
                    y += 24;
                }

                // Add button
                Rect addButtonRect = new Rect(insetMargin, y + 4, AddButtonSize.x, AddButtonSize.y);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.white;
                if (Widgets.ButtonText(addButtonRect, "EdB.PC.Panel.AvailableEquipment.AddButtonLabel".Translate(), true, false, true)) {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    EquipmentAdded?.Invoke(SelectedValues.CreateCopy());
                }
                Text.Font = GameFont.Small;
                y += addButtonRect.height + 8;

                // Info card button
                if (SelectedOption?.ThingDef != null) {
                    Rect infoButtonRect = new Rect(width - 32f, top + 12f, 24, 24);
                    if (SelectedValues.StuffDef != null) {
                        WidgetInfoButton.Draw(infoButtonRect, SelectedOption.ThingDef, SelectedValues.StuffDef);
                    }
                    else {
                        WidgetInfoButton.Draw(infoButtonRect, SelectedOption.ThingDef);
                    }
                }

                return y - top;
            }
            finally {
                guiState.Restore();
            }


        }

        protected void UpdateAvailableMaterials() {
            //ViewEquipmentList view = CurrentView;
            //stuffFilterSet.Clear();
            // TODO
            //foreach (var item in view.List) {
            //    if (item.stuffDef != null) {
            //        stuffFilterSet.Add(item.stuffDef);
            //    }
            //}
            //if (filterStuff != null && !stuffFilterSet.Contains(filterStuff)) {
            //    filterStuff = null;
            //}
        }

        protected readonly Vector2 ProgressBarSize = new Vector2(250, 18);
        protected void DrawLoadingProgress() {
            Rect progressBarRect = new Rect(PanelRect.HalfWidth() - ProgressBarSize.x * 0.5f, PanelRect.HalfHeight() - ProgressBarSize.y * 0.5f,
                ProgressBarSize.x, ProgressBarSize.y);
            var progress = ProviderEquipment.LoadingProgress;
            GUI.color = Color.gray;
            Widgets.DrawBox(progressBarRect);
            if (progress.defCount > 0) {
                int totalCount = progress.defCount * 2;
                int processed = progress.stuffProcessed + progress.thingsProcessed;
                float percent = (float)processed / (float)totalCount;
                float barWidth = progressBarRect.width * percent;
                Widgets.DrawRectFast(new Rect(progressBarRect.x, progressBarRect.y, barWidth, progressBarRect.height), Color.green);
            }
            GUI.color = Style.ColorText;
            Text.Font = GameFont.Tiny;
            string label = "EdB.PC.Equipment.LoadingProgress.Initializing".Translate();
            if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingStuff) {
                label = "EdB.PC.Equipment.LoadingProgress.StuffDefs".Translate();
            }
            else if (progress.phase == EquipmentDatabase.LoadingPhase.ProcessingThings) {
                label = "EdB.PC.Equipment.LoadingProgress.ThingDefs".Translate();
            }
            else if (progress.phase == EquipmentDatabase.LoadingPhase.Loaded) {
                label = "EdB.PC.Equipment.LoadingProgress.Finished".Translate();
            }
            Widgets.Label(new Rect(progressBarRect.x, progressBarRect.yMax + 2, progressBarRect.width, 20), label) ;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        //protected void DrawFilters(ViewEquipmentList view) {
        //    string label = selectedType.Label.Translate();
        //    if (WidgetDropdown.Button(RectDropdownTypes, label, true, false, true)) {
        //        List<FloatMenuOption> list = new List<FloatMenuOption>();
        //        foreach (var type in ProviderEquipment.Types) {
        //            EquipmentType localType = type;
        //            list.Add(new FloatMenuOption(type.Label.Translate(), () => {
        //                this.selectedType = localType;
        //                this.UpdateAvailableMaterials();
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //        }
        //        Find.WindowStack.Add(new FloatMenu(list, null, false));
        //    }

        //    if (StuffFilterVisible) {
        //        string stuffLabel = null;
        //        if (!filterMadeFromStuff) {
        //            stuffLabel = "EdB.PC.Panel.AvailableEquipment.Materials.None".Translate();
        //        }
        //        else if (filterStuff == null) {
        //            stuffLabel = "EdB.PC.Panel.AvailableEquipment.Materials.All".Translate();
        //        }
        //        else {
        //            stuffLabel = filterStuff.LabelCap;
        //        }
        //        if (WidgetDropdown.Button(RectDropdownMaterials, stuffLabel, true, false, true)) {
        //            List<FloatMenuOption> stuffFilterOptions = new List<FloatMenuOption>();
        //            stuffFilterOptions.Add(new FloatMenuOption("EdB.PC.Panel.AvailableEquipment.Materials.All".Translate(), () => {
        //                UpdateStuffFilter(true, null);
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            stuffFilterOptions.Add(new FloatMenuOption("EdB.PC.Panel.AvailableEquipment.Materials.None".Translate(), () => {
        //                UpdateStuffFilter(false, null);
        //            }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            foreach (var item in stuffFilterSet.OrderBy((ThingDef def) => { return def.LabelCap.Resolve(); })) {
        //                stuffFilterOptions.Add(new FloatMenuOption(item.LabelCap, () => {
        //                    UpdateStuffFilter(true, item);
        //                }, MenuOptionPriority.Default, null, null, 0, null, null));
        //            }
        //            Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null, false));
        //        }
        //    }
        //}

        //protected void UpdateStuffFilter(bool madeFromStuff, ThingDef stuff) {
        //    this.filterMadeFromStuff = madeFromStuff;
        //    this.filterStuff = stuff;
        //    ViewEquipmentList view = CurrentView;
        //    IEnumerable<EquipmentOption> entries = FilterEquipmentList(view);
        //    if (!entries.Any((EquipmentOption e) => {
        //        return e == view.Table.Selected;
        //    })) {
        //        view.Table.Selected = entries.FirstOrDefault();
        //    }
        //}

        //protected bool StuffFilterVisible {
        //    get {
        //        return stuffFilterSet.Count > 0;
        //    }
        //}

        //protected void DrawEquipmentList(ViewEquipmentList view) {
        //    SortField sortField = ViewState.EquipmentSortField;
        //    //view.Table.Draw(view.List);
        //    view.Table.Draw(FilterEquipmentList(view));
        //    view.Table.BackgroundColor = Style.ColorPanelBackgroundDeep;
        //}

        protected IEnumerable<EquipmentOption> FilterEquipmentList(ViewEquipmentList view) {
            // TODO:
            //if (StuffFilterVisible) {
            //    return view.List.FindAll((EquipmentOption entry) => {
            //        if (filterMadeFromStuff) {
            //            return filterStuff == null || filterStuff == entry.stuffDef;
            //        }
            //        else {
            //            return !entry.def.MadeFromStuff;
            //        }
            //    });
            //}
            return view.List;
        }

        protected void DoSort(WidgetTable<EquipmentOption>.Column column, int direction) {
            //var view = equipmentViews[selectedType];
            //if (view == null) {
            //    return;
            //}
            //if (column != null) {
            //    if (column.Name == ColumnNameName) {
            //        SortByName(view, direction);
            //        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            //    }
            //    else if (column.Name == ColumnNameCost) {
            //        SortByCost(view, direction);
            //        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            //    }
            //}
        }

        protected void SortByName(ViewEquipmentList view, int direction) {
            // TODO: Remove sorting?
            //if (direction == 1) {
            //    view.List.SortBy((EquipmentOption arg) => { return arg.Label; });
            //}
            //else {
            //    view.List.SortByDescending((EquipmentOption arg) => { return arg.Label; });
            //}
        }
        protected void SortByCost(ViewEquipmentList view, int direction) {
            // TODO: Remove sorting?
            //view.List.Sort((EquipmentOption x, EquipmentOption y) => {
            //    if (direction == 1) {
            //        int result = x.cost.CompareTo(y.cost);
            //        if (result != 0) {
            //            return result;
            //        }
            //    }
            //    else {
            //        int result = y.cost.CompareTo(x.cost);
            //        if (result != 0) {
            //            return result;
            //        }
            //    }
            //    return string.Compare(x.Label, y.Label);
            //});
        }
    }
}
