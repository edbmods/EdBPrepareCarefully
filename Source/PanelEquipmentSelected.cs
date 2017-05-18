using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class PanelEquipmentSelected : PanelBase {
        public delegate void RemoveEquipmentHandler(EquipmentSelection entry);
        public delegate void UpdateEquipmentCountHandler(EquipmentSelection equipment, int count);

        public event RemoveEquipmentHandler EquipmentRemoved;
        public event UpdateEquipmentCountHandler EquipmentCountUpdated;

        protected Rect RectRemoveButton;
        protected Rect RectRow;
        protected WidgetTable<EquipmentSelection> table;
        private DragSlider Slider = new DragSlider(0.3f, 12, 400);
        private EquipmentRecord ScrollToEntry = null;

        public PanelEquipmentSelected() {
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 padding = new Vector2(12, 12);

            Vector2 sizeButton = new Vector2(160, 34);
            RectRemoveButton = new Rect(PanelRect.HalfWidth() - sizeButton.HalfX(),
                PanelRect.height - padding.y - sizeButton.y, sizeButton.x, sizeButton.y);

            Vector2 listSize = new Vector2();
            listSize.x = rect.width - padding.x * 2;
            listSize.y = rect.height - padding.y * 3 - RectRemoveButton.height;
            float listHeaderHeight = 20;
            float listBodyHeight = listSize.y - listHeaderHeight;

            Rect rectTable = new Rect(padding.x, padding.y, listSize.x, listSize.y);
            RectRow = new Rect(0, 0, rectTable.width, 42);

            float columnWidthIcon = 64;
            float columnWidthCount = 112;
            float columnWidthName = RectRow.width - columnWidthIcon - columnWidthCount;

            table = new WidgetTable<EquipmentSelection>();
            table.Rect = rectTable;
            table.BackgroundColor = Style.ColorPanelBackgroundDeep;
            table.RowColor = Style.ColorTableRow1;
            table.AlternateRowColor = Style.ColorTableRow2;
            table.SelectedRowColor = Style.ColorTableRowSelected;
            table.SupportSelection = true;
            table.RowHeight = 42;
            table.SelectedAction = (EquipmentSelection entry) => {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
            };
            table.DoubleClickAction = (EquipmentSelection entry) => {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                if (entry.Count > 0) {
                    EquipmentCountUpdated(entry, entry.Count - 1);
                }
                else {
                    EquipmentRemoved(entry);
                }
            };
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthIcon,
                Name = "Icon",
                DrawAction = (EquipmentSelection entry, Rect rowRect) => {
                    WidgetEquipmentIcon.Draw(rowRect, entry.Record);
                }
            });
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthName,
                Name = "Name",
                Label = "Name",
                DrawAction = (EquipmentSelection entry, Rect rowRect) => {
                    GUI.color = Style.ColorText;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rowRect, entry.Record.Label);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthCount,
                Name = "Count",
                Label = "Count",
                AdjustForScrollbars = true,
                DrawAction = (EquipmentSelection entry, Rect rowRect) => {
                    GUI.color = Style.ColorText;
                    Text.Font = GameFont.Small;

                    Rect fieldRect = new Rect(rowRect.x + 17, rowRect.y + 7, 60, 28);
                    Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                    Slider.OnGUI(fieldRect, entry.Count, (int value) => {
                        EquipmentCountUpdated(entry, value);
                    });
                    bool dragging = DragSlider.IsDragging();

                    Rect decrementRect = new Rect(fieldRect.x - 17, fieldRect.y + 6, 16, 16);
                    Style.SetGUIColorForButton(decrementRect);
                    GUI.DrawTexture(decrementRect, Textures.TextureButtonPrevious);
                    if (Widgets.ButtonInvisible(decrementRect, false)) {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera();
                        int amount = Event.current.shift ? 10 : 1;
                        int value = entry.Count - amount;
                        EquipmentCountUpdated(entry, value);
                    }

                    Rect incrementRect = new Rect(fieldRect.xMax + 1, fieldRect.y + 6, 16, 16);
                    Style.SetGUIColorForButton(incrementRect);
                    GUI.DrawTexture(incrementRect, Textures.TextureButtonNext);
                    if (Widgets.ButtonInvisible(incrementRect, false)) {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera();
                        int amount = Event.current.shift ? 10 : 1;
                        int value = entry.Count + amount;
                        EquipmentCountUpdated(entry, value);
                    }

                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
        }
        protected override void DrawPanelContent(State state) {
            base.DrawPanelContent(state);

            IEnumerable<EquipmentSelection> entries = PrepareCarefully.Instance.Equipment.Select((EquipmentSelection equipment) => {
                return FindEntry(equipment);
            }).Where((EquipmentSelection equipment) => { return equipment != null; });

            if (table.Selected == null) {
                table.Selected = entries.FirstOrDefault();
            }

            table.Draw(entries);

            if (ScrollToEntry != null) {
                ScrollTo(ScrollToEntry);
                ScrollToEntry = null;
            }

            if (Widgets.ButtonText(RectRemoveButton, "Remove All", true, false, table.Selected != null)) {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                EquipmentRemoved(table.Selected);
                table.Selected = null;
            }
        }

        protected IEnumerable<EquipmentSelection> GetEquipment() {
            IEnumerable<EquipmentSelection> list = PrepareCarefully.Instance.Equipment.Select((EquipmentSelection equipment) => {
                return FindEntry(equipment);
            }).Where((EquipmentSelection equipment) => { return equipment != null; });
            return list;
        }

        protected void ScrollTo(EquipmentRecord entry) {
            var equipment = GetEquipment();
            int count = equipment.TakeWhile((EquipmentSelection e) => {
                return e.Record.def != entry.def || e.Record.stuffDef != entry.stuffDef;
            }).Count();
            if (count < equipment.Count()) {
                int index = count;

                float min = table.ScrollView.Position.y;
                float max = min + table.Rect.height;
                float rowTop = (float)index * RectRow.height;
                float rowBottom = rowTop + RectRow.height;
                float pos = (float)index * RectRow.height;
                if (rowTop < min) {
                    float amount = min - rowTop;
                    table.ScrollView.Position = new Vector2(table.ScrollView.Position.x, table.ScrollView.Position.y - amount);
                }
                else if (rowBottom > max) {
                    float amount = rowBottom - max;
                    table.ScrollView.Position = new Vector2(table.ScrollView.Position.x, table.ScrollView.Position.y + amount);
                }
            }
        }
        protected EquipmentSelection FindEntry(EquipmentSelection equipment) {
            ThingDef def = equipment.ThingDef;
            EquipmentRecord entry = PrepareCarefully.Instance.EquipmentDatabase[equipment.Key];
            if (entry == null) {
                string thing = def != null ? def.defName : "null";
                string stuff = equipment.StuffDef != null ? equipment.StuffDef.defName : "null";
                Log.Warning(string.Format("Could not draw unrecognized resource/equipment.  Invalid item was removed.  This may have been caused by an invalid thing/stuff combination. (thing = {0}, stuff={1})", thing, stuff));
                PrepareCarefully.Instance.RemoveEquipment(equipment);
                return null;
            }
            return PrepareCarefully.Instance.Find(entry);
        }

        public void EquipmentAdded(EquipmentRecord entry) {
            EquipmentSelection loadoutRecord = PrepareCarefully.Instance.Find(entry);
            if (loadoutRecord != null) {
                table.Selected = loadoutRecord;
                // Mark that we want to scroll to the newly added entry.  We can only scroll to it once
                // it's already been drawn once in the list, so we need to temporarily store a value that
                // we'll use on the next draw pass.
                ScrollToEntry = loadoutRecord.Record;
            }
        }

    }
}
