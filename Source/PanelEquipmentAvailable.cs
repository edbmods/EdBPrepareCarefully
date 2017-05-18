using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelEquipmentAvailable : PanelBase {
        public delegate void AddEquipmentHandler(EquipmentRecord entry);

        public event AddEquipmentHandler EquipmentAdded;

        public class ViewEquipmentList {
            public WidgetTable<EquipmentRecord> Table;
            public List<EquipmentRecord> List;
        }
        public static readonly string ColumnNameIcon = "Icon";
        public static readonly string ColumnNameName = "Name";
        public static readonly string ColumnNameCost = "Cost";

        protected Rect RectDropdownTypes;
        protected Rect RectDropdownMaterials;
        protected Rect RectDropdownQuality;
        protected Rect RectListHeader;
        protected Rect RectListBody;
        protected Rect RectColumnHeaderName;
        protected Rect RectColumnHeaderCost;
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;
        protected Rect RectRow;
        protected Rect RectItem;
        protected Rect RectAddButton;
        protected static Vector2 SizeTextureSortIndicator = new Vector2(8, 4);
        protected ProviderEquipmentTypes providerEquipment;
        protected EquipmentType selectedType = null;
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected Dictionary<EquipmentType, ViewEquipmentList> equipmentViews =
            new Dictionary<EquipmentType, ViewEquipmentList>();
        private HashSet<ThingDef> stuffFilterSet = new HashSet<ThingDef>();
        private ThingDef filterStuff = null;
        private bool filterMadeFromStuff = true;

        public PanelEquipmentAvailable() {
            providerEquipment = new ProviderEquipmentTypes();
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 padding = new Vector2(12, 12);

            RectDropdownTypes = new Rect(padding.x, padding.y, 140, 28);
            RectDropdownMaterials = new Rect(RectDropdownTypes.xMax + 8, RectDropdownTypes.yMin, 160, 28);

            Vector2 sizeAddButton = new Vector2(160, 34);
            RectAddButton = new Rect(PanelRect.HalfWidth() - sizeAddButton.HalfX(),
                PanelRect.height - padding.y - sizeAddButton.y, sizeAddButton.x, sizeAddButton.y);

            Vector2 listSize = new Vector2();
            listSize.x = rect.width - padding.x * 2;
            listSize.y = rect.height - RectDropdownTypes.yMax - (padding.y * 3) - RectAddButton.height;
            float listHeaderHeight = 20;
            float listBodyHeight = listSize.y - listHeaderHeight;

            Rect rectTable = new Rect(padding.x, padding.y + RectDropdownTypes.yMax, listSize.x, listSize.y);

            RectListHeader = new Rect(padding.x, RectDropdownTypes.yMax + 4, listSize.x, listHeaderHeight);
            RectListBody = new Rect(padding.x, RectListHeader.yMax, listSize.x, listBodyHeight);

            RectColumnHeaderName = new Rect(RectListHeader.x + 64, RectListHeader.y, 240, RectListHeader.height);
            RectColumnHeaderCost = new Rect(RectListHeader.xMax - 100, RectListHeader.y, 100, RectListHeader.height);

            RectScrollFrame = RectListBody;
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);

            RectRow = new Rect(0, 0, RectScrollView.width, 42);
            RectItem = new Rect(10, 2, 38, 38);

            float columnWidthIcon = 64;
            float columnWidthCost = 100;
            float columnWidthName = RectRow.width - columnWidthIcon - columnWidthCost - 10;

            foreach (var type in providerEquipment.Types) {
                if (!equipmentViews.ContainsKey(type)) {
                    WidgetTable<EquipmentRecord> table = new WidgetTable<EquipmentRecord>();
                    table.Rect = rectTable;
                    table.BackgroundColor = Style.ColorPanelBackgroundDeep;
                    table.RowColor = Style.ColorTableRow1;
                    table.AlternateRowColor = Style.ColorTableRow2;
                    table.SelectedRowColor = Style.ColorTableRowSelected;
                    table.SupportSelection = true;
                    table.RowHeight = 42;
                    table.ShowHeader = true;
                    table.SortAction = DoSort;
                    table.SelectedAction = (EquipmentRecord entry) => {
                        SoundDefOf.TickTiny.PlayOneShotOnCamera();
                    };
                    table.DoubleClickAction = (EquipmentRecord entry) => {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera();
                        EquipmentAdded(entry);
                    };
                    table.AddColumn(new WidgetTable<EquipmentRecord>.Column() {
                        Width = columnWidthIcon,
                        Name = ColumnNameIcon,
                        DrawAction = (EquipmentRecord entry, Rect columnRect) => {
                            WidgetEquipmentIcon.Draw(columnRect, entry);
                        }
                    });
                    table.AddColumn(new WidgetTable<EquipmentRecord>.Column() {
                        Width = columnWidthName,
                        Name = ColumnNameName,
                        Label = "Name",
                        AdjustForScrollbars = true,
                        AllowSorting = true,
                        DrawAction = (EquipmentRecord entry, Rect columnRect) => {
                            GUI.color = Style.ColorText;
                            Text.Font = GameFont.Small;
                            Text.Anchor = TextAnchor.MiddleLeft;
                            Widgets.Label(columnRect, entry.Label);
                            GUI.color = Color.white;
                            Text.Anchor = TextAnchor.UpperLeft;
                        }
                    });
                    table.AddColumn(new WidgetTable<EquipmentRecord>.Column() {
                        Width = columnWidthCost,
                        Name = ColumnNameCost,
                        Label = "Cost",
                        AdjustForScrollbars = false,
                        AllowSorting = true,
                        DrawAction = (EquipmentRecord entry, Rect columnRect) => {
                            GUI.color = Style.ColorText;
                            Text.Font = GameFont.Small;
                            Text.Anchor = TextAnchor.MiddleRight;
                            Widgets.Label(new Rect(columnRect.x, columnRect.y, columnRect.width, columnRect.height),
                                          "" + entry.cost);
                            GUI.color = Color.white;
                            Text.Anchor = TextAnchor.UpperLeft;
                        },
                        Alignment = TextAnchor.LowerRight
                    });
                    table.SetSortState(ColumnNameName, 1);
                    ViewEquipmentList view = new ViewEquipmentList() {
                        Table = table,
                        List = providerEquipment.AllEquipmentOfType(type).ToList()
                    };
                    SortByName(view, 1);
                    equipmentViews.Add(type, view);
                }
            }
        }
        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            // Find the view.  Select the first row in the equipment list if none is selected.
            var view = CurrentView;
            if (view.Table.Selected == null) {
                view.Table.Selected = view.List.FirstOrDefault();
            }

            DrawFilters(view);
            DrawEquipmentList(view);

            if (Widgets.ButtonText(RectAddButton, "Add Equipment", true, false, view.Table.Selected != null)) {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                EquipmentAdded(view.Table.Selected);
            }
        }

        protected void UpdateAvailableMaterials() {
            ViewEquipmentList view = CurrentView;
            stuffFilterSet.Clear();
            foreach (var item in view.List) {
                if (item.stuffDef != null) {
                    stuffFilterSet.Add(item.stuffDef);
                }
            }
            if (filterStuff != null && !stuffFilterSet.Contains(filterStuff)) {
                filterStuff = null;
            }
        }

        protected void DrawFilters(ViewEquipmentList view) {
            string label = selectedType.Label.Translate();
            if (WidgetDropdown.Button(RectDropdownTypes, label, true, false, true)) {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (var type in providerEquipment.Types) {
                    EquipmentType localType = type;
                    list.Add(new FloatMenuOption(type.Label.Translate(), () => {
                        this.selectedType = localType;
                        this.UpdateAvailableMaterials();
                    }, MenuOptionPriority.Default, null, null, 0, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(list, null, false));
            }

            if (StuffFilterVisible) {
                string stuffLabel = null;
                if (!filterMadeFromStuff) {
                    stuffLabel = "No Materials";
                }
                else if (filterStuff == null) {
                    stuffLabel = "All Materials";
                }
                else {
                    stuffLabel = filterStuff.LabelCap;
                }
                if (WidgetDropdown.Button(RectDropdownMaterials, stuffLabel, true, false, true)) {
                    List<FloatMenuOption> stuffFilterOptions = new List<FloatMenuOption>();
                    stuffFilterOptions.Add(new FloatMenuOption("All Materials", () => {
                        UpdateStuffFilter(true, null);
                    }, MenuOptionPriority.Default, null, null, 0, null, null));
                    stuffFilterOptions.Add(new FloatMenuOption("No Materials", () => {
                        UpdateStuffFilter(false, null);
                    }, MenuOptionPriority.Default, null, null, 0, null, null));
                    foreach (var item in stuffFilterSet.OrderBy((ThingDef def) => { return def.LabelCap; })) {
                        stuffFilterOptions.Add(new FloatMenuOption(item.LabelCap, () => {
                            UpdateStuffFilter(true, item);
                        }, MenuOptionPriority.Default, null, null, 0, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(stuffFilterOptions, null, false));
                }
            }
        }

        protected ViewEquipmentList CurrentView {
            get {
                if (selectedType == null) {
                    selectedType = providerEquipment.Types.First();
                    UpdateAvailableMaterials();
                }
                return equipmentViews[selectedType];
            }
        }

        protected void UpdateStuffFilter(bool madeFromStuff, ThingDef stuff) {
            this.filterMadeFromStuff = madeFromStuff;
            this.filterStuff = stuff;
            ViewEquipmentList view = CurrentView;
            IEnumerable<EquipmentRecord> entries = FilterEquipmentList(view);
            if (!entries.Any((EquipmentRecord e) => {
                return e == view.Table.Selected;
            })) {
                view.Table.Selected = entries.FirstOrDefault();
            }
        }

        protected bool StuffFilterVisible {
            get {
                return stuffFilterSet.Count > 0;
            }
        }

        protected void DrawEquipmentList(ViewEquipmentList view) {
            SortField sortField = PrepareCarefully.Instance.SortField;
            view.Table.Draw(FilterEquipmentList(view));
            view.Table.BackgroundColor = Style.ColorPanelBackgroundDeep;
        }

        protected IEnumerable<EquipmentRecord> FilterEquipmentList(ViewEquipmentList view) {
            if (StuffFilterVisible) {
                return view.List.FindAll((EquipmentRecord entry) => {
                    if (filterMadeFromStuff) {
                        return filterStuff == null || filterStuff == entry.stuffDef;
                    }
                    else {
                        return !entry.def.MadeFromStuff;
                    }
                });
            }
            return view.List;
        }

        protected void DoSort(WidgetTable<EquipmentRecord>.Column column, int direction) {
            var view = equipmentViews[selectedType];
            if (view == null) {
                return;
            }
            if (column != null) {
                if (column.Name == ColumnNameName) {
                    SortByName(view, direction);
                    SoundDefOf.TickTiny.PlayOneShotOnCamera();
                }
                else if (column.Name == ColumnNameCost) {
                    SortByCost(view, direction);
                    SoundDefOf.TickTiny.PlayOneShotOnCamera();
                }
            }
        }

        protected void SortByName(ViewEquipmentList view, int direction) {
            if (direction == 1) {
                view.List.SortBy((EquipmentRecord arg) => { return arg.Label; });
            }
            else {
                view.List.SortByDescending((EquipmentRecord arg) => { return arg.Label; });
            }
        }
        protected void SortByCost(ViewEquipmentList view, int direction) {
            view.List.Sort((EquipmentRecord x, EquipmentRecord y) => {
                if (direction == 1) {
                    int result = x.cost.CompareTo(y.cost);
                    if (result != 0) {
                        return result;
                    }
                }
                else {
                    int result = y.cost.CompareTo(x.cost);
                    if (result != 0) {
                        return result;
                    }
                }
                return x.Label.CompareTo(y.Label);
            });
        }
    }
}
