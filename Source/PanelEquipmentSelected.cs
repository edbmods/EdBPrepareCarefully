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
        public delegate void RemoveEquipmentHandler(CustomizedEquipment entry);
        public delegate void UpdateEquipmentCountHandler(CustomizedEquipment equipment, int count);
        public delegate void RemovePossessionHandler(CustomizedPawn pawn, ThingDef thingDef);
        public delegate void UpdatePossessionCountHandler(CustomizedPawn pawn, ThingDef thingDef, int count);

        public event RemoveEquipmentHandler EquipmentRemoved;
        public event UpdateEquipmentCountHandler EquipmentCountUpdated;
        public event RemovePossessionHandler PossessionRemoved;
        public event UpdatePossessionCountHandler PossessionCountUpdated;

        protected Rect RectRemoveButton;
        protected Rect RectRow;
        private CustomizedEquipment ScrollToEntry = null;
        private WidgetScrollViewVertical ScrollView = new WidgetScrollViewVertical();

        private CustomizedEquipment equipmentToDelete = null;
        private ThingDef possessionToDelete = null;
        private ThingDef possessionToUpdate = null;
        private int possessionUpdateCount = 0;
        private CustomizedPawn possessionPawn = null;
        private List<WidgetNumberField> numberFields = new List<WidgetNumberField>();

        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }

        public Rect RectList { get; set; }

        public override void Resize(Rect rect) {
            base.Resize(rect);

            Vector2 padding = new Vector2(12, 12);

            //Vector2 sizeInfoButton = new Vector2(24, 24);
            Vector2 sizeButton = new Vector2(160, 34);
            RectRemoveButton = new Rect(PanelRect.HalfWidth() - sizeButton.HalfX(),
                PanelRect.height - padding.y - sizeButton.y, sizeButton.x, sizeButton.y);

            Vector2 listSize = new Vector2(rect.width - padding.x * 2, rect.height - padding.y * 2);
            RectList = new Rect(padding.x, padding.y, listSize.x, listSize.y);
            RectRow = new Rect(0, 0, RectList.width, 42);
        }

        public IEnumerable<CustomizedEquipment> SortedEquipment {
            get {
                if (State.Customizations.Equipment == null) {
                    yield break; 
                }
                foreach (var equipment in State.Customizations.Equipment.OrderBy(e => e.SpawnType)) {
                    yield return equipment;
                }
            }
        }

        protected override void DrawPanelContent() {
            base.DrawPanelContent();

            var savedGUIState = UtilityGUIState.Save();
            float y = 0;
            float? scrollToY = null;
            ScrollView.Begin(RectList);
            try {
                float width = ScrollView.CurrentViewWidth;
                EquipmentSpawnType? spawnType = null;
                int index = 0;
                foreach (var equipment in SortedEquipment) {
                    if (spawnType == null || equipment.SpawnType != spawnType.Value) {
                        Rect spawnTypeRect = new Rect(0, y, width, 22);
                        GUI.color = Color.black;
                        GUI.DrawTexture(spawnTypeRect, BaseContent.WhiteTex);
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Text.Font = GameFont.Tiny;
                        Widgets.Label(spawnTypeRect.OffsetBy(10, 0), UtilityEquipmentSpawnType.LabelForSpawnTypeHeader(equipment.SpawnType.Value));
                        spawnType = equipment.SpawnType;
                        y += spawnTypeRect.height + 6;
                        Text.Anchor = TextAnchor.UpperLeft;
                        Text.Font = GameFont.Small;
                    }

                    if (ScrollToEntry == equipment) {
                        scrollToY = y;
                    }

                    DrawSelectedEquipment(y, width, index, equipment);
                    y += RectRow.height;
                    index++;
                }

                foreach (var colonist in State.Customizations.ColonyPawns) {
                    var possessions = colonist.Customizations.Possessions;
                    if (!possessions.Any()) {
                        continue;
                    }
                    Rect spawnTypeRect = new Rect(0, y, width, 22);
                    GUI.color = Color.black;
                    GUI.DrawTexture(spawnTypeRect, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(spawnTypeRect.OffsetBy(10, 0), "EdB.PC.Equipment.SelectedEquipment.SpawnType.Possession".Translate(colonist.Pawn.LabelShortCap));
                    y += spawnTypeRect.height + 6;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    foreach (var p in possessions) {
                        DrawPossession(y, width, index, colonist, p);
                        y += RectRow.height;
                        index++;
                    }
                }
            }
            finally {
                ScrollView.End(y);
                savedGUIState.Restore();
            }

            if (scrollToY != null) {
                ScrollView.ScrollTo(y);
                scrollToY = null;
                ScrollToEntry = null;
            }

            if (equipmentToDelete != null) {
                EquipmentRemoved?.Invoke(equipmentToDelete);
                equipmentToDelete = null;
            }
            if (possessionToDelete != null) {
                PossessionRemoved?.Invoke(possessionPawn, possessionToDelete);
                possessionToDelete = null;
                possessionPawn = null;
            }
            else if (possessionToUpdate != null) {
                PossessionCountUpdated?.Invoke(possessionPawn, possessionToUpdate, possessionUpdateCount);
                possessionToUpdate = null;
                possessionPawn = null;
            }

        }

        public float DrawSelectedEquipment(float y, float width, int index, CustomizedEquipment equipment) {
            float top = y;
            float deleteButtonMargin = 10;
            float deleteButtonSize = 12;
            float deleteButtonPlusMarginSize = deleteButtonMargin + deleteButtonMargin + deleteButtonSize;
            Rect rowRect = new Rect(0, y, width - deleteButtonPlusMarginSize, 36);
            Rect deleteRect = new Rect(rowRect.xMax + deleteButtonMargin, rowRect.y + rowRect.HalfHeight() - deleteButtonSize * 0.5f, deleteButtonSize, deleteButtonSize);

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

                if (equipment.EquipmentOption.ThingDef?.DrawMatSingle != null && equipment.EquipmentOption?.ThingDef?.DrawMatSingle.mainTexture != null) {
                    Rect iconRect = new Rect(8f, y + 2, 32f, 32f);
                    ThingDef stuff = equipment.StuffDef;
                    if (stuff == null && equipment.EquipmentOption.ThingDef.MadeFromStuff) {
                        stuff = equipment.EquipmentOption.ThingDef.defaultStuff;
                    }
                    Widgets.ThingIcon(iconRect, equipment.EquipmentOption.ThingDef, stuff);
                }
                else if (equipment.EquipmentOption.RandomAnimal) {
                    GUI.color = Style.ColorTextSecondary;
                    Rect iconRect = new Rect(12f, y + 6, 24f, 24f);
                    GUI.DrawTexture(iconRect, Textures.TextureButtonRandom);
                    GUI.color = Color.white;
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                float labelLeftMargin = 48f;
                Rect labelRect = new Rect(labelLeftMargin, y, rowRect.width - labelLeftMargin, 24f);
                Rect subtitleRect = new Rect(labelLeftMargin, labelRect.y + 17f, rowRect.width - labelLeftMargin, 18f);
                string text = equipment.EquipmentOption.Label;
                GUI.color = Style.ColorText;
                Text.WordWrap = false;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                bool useStuff = equipment.StuffDef != null;
                bool supportsQuality = UtilityQuality.ThingDefSupportsQuality(equipment.EquipmentOption.ThingDef);
                bool useDefaultQuality = supportsQuality && !equipment.Quality.HasValue;
                bool showQuality = supportsQuality && !useDefaultQuality;

                if (useStuff && showQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = equipment.StuffDef.LabelCap + ", " + equipment.Quality.Value.GetLabel();
                    Widgets.Label(subtitleRect, subtitleText.Truncate(subtitleRect.width - 48));
                }
                else if (useStuff && !showQuality) {
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(subtitleRect, equipment.StuffDef.LabelCap.Truncate(subtitleRect.width - 48));
                }
                else if (!useStuff && showQuality) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = equipment.Quality.Value.GetLabel().CapitalizeFirst();
                    Widgets.Label(subtitleRect, subtitleText);
                }
                else if (equipment.Gender != null) {
                    Text.Font = GameFont.Tiny;
                    string subtitleText = equipment.Gender.Value.GetLabel().CapitalizeFirst();
                    Widgets.Label(subtitleRect, subtitleText.Truncate(subtitleRect.width - 48));
                }
                else {
                    labelRect = new Rect(labelLeftMargin, y, rowRect.width - labelLeftMargin, subtitleRect.yMax - labelRect.y);
                    Text.Anchor = TextAnchor.MiddleLeft;
                }

                Text.Font = GameFont.Small;
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Text.WordWrap = true;


                // Draw number field
                Rect fieldRect = new Rect(rowRect.x + rowRect.width - 80f, rowRect.y + 4f, 60f, 28f);
                Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                if (index >= numberFields.Count) {
                    numberFields.Add(new WidgetNumberField() {
                        MaxValue = 100000
                    });
                }
                WidgetNumberField field = numberFields[index];
                field.UpdateAction = (int value) => {
                    EquipmentCountUpdated?.Invoke(equipment, value);
                };
                field.Draw(fieldRect, equipment.Count);

                // Delete button
                if (deleteRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                if (Widgets.ButtonInvisible(deleteRect, false)) {
                    equipmentToDelete = equipment;
                }
            }
            finally {
                guiState.Restore();
            }

            //Widgets.InfoCardButton(width - 24f, top, equipment.EquipmentOption.ThingDef);

            y += rowRect.height;
            return y - top;

        }

        public float DrawPossession(float y, float width, int index, CustomizedPawn pawn, CustomizedPossession possession) {
            float top = y;
            float deleteButtonMargin = 10;
            float deleteButtonSize = 12;
            float deleteButtonPlusMarginSize = deleteButtonMargin + deleteButtonMargin + deleteButtonSize;
            Rect rowRect = new Rect(0, y, width - deleteButtonPlusMarginSize, 36);
            Rect deleteRect = new Rect(rowRect.xMax + deleteButtonMargin, rowRect.y + rowRect.HalfHeight() - deleteButtonSize * 0.5f, deleteButtonSize, deleteButtonSize);

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

                if (possession.ThingDef.DrawMatSingle != null && possession.ThingDef.DrawMatSingle.mainTexture != null) {
                    Rect iconRect = new Rect(8f, y + 2, 32f, 32f);
                    Widgets.ThingIcon(iconRect, possession.ThingDef);
                }
                float labelLeftMargin = 48f;
                Rect labelRect = new Rect(labelLeftMargin, y, rowRect.width - labelLeftMargin, rowRect.height);
                GUI.color = Style.ColorText;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;
                string text = possession.ThingDef.LabelCap;
                Widgets.Label(labelRect, text.Truncate(labelRect.width - 48));
                Text.WordWrap = true;

                // Draw number field
                Rect fieldRect = new Rect(rowRect.x + rowRect.width - 80f, rowRect.y + 4f, 60f, 28f);
                Widgets.DrawAtlas(fieldRect, Textures.TextureFieldAtlas);

                if (index >= numberFields.Count) {
                    numberFields.Add(new WidgetNumberField() {
                        MaxValue = 100000
                    });
                }
                WidgetNumberField field = numberFields[index];
                field.UpdateAction = (int value) => {
                    possessionToUpdate = possession.ThingDef;
                    possessionUpdateCount = value;
                    possessionPawn = pawn;
                };
                field.Draw(fieldRect, possession.Count);

                // Delete button
                if (deleteRect.Contains(Event.current.mousePosition)) {
                    GUI.color = Style.ColorButtonHighlight;
                }
                else {
                    GUI.color = Style.ColorButton;
                }
                GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                if (Widgets.ButtonInvisible(deleteRect, false)) {
                    possessionToDelete = possession.ThingDef;
                    possessionPawn = pawn;
                }
            }
            finally {
                guiState.Restore();
            }

            //Widgets.InfoCardButton(width - 24f, top, equipment.EquipmentOption.ThingDef);

            y += rowRect.height;
            return y - top;
        }

        public void EquipmentAdded(CustomizedEquipment equipment) {
            CustomizedEquipment matchingEquipment = FindEquipmentSelectionRefactored(equipment);
            if (matchingEquipment != null) {
                // Mark that we want to scroll to the newly added entry.  We can only scroll to it once
                // it's already been drawn once in the list, so we need to temporarily store a value that
                // we'll use on the next draw pass.
                ScrollToEntry = matchingEquipment;
            }
        }

        public CustomizedEquipment FindEquipmentSelectionRefactored(CustomizedEquipment equipment) {
            return State.Customizations.Equipment.Find((CustomizedEquipment e) => {
                return Equals(e, equipment);
            });
        }
    }

}
