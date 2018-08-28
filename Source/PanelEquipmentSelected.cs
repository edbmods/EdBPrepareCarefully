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

        private List<WidgetNumberField> numberFields = new List<WidgetNumberField>();

        public PanelEquipmentSelected() {
        }
        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 padding = new Vector2(12, 12);

            Vector2 sizeInfoButton = new Vector2(24, 24);
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

            Vector2 nameOffset = new Vector2(10, 0);
            float columnWidthInfo = 36;
            float columnWidthIcon = 42;
            float columnWidthCount = 112;
            float columnWidthName = RectRow.width - columnWidthInfo - columnWidthIcon - columnWidthCount;

            table = new WidgetTable<EquipmentSelection>();
            table.Rect = rectTable;
            table.BackgroundColor = Style.ColorPanelBackgroundDeep;
            table.RowColor = Style.ColorTableRow1;
            table.AlternateRowColor = Style.ColorTableRow2;
            table.SelectedRowColor = Style.ColorTableRowSelected;
            table.SupportSelection = true;
            table.RowHeight = 42;
            table.SelectedAction = (EquipmentSelection entry) => {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            };
            table.DoubleClickAction = (EquipmentSelection entry) => {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                if (entry.Count > 0) {
                    EquipmentCountUpdated(entry, entry.Count - 1);
                }
                else {
                    EquipmentRemoved(entry);
                }
            };
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthInfo,
                Name = "Info",
                DrawAction = (EquipmentSelection entry, Rect columnRect, WidgetTable<EquipmentSelection>.Metadata metadata) => {
                    Rect infoRect = new Rect(columnRect.MiddleX() - sizeInfoButton.HalfX(), columnRect.MiddleY() - sizeInfoButton.HalfY(), sizeInfoButton.x, sizeInfoButton.y);
                    Style.SetGUIColorForButton(infoRect);
                    GUI.DrawTexture(infoRect, Textures.TextureButtonInfo);
                    if (Widgets.ButtonInvisible(infoRect)) {
                        if (entry.record.animal) {
                            Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.record.thing));
                        }
                        else if (entry.StuffDef != null) {
                            Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.ThingDef, entry.StuffDef));
                        }
                        else {
                            Find.WindowStack.Add((Window)new Dialog_InfoCard(entry.ThingDef));
                        }
                    }
                    GUI.color = Color.white;
                }
            });
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthIcon,
                Name = "Icon",
                DrawAction = (EquipmentSelection entry, Rect columnRect, WidgetTable<EquipmentSelection>.Metadata metadata) => {
                    WidgetEquipmentIcon.Draw(columnRect, entry.Record);
                }
            });
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthName,
                Name = "Name",
                Label = "Name",
                DrawAction = (EquipmentSelection entry, Rect columnRect, WidgetTable<EquipmentSelection>.Metadata metadata) => {
                    columnRect = columnRect.InsetBy(nameOffset.x, 0, 0, 0);
                    GUI.color = Style.ColorText;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(columnRect, entry.Record.Label);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
            table.AddColumn(new WidgetTable<EquipmentSelection>.Column() {
                Width = columnWidthCount,
                Name = "Count",
                Label = "Count",
                AdjustForScrollbars = true,
                DrawAction = (EquipmentSelection entry, Rect columnRect, WidgetTable<EquipmentSelection>.Metadata metadata) => {
                    Rect fieldRect = new Rect(columnRect.x + 17, columnRect.y + 7, 60, 28);
                    Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                    if (metadata.rowIndex <= numberFields.Count) {
                        numberFields.Add(new WidgetNumberField() {
                            MaxValue = 100000
                        });
                    }
                    WidgetNumberField field = numberFields[metadata.rowIndex];
                    field.UpdateAction = (int value) => {
                        EquipmentCountUpdated(entry, value);
                    };
                    field.Draw(fieldRect, entry.Count);
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

            if (Widgets.ButtonText(RectRemoveButton, "EdB.PC.Panel.SelectedEquipment.Remove".Translate(), true, false, table.Selected != null)) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
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
