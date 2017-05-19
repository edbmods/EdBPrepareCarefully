using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class WidgetTable<T> where T : class {
        protected static Vector2 SizeSortIndicator = new Vector2(8, 4);
        protected Rect tableRect;
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected Action<T> doubleClickAction = null;
        protected Action<T> selectedAction = null;
        protected Func<T, bool> enabledFunc = (T) => { return true; };
        protected Column sortedColumn = null;
        protected int sortDirection = 1;
        protected T scrollTo;
        public class RowGroup {
            public string Label;
            public IEnumerable<T> Rows;
            public RowGroup() {
            }
            public RowGroup(string label, IEnumerable<T> rows) {
                this.Label = label;
                this.Rows = rows;
            }
        }
        public class Column {
            public float Width;
            public string Name;
            public string Label;
            public TextAnchor Alignment = TextAnchor.LowerLeft;
            public bool AdjustForScrollbars = false;
            public Action<T, Rect> DrawAction = (T, Rect) => { };
            public bool AllowSorting = false;
        }
        protected List<Column> columns = new List<Column>();
        public WidgetTable() {
            SupportSelection = false;
        }
        public Rect Rect {
            get {
                return tableRect;
            }
            set {
                tableRect = value;
            }
        }
        public bool ShowHeader {
            get;
            set;
        }
        public Color BackgroundColor {
            get;
            set;
        }
        public Color RowColor {
            get;
            set;
        }
        public Color AlternateRowColor {
            get;
            set;
        }
        public Color SelectedRowColor {
            get;
            set;
        }
        public List<T> Items {
            get;
            set;
        }
        public float RowHeight {
            get;
            set;
        }
        public float RowGroupHeaderHeight {
            get;
            set;
        }
        public bool SupportSelection {
            get;
            set;
        }
        public T Selected {
            get;
            set;
        }
        public Action<Column, int> SortAction {
            get;
            set;
        }
        public ScrollViewVertical ScrollView {
            get {
                return scrollView;
            }
        }
        public Action<T> DoubleClickAction {
            get {
                return doubleClickAction;
            }
            set {
                doubleClickAction = value;
            }
        }
        public Action<T> SelectedAction {
            get {
                return selectedAction;
            }
            set {
                selectedAction = value;
            }
        }
        public Func<T, bool> RowEnabledFunc {
            get {
                return enabledFunc;
            }
            set {
                enabledFunc = value;
            }
        }
        public void ScrollTo(T row) {
            this.scrollTo = row;
        }
        public void SetSortState(string name, int direction) {
            sortDirection = direction;
            Column column = columns.FirstOrDefault((Column arg) => { return arg.Name == name; });
            sortedColumn = column;
        }
        public void Sort(int direction) {
            if (direction == -1 || direction == 1) {
                if (direction != sortDirection) {
                    sortDirection = direction;
                    if (sortedColumn != null) {
                        DoSortAction();
                    }
                }
            }
        }
        public void Sort(Column column, int direction) {
            if (column != sortedColumn || direction != sortDirection) {
                sortedColumn = column;
                sortDirection = direction;
                DoSortAction();
            }
        }
        private void DoSortAction() {
            if (SortAction != null) {
                SortAction(sortedColumn, sortDirection);
            }
        }
        public void AddColumn(Column column) {
            columns.Add(column);
        }
        public void Draw(IEnumerable<T> rows) {
            Rect tableRect = this.tableRect;
            if (ShowHeader) {
                DrawHeader(new Rect(tableRect.x, tableRect.y, tableRect.width, 20));
                tableRect = tableRect.InsetBy(0, 20, 0, 0);
            }
            GUI.color = BackgroundColor;
            GUI.DrawTexture(tableRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float cursor = 0;
            GUI.BeginGroup(tableRect);
            scrollView.Begin(new Rect(0, 0, tableRect.width, tableRect.height));
            int index = 0;
            try {
                foreach (T row in rows) {
                    cursor = DrawRow(cursor, row, index);
                    index++;
                }
            }
            finally {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            // Scroll to the specific row, if any.  Need to do this after all of the rows have been drawn.
            if (scrollTo != null) {
                DoScroll(rows, scrollTo);
                scrollTo = null;
            }
        }
        public void Draw(IEnumerable<RowGroup> rowGroups) {
            Rect tableRect = this.tableRect;
            if (ShowHeader) {
                DrawHeader(new Rect(tableRect.x, tableRect.y, tableRect.width, 20));
                tableRect = tableRect.InsetBy(0, 20, 0, 0);
            }
            GUI.color = BackgroundColor;
            GUI.DrawTexture(tableRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float cursor = 0;
            GUI.BeginGroup(tableRect);
            scrollView.Begin(new Rect(0, 0, tableRect.width, tableRect.height));
            int index = 0;
            try {
                foreach (var group in rowGroups) {
                    if (group.Rows.DefaultIfEmpty() == null) {
                        continue;
                    }
                    if (group.Label != null) {
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.LowerLeft;
                        Rect headerRect = new Rect(tableRect.x + 1, cursor - 2, tableRect.width - 4, RowGroupHeaderHeight);
                        if (scrollView.ScrollbarsVisible) {
                            headerRect.width -= 16;
                        }
                        Widgets.Label(headerRect, group.Label);
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = Color.white;
                        cursor += RowGroupHeaderHeight;
                        index = 0;
                    }

                    foreach (T row in group.Rows) {
                        cursor = DrawRow(cursor, row, index);
                        index++;
                    }
                }
            }
            finally {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            // Scroll to the specific row, if any.  Need to do this after all of the rows have been drawn.
            if (scrollTo != null) {
                DoScroll(rowGroups, scrollTo);
                scrollTo = null;
            }
        }
        protected float DrawRow(float cursor, T row, int index) {
            Rect rowRect = new Rect(0, cursor, tableRect.width, RowHeight);
            if (cursor + rowRect.height >= scrollView.Position.y
                    && cursor <= scrollView.Position.y + scrollView.ViewHeight) {
                GUI.color = (index % 2 == 0) ? RowColor : AlternateRowColor;
                if (row == Selected && SelectedRowColor.a != 0) {
                    GUI.color = SelectedRowColor;
                }
                if (GUI.color.a != 0) {
                    GUI.DrawTexture(rowRect, BaseContent.WhiteTex);
                }
                GUI.color = Color.white;

                float columnCursor = 0;
                foreach (var column in columns) {
                    Rect columnRect = new Rect(columnCursor, rowRect.y, column.Width, rowRect.height);
                    if (column.AdjustForScrollbars && scrollView.ScrollbarsVisible) {
                        columnRect.width = columnRect.width - 16;
                    }
                    if (column.DrawAction != null) {
                        column.DrawAction(row, columnRect);
                    }
                    columnCursor += columnRect.width;
                }

                if (SupportSelection) {
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition)) {
                        if (Event.current.button == 0) {
                            if (Event.current.clickCount == 1) {
                                Selected = row;
                                if (selectedAction != null) {
                                    selectedAction(row);
                                }
                            }
                            else if (Event.current.clickCount == 2) {
                                if (doubleClickAction != null) {
                                    doubleClickAction(row);
                                }
                            }
                        }
                    }
                }
            }
            cursor += RowHeight;
            return cursor;
        }
        protected void DoScroll(IEnumerable<T> rows, T scrollTo) {
            int index = -1;
            foreach (var row in rows) {
                index++;
                if (object.Equals(row, scrollTo)) {
                    break;
                }
            }
            if (index < 0) {
                return;
            }
            float min = ScrollView.Position.y;
            float max = min + Rect.height;
            float rowTop = (float)index * RowHeight;
            float rowBottom = rowTop + RowHeight;
            float pos = (float)index * RowHeight;
            if (rowTop < min) {
                float amount = min - rowTop;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y - amount);
            }
            else if (rowBottom > max) {
                float amount = rowBottom - max;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y + amount);
            }
        }
        protected void DoScroll(IEnumerable<RowGroup> rowGroups, T scrollTo) {
            int rowIndex = -1;
            int groupIndex = -1;
            foreach (var group in rowGroups) {
                groupIndex++;
                foreach (var row in group.Rows) {
                    rowIndex++;
                    if (object.Equals(row, scrollTo)) {
                        break;
                    }
                }
            }
            if (rowIndex < 0) {
                return;
            }
            float min = ScrollView.Position.y;
            float max = min + Rect.height;
            float rowTop = (float)rowIndex * RowHeight;
            float rowBottom = rowTop + RowHeight;
            float pos = (float)rowIndex * RowHeight + groupIndex * RowGroupHeaderHeight;
            if (rowTop < min) {
                float amount = min - rowTop;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y - amount);
            }
            else if (rowBottom > max) {
                float amount = rowBottom - max;
                ScrollView.Position = new Vector2(ScrollView.Position.x, ScrollView.Position.y + amount);
            }
        }
        public void DrawHeader(Rect rect) {
            Column clickedColumn = null;
            GUI.color = Style.ColorTableHeader;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Style.ColorTableHeaderBorder;
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), BaseContent.WhiteTex);

            float cursor = rect.x;
            GUI.color = Style.ColorText;
            Text.Font = GameFont.Tiny;
            foreach (var column in columns) {
                if (column.Label != null) {
                    Text.Anchor = column.Alignment;
                    Rect labelRect = new Rect(cursor, rect.y, column.Width, rect.height);
                    if (column.AdjustForScrollbars && scrollView.ScrollbarsVisible) {
                        labelRect.width -= 16;
                    }

                    if (column.AllowSorting) {
                        float columnWidth = labelRect.width;
                        Vector2 textSize = Text.CalcSize(column.Label);
                        Rect textRect;
                        Rect sortRect;
                        if (column.Alignment == TextAnchor.LowerLeft) {
                            textRect = new Rect(labelRect.x, labelRect.y, textSize.x, textSize.y);
                            sortRect = new Rect(labelRect.x + textSize.x + 2, labelRect.yMax - 11, SizeSortIndicator.x, SizeSortIndicator.y);
                        }
                        else {
                            textRect = new Rect(labelRect.xMax - textSize.x - SizeSortIndicator.x - 2, labelRect.yMax - textSize.y, textSize.x, textSize.y);
                            sortRect = new Rect(labelRect.xMax - SizeSortIndicator.x, labelRect.yMax - 11, SizeSortIndicator.x, SizeSortIndicator.y);
                            labelRect = labelRect.InsetBy(0, 0, SizeSortIndicator.x + 2, 0);
                        }
                        Rect highlightRect = textRect.Combined(sortRect);
                        Style.SetGUIColorForButton(highlightRect);
                        if (Widgets.ButtonInvisible(highlightRect, false)) {
                            clickedColumn = column;
                        }
                        if (sortedColumn == column) {
                            if (sortDirection == 1) {
                                GUI.DrawTexture(sortRect, Textures.TextureSortAscending);
                            }
                            else {
                                GUI.DrawTexture(sortRect, Textures.TextureSortDescending);
                            }
                        }
                        Widgets.Label(labelRect, column.Label);
                        GUI.color = Style.ColorText;
                        cursor += columnWidth;
                    }
                    else {
                        Widgets.Label(labelRect, column.Label);
                        cursor += labelRect.width;
                    }
                }
                else {
                    cursor += column.Width;
                }
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (clickedColumn != null) {
                if (sortedColumn != clickedColumn) {
                    Sort(clickedColumn, 1);
                }
                else {
                    Sort(-sortDirection);
                }
            }
        }
    }
}
